using System;
using System.Collections.Generic;
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

    // Sprite color state
    private int spriteColorTimer = 0;
    private Color spriteColor    = Color.White;

    // Hair color state
    private int hairColorTimer = 0;
    private Color hairColor    = Color.White;

    private Random random = new();

    // =================================================================================================================
    public void DoLintResponse<ModeT>(MovementLinterModuleSettings.LintRuleSettings<ModeT> lintRuleSettings,
                                      string singularWarnId, string pluralWarnId, int warnParam) {
        if (!MovementLinterModule.Settings.Enabled || !lintRuleSettings.IsEnabled()) {
            return;
        }

        string warning = (warnParam == 1) ? Dialog.Clean(singularWarnId)
                                          : string.Format(Dialog.Get(pluralWarnId), warnParam);

        switch (lintRuleSettings.Response) {
        case MovementLinterModuleSettings.LintResponse.Tooltip:
            pendingTooltips.Enqueue(warning);
            break;
        case MovementLinterModuleSettings.LintResponse.Dialog:
            string portrait = lintRuleSettings.DialogCharacter switch {
                MovementLinterModuleSettings.CharacterOption.Madeline => DialogIds.MadelinePortrait,
                MovementLinterModuleSettings.CharacterOption.Badeline => DialogIds.BadelinePortrait,
                MovementLinterModuleSettings.CharacterOption.Granny   => DialogIds.GrannyPortrait,
                MovementLinterModuleSettings.CharacterOption.Theo     => DialogIds.TheoPortrait,
                MovementLinterModuleSettings.CharacterOption.Oshiro   => DialogIds.OshiroPortrait,
                _ => "c# is dumb"
            };
            pendingDialog.Enqueue(portrait + warning);
            break;
        case MovementLinterModuleSettings.LintResponse.Kill:
            // We could be getting called from anywhere, maybe this is a bad time to kill the player
            // (if the player even exists right now), so just set this flag and we'll handle it in player update.
            pendingKill = true;
            break;

        case MovementLinterModuleSettings.LintResponse.SFX:
            switch (lintRuleSettings.SFX) {
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

        case MovementLinterModuleSettings.LintResponse.SpriteColor:
            pendingSpriteColors.Enqueue(ColorOptionToColor(lintRuleSettings.SpriteColor,
                                                           lintRuleSettings.CustomSpriteColor));
            break;
        
        case MovementLinterModuleSettings.LintResponse.HairColor:
            pendingHairColors.Enqueue(ColorOptionToColor(lintRuleSettings.HairColor,
                                                         lintRuleSettings.CustomHairColor));
            break;

        case MovementLinterModuleSettings.LintResponse.Hiccup:
            pendingHiccup = true;
            break;
        }
    }

    // =================================================================================================================
    public void ProcessPendingResponses(Player player) {
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
