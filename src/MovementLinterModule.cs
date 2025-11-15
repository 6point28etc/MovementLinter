using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Celeste.Mod.SpeedrunTool.SaveLoad;
using Celeste.Mod.TasTestSuite;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using YamlDotNet.Serialization;
using System.Collections.Generic;

namespace Celeste.Mod.MovementLinter;

public class MovementLinterModule : EverestModule {
    // =================================================================================================================
    // EverestModule boilerplate
    public static MovementLinterModule Instance { get; private set; }
    public override Type SettingsType => typeof(MovementLinterModuleSettings);
    public static MovementLinterModuleSettings Settings => (MovementLinterModuleSettings) Instance._Settings;

    public MovementLinterModule() {
        Instance = this;
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, FMOD.Studio.EventInstance pauseSnapshot) {
        CreateModMenuSectionHeader(menu, inGame, pauseSnapshot);
        Settings.CreateMenu(menu, inGame);
    }

    public override void SaveSettings() {
        // The YamlDotNet API made me quit modding a while back. Still makes me retch.
        ISerializer restoreSerializer = YamlHelper.Serializer;
        if (!Settings.MemorialTextEnabled) {
            YamlHelper.Serializer = new SerializerBuilder().WithAttributeOverride<MovementLinterModuleSettings>(
                (MovementLinterModuleSettings settings) => settings.MemorialTextEnabled,
                new YamlIgnoreAttribute()
            ).Build();
        }
        base.SaveSettings();
        YamlHelper.Serializer = restoreSerializer;
    }

    // =================================================================================================================
    // Some extra stuff we need to set up while loading
    private static bool speedrunToolIsLoaded   = false;
    private static object saveLoadAction       = null;
    private static ILHook PlayerOrigUpdateHook = null;

    // =================================================================================================================
    // Constants
    const int UpEntryDashLockoutFrames = 11;

    // All the state needed for detection logic, wrapped in a struct for easy savestates.
    private const int BeyondShortDurationFrames = MovementLinterModuleSettings.MaxShortDurationFrames + 1;
    public struct Detection {
        public Detection() {}

        // Some general extra state tracking
        public int FrameStartPlayerState = Player.StNormal;
        public bool RoomLoadJustHappened = false;
        public bool OnGround             = false;

        // Jump release
        public int JumpReleaseFrames   = BeyondShortDurationFrames;
        public bool JumpReleaseMatters = false;
        public bool AutoJumpWasActive  = false;

        // Move after land
        public int FramesAfterLand     = BeyondShortDurationFrames;
        public bool UltradSinceLanding = false;
        public bool CanDashThisFrame   = true;
        public bool CouldDashLastFrame = true;

        // Move after gain control
        public bool WasInControl        = false;
        public int InControlFrames      = 0;
        public bool WasSkippingCutscene = false;
        public bool CanJumpThisFrame    = true;
        public bool CouldJumpLastFrame  = true;

        // Dash after up transition
        public int FramesSinceUpTransition   = BeyondShortDurationFrames;
        public bool UpTransitionJustHappened = false;

        // Fastbubbles
        public bool CouldDashBeforeBubble = true;
        public int FramesBeforeFastBubble = 0;

        // moveX checks
        public Vector2 FrameStartPlayerSpeed = Vector2.Zero;
        public bool ForceMoveXActive         = false;
        public int LastMoveX                 = 0;
        public bool LastMoveXWasForward      = false;
        public int ReleaseWFrames            = BeyondShortDurationFrames;
        public bool MoveXUsedThisFrame       = false;
        public bool ReleaseWMatters          = false;
        public int LastWallHitDir            = 0;
        public int LastWallHitPlayerX        = int.MinValue;
        public bool HeldTowardLastWallHit    = false;

        // Fastfall
        public bool FastfallCheckedThisFrame = false;
        public bool FastfallCheckedLastFrame = false;
        public bool MoveYIsFastfall          = false;
        public int FastfallMoveYFrames       = 0;

        // Buffered ultra
        public bool UltradLastFrame = false;
    };
    private static Detection det = new(), savedDet;

    // Everything we need to implement the lint responses
    private static LintResponder res = LintResponder.Instance;

    // =================================================================================================================
    // Load and unload
    public override void Load() {
        // These need to be loaded before any hooks of the functions that call them,
        // I'm guessing due to virtual function shenanigans
        On.Monocle.VirtualButton.ConsumeBuffer += OnVirtualButtonConsumeBuffer;
        On.Monocle.VirtualButton.ConsumePress  += OnVirtualButtonConsumePress;

        On.Celeste.Level.Update += OnLevelUpdate;

        // Need to do some weird method lookup to patch the player update function since Everest hooks it
        On.Celeste.Player.Update += OnPlayerUpdate;
        PlayerOrigUpdateHook = new ILHook(typeof(Player).GetMethod("orig_Update"), PatchPlayerUpdate);

        IL.Celeste.Player.NormalUpdate  += PatchPlayerNormalUpdate;
        On.Celeste.Player.ClimbBegin    += OnPlayerClimbBegin;
        IL.Celeste.Player.SwimUpdate    += PatchPlayerSwimUpdate;
        On.Celeste.Player.StarFlyUpdate += OnPlayerStarFlyUpdate;

        On.Celeste.Player.Jump          += OnPlayerJump;
        On.Celeste.Player.WallJump      += OnPlayerWallJump;
        On.Celeste.Player.SuperJump     += OnPlayerSuperJump;
        On.Celeste.Player.SuperWallJump += OnPlayerSuperWallJump;
        On.Celeste.Player.Boost         += OnPlayerBoost;
        On.Celeste.Player.RedBoost      += OnPlayerRedBoost;
        On.Celeste.Player.BoostUpdate   += OnPlayerBoostUpdate;
        On.Celeste.Player.OnCollideH    += OnPlayerOnCollideH;
        IL.Celeste.Player.OnCollideV    += PatchPlayerOnCollideV;
        On.Celeste.Level.LoadLevel      += OnLevelLoadLevel;
        On.Celeste.Level.TransitionTo   += OnLevelTransitionTo;

        speedrunToolIsLoaded = Everest.Modules.Any((EverestModule module) => module.Metadata.Name == "SpeedrunTool");
        if (speedrunToolIsLoaded) {
            AddSaveLoadAction();
        }

        // Mods for response. Keeping everything in one big list here rather than splitting things out because I want to
        // easily tell if there's a conflict / double-mod of a method.
        IL.Celeste.Player.Render                  += LintResponder.PatchPlayerRender;
        On.Celeste.BadelineOldsite.CanChangeMusic += LintResponder.OnBadelineOldsiteCanChangeMusic;
        IL.Celeste.BadelineOldsite.Added          += LintResponder.PatchBadelineOldsiteAdded;

        // Mods for config
        IL.Celeste.Level.Update += MovementLinterModuleSettings.PatchLevelUpdate;
    }

    public override void Unload() {
        On.Monocle.VirtualButton.ConsumeBuffer -= OnVirtualButtonConsumeBuffer;
        On.Monocle.VirtualButton.ConsumePress  -= OnVirtualButtonConsumePress;

        On.Celeste.Level.Update -= OnLevelUpdate;

        On.Celeste.Player.Update -= OnPlayerUpdate;
        PlayerOrigUpdateHook?.Dispose();

        IL.Celeste.Player.NormalUpdate  -= PatchPlayerNormalUpdate;
        On.Celeste.Player.ClimbBegin    -= OnPlayerClimbBegin;
        IL.Celeste.Player.SwimUpdate    -= PatchPlayerSwimUpdate;
        On.Celeste.Player.StarFlyUpdate -= OnPlayerStarFlyUpdate;

        On.Celeste.Player.Jump          -= OnPlayerJump;
        On.Celeste.Player.WallJump      -= OnPlayerWallJump;
        On.Celeste.Player.SuperJump     -= OnPlayerSuperJump;
        On.Celeste.Player.SuperWallJump -= OnPlayerSuperWallJump;
        On.Celeste.Player.Boost         -= OnPlayerBoost;
        On.Celeste.Player.RedBoost      -= OnPlayerRedBoost;
        On.Celeste.Player.BoostUpdate   -= OnPlayerBoostUpdate;
        On.Celeste.Player.OnCollideH    -= OnPlayerOnCollideH;
        IL.Celeste.Player.OnCollideV    -= PatchPlayerOnCollideV;
        On.Celeste.Level.LoadLevel      -= OnLevelLoadLevel;
        On.Celeste.Level.TransitionTo   -= OnLevelTransitionTo;

        if (speedrunToolIsLoaded) {
            RemoveSaveLoadAction();
        }

        IL.Celeste.Player.Render                  -= LintResponder.PatchPlayerRender;
        On.Celeste.BadelineOldsite.CanChangeMusic -= LintResponder.OnBadelineOldsiteCanChangeMusic;
        IL.Celeste.BadelineOldsite.Added          -= LintResponder.PatchBadelineOldsiteAdded;

        IL.Celeste.Level.Update -= MovementLinterModuleSettings.PatchLevelUpdate;
    }

    // =================================================================================================================
    // Combine hooks into one per method
    private static void OnLevelLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level,
                                         Player.IntroTypes playerIntro, bool isFromLoader) {
        orig(level, playerIntro, isFromLoader);
        DetectionOnLevelLoadLevel();
        LintResponder.OnLevelLoadLevel();
        MovementLinterModuleSettings.LoadOverrides(level);
    }

    // =================================================================================================================
    // Savestates
    private static void AddSaveLoadAction() {
        saveLoadAction = new SaveLoadAction(
            saveState: (_, _) => { savedDet = det; },
            loadState: (Dictionary<Type, Dictionary<string, object>> _, Level level) => {
                det = savedDet;
                MovementLinterModuleSettings.LoadOverrides(level);
            },
            clearState: null,
            beforeSaveState: null,
            preCloneEntities: null
        );
        SaveLoadAction.Add((SaveLoadAction) saveLoadAction);
    }

    private static void RemoveSaveLoadAction() {
        if (saveLoadAction != null) {
            SaveLoadAction.Remove((SaveLoadAction) saveLoadAction);
        }
    }

    // =================================================================================================================
    private static void OnLevelUpdate(On.Celeste.Level.orig_Update orig, Level level) {
        if (!level.SkippingCutscene && det.WasSkippingCutscene) {
            det.InControlFrames = 0;
        }
        det.WasSkippingCutscene = level.SkippingCutscene;
        orig(level);
    }

    // =================================================================================================================
    // Helper since Player.InControl doesn't include templefall for some reason
    private static bool PlayerInControl(Player player) {
        return player.InControl && player.StateMachine.State != Player.StTempleFall;
    }

    // =================================================================================================================
    private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player player) {
        // Start of frame state tracking
        det.FrameStartPlayerState = player.StateMachine.State;
        // Jump release
        bool jumpPressed                = Input.Jump.Pressed;
        bool autoJumpWasActiveLastFrame = det.AutoJumpWasActive;
        // Move after gain control
        bool inControl = PlayerInControl(player);
        if (inControl && !det.WasInControl) {
            det.InControlFrames = 0;
        }
        det.WasInControl = inControl;
        // moveX
        bool forceMoveXWillBeActive = (player.forceMoveXTimer > 0f);
        det.MoveXUsedThisFrame      = false;
        // Fastfall
        det.FastfallCheckedThisFrame = false;

        // Run the frame
        orig(player);

        // End of frame state tracking
        // Jump release
        if ((jumpPressed || (det.AutoJumpWasActive && !autoJumpWasActiveLastFrame)) &&
                det.JumpReleaseFrames > 0 &&
                det.JumpReleaseFrames <= Settings.JumpReleaseJump.Frames &&
                det.JumpReleaseMatters) {
            string warnSingular = jumpPressed ? DialogIds.JumpReleaseJumpWarnSingular
                                              : DialogIds.JumpReleaseAutoJumpWarnSingular;
            string warnPlural   = jumpPressed ? DialogIds.JumpReleaseJumpWarnPlural
                                              : DialogIds.JumpReleaseAutoJumpWarnPlural;
            res.DoLintResponses(Settings.JumpReleaseJump, warnSingular, warnPlural, det.JumpReleaseFrames);
        }
        if (Input.Jump.Check) {
            det.JumpReleaseFrames  = 0;
            det.JumpReleaseMatters = false;
        } else {
            ++det.JumpReleaseFrames;
        }

        // Move after land
        ++det.FramesAfterLand;
        det.CouldDashLastFrame = det.CanDashThisFrame;
        // Move after gain control
        ++det.InControlFrames;
        det.CouldJumpLastFrame = det.CanJumpThisFrame;
        // Dash after up transition
        ++det.FramesSinceUpTransition;
        det.UpTransitionJustHappened = false;

        // moveX
        det.ForceMoveXActive    = forceMoveXWillBeActive;
        bool thisMoveXIsForward = det.HeldTowardLastWallHit ||
                                      ((det.FrameStartPlayerSpeed.X != 0) &&
                                       (Math.Sign(player.moveX) == Math.Sign(det.FrameStartPlayerSpeed.X)));
        if (thisMoveXIsForward) {
            // If we're ever holding forward with this moveX, consider it good forever
            det.ReleaseWFrames  = BeyondShortDurationFrames;
            det.ReleaseWMatters = false;
        } else {
            if (player.moveX != det.LastMoveX && det.LastMoveXWasForward && !det.RoomLoadJustHappened) {
                // Start the timer when we detect we released forward, whether it matters yet or not
                det.ReleaseWFrames  = 1;
                det.ReleaseWMatters = false;
            } else {
                ++det.ReleaseWFrames;
            }
            det.ReleaseWMatters |= det.MoveXUsedThisFrame && (det.LastWallHitDir == 0);
        }
        det.LastMoveX           = player.moveX;
        det.LastMoveXWasForward = thisMoveXIsForward;

        // Fastfall
        det.FastfallCheckedLastFrame = det.FastfallCheckedThisFrame;

        // Do this last so it applies for the whole frame
        det.RoomLoadJustHappened = false;

        // Poll any responses that need or want the player to exist or that have something to do every frame
        res.ProcessPendingResponses(player);

        // Poll the self-test engine
        TestSuite.Update(player);
    }

    // =================================================================================================================
    // Player update patches
    private static void PatchPlayerUpdate(ILContext il) {
        ILCursor cursor = new(il);

        // Check if we just landed on the ground, right after we set the onGround and OnSafeGround values
        cursor.GotoNext(MoveType.Before,
                        instr => instr.MatchCallvirt(typeof(Player), "get_OnSafeGround"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(CheckLandedOnGround);

        // Run the short wallboost check while wallboosting
        cursor.GotoNext(MoveType.After,
                        instr => instr.MatchLdfld(typeof(Player), "wallBoostDir"),
                        instr => instr.OpCode == OpCodes.Bne_Un_S);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(CheckShortWallboost);

        // Do some checks right before the state machine runs,
        // after some relevant variables like onGround and moveX have been updated
        cursor.GotoNext(MoveType.Before,
                        instr => instr.MatchCall(typeof(Actor), "Update"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(BeforePlayerStateMachine);

        // Reset ultra check after state machine runs, before movement
        cursor.GotoNext(MoveType.After,
                        instr => instr.MatchCall(typeof(Actor), "Update"));
        cursor.EmitDelegate<Action>(ResetUltraCheck);
    }

    private static void CheckLandedOnGround(Player player) {
        if (player.onGround && !player.wasOnGround) {
            det.FramesAfterLand    = (det.RoomLoadJustHappened || !PlayerInControl(player)) ? BeyondShortDurationFrames
                                                                                            : 0;
            det.UltradSinceLanding = det.UltradLastFrame;
        }
        // Sometimes we want to check onGround without direct access to the player object
        det.OnGround = player.onGround;
    }

    private static void CheckShortWallboost(Player player) {
        // There are multiple competing off-by-one... quirks... surrounding wallboosts.
        // I'm not going to explain all of them, just trust me, this is the right number to use.
        int wallBoostFrames = (int) Math.Round((Player.ClimbJumpBoostTime - player.wallBoostTimer) * 60f) - 1;
        if (wallBoostFrames <= Settings.ShortWallboost.Frames) {
            res.DoLintResponses(Settings.ShortWallboost, DialogIds.ShortWallboostWarnSingular,
                                DialogIds.ShortWallboostWarnPlural, wallBoostFrames);
        }
    }

    private static void BeforePlayerStateMachine(Player player) {
        // Can dash check
        det.CanDashThisFrame = CanDashIfDashPressed(player);
        // Can jump check -- far from exhaustive but should cover the common cases for jumping upon gaining control
        bool canUnduck       = player.CanUnDuck;
        bool wallJumpCheck   = player.WallJumpCheck(-1) || player.WallJumpCheck(1);
        det.CanJumpThisFrame = (player.StateMachine.State == Player.StNormal && player.jumpGraceTimer > 0f) ||
                               (player.StateMachine.State == Player.StNormal &&
                                    player.CollideFirst<Water>(player.Position + Vector2.UnitY * 2f) != null) ||
                               (player.StateMachine.State == Player.StSwim && player.SwimJumpCheck()) ||
                               (player.StateMachine.State == Player.StNormal && canUnduck && wallJumpCheck) ||
                               (player.StateMachine.State == Player.StClimb && (!player.Ducking || canUnduck));
        // Check if we're holding toward a wall we just hit
        det.HeldTowardLastWallHit = false;
        if (det.LastWallHitDir != 0) {
            // If we've detected we're against a wall we just hit, check if we left it
            if ((int) player.Position.X != det.LastWallHitPlayerX ||
                    !player.CollideCheck<Solid>(player.Position + Vector2.UnitX * det.LastWallHitDir)) {
                det.LastWallHitDir = 0;
            } else {
                // If we're still against that wall, we're holding forward if we're holding toward it
                det.HeldTowardLastWallHit = (Math.Sign(player.moveX) == det.LastWallHitDir);
            }
        }
        // Need to take this measurement after retained speed applies
        det.FrameStartPlayerSpeed = player.Speed;
    }

    private static bool CanDashIfDashPressed(Player player) {
        return player.dashCooldownTimer <= 0f && player.Dashes > 0;
    }

    private static void ResetUltraCheck() {
        det.UltradLastFrame = false;
    }

    // =================================================================================================================
    // StNormal patches
    private static void PatchPlayerNormalUpdate(ILContext il) {
        ILCursor cursor = new(il);

        // Set moveX matters right before the game checks the moveX input
        cursor.GotoNext(MoveType.After,
                        instr => instr.OpCode == OpCodes.Ldarg_0,
                        instr => instr.MatchLdflda(typeof(Player), "Speed"),
                        instr => instr.MatchLdfld(typeof(Vector2), "X"),
                        instr => instr.MatchCall(typeof(Math), "Abs"));
        cursor.EmitDelegate<Action>(SetMoveXUsed);

        // Do our fastfall checks right before the vanilla ones
        cursor.GotoNext(MoveType.After,
                        instr => instr.MatchStfld(typeof(Player), "maxFall"),
                        instr => instr.OpCode == OpCodes.Br,
                        instr => instr.MatchLdsfld(typeof(Input), "MoveY"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc, 7);
        cursor.EmitDelegate<Action<Player, float>>(CheckFastfallInput);

        // Check if autojump is active and if holding jump matters right before the game starts checking if jump is held
        cursor.GotoNext(MoveType.Before,
                        instr => instr.OpCode == OpCodes.Bge_Un_S,
                        instr => instr.MatchLdsfld(typeof(Input), "Jump"),
                        instr => instr.MatchCallvirt(typeof(Monocle.VirtualButton), "get_Check"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(CheckJumpReleaseMattersAndAutoJump);

        // Check for a buffered ultra when we're about to ground jump
        cursor.GotoNext(MoveType.Before,
                        instr => instr.MatchCallvirt(typeof(Player), "Jump"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(CheckBufferedUltra);
    }

    private static void SetMoveXUsed() {
        det.MoveXUsedThisFrame = true;
    }

    private static void ClearMoveXUsed() {
        // Used so I can cancel the moveX used flag by patching rather than needing to look ahead
        // to all scenarios where speed will be overwritten
        det.MoveXUsedThisFrame = false;
    }

    private static void CheckFastfallInput(Player player, float fastfallThreshold) {
        // Always track the status of the fastfall input
        bool moveYIsFastfall = (Input.MoveY.Value == 1);
        if (moveYIsFastfall != det.MoveYIsFastfall && !det.RoomLoadJustHappened) {
            det.FastfallMoveYFrames = 0;
        }
        det.MoveYIsFastfall = moveYIsFastfall;
        ++det.FastfallMoveYFrames;
        // Decide whether we're actually using the fastfall input this frame
        if (player.Speed.Y >= fastfallThreshold && !player.onGround) {
            det.FastfallCheckedThisFrame = true;
        }
    }

    private static void CheckJumpReleaseMattersAndAutoJump(Player player) {
        det.JumpReleaseMatters |= !(player.AutoJump || Input.Jump.Check) &&
                                   (player.varJumpTimer > 0f || Math.Abs(player.Speed.Y) < 40f);
        det.AutoJumpWasActive = player.AutoJump;
    }

    private static void CheckBufferedUltra(Player player) {
        if (!player.wasOnGround &&
                ((player.DashDir.X != 0f && player.DashDir.Y > 0f && player.Speed.Y > 0f) ||
                 (Settings.BufferedUltra.Mode == MovementLinterModuleSettings.BufferedUltraMode.Always && det.UltradLastFrame))) {
            res.DoLintResponses(Settings.BufferedUltra, DialogIds.BufferedUltraWarn, DialogIds.BufferedUltraWarn, 0);
        }
    }

    // =================================================================================================================
    private static void OnPlayerClimbBegin(On.Celeste.Player.orig_ClimbBegin orig, Player player) {
        ClearMoveXUsed();
        orig(player);
    }

    // =================================================================================================================
    private static void PatchPlayerSwimUpdate(ILContext il) {
        // Set moveX matters right before we start updating speed. Technically swim uses analog input so
        // this is just an approximation but it should be good enough.
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.Before,
                        instr => instr.MatchLdflda(typeof(Player), "Speed"),
                        instr => instr.MatchLdfld(typeof(Vector2), "X"),
                        instr => instr.MatchCall(typeof(Math), "Abs"),
                        instr => instr.MatchLdcR4(80f));
        cursor.EmitDelegate<Action>(SetMoveXUsed);
    }

    // =================================================================================================================
    private static int OnPlayerStarFlyUpdate(On.Celeste.Player.orig_StarFlyUpdate orig, Player player) {
        // We'll use the analog input every time except when starFlyTransforming. This may be cancelled later in the
        // frame, but that's handled separately by ClearMoveXUsed. And yes analog != moveX blah blah whatever.
        if (!player.starFlyTransforming) {
            SetMoveXUsed();
        }
        return orig(player);
    }

    // =================================================================================================================
    // These hooks let us run some code any time a button press is "used", i.e., any time the game checks if the button
    // was pressed (specifically VirtualButton.Pressed) and does something if it was. We use this to do something any
    // time a jump or dash actually happens, without having to separately hook all of the places that might happen.
    private static void OnVirtualButtonConsumeBuffer(On.Monocle.VirtualButton.orig_ConsumeBuffer orig,
                                                     Monocle.VirtualButton button) {
        OnButtonPressUsed(button);
        orig(button);
    }

    private static void OnVirtualButtonConsumePress(On.Monocle.VirtualButton.orig_ConsumePress orig,
                                                    Monocle.VirtualButton button) {
        OnButtonPressUsed(button);
        orig(button);
    }

    private static void OnButtonPressUsed(Monocle.VirtualButton button) {
        if (button == Input.Jump) {
            det.FastfallCheckedThisFrame = false;
        }
        if (button == Input.Dash) {
            ClearMoveXUsed();
            if (det.JumpReleaseFrames > 0 &&
                    det.JumpReleaseFrames <= Settings.JumpReleaseDash.Frames &&
                    det.JumpReleaseMatters) {
                res.DoLintResponses(Settings.JumpReleaseDash, DialogIds.JumpReleaseDashWarnSingular,
                                    DialogIds.JumpReleaseDashWarnPlural, det.JumpReleaseFrames);
            }
            if (det.ReleaseWFrames > 0 &&
                    det.ReleaseWFrames <= Settings.ReleaseWBeforeDash.Frames &&
                    det.ReleaseWMatters &&
                    !det.ForceMoveXActive) {
                res.DoLintResponses(Settings.ReleaseWBeforeDash, DialogIds.ReleaseWBeforeDashWarnSingular,
                                    DialogIds.ReleaseWBeforeDashWarnPlural, det.ReleaseWFrames);
            }
            if (det.FastfallMoveYFrames > 0 &&
                    det.FastfallMoveYFrames <= Settings.FastfallGlitchBeforeDash.Frames &&
                    det.FastfallCheckedLastFrame &&
                    !det.OnGround) {
                res.DoLintResponses(Settings.FastfallGlitchBeforeDash, DialogIds.FastfallGlitchBeforeDashWarnSingular,
                                    DialogIds.FastfallGlitchBeforeDashWarnPlural, det.FastfallMoveYFrames);
            }
            if (((Settings.MoveAfterLand.Mode == MovementLinterModuleSettings.MoveAfterLandMode.DashOnly) ||
                (Settings.MoveAfterLand.Mode == MovementLinterModuleSettings.MoveAfterLandMode.DashOrJump)) &&
                    det.CouldDashLastFrame &&
                    det.FramesAfterLand > 0 &&
                    det.FramesAfterLand <= Settings.MoveAfterLand.Frames) {
                res.DoLintResponses(Settings.MoveAfterLand, DialogIds.MoveAfterLandWarnSingular,
                                    DialogIds.MoveAfterLandWarnPlural, det.FramesAfterLand);
            }
            int dashLateFramesAfterUpEntry = det.FramesSinceUpTransition - UpEntryDashLockoutFrames;
            if (dashLateFramesAfterUpEntry > 0 &&
                    dashLateFramesAfterUpEntry <= Settings.DashAfterUpEntry.Frames) {
                res.DoLintResponses(Settings.DashAfterUpEntry, DialogIds.DashAfterUpEntryWarnSingular,
                                    DialogIds.DashAfterUpEntryWarnPlural, dashLateFramesAfterUpEntry);
            }
            // Forget about any previous jump release, we want to handle it exclusively via jump-release-dash
            det.JumpReleaseFrames  = BeyondShortDurationFrames;
            det.JumpReleaseMatters = false;
        }
        if (((button == Input.Jump && det.CouldJumpLastFrame) || (button == Input.Dash && det.CouldDashLastFrame)) &&
                det.InControlFrames > 0 &&
                det.InControlFrames <= Settings.MoveAfterGainControl.Frames) {
            res.DoLintResponses(Settings.MoveAfterGainControl, DialogIds.MoveAfterGainControlWarnSingular,
                                DialogIds.MoveAfterGainControlWarnPlural, det.InControlFrames);
        }
        // Don't trigger twice if we do multiple actions (e.g. instant hyper)
        det.InControlFrames = BeyondShortDurationFrames;
    }

    // =================================================================================================================
    private static void OnPlayerJump(On.Celeste.Player.orig_Jump orig, Player player,
                                     bool particles, bool playSfx) {
        orig(player, particles, playSfx);
        if (((Settings.MoveAfterLand.Mode == MovementLinterModuleSettings.MoveAfterLandMode.DashOrJump) ||
             (Settings.MoveAfterLand.Mode == MovementLinterModuleSettings.MoveAfterLandMode.JumpOnly)) &&
                det.FramesAfterLand > 0 &&
                det.FramesAfterLand <= Settings.MoveAfterLand.Frames &&
                !(Settings.MoveAfterLand.IgnoreUltras && det.UltradSinceLanding)) {
            res.DoLintResponses(Settings.MoveAfterLand, DialogIds.MoveAfterLandWarnSingular,
                                DialogIds.MoveAfterLandWarnPlural, det.FramesAfterLand);
        }
    }

    // =================================================================================================================
    private static void OnPlayerWallJump(On.Celeste.Player.orig_WallJump orig, Player player, int dir) {
        ClearMoveXUsed();
        // dir is the direction you're jumping, not the direction of the location of the wall compared to madeline
        if (det.ReleaseWFrames > 0 &&
                det.ReleaseWFrames <= Settings.TurnBeforeWallkick.Frames &&
                det.ReleaseWMatters &&
                !det.ForceMoveXActive &&
                player.moveX != 0 &&
                Math.Sign(det.FrameStartPlayerSpeed.X) != dir) {
            res.DoLintResponses(Settings.TurnBeforeWallkick, DialogIds.TurnBeforeWallkickWarnSingular,
                                DialogIds.TurnBeforeWallkickWarnPlural, det.ReleaseWFrames);
        }
        orig(player, dir);
    }

    // =================================================================================================================
    private static void OnPlayerSuperJump(On.Celeste.Player.orig_SuperJump orig, Player player) {
        ClearMoveXUsed();
        orig(player);
    }

    // =================================================================================================================
    private static void OnPlayerSuperWallJump(On.Celeste.Player.orig_SuperWallJump orig, Player player, int dir) {
        ClearMoveXUsed();
        orig(player, dir);
    }

    // =================================================================================================================
    // Bubble-related hooks
    private static void OnPlayerBoost(On.Celeste.Player.orig_Boost orig, Player player, Booster booster) {
        OnEnterBubble(player);
        orig(player, booster);
    }

    private static void OnPlayerRedBoost(On.Celeste.Player.orig_RedBoost orig, Player player, Booster booster) {
        OnEnterBubble(player);
        orig(player, booster);
    }

    private static void OnEnterBubble(Player player) {
        det.CouldDashBeforeBubble  = det.CanDashThisFrame;
        det.FramesBeforeFastBubble = 0;
    }

    private static int OnPlayerBoostUpdate(On.Celeste.Player.orig_BoostUpdate orig, Player player) {
        if ((Input.DashPressed || Input.CrouchDashPressed) &&
                !det.CouldDashBeforeBubble &&
                det.FramesBeforeFastBubble > 0 &&
                det.FramesBeforeFastBubble <= Settings.FastBubble.Frames) {
            res.DoLintResponses(Settings.FastBubble, DialogIds.FastBubbleWarnSingular, DialogIds.FastBubbleWarnPlural,
                                det.FramesBeforeFastBubble);
        }
        ++det.FramesBeforeFastBubble;
        return orig(player);
    }

    // =================================================================================================================
    // Collision hooks / patches
    private static void OnPlayerOnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player player, CollisionData data) {
        orig(player, data);
        det.LastWallHitDir     = (int) data.Direction.X;
        det.LastWallHitPlayerX = (int) player.Position.X;
    }

    private static void PatchPlayerOnCollideV(ILContext il) {
        ILCursor cursor = new(il);

        // Track that we just ultra'd when we apply the speed boost
        cursor.GotoNext(MoveType.After,
                        instr => instr.MatchLdcR4(1.2f));
        cursor.EmitDelegate<Action>(SetUltraCheck);
    }

    private static void SetUltraCheck() {
        det.UltradLastFrame    = true;
        det.UltradSinceLanding = true;
    }

    // =================================================================================================================
    private static void DetectionOnLevelLoadLevel() {
        det.RoomLoadJustHappened    = true;
        det.JumpReleaseFrames       = BeyondShortDurationFrames;
        det.JumpReleaseMatters      = false;
        det.FramesAfterLand         = BeyondShortDurationFrames;
        det.ReleaseWFrames          = BeyondShortDurationFrames;
        det.ReleaseWMatters         = false;
        det.FramesSinceUpTransition = det.UpTransitionJustHappened ?
                                          0 : BeyondShortDurationFrames + UpEntryDashLockoutFrames;
        det.FastfallMoveYFrames     = BeyondShortDurationFrames;
    }

    // =================================================================================================================
    private static void OnLevelTransitionTo(On.Celeste.Level.orig_TransitionTo orig, Level level, LevelData next,
                                            Vector2 direction) {
        orig(level, next, direction);
        if (((Settings.JumpReleaseExit.Mode == MovementLinterModuleSettings.TransitionDirection.UpOnly && direction.Y == -1) ||
             (Settings.JumpReleaseExit.Mode == MovementLinterModuleSettings.TransitionDirection.NotDown && direction.Y <= 0) ||
             (Settings.JumpReleaseExit.Mode == MovementLinterModuleSettings.TransitionDirection.Any)) &&
                det.JumpReleaseFrames > 0 &&
                det.JumpReleaseFrames <= Settings.JumpReleaseExit.Frames &&
                det.JumpReleaseMatters &&
                det.FrameStartPlayerState == Player.StNormal) {
            res.DoLintResponses(Settings.JumpReleaseExit, DialogIds.JumpReleaseExitWarnSingular,
                                DialogIds.JumpReleaseExitWarnPlural, det.JumpReleaseFrames);
        }
        if (direction.Y == 0 &&
                det.ReleaseWFrames > 0 &&
                det.ReleaseWFrames <= Settings.ReleaseWBeforeExit.Frames &&
                det.ReleaseWMatters &&
                !det.ForceMoveXActive) {
            res.DoLintResponses(Settings.ReleaseWBeforeExit, DialogIds.ReleaseWBeforeExitWarnSingular,
                                DialogIds.ReleaseWBeforeExitWarnPlural, det.ReleaseWFrames);
        }
        det.UpTransitionJustHappened = (direction.Y == -1);
    }
}
