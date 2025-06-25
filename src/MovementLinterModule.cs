using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Celeste.Mod.SpeedrunTool.SaveLoad;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

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

    // =================================================================================================================
    // Some extra stuff we need to set up while loading
    private static bool speedrunToolIsLoaded   = false;
    private static object saveLoadAction       = null;
    private static ILHook PlayerOrigUpdateHook = null;

    // =================================================================================================================
    // All the state needed for detection logic, wrapped in a struct for easy savestates.
    private const int BeyondShortDurationFrames = MovementLinterModuleSettings.MaxShortDurationFrames + 1;
    public struct Detection {
        public Detection() {}

        // Some general extra state tracking
        public int FrameStartPlayerState   = Player.StNormal;
        public int LastFinishedUpdateState = Player.StNormal;

        // Jump release
        public int JumpReleaseFrames   = BeyondShortDurationFrames;
        public bool JumpReleaseMatters = false;

        // Move after land
        public int FramesAfterLand     = BeyondShortDurationFrames;
        public bool UltradSinceLanding = false;

        // Move after gain control
        public bool WasInControl        = false;
        public int InControlFrames      = 0;
        public bool WasSkippingCutscene = false;

        // Dash after up transition
        public int FramesSinceUpTransition = BeyondShortDurationFrames;

        // moveX checks
        public Vector2 FrameStartPlayerSpeed = Vector2.Zero;
        public bool ForceMoveXActive         = false;
        public int LastMoveX                 = 0;
        public bool LastMoveXWasForward      = false;
        public int MoveXFrames               = BeyondShortDurationFrames;
        public bool FirstMoveX               = true;
        public bool TransitionJustHappened   = false;

        // Fastfall
        public bool FastfallCheckedThisFrame = false;
        public bool FastfallCheckedLastFrame = false;
        public bool FirstFastfallInput       = true;
        public bool MoveYIsFastfall          = false;
        public int FastfallMoveYFrames       = 0;

        // Buffered ultra
        public bool UltradLastFrame = false;
    };
    private static Detection det = new(), savedDet;

    // Everything we need to implement the lint responses
    private static LintResponder res = new();

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

        On.Celeste.Player.NormalBegin  += OnPlayerNormalBegin;
        IL.Celeste.Player.NormalUpdate += PatchPlayerNormalUpdate;

        On.Celeste.Player.Jump        += OnPlayerJump;
        On.Celeste.Player.WallJump    += OnPlayerWallJump;
        On.Celeste.Player.StartDash   += OnPlayerStartDash;
        IL.Celeste.Player.OnCollideV  += PatchPlayerOnCollideV;
        On.Celeste.Level.TransitionTo += OnLevelTransitionTo;

        speedrunToolIsLoaded = Everest.Modules.Any((EverestModule module) => module.Metadata.Name == "SpeedrunTool");
        if (speedrunToolIsLoaded) {
            AddSaveLoadAction();
        }
    }

    public override void Unload() {
        On.Monocle.VirtualButton.ConsumeBuffer -= OnVirtualButtonConsumeBuffer;
        On.Monocle.VirtualButton.ConsumePress  -= OnVirtualButtonConsumePress;

        On.Celeste.Level.Update -= OnLevelUpdate;

        On.Celeste.Player.Update -= OnPlayerUpdate;
        PlayerOrigUpdateHook?.Dispose();

        On.Celeste.Player.NormalBegin  -= OnPlayerNormalBegin;
        IL.Celeste.Player.NormalUpdate -= PatchPlayerNormalUpdate;

        On.Celeste.Player.Jump        -= OnPlayerJump;
        On.Celeste.Player.WallJump    -= OnPlayerWallJump;
        On.Celeste.Player.StartDash   -= OnPlayerStartDash;
        IL.Celeste.Player.OnCollideV  -= PatchPlayerOnCollideV;
        On.Celeste.Level.TransitionTo -= OnLevelTransitionTo;

        if (speedrunToolIsLoaded) {
            RemoveSaveLoadAction();
        }
    }

    // =================================================================================================================
    // Savestates
    private static void AddSaveLoadAction() {
        saveLoadAction = new SaveLoadAction(
            saveState: (_, _) => { savedDet = det; },
            loadState: (_, _) => { det = savedDet; },
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
    private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player player) {
        // Start of frame state tracking
        det.FrameStartPlayerState = player.StateMachine.State;
        det.FrameStartPlayerSpeed = player.Speed;
        // Jump release
        bool jumpPressed = Input.Jump.Pressed;
        // Move after gain control
        if (player.InControl && !det.WasInControl) {
            det.InControlFrames = 0;
        }
        det.WasInControl = player.InControl;
        // moveX
        bool forceMoveXWillBeActive = (player.forceMoveXTimer > 0f);
        // Fastfall
        det.FastfallCheckedThisFrame = false;

        // Run the frame
        orig(player);

        // End of frame state tracking
        det.LastFinishedUpdateState = det.FrameStartPlayerState;

        // Jump release
        if (det.LastFinishedUpdateState == Player.StNormal &&
                jumpPressed &&
                det.JumpReleaseFrames > 0 &&
                det.JumpReleaseFrames <= Settings.JumpReleaseJump.Frames &&
                det.JumpReleaseMatters) {
            res.DoLintResponse(Settings.JumpReleaseJump, DialogIds.JumpReleaseJumpWarnSingular,
                               DialogIds.JumpReleaseJumpWarnPlural, det.JumpReleaseFrames);
        }
        if (Input.Jump.Check) {
            det.JumpReleaseFrames  = 0;
            det.JumpReleaseMatters = false;
        } else {
            ++det.JumpReleaseFrames;
        }

        // Move after land
        ++det.FramesAfterLand;
        // Move after gain control
        ++det.InControlFrames;
        // Dash after up transition
        ++det.FramesSinceUpTransition;

        // moveX
        det.ForceMoveXActive    = forceMoveXWillBeActive;
        bool thisMoveXIsForward = (player.Speed.X != 0) && (Math.Sign(player.moveX) == Math.Sign(player.Speed.X));
        if (player.moveX != det.LastMoveX &&
                thisMoveXIsForward != det.LastMoveXWasForward &&
                !det.TransitionJustHappened) {
            det.MoveXFrames = 1;
            det.FirstMoveX  = false;
        } else {
            ++det.MoveXFrames;
        }
        det.TransitionJustHappened = false;
        det.LastMoveX              = player.moveX;
        det.LastMoveXWasForward    = thisMoveXIsForward;

        // Fastfall
        det.FastfallCheckedLastFrame = det.FastfallCheckedThisFrame;

        // Do the player kill here at the end of the frame so we have a consistent place to do it
        // where we know the player exists
        if (res.PendingKill) {
            res.PendingKill = false;
            player.Die(Vector2.Zero, true);
        }
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

        // Reset ultra check after state machine runs, before movement
        cursor.GotoNext(MoveType.After,
                        instr => instr.MatchCall(typeof(Actor), "Update"));
        cursor.EmitDelegate<Action>(ResetUltraCheck);
    }

    private static void CheckLandedOnGround(Player player) {
        if (player.onGround && !player.wasOnGround) {
            det.FramesAfterLand    = (player.StateMachine.State == Player.StNormal) ? 0 : BeyondShortDurationFrames;
            det.UltradSinceLanding = det.UltradLastFrame;
        }
    }

    private static void CheckShortWallboost(Player player) {
        // There are multiple competing off-by-one... quirks... surrounding wallboosts.
        // I'm not going to explain all of them, just trust me, this is the right number to use.
        int wallBoostFrames = (int) Math.Round((Player.ClimbJumpBoostTime - player.wallBoostTimer) * 60f) - 1;
        if (wallBoostFrames <= Settings.ShortWallboost.Frames) {
            res.DoLintResponse(Settings.ShortWallboost, DialogIds.ShortWallboostWarnSingular,
                               DialogIds.ShortWallboostWarnPlural, wallBoostFrames);
        }
    }

    private static void ResetUltraCheck() {
        det.UltradLastFrame = false;
    }

    // =================================================================================================================
    // StNormal hooks / patches
    private static void OnPlayerNormalBegin(On.Celeste.Player.orig_NormalBegin orig, Player player) {
        orig(player);
        det.JumpReleaseFrames  = BeyondShortDurationFrames;
        det.JumpReleaseMatters = false;
    }

    private static void PatchPlayerNormalUpdate(ILContext il) {
        ILCursor cursor = new(il);

        // Do our fastfall checks right before the vanilla ones
        cursor.GotoNext(MoveType.After,
                        instr => instr.MatchStfld(typeof(Player), "maxFall"),
                        instr => instr.OpCode == OpCodes.Br,
                        instr => instr.MatchLdsfld(typeof(Input), "MoveY"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc, 7);
        cursor.EmitDelegate<Action<Player, float>>(CheckFastfallInput);

        // Check if holding jump matters right before the game starts checking if jump is held
        cursor.GotoNext(MoveType.Before,
                        instr => instr.OpCode == OpCodes.Bge_Un_S,
                        instr => instr.MatchLdsfld(typeof(Input), "Jump"),
                        instr => instr.MatchCallvirt(typeof(Monocle.VirtualButton), "get_Check"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(CheckJumpReleaseMatters);

        // Check for a buffered ultra when we're about to ground jump
        cursor.GotoNext(MoveType.Before,
                        instr => instr.MatchCallvirt(typeof(Player), "Jump"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(CheckBufferedUltra);
    }

    private static void CheckFastfallInput(Player player, float fastfallThreshold) {
        if (player.Speed.Y < fastfallThreshold || player.onGround) {
            // Fastfall input not going to matter
            return;
        }
        bool moveYIsFastfall = (Input.MoveY.Value == 1);
        if (!det.FastfallCheckedLastFrame) {
            // Just entered fastfall conditions, reset tracking
            det.FirstFastfallInput  = true;
            det.FastfallMoveYFrames = 0;
        } else {
            // Check if the fastfall input status changed
            if (moveYIsFastfall != det.MoveYIsFastfall) {
                det.FirstFastfallInput  = false;
                det.FastfallMoveYFrames = 0;
            }
        }
        det.FastfallCheckedThisFrame = true;
        det.MoveYIsFastfall          = moveYIsFastfall;
        ++det.FastfallMoveYFrames;
    }

    private static void CheckJumpReleaseMatters(Player player) {
        det.JumpReleaseMatters |= !(player.AutoJump || Input.Jump.Check) &&
                                   (player.varJumpTimer > 0f || Math.Abs(player.Speed.Y) < 40f);
    }

    private static void CheckBufferedUltra(Player player) {
        if (!player.wasOnGround &&
                ((player.DashDir.X != 0f && player.DashDir.Y > 0f && player.Speed.Y > 0f) ||
                 (Settings.BufferedUltra.Mode == MovementLinterModuleSettings.BufferedUltraMode.Always && det.UltradLastFrame))) {
            res.DoLintResponse(Settings.BufferedUltra, DialogIds.BufferedUltraWarn, DialogIds.BufferedUltraWarn, 0);
        }
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
        if (button == Input.Jump || button == Input.Dash) {
            if (det.InControlFrames > 0 &&
                    det.InControlFrames <= Settings.MoveAfterGainControl.Frames &&
                    button.bufferCounter == button.BufferTime) {
                res.DoLintResponse(Settings.MoveAfterGainControl, DialogIds.MoveAfterGainControlWarnSingular,
                                   DialogIds.MoveAfterGainControlWarnPlural, det.InControlFrames);
            }
            det.InControlFrames = BeyondShortDurationFrames;
        }
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
            res.DoLintResponse(Settings.MoveAfterLand, DialogIds.MoveAfterLandWarnSingular,
                               DialogIds.MoveAfterLandWarnPlural, det.FramesAfterLand);
        }
    }

    // =================================================================================================================
    private static void OnPlayerWallJump(On.Celeste.Player.orig_WallJump orig, Player player, int dir) {
        // dir is the direction you're jumping, not the direction of the location of the wall compared to madeline
        if (det.LastFinishedUpdateState == Player.StNormal &&
                !det.ForceMoveXActive &&
                !det.FirstMoveX &&
                dir != 0 &&
                Math.Sign(det.FrameStartPlayerSpeed.X) != dir &&
                !det.LastMoveXWasForward &&
                det.MoveXFrames <= Settings.TurnBeforeWallkick.Frames) {
            res.DoLintResponse(Settings.TurnBeforeWallkick, DialogIds.TurnBeforeWallkickWarnSingular,
                               DialogIds.TurnBeforeWallkickWarnPlural, det.MoveXFrames);
        }
        orig(player, dir);
    }

    // =================================================================================================================
    private static int OnPlayerStartDash(On.Celeste.Player.orig_StartDash orig, Player player) {
        const int UpEntryDashLockoutFrames = 11;
        if (det.LastFinishedUpdateState == Player.StNormal &&
                det.JumpReleaseFrames > 0 &&
                det.JumpReleaseFrames <= Settings.JumpReleaseDash.Frames &&
                det.JumpReleaseMatters) {
            res.DoLintResponse(Settings.JumpReleaseDash, DialogIds.JumpReleaseDashWarnSingular,
                               DialogIds.JumpReleaseDashWarnPlural, det.JumpReleaseFrames);
        }
        if (det.LastFinishedUpdateState == Player.StNormal &&
                !det.ForceMoveXActive &&
                !det.LastMoveXWasForward &&
                det.MoveXFrames <= Settings.ReleaseWBeforeDash.Frames) {
            res.DoLintResponse(Settings.ReleaseWBeforeDash, DialogIds.ReleaseWBeforeDashWarnSingular,
                               DialogIds.ReleaseWBeforeDashWarnPlural, det.MoveXFrames);
        }
        if (det.FastfallCheckedLastFrame &&
                !det.FirstFastfallInput &&
                det.FastfallMoveYFrames <= Settings.FastfallGlitchBeforeDash.Frames) {
            res.DoLintResponse(Settings.FastfallGlitchBeforeDash, DialogIds.FastfallGlitchBeforeDashWarnSingular,
                               DialogIds.FastfallGlitchBeforeDashWarnPlural, det.FastfallMoveYFrames);
        }
        if (((Settings.MoveAfterLand.Mode == MovementLinterModuleSettings.MoveAfterLandMode.DashOnly) ||
             (Settings.MoveAfterLand.Mode == MovementLinterModuleSettings.MoveAfterLandMode.DashOrJump)) &&
                det.FramesAfterLand > 0 &&
                det.FramesAfterLand <= Settings.MoveAfterLand.Frames) {
            res.DoLintResponse(Settings.MoveAfterLand, DialogIds.MoveAfterLandWarnSingular,
                               DialogIds.MoveAfterLandWarnPlural, det.FramesAfterLand);
        }
        int dashLateFramesAfterUpEntry = det.FramesSinceUpTransition - UpEntryDashLockoutFrames;
        if (dashLateFramesAfterUpEntry > 0 &&
                dashLateFramesAfterUpEntry <= Settings.DashAfterUpEntry.Frames) {
            res.DoLintResponse(Settings.DashAfterUpEntry, DialogIds.DashAfterUpEntrySingular,
                               DialogIds.DashAfterUpEntryPlural, dashLateFramesAfterUpEntry);
        }
        return orig(player);
    }

    // =================================================================================================================
    // Collision patches
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
            res.DoLintResponse(Settings.JumpReleaseExit, DialogIds.JumpReleaseExitWarnSingular,
                               DialogIds.JumpReleaseExitWarnPlural, det.JumpReleaseFrames);
        }
        det.JumpReleaseFrames  = BeyondShortDurationFrames;
        det.JumpReleaseMatters = false;
        det.FirstMoveX              = true;
        det.TransitionJustHappened  = true;
        det.FramesSinceUpTransition = (direction.Y == -1) ? 0 : BeyondShortDurationFrames;
    }
}
