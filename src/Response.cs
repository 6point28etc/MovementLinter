using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.MovementLinter;

public class LintResponder {
    public static LintResponder Instance = new();
    private LintResponder() {}

    // Response queues. Some of these may not be strictly necessary given how the responses work,
    // but I like having  them as a general system.
    private bool pendingKill                 = false;
    private Queue<string> pendingTooltips    = [];
    private Queue<string> pendingDialog      = [];
    private Queue<Color> pendingSpriteColors = [];
    private Queue<Color> pendingHairColors   = [];
    private bool pendingHiccup               = false;
    private Queue<MovementLinterModuleSettings.HazardOption> pendingHazards = [];

    // Sprite color state
    private int spriteColorTimer = 0;
    private Color spriteColor    = Color.White;

    // Hair color state
    private int hairColorTimer = 0;
    private Color hairColor    = Color.White;

    // Hazard state
    private int badelineChaserIdx = 0;

    private Random random           = new();
    private int memorialTextCounter = 0;
    private bool memorialTextDrawn  = false;

    // =================================================================================================================
    public void DoLintResponses(MovementLinterModuleSettings.LintRuleSettings lintRuleSettings,
                                string singularWarnId, string pluralWarnId, int warnParam) {
        if (!MovementLinterModule.Settings.Enabled || !lintRuleSettings.IsEnabled()) {
            return;
        }
        string warning = (warnParam == 1) ? Dialog.Clean(singularWarnId)
                                          : string.Format(Dialog.Get(pluralWarnId), warnParam);
        foreach (MovementLinterModuleSettings.LintResponse response in lintRuleSettings.Responses) {
            DoLintResponse(response, warning);
        }
        if (++memorialTextCounter >= MovementLinterModuleSettings.MemorialTextThreshold) {
            MovementLinterModule.Settings.MemorialTextEnabled = true;
        }
    }

    private void DoLintResponse(MovementLinterModuleSettings.LintResponse response, string warning) {
        switch (response.Option) {
        case MovementLinterModuleSettings.LintResponseOption.Tooltip:
            pendingTooltips.Enqueue(warning);
            break;
        case MovementLinterModuleSettings.LintResponseOption.Dialog:
            string portrait = response.DialogCharacter switch {
                MovementLinterModuleSettings.CharacterOption.Madeline => DialogIds.MadelinePortrait,
                MovementLinterModuleSettings.CharacterOption.Badeline => DialogIds.BadelinePortrait,
                MovementLinterModuleSettings.CharacterOption.Granny   => DialogIds.GrannyPortrait,
                MovementLinterModuleSettings.CharacterOption.Theo     => DialogIds.TheoPortrait,
                MovementLinterModuleSettings.CharacterOption.Oshiro   => DialogIds.OshiroPortrait,
                _ => "c# is dumb"
            };
            pendingDialog.Enqueue(portrait + warning);
            break;
        case MovementLinterModuleSettings.LintResponseOption.Kill:
            // We could be getting called from anywhere, maybe this is a bad time to kill the player
            // (if the player even exists right now), so just set this flag and we'll handle it in player update.
            pendingKill = true;
            break;

        case MovementLinterModuleSettings.LintResponseOption.SFX:
            switch (response.SFX) {
            case MovementLinterModuleSettings.SFXOption.Caw:
                Audio.Play(SFX.game_gen_bird_squawk);
                break;
            case MovementLinterModuleSettings.SFXOption.BerryEscape:
                Audio.Play(SFXExt.strawberry_escape);
                break;
            case MovementLinterModuleSettings.SFXOption.Death:
                Audio.Play(random.Next(4096) == 0 ? SFX.char_mad_death_golden : SFX.char_mad_death);
                break;
            case MovementLinterModuleSettings.SFXOption.DingDong:
                Audio.Play(SFX.game_gen_touchswitch_last_oneshot);
                break;
            case MovementLinterModuleSettings.SFXOption.Console:
                switch (random.Next(5)) {
                case 0:
                    Audio.Play(SFX.game_01_console_blue);
                    break;
                case 1:
                    Audio.Play(SFX.game_01_console_purple);
                    break;
                case 2:
                    Audio.Play(SFX.game_01_console_red);
                    break;
                case 3:
                    Audio.Play(SFX.game_01_console_white);
                    break;
                case 4:
                    Audio.Play(SFX.game_01_console_yellow);
                    break;
                }
                break;
            case MovementLinterModuleSettings.SFXOption.BirdBros:
                Audio.Play(SFX.game_01_birdbros_thrust);
                break;
            case MovementLinterModuleSettings.SFXOption.Boop:
                Audio.Play(SFX.game_gen_thing_booped);
                break;
            case MovementLinterModuleSettings.SFXOption.Flag:
                Audio.Play(SFX.game_07_checkpointconfetti);
                break;
            case MovementLinterModuleSettings.SFXOption.FishSplode:
                Audio.Play(SFX.game_10_puffer_splode);
                break;
            case MovementLinterModuleSettings.SFXOption.PicoFlag:
                Audio.Play(SFX.game_10_pico8_flag);
                break;
            case MovementLinterModuleSettings.SFXOption.Secret:
                Audio.Play(SFX.game_gen_secret_revealed);
                break;
            case MovementLinterModuleSettings.SFXOption.Spring:
                Audio.Play(SFX.game_gen_spring);
                break;
            case MovementLinterModuleSettings.SFXOption.Kevin:
                // Not bothering to set Submerged here since I don't want to deal with making an Entity
                if (SaveData.Instance != null && SaveData.Instance.Name != null &&
                        SaveData.Instance.Name.StartsWith("FWAHAHA", StringComparison.InvariantCultureIgnoreCase)) {
                    Audio.Play(SFXExt.covert_single);
                } else {
                    Audio.Play(SFXExt.kevin_single);
                }
                break;
            case MovementLinterModuleSettings.SFXOption.Bumper:
                Audio.Play(SFX.game_06_pinballbumper_hit);
                break;
            case MovementLinterModuleSettings.SFXOption.Bonk:
                Audio.Play(SFXExt.bonk);
                break;
            case MovementLinterModuleSettings.SFXOption.Hey:
                Audio.Play(SFXExt.hey);
                break;
            case MovementLinterModuleSettings.SFXOption.GitGud:
                Audio.Play(SFXExt.git_gud);
                break;
            case MovementLinterModuleSettings.SFXOption.Uhoh:
                Audio.Play(SFXExt.uhoh);
                break;
            case MovementLinterModuleSettings.SFXOption.Alert:
                Audio.Play(SFXExt.alert);
                break;
            case MovementLinterModuleSettings.SFXOption.OhMyGott:
                Audio.Play(SFXExt.omg);
                break;
            case MovementLinterModuleSettings.SFXOption.Boom:
                Audio.Play(SFXExt.boom);
                break;
            }
            break;

        case MovementLinterModuleSettings.LintResponseOption.SpriteColor:
            pendingSpriteColors.Enqueue(ColorOptionToColor(response.SpriteColor,
                                                           response.CustomSpriteColor));
            break;
        
        case MovementLinterModuleSettings.LintResponseOption.HairColor:
            pendingHairColors.Enqueue(ColorOptionToColor(response.HairColor,
                                                         response.CustomHairColor));
            break;

        case MovementLinterModuleSettings.LintResponseOption.Hiccup:
            pendingHiccup = true;
            break;

        case MovementLinterModuleSettings.LintResponseOption.Hazard:
            pendingHazards.Enqueue(response.Hazard);
            break;

        case MovementLinterModuleSettings.LintResponseOption.MemorialTextOption:
            if (memorialTextDrawn) {
                break;
            }
            MovementLinterModuleSettings.LintResponse r = new();

            r.Option = MovementLinterModuleSettings.LintResponseOption.Tooltip;
            DoLintResponse(r, warning);

            r.Option = MovementLinterModuleSettings.LintResponseOption.Dialog;
            foreach (MovementLinterModuleSettings.CharacterOption character in
                         Enum.GetValues(typeof(MovementLinterModuleSettings.CharacterOption))) {
                r.DialogCharacter = character;
                DoLintResponse(r, warning);
            }

            r.Option = MovementLinterModuleSettings.LintResponseOption.Kill;
            DoLintResponse(r, warning);

            r.Option = MovementLinterModuleSettings.LintResponseOption.SFX;
            foreach (MovementLinterModuleSettings.SFXOption sfx in
                         Enum.GetValues(typeof(MovementLinterModuleSettings.SFXOption))) {
                r.SFX = sfx;
                DoLintResponse(r, warning);
            }

            r.Option            = MovementLinterModuleSettings.LintResponseOption.SpriteColor;
            r.SpriteColor       = MovementLinterModuleSettings.ColorOption.Custom;
            r.CustomSpriteColor = $"{random.Next(0x100):X2}{random.Next(0x100):X2}{random.Next(0x100):X2}";
            DoLintResponse(r, warning);

            r.Option          = MovementLinterModuleSettings.LintResponseOption.HairColor;
            r.HairColor       = MovementLinterModuleSettings.ColorOption.Custom;
            r.CustomHairColor = $"{random.Next(0x100):X2}{random.Next(0x100):X2}{random.Next(0x100):X2}";
            DoLintResponse(r, warning);

            r.Option = MovementLinterModuleSettings.LintResponseOption.Hiccup;
            DoLintResponse(r, warning);

            r.Option = MovementLinterModuleSettings.LintResponseOption.Hazard;
            foreach (MovementLinterModuleSettings.HazardOption hazard in
                         Enum.GetValues(typeof(MovementLinterModuleSettings.HazardOption))) {
                r.Hazard = hazard;
                DoLintResponse(r, warning);
            }

            memorialTextDrawn = true;
            break;
        }
    }

    // =================================================================================================================
    public void ProcessPendingResponses(Player player) {
        memorialTextCounter = 0;
        memorialTextDrawn   = false;
        if (player.StateMachine.State == Player.StDummy ||
                player.StateMachine.State == Player.StIntroWalk ||
                player.StateMachine.State == Player.StIntroJump ||
                player.StateMachine.State == Player.StIntroRespawn ||
                player.StateMachine.State == Player.StIntroWakeUp) {
            // Keep pending until we have a more "real" state
            return;
        }

        // If we're killing Madeline, wait to do any other responses until we get polled again after she respawns
        if (pendingKill) {
            player.Die(Vector2.Zero, true);
            pendingKill = false;
            return;
        }

        while (pendingTooltips.Count != 0) {
            Tooltip.Show(pendingTooltips.Dequeue());
        }
        while (pendingDialog.Count != 0) {
            // This can cause multiple dialogs to talk over each other. I don't want to space them out temporally
            // (since then I'm throwing a warning long after the actual detection) and it's funny, so I allow it.
            player.Scene.Add(new CustomMiniTextbox(pendingDialog.Dequeue()));
        }
        if (pendingSpriteColors.Count != 0) {
            // Cut short any existing color response when we get a new one
            spriteColorTimer = 30;
            spriteColor      = pendingSpriteColors.Dequeue();
        }
        if (pendingHairColors.Count != 0) {
            // Cut short any existing color response when we get a new one
            hairColorTimer = 30;
            hairColor      = pendingHairColors.Dequeue();
        }
        if (pendingHiccup) {
            player.HiccupJump();
            pendingHiccup = false;
        }
        while (pendingHazards.Count != 0) {
            switch (pendingHazards.Dequeue()) {
            case MovementLinterModuleSettings.HazardOption.BadelineChaser:
                EntityData entityData = new();
                entityData.Values     = new() {{ "canChangeMusic", false }};
                player.level.Add(new BadelineOldsite(entityData, player.Position, badelineChaserIdx++));
                break;
            case MovementLinterModuleSettings.HazardOption.Oshiro:
                player.level.Add(new AngryOshiro(
                    new Vector2(player.level.Bounds.Left - 32,
                                player.level.Bounds.Top + player.level.Bounds.Height / 2),
                    false));
                break;
            case MovementLinterModuleSettings.HazardOption.Snowball:
                player.level.Add(new Snowball());
                break;
            case MovementLinterModuleSettings.HazardOption.Seeker:
                player.GetChasePosition(player.level.TimeActive, 1f, out Player.ChaserState chaseState);
                player.level.Add(new Seeker(chaseState.Position, []));
                break;
            }
        }
    }

    // =================================================================================================================
    // Player render patches
    public static void PatchPlayerRender (ILContext il) {
        ILCursor cursor = new(il);

        // Vanilla hair color is set from Update rather than Render, so just set it first thing
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(SetHairColor);

        // Override sprite color after vanilla sets it
        cursor.GotoNext(MoveType.After,
                        instr => instr.MatchCall(typeof(Color), "get_White"),
                        instr => instr.MatchStfld(typeof(GraphicsComponent), "Color"),
                        instr => instr.OpCode == OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(SetSpriteColor);
    }

    private static void SetHairColor(Player player) {
        if (Instance.hairColorTimer > 0) {
            --Instance.hairColorTimer;
            player.Hair.Color = Instance.hairColor;
        }
    }

    private static void SetSpriteColor(Player player) {
        if (Instance.spriteColorTimer > 0) {
            --Instance.spriteColorTimer;
            player.Sprite.Color = Instance.spriteColor;
        }
    }

    // =================================================================================================================
    // Badeline chaser mods
    public static bool OnBadelineOldsiteCanChangeMusic(On.Celeste.BadelineOldsite.orig_CanChangeMusic orig,
                                                       BadelineOldsite chaser, bool val) {
        // Vanilla chasers always default to true, so if canChangeMusic is false I know I should respect it
        if (!chaser.canChangeMusic) {
            return false;
        }
        // Otherwise, use the Everest behavior which leaves vanilla levels untouched. I never set canChangeMusic to true
        // so no need to force that in vanilla.
        return orig(chaser, val);
    }

    public static void PatchBadelineOldsiteAdded(ILContext il) {
        // Vanilla does some room and session-based checks to start the Badeline intro cutscene and force-remove
        // chasers. Everest overrides this to always act normal in non-vanilla levels. I want to always act normal for
        // chasers I've spawned, so I check canChangeMusic as a hacky way to identify my chasers (since I always set it
        // to false), then default to the Everest behavior. Have to do this in IL rather than just hooking it because
        // C# sucks.
        ILCursor cursor = new(il);

        // Before checking if the level is a vanilla one, check canChangeMusic
        cursor.GotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Ldloc_0);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, typeof(BadelineOldsite).GetField("canChangeMusic",
                                                                    BindingFlags.NonPublic | BindingFlags.Instance));

        // Find the call to Entity::Added, which is the first step of the branch that avoids orig_Added
        ILCursor branchToCursor = cursor.Clone();
        branchToCursor.GotoNext(MoveType.Before,
                                instr => instr.OpCode == OpCodes.Ldarg_0,
                                instr => instr.OpCode == OpCodes.Ldarg_1,
                                instr => instr.MatchCall(typeof(Entity), "Added"));

        // Branch to the non-orig_Added path if canChangeMusic is false
        cursor.Emit(OpCodes.Brfalse_S, branchToCursor.Next);
    }

    // =================================================================================================================
    public static void OnLevelLoadLevel() {
        Instance.badelineChaserIdx = 0;
    }

    // =================================================================================================================
    private static Color ColorOptionToColor(MovementLinterModuleSettings.ColorOption color, string customHexCode) {
        return color switch {
            MovementLinterModuleSettings.ColorOption.Red    => Calc.HexToColor("ff0000"),
            MovementLinterModuleSettings.ColorOption.Green  => Calc.HexToColor("00ff00"),
            MovementLinterModuleSettings.ColorOption.Blue   => Calc.HexToColor("0000ff"),
            MovementLinterModuleSettings.ColorOption.Purple => Calc.HexToColor("8000ff"),
            MovementLinterModuleSettings.ColorOption.Orange => Calc.HexToColor("ff8000"),
            MovementLinterModuleSettings.ColorOption.Yellow => Calc.HexToColor("ffff00"),
            MovementLinterModuleSettings.ColorOption.White  => Calc.HexToColor("ffffff"),
            MovementLinterModuleSettings.ColorOption.Gray   => Calc.HexToColor("808080"),
            MovementLinterModuleSettings.ColorOption.Black  => Calc.HexToColor("000000"),
            MovementLinterModuleSettings.ColorOption.Custom => Calc.HexToColor(customHexCode),
            _ => Color.White,
        };
    }
}
