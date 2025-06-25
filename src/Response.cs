using System;

namespace Celeste.Mod.MovementLinter;

public class LintResponder {
    /// <summary>
    /// True if we should kill Madeline at the next available opportunity
    /// </summary>
    public bool PendingKill = false;

    private Random random = new();

    public void DoLintResponse<ModeT>(MovementLinterModuleSettings.LintRuleSettings<ModeT> lintRuleSettings,
                                      string singularWarnId, string pluralWarnId, int warnParam) {
        if (!MovementLinterModule.Settings.Enabled || !lintRuleSettings.IsEnabled()) {
            return;
        }

        string warning = (warnParam == 1) ? Dialog.Clean(singularWarnId)
                                          : string.Format(Dialog.Get(pluralWarnId), warnParam);

        switch (lintRuleSettings.Response) {
        case MovementLinterModuleSettings.LintResponse.Tooltip:
            Tooltip.Show(warning);
            break;

        case MovementLinterModuleSettings.LintResponse.Kill:
            // We could be getting called from anywhere, maybe this is a bad time to kill the player
            // (if the player even exists right now), so just set this flag and we'll handle it in player update.
            PendingKill = true;
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
        }
    }
}
