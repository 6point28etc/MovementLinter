using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.TextMenuLib;

namespace Celeste.Mod.MovementLinter;

[SettingName(DialogIds.MovementLinter)]
public class MovementLinterModuleSettings : EverestModuleSettings {
    // =================================================================================================================
    public enum LintResponseOption {
        Tooltip,
        Dialog,
        Kill,
        SFX,
        SpriteColor,
        HairColor,
        Hiccup,
        Hazard,
        MemorialTextOption,
    }
    public enum CharacterOption {
        Madeline,
        Badeline,
        Granny,
        Theo,
        Oshiro,
    }
    public enum SFXOption {
        Caw,
        BerryEscape,
        Death,
        DingDong,
        Console,
        BirdBros,
        Boop,
        Flag,
        FishSplode,
        PicoFlag,
        Secret,
        Spring,
        Kevin,
        Bumper,
        Bonk,
        Hey,
        GitGud,
        Uhoh,
        Alert,
        OhMyGott,
        Boom,
    }
    public enum ColorOption {
        Red,
        Green,
        Blue,
        Purple,
        Orange,
        Yellow,
        White,
        Gray,
        Black,
        Custom,
    }
    public enum HazardOption {
        BadelineChaser,
        Oshiro,
        Snowball,
        Seeker,
    }
    public enum TransitionDirection {
        None,
        UpOnly,
        NotDown,
        Any,
    }
    public enum MoveAfterLandMode {
        Disabled,
        DashOnly,
        DashOrJump,
        JumpOnly,
    }
    public enum BufferedUltraMode {
        Disabled,
        OnlyWhenMattered,
        Always,
    }
    public const int MaxShortDurationFrames  = 99;
    public const int MaxShortWallboostFrames = 11;
    public const int MemorialTextThreshold   = 3;

    // =================================================================================================================
    public bool Enabled { get; set; }             = true;
    public bool MemorialTextEnabled { get; set; } = false;

    // =================================================================================================================
    public class LintResponse {
        public LintResponseOption Option { get; set; }       = LintResponseOption.Tooltip;
        public CharacterOption DialogCharacter { get; set; } = CharacterOption.Madeline;
        public SFXOption SFX { get; set; }                   = SFXOption.Caw;
        public ColorOption SpriteColor { get; set; }         = ColorOption.Red;
        public string CustomSpriteColor { get; set; }        = "6487ed";
        public ColorOption HairColor { get; set; }           = ColorOption.Green;
        public string CustomHairColor { get; set; }          = "6487ed";
        public HazardOption Hazard { get; set; }             = HazardOption.BadelineChaser;

        // -------------------------------------------------------------------------------------------------------------
        public RecursiveOptionSubMenu MakeSubMenu(bool inGame, TextMenu topMenu, float compactRightWidth,
                                                  bool memorialTextEnabled) {
            // Make the response menu first since some items need access to it
            RecursiveOptionSubMenu responseMenu = new(label: Dialog.Clean(DialogIds.LintResponse),
                                                      initialMenuSelection: (int) Option,
                                                      compactRightWidth: compactRightWidth);

            // Dialog
            BetterWidthOption<CharacterOption> characterSlider = new(Dialog.Clean(DialogIds.CharacterSelect));
            characterSlider.Add(Dialog.Clean(DialogIds.Madeline), CharacterOption.Madeline, DialogCharacter == CharacterOption.Madeline)
                           .Add(Dialog.Clean(DialogIds.Badeline), CharacterOption.Badeline, DialogCharacter == CharacterOption.Badeline)
                           .Add(Dialog.Clean(DialogIds.Granny),   CharacterOption.Granny,   DialogCharacter == CharacterOption.Granny)
                           .Add(Dialog.Clean(DialogIds.Theo),     CharacterOption.Theo,     DialogCharacter == CharacterOption.Theo)
                           .Add(Dialog.Clean(DialogIds.Oshiro),   CharacterOption.Oshiro,   DialogCharacter == CharacterOption.Oshiro)
                           .Change((CharacterOption val) => DialogCharacter = val);

            // SFX
            BetterWidthOption<SFXOption> sfxSlider = new(Dialog.Clean(DialogIds.SFXSelect));
            sfxSlider.Add(Dialog.Clean(DialogIds.SFXCaw),         SFXOption.Caw,         SFX == SFXOption.Caw)
                     .Add(Dialog.Clean(DialogIds.SFXBerryEscape), SFXOption.BerryEscape, SFX == SFXOption.BerryEscape)
                     .Add(Dialog.Clean(DialogIds.SFXDeath),       SFXOption.Death,       SFX == SFXOption.Death)
                     .Add(Dialog.Clean(DialogIds.SFXDingDong),    SFXOption.DingDong,    SFX == SFXOption.DingDong)
                     .Add(Dialog.Clean(DialogIds.SFXConsole),     SFXOption.Console,     SFX == SFXOption.Console)
                     .Add(Dialog.Clean(DialogIds.SFXBirdBros),    SFXOption.BirdBros,    SFX == SFXOption.BirdBros)
                     .Add(Dialog.Clean(DialogIds.SFXBoop),        SFXOption.Boop,        SFX == SFXOption.Boop)
                     .Add(Dialog.Clean(DialogIds.SFXFlag),        SFXOption.Flag,        SFX == SFXOption.Flag)
                     .Add(Dialog.Clean(DialogIds.SFXFishSplode),  SFXOption.FishSplode,  SFX == SFXOption.FishSplode)
                     .Add(Dialog.Clean(DialogIds.SFXPicoFlag),    SFXOption.PicoFlag,    SFX == SFXOption.PicoFlag)
                     .Add(Dialog.Clean(DialogIds.SFXSecret),      SFXOption.Secret,      SFX == SFXOption.Secret)
                     .Add(Dialog.Clean(DialogIds.SFXSpring),      SFXOption.Spring,      SFX == SFXOption.Spring)
                     .Add(Dialog.Clean(DialogIds.SFXKevin),       SFXOption.Kevin,       SFX == SFXOption.Kevin)
                     .Add(Dialog.Clean(DialogIds.SFXBumper),      SFXOption.Bumper,      SFX == SFXOption.Bumper)
                     .Add(Dialog.Clean(DialogIds.SFXBonk),        SFXOption.Bonk,        SFX == SFXOption.Bonk)
                     .Add(Dialog.Clean(DialogIds.SFXHey),         SFXOption.Hey,         SFX == SFXOption.Hey)
                     .Add(Dialog.Clean(DialogIds.SFXGitGud),      SFXOption.GitGud,      SFX == SFXOption.GitGud)
                     .Add(Dialog.Clean(DialogIds.SFXUhoh),        SFXOption.Uhoh,        SFX == SFXOption.Uhoh)
                     .Add(Dialog.Clean(DialogIds.SFXAlert),       SFXOption.Alert,       SFX == SFXOption.Alert)
                     .Add(Dialog.Clean(DialogIds.SFXOhMyGott),    SFXOption.OhMyGott,    SFX == SFXOption.OhMyGott)
                     .Add(Dialog.Clean(DialogIds.SFXBoom),        SFXOption.Boom,        SFX == SFXOption.Boom)
                     .Change((SFXOption val) => SFX = val);

            // Sprite color
            BetterWidthOption<ColorOption> spriteColorSlider = new(Dialog.Clean(DialogIds.ColorSelect));
            ParentAwareEaseInSubHeader customSpriteColorHint = new(Dialog.Clean(DialogIds.CustomColorHint),
                                                                   SpriteColor == ColorOption.Custom,
                                                                   topMenu, responseMenu){ HeightExtra = 0f };
            spriteColorSlider.Add(Dialog.Clean(DialogIds.Red),    ColorOption.Red,    SpriteColor == ColorOption.Red)
                             .Add(Dialog.Clean(DialogIds.Green),  ColorOption.Green,  SpriteColor == ColorOption.Green)
                             .Add(Dialog.Clean(DialogIds.Blue),   ColorOption.Blue,   SpriteColor == ColorOption.Blue)
                             .Add(Dialog.Clean(DialogIds.Purple), ColorOption.Purple, SpriteColor == ColorOption.Purple)
                             .Add(Dialog.Clean(DialogIds.Orange), ColorOption.Orange, SpriteColor == ColorOption.Orange)
                             .Add(Dialog.Clean(DialogIds.Gray),   ColorOption.Gray,   SpriteColor == ColorOption.Gray)
                             .Add(Dialog.Clean(DialogIds.Black),  ColorOption.Black,  SpriteColor == ColorOption.Black)
                             .Add("#" + CustomSpriteColor,        ColorOption.Custom, SpriteColor == ColorOption.Custom)
                             .Change((ColorOption val) => {
                                         SpriteColor                       = val;
                                         customSpriteColorHint.FadeVisible = (val == ColorOption.Custom);
                                     });
            ColorEntryPage customSpritePage = new(topMenu, responseMenu, Dialog.Clean(DialogIds.SpriteColorHeader));
            spriteColorSlider.Pressed(delegate {
                if (spriteColorSlider.Index == spriteColorSlider.Values.Count - 1) {
                    Audio.Play(global::Celeste.SFX.ui_main_button_select);
                    customSpritePage.Enter();
                }
            });
            customSpritePage.OnAccept = (string val) => {
                CustomSpriteColor = val;
                spriteColorSlider.Values[spriteColorSlider.Values.Count - 1] =
                    new("#" + val, spriteColorSlider.Values.Last().Item2);
            };

            // Hair color
            BetterWidthOption<ColorOption> hairColorSlider = new(Dialog.Clean(DialogIds.ColorSelect));
            ParentAwareEaseInSubHeader customHairColorHint = new(Dialog.Clean(DialogIds.CustomColorHint),
                                                                 HairColor == ColorOption.Custom,
                                                                 topMenu, responseMenu){ HeightExtra = 0f };
            hairColorSlider.Add(Dialog.Clean(DialogIds.Red),    ColorOption.Red,    HairColor == ColorOption.Red)
                           .Add(Dialog.Clean(DialogIds.Green),  ColorOption.Green,  HairColor == ColorOption.Green)
                           .Add(Dialog.Clean(DialogIds.Blue),   ColorOption.Blue,   HairColor == ColorOption.Blue)
                           .Add(Dialog.Clean(DialogIds.Purple), ColorOption.Purple, HairColor == ColorOption.Purple)
                           .Add(Dialog.Clean(DialogIds.Orange), ColorOption.Orange, HairColor == ColorOption.Orange)
                           .Add(Dialog.Clean(DialogIds.Yellow), ColorOption.Yellow, HairColor == ColorOption.Yellow)
                           .Add(Dialog.Clean(DialogIds.White),  ColorOption.White,  HairColor == ColorOption.White)
                           .Add(Dialog.Clean(DialogIds.Gray),   ColorOption.Gray,   HairColor == ColorOption.Gray)
                           .Add(Dialog.Clean(DialogIds.Black),  ColorOption.Black,  HairColor == ColorOption.Black)
                           .Add("#" + CustomHairColor,          ColorOption.Custom, HairColor == ColorOption.Custom)
                           .Change((ColorOption val) => {
                                       HairColor                       = val;
                                       customHairColorHint.FadeVisible = (val == ColorOption.Custom);
                                   });
            ColorEntryPage customHairPage = new(topMenu, responseMenu, Dialog.Clean(DialogIds.HairColorHeader));
            hairColorSlider.Pressed(delegate {
                if (hairColorSlider.Index == hairColorSlider.Values.Count - 1) {
                    Audio.Play(global::Celeste.SFX.ui_main_button_select);
                    customHairPage.Enter();
                }
            });
            customHairPage.OnAccept = (string val) => {
                CustomHairColor = val;
                hairColorSlider.Values[hairColorSlider.Values.Count - 1] =
                    new("#" + val, hairColorSlider.Values.Last().Item2);
            };

            // Hazards
            BetterWidthOption<HazardOption> hazardSlider = new(Dialog.Clean(DialogIds.HazardSelect));
            hazardSlider.Add(Dialog.Clean(DialogIds.BadelineChaser), HazardOption.BadelineChaser, Hazard == HazardOption.BadelineChaser)
                        .Add(Dialog.Clean(DialogIds.AngryOshiro),    HazardOption.Oshiro,         Hazard == HazardOption.Oshiro)
                        .Add(Dialog.Clean(DialogIds.Snowball),       HazardOption.Snowball,       Hazard == HazardOption.Snowball)
                        .Add(Dialog.Clean(DialogIds.Seeker),         HazardOption.Seeker,         Hazard == HazardOption.Seeker)
                        .Change((HazardOption val) => Hazard = val);

            // Add all options to the response menu
            responseMenu.AddMenu(Dialog.Clean(DialogIds.LintResponseTooltip), [])
                        .AddMenu(Dialog.Clean(DialogIds.LintResponseDialog), [characterSlider])
                        .AddMenu(Dialog.Clean(DialogIds.LintResponseKill), [])
                        .AddMenu(Dialog.Clean(DialogIds.LintResponseSFX), [sfxSlider])
                        .AddMenu(Dialog.Clean(DialogIds.LintResponseSpriteColor), [spriteColorSlider,
                                                                                   customSpriteColorHint])
                        .AddMenu(Dialog.Clean(DialogIds.LintResponseHairColor), [hairColorSlider,
                                                                                 customHairColorHint])
                        .AddMenu(Dialog.Clean(DialogIds.LintResponseHiccup), [])
                        .AddMenu(Dialog.Clean(DialogIds.LintResponseHazard), [hazardSlider]);
            if (memorialTextEnabled) {
                responseMenu.AddMenu(Dialog.Clean(DialogIds.LintResponseMemorialText), []);
            }
            responseMenu.Change((int val) => Option = (LintResponseOption) val);
            return responseMenu;
        }
    }

    // =================================================================================================================
    /// <summary>
    ///     Base class for all the settings associated with a single heuristic rule / check
    /// </summary>
    /// <typeparam name="ModeT">
    ///     Type of the "mode" setting for this rule
    ///     (bool for a simple on/off toggle, or some enum for a richer set of mode options)
    /// </typeparam>
    public abstract class LintRuleSettings<ModeT> {
        private const int maxResponses = 3;

        private readonly string titleId;
        private readonly string hintId;

        public ModeT Mode { get; set; }
        public List<LintResponse> Responses { get; set; } = [new()];

        public LintRuleSettings(ModeT defaultMode, string titleId, string hintId) {
            this.Mode    = defaultMode;
            this.titleId = titleId;
            this.hintId  = hintId;
        }

        /// <summary>
        ///     Return a submenu to control the settings for this lint rule
        /// </summary>
        /// <param name="inGame"> Whether the menu was opened in-game vs from the main menu</param>
        /// <param name="topMenu">The top-level TextMenu containing this submenu</param>
        /// <param name="compactRightWidth">The right-width to use in compact mode</param>
        public RecursiveSubMenu MakeSubMenu(bool inGame, TextMenu topMenu, float compactRightWidth,
                                            bool memorialTextEnabled) {
            // Hint, mode, mode preview
            TextMenuExt.EaseInSubHeaderExt ruleHint = new(Dialog.Clean(hintId), true, topMenu){ HeightExtra = 0f };
            BetterWidthOption<ModeT> modeItem = MakeModeMenuItem();
            modeItem.Change((ModeT val) => Mode = val);
            OptionPreview<ModeT> modePreview = new(modeItem);

            // Make the submenu now since we need to manipulate it from the response items
            RecursiveSubMenu ruleMenu = new(label: Dialog.Clean(titleId), compactRightWidth: compactRightWidth,
                                            preview: modePreview);
            ruleMenu.AddItem(ruleHint)
                    .AddItem(modeItem);
            foreach (TextMenu.Item item in MakeUniqueMenuItems(inGame)) {
                ruleMenu.AddItem(item);
            }

            // Make add response button and the hint that goes with it, don't add them yet
            TextMenu.Button addResponseButton = new(Dialog.Clean(DialogIds.AddResponse)){
                Disabled = (Responses.Count >= maxResponses)
            };
            ParentAwareEaseInSubHeader removeResponseHint = new(Dialog.Clean(DialogIds.RemoveResponseHint),
                                                                addResponseButton.Disabled,
                                                                topMenu, ruleMenu){ HeightExtra = 0f };
            addResponseButton.Enter(delegate { removeResponseHint.FadeVisible = true; })
                             .Leave(delegate { removeResponseHint.FadeVisible = addResponseButton.Disabled; });

            // Make and add response menus for the existing responses from the existing settings
            foreach (LintResponse response in Responses) {
                RecursiveOptionSubMenu responseMenu = response.MakeSubMenu(inGame, topMenu, compactRightWidth,
                                                                           memorialTextEnabled);
                SetDeleteResponseBind(response, responseMenu, ruleMenu, addResponseButton, removeResponseHint);
                ruleMenu.AddItem(responseMenu);
            };

            // Callback to add a response and its menu
            addResponseButton.Pressed(delegate {
                if (Responses.Count >= maxResponses) {
                    // Prevent violating the max responses during the response add animation
                    // before we've moved ourselves off the button
                    return;
                }
                LintResponse newResponse = new();
                Responses.Add(newResponse);
                RecursiveOptionSubMenu newResponseMenu = newResponse.MakeSubMenu(inGame, topMenu, compactRightWidth,
                                                                                 memorialTextEnabled);
                SetDeleteResponseBind(newResponse, newResponseMenu, ruleMenu, addResponseButton, removeResponseHint);
                ruleMenu.InsertItem(ruleMenu.CurrentMenu.IndexOf(addResponseButton), newResponseMenu, true,
                                    (TextMenu.Item item) => {
                                        // Force the selection off the button after the new response finishes adding
                                        if (addResponseButton.Disabled && ruleMenu.Current == addResponseButton) {
                                            ruleMenu.MoveSelection(-1, false, true);
                                        }
                                    });
                if (Responses.Count >= maxResponses) {
                    addResponseButton.Disabled = true;
                }
            });

            // Add the add response button and its hint after all the responses
            ruleMenu.AddItem(addResponseButton);
            ruleMenu.AddItem(removeResponseHint);

            return ruleMenu;
        }

        private void SetDeleteResponseBind(LintResponse response, RecursiveOptionSubMenu responseMenu,
                                           RecursiveSubMenu ruleMenu, TextMenu.Button addResponseButton,
                                           ParentAwareEaseInSubHeader removeResponseHint) {
            responseMenu.AltPressed(delegate {
                if (Responses.Count > 1) {
                    Responses.Remove(response);
                    ruleMenu.RemoveItem(responseMenu, true);
                    Audio.Play(SFX.ui_main_button_back);
                    if (Responses.Count < maxResponses) {
                        addResponseButton.Disabled     = false;
                        removeResponseHint.FadeVisible = false;
                    }
                } else {
                    Audio.Play(SFX.ui_main_button_invalid);
                }
            });
        }

        /// <summary>
        ///     Returns true if this rule should be enabled in some form, false if it should be completely disabled
        /// </summary>
        public abstract bool IsEnabled();

        /// <summary>
        ///     Make the menu widget that controls the <see cref="Mode"/> property
        /// </summary>
        protected abstract BetterWidthOption<ModeT> MakeModeMenuItem();

        /// <summary>
        ///     Make any additional menu widgets to control properties not defined in the base class
        /// </summary>
        /// <param name="inGame">Whether the menu was opened in-game vs from the main menu</param>
        protected abstract List<TextMenu.Item> MakeUniqueMenuItems(bool inGame);
    }

    // =================================================================================================================
    public class JumpReleaseJumpSettings : LintRuleSettings<bool> {
        public int Frames { get; set; } = 2;

        public JumpReleaseJumpSettings() : base(true, DialogIds.JumpReleaseJump, DialogIds.JumpReleaseJumpHint) {}

        public override bool IsEnabled() => Mode;

        protected override BetterWidthOption<bool> MakeModeMenuItem() {
            return new BetterWidthOnOff(Dialog.Clean(DialogIds.Enabled), Mode);
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [
                new BetterWidthSlider(
                    label  : Dialog.Clean(DialogIds.JumpReleaseJumpFrames),
                    values : (int val) => val.ToString(),
                    min    : 1,
                    max    : MaxShortDurationFrames,
                    value  : Frames
                ).Change((int val) => Frames = val)
            ];
        }
    }
    public JumpReleaseJumpSettings JumpReleaseJump { get; set; } = new();

    // =================================================================================================================
    public class JumpReleaseDashSettings : LintRuleSettings<bool>{
        public int Frames { get; set; } = 3;

        public JumpReleaseDashSettings() : base(true, DialogIds.JumpReleaseDash, DialogIds.JumpReleaseDashHint) {}

        public override bool IsEnabled() => Mode;

        protected override BetterWidthOption<bool> MakeModeMenuItem() {
            return new BetterWidthOnOff(Dialog.Clean(DialogIds.Enabled), Mode);
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [
                new BetterWidthSlider(
                    label  : Dialog.Clean(DialogIds.JumpReleaseDashFrames),
                    values : (int val) => val.ToString(),
                    min    : 1,
                    max    : MaxShortDurationFrames,
                    value  : Frames
                ).Change((int val) => Frames = val)
            ];
        }
    }
    public JumpReleaseDashSettings JumpReleaseDash { get; set; } = new();

    // =================================================================================================================
    public class JumpReleaseExitSettings : LintRuleSettings<TransitionDirection> {
        public int Frames { get; set; } = 6;

        public JumpReleaseExitSettings()
            : base(TransitionDirection.UpOnly, DialogIds.JumpReleaseExit, DialogIds.JumpReleaseExitHint) {}

        public override bool IsEnabled() => Mode != TransitionDirection.None;

        protected override BetterWidthOption<TransitionDirection> MakeModeMenuItem() {
            BetterWidthOption<TransitionDirection> item = new(Dialog.Clean(DialogIds.JumpReleaseExitDirection));
            item.Add(Dialog.Clean(DialogIds.None), TransitionDirection.None,
                     Mode == TransitionDirection.None)
                .Add(Dialog.Clean(DialogIds.JumpReleaseExitUp), TransitionDirection.UpOnly,
                     Mode == TransitionDirection.UpOnly)
                .Add(Dialog.Clean(DialogIds.JumpReleaseExitNotDown), TransitionDirection.NotDown,
                     Mode == TransitionDirection.NotDown)
                .Add(Dialog.Clean(DialogIds.JumpReleaseExitAny), TransitionDirection.Any,
                     Mode == TransitionDirection.Any);
            return item;
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [
                new BetterWidthSlider(
                    label  : Dialog.Clean(DialogIds.JumpReleaseExitFrames),
                    values : (int val) => val.ToString(),
                    min    : 1,
                    max    : MaxShortDurationFrames,
                    value  : Frames
                ).Change((int val) => Frames = val)
            ];
        }
    }
    public JumpReleaseExitSettings JumpReleaseExit { get; set; } = new();

    // =================================================================================================================
    public class MoveAfterLandSettings : LintRuleSettings<MoveAfterLandMode> {
        public bool IgnoreUltras { get; set; } = true;
        public int Frames { get; set; }        = 3;

        public MoveAfterLandSettings()
            : base(MoveAfterLandMode.DashOnly, DialogIds.MoveAfterLand, DialogIds.MoveAfterLandHint) {}

        public override bool IsEnabled() => Mode != MoveAfterLandMode.Disabled;

        protected override BetterWidthOption<MoveAfterLandMode> MakeModeMenuItem() {
            BetterWidthOption<MoveAfterLandMode> item = new(Dialog.Clean(DialogIds.MoveAfterLandMode));
            item.Add(Dialog.Clean(DialogIds.None), MoveAfterLandMode.Disabled,
                     Mode == MoveAfterLandMode.Disabled)
                .Add(Dialog.Clean(DialogIds.MoveAfterLandDashOnly), MoveAfterLandMode.DashOnly,
                     Mode == MoveAfterLandMode.DashOnly)
                .Add(Dialog.Clean(DialogIds.MoveAfterLandDashOrJump), MoveAfterLandMode.DashOrJump,
                     Mode == MoveAfterLandMode.DashOrJump)
                .Add(Dialog.Clean(DialogIds.MoveAfterLandJumpOnly), MoveAfterLandMode.JumpOnly,
                     Mode == MoveAfterLandMode.JumpOnly);
            return item;
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [
                new BetterWidthOnOff(Dialog.Clean(DialogIds.MoveAfterLandIgnoreUltras), IgnoreUltras).Change(
                    (bool val) => IgnoreUltras = val),
                new BetterWidthSlider(
                    label  : Dialog.Clean(DialogIds.MoveAfterLandFrames),
                    values : (int val) => val.ToString(),
                    min    : 1,
                    max    : MaxShortDurationFrames,
                    value  : Frames
                ).Change((int val) => Frames = val)
            ];
        }
    }
    public MoveAfterLandSettings MoveAfterLand { get; set; } = new();

    // =================================================================================================================
    public class MoveAfterGainControlSettings : LintRuleSettings<bool>{
        public int Frames { get; set; } = 3;

        public MoveAfterGainControlSettings()
            : base(true, DialogIds.MoveAfterGainControl, DialogIds.MoveAfterGainControlHint) {}

        public override bool IsEnabled() => Mode;

        protected override BetterWidthOption<bool> MakeModeMenuItem() {
            return new BetterWidthOnOff(Dialog.Clean(DialogIds.Enabled), Mode);
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [
                new BetterWidthSlider(
                    label  : Dialog.Clean(DialogIds.MoveAfterGainControlFrames),
                    values : (int val) => val.ToString(),
                    min    : 1,
                    max    : MaxShortDurationFrames,
                    value  : Frames
                ).Change((int val) => Frames = val)
            ];
        }
    }
    public MoveAfterGainControlSettings MoveAfterGainControl { get; set; } = new();

    // =================================================================================================================
    public class DashAfterUpEntrySettings : LintRuleSettings<bool>{
        public int Frames { get; set; } = 3;

        public DashAfterUpEntrySettings() : base(true, DialogIds.DashAfterUpEntry, DialogIds.DashAfterUpEntryHint) {}

        public override bool IsEnabled() => Mode;

        protected override BetterWidthOption<bool> MakeModeMenuItem() {
            return new BetterWidthOnOff(Dialog.Clean(DialogIds.Enabled), Mode);
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [
                new BetterWidthSlider(
                    label  : Dialog.Clean(DialogIds.DashAfterUpEntryFrames),
                    values : (int val) => val.ToString(),
                    min    : 1,
                    max    : MaxShortDurationFrames,
                    value  : Frames
                ).Change((int val) => Frames = val)
            ];
        }
    }
    public DashAfterUpEntrySettings DashAfterUpEntry { get; set; } = new();

    // =================================================================================================================
    public class ReleaseWBeforeDashSettings : LintRuleSettings<bool>{
        public int Frames { get; set; } = 4;

        public ReleaseWBeforeDashSettings()
            : base(true, DialogIds.ReleaseWBeforeDash, DialogIds.ReleaseWBeforeDashHint) {}

        public override bool IsEnabled() => Mode;

        protected override BetterWidthOption<bool> MakeModeMenuItem() {
            return new BetterWidthOnOff(Dialog.Clean(DialogIds.Enabled), Mode);
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [
                new BetterWidthSlider(
                    label  : Dialog.Clean(DialogIds.ReleaseWBeforeDashFrames),
                    values : (int val) => val.ToString(),
                    min    : 1,
                    max    : MaxShortDurationFrames,
                    value  : Frames
                ).Change((int val) => Frames = val)
            ];
        }
    }
    public ReleaseWBeforeDashSettings ReleaseWBeforeDash { get; set; } = new();

    // =================================================================================================================
    public class FastfallGlitchBeforeDashSettings : LintRuleSettings<bool>{
        public int Frames { get; set; } = 4;

        public FastfallGlitchBeforeDashSettings()
            : base(true, DialogIds.FastfallGlitchBeforeDash, DialogIds.FastfallGlitchBeforeDashHint) {}

        public override bool IsEnabled() => Mode;

        protected override BetterWidthOption<bool> MakeModeMenuItem() {
            return new BetterWidthOnOff(Dialog.Clean(DialogIds.Enabled), Mode);
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [
                new BetterWidthSlider(
                    label  : Dialog.Clean(DialogIds.FastfallGlitchBeforeDashFrames),
                    values : (int val) => val.ToString(),
                    min    : 1,
                    max    : MaxShortDurationFrames,
                    value  : Frames
                ).Change((int val) => Frames = val)
            ];
        }
    }
    public FastfallGlitchBeforeDashSettings FastfallGlitchBeforeDash { get; set; } = new();

    // =================================================================================================================
    public class TurnBeforeWallkickSettings : LintRuleSettings<bool>{
        public int Frames { get; set; } = 4;

        public TurnBeforeWallkickSettings()
            : base(true, DialogIds.TurnBeforeWallkick, DialogIds.TurnBeforeWallkickHint) {}

        public override bool IsEnabled() => Mode;

        protected override BetterWidthOption<bool> MakeModeMenuItem() {
            return new BetterWidthOnOff(Dialog.Clean(DialogIds.Enabled), Mode);
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [
                new BetterWidthSlider(
                    label  : Dialog.Clean(DialogIds.TurnBeforeWallkickFrames),
                    values : (int val) => val.ToString(),
                    min    : 1,
                    max    : MaxShortDurationFrames,
                    value  : Frames
                ).Change((int val) => Frames = val)
            ];
        }
    }
    public TurnBeforeWallkickSettings TurnBeforeWallkick { get; set; } = new();

    // =================================================================================================================
    public class ShortWallboostSettings : LintRuleSettings<bool>{
        public int Frames { get; set; } = 3;

        public ShortWallboostSettings() : base(true, DialogIds.ShortWallboost, DialogIds.ShortWallboostHint) {}

        public override bool IsEnabled() => Mode;

        protected override BetterWidthOption<bool> MakeModeMenuItem() {
            return new BetterWidthOnOff(Dialog.Clean(DialogIds.Enabled), Mode);
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [
                new BetterWidthSlider(
                    label  : Dialog.Clean(DialogIds.ShortWallboostFrames),
                    values : (int val) => val.ToString(),
                    min    : 1,
                    max    : MaxShortWallboostFrames,
                    value  : Frames
                ).Change((int val) => Frames = val)
            ];
        }
    }
    public ShortWallboostSettings ShortWallboost { get; set; } = new();

    // =================================================================================================================
    public class BufferedUltraSettings : LintRuleSettings<BufferedUltraMode> {
        public BufferedUltraSettings()
            : base(BufferedUltraMode.OnlyWhenMattered, DialogIds.BufferedUltra, DialogIds.BufferedUltraHint) {}

        public override bool IsEnabled() => Mode != BufferedUltraMode.Disabled;

        protected override BetterWidthOption<BufferedUltraMode> MakeModeMenuItem() {
            BetterWidthOption<BufferedUltraMode> item = new(Dialog.Clean(DialogIds.Mode));
            item.Add(Dialog.Clean(DialogIds.Off), BufferedUltraMode.Disabled,
                     Mode == BufferedUltraMode.Disabled)
                .Add(Dialog.Clean(DialogIds.BufferedUltraOnlyWhenMattered), BufferedUltraMode.OnlyWhenMattered,
                     Mode == BufferedUltraMode.OnlyWhenMattered)
                .Add(Dialog.Clean(DialogIds.BufferedUltraAlways), BufferedUltraMode.Always,
                     Mode == BufferedUltraMode.Always);
            return item;
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [];
        }
    }
    public BufferedUltraSettings BufferedUltra { get; set; } = new();

    // =================================================================================================================
    public void CreateMenu(TextMenu menu, bool inGame) {
        BetterWidthOnOff mainEnable       = new(Dialog.Clean(DialogIds.Enabled), Enabled);
        RecursiveNakedSubMenu mainSubMenu = new(initiallyExpanded: Enabled, compactRightWidth: mainEnable.RightWidth());
        mainEnable.Change((bool val) => {
            Enabled = val;
            mainSubMenu.Expanded = val;
        });

        mainSubMenu.AddItem(JumpReleaseJump.MakeSubMenu(inGame, menu, mainEnable.RightWidth(), MemorialTextEnabled))
                   .AddItem(JumpReleaseDash.MakeSubMenu(inGame, menu, mainEnable.RightWidth(), MemorialTextEnabled))
                   .AddItem(JumpReleaseExit.MakeSubMenu(inGame, menu, mainEnable.RightWidth(), MemorialTextEnabled))
                   .AddItem(MoveAfterLand.MakeSubMenu(inGame, menu, mainEnable.RightWidth(), MemorialTextEnabled))
                   .AddItem(MoveAfterGainControl.MakeSubMenu(inGame, menu, mainEnable.RightWidth(), MemorialTextEnabled))
                   .AddItem(DashAfterUpEntry.MakeSubMenu(inGame, menu, mainEnable.RightWidth(), MemorialTextEnabled))
                   .AddItem(ReleaseWBeforeDash.MakeSubMenu(inGame, menu, mainEnable.RightWidth(), MemorialTextEnabled))
                   .AddItem(FastfallGlitchBeforeDash.MakeSubMenu(inGame, menu, mainEnable.RightWidth(), MemorialTextEnabled))
                   .AddItem(TurnBeforeWallkick.MakeSubMenu(inGame, menu, mainEnable.RightWidth(), MemorialTextEnabled))
                   .AddItem(ShortWallboost.MakeSubMenu(inGame, menu, mainEnable.RightWidth(), MemorialTextEnabled))
                   .AddItem(BufferedUltra.MakeSubMenu(inGame, menu, mainEnable.RightWidth(), MemorialTextEnabled));

        menu.Add(mainEnable);
        menu.Add(mainSubMenu);
    }
}
