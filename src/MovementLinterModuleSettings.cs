using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Celeste.Mod.MenuTools;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using YamlDotNet.Serialization;

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
    public const int MaxFastBubbleFrames     = 16;
    public const int MaxShortWallboostFrames = 11;
    public const int MemorialTextThreshold   = 3;
    private static Regex ParseRoomNameRegex  = new("^(?<number>-?[0-9]+)?(?<rest>.*)$", RegexOptions.Compiled);

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
        public bool OverrideActive { get; set; }          = false;
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
        public RecursiveSubMenuBase MakeSubMenu(bool inGame, TextMenu topMenu, float compactRightWidth,
                                                bool memorialTextEnabled, bool isOverrideMenu) {
            // Hint, mode
            TextMenuExt.EaseInSubHeaderExt ruleHint = new(Dialog.Clean(hintId), true, topMenu){ HeightExtra = 0f };
            BetterWidthOption<ModeT> modeItem = MakeModeMenuItem();
            modeItem.Change((ModeT val) => Mode = val);

            // Make the submenu now since we need to manipulate it from the response items
            RecursiveSubMenuBase ruleMenu;
            if (isOverrideMenu) {
                // Make an override active toggle if we're an override menu
                RecursiveOptionSubMenu ruleOptionMenu = new (label: Dialog.Clean(titleId),
                                                             initialMenuSelection: OverrideActive ? 1 : 0,
                                                             compactRightWidth: compactRightWidth);
                ruleOptionMenu.AddMenu(Dialog.Clean(DialogIds.OverrideInactive), [])
                              .AddMenu(Dialog.Clean(DialogIds.OverrideActive), [])
                              .Change((int val) => OverrideActive = (val != 0));
                ruleMenu = ruleOptionMenu;
            } else {
                // Make a normal submenu and preview the mode if we're a base rule menu
                OptionPreview<ModeT> modePreview = new(modeItem);
                ruleMenu = new RecursiveSubMenu(label: Dialog.Clean(titleId), compactRightWidth: compactRightWidth,
                                                preview: modePreview);
            }

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
                                            ruleMenu.MoveSelection(-1, false, true, out _);
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
                                           RecursiveSubMenuBase ruleMenu, TextMenu.Button addResponseButton,
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

    // =================================================================================================================
    public class FastBubbleSettings : LintRuleSettings<bool> {
        public int Frames { get; set; } = 5;

        public FastBubbleSettings() : base(true, DialogIds.FastBubble, DialogIds.FastBubbleHint) {}

        public override bool IsEnabled() => Mode;

        protected override BetterWidthOption<bool> MakeModeMenuItem() {
            return new BetterWidthOnOff(Dialog.Clean(DialogIds.Enabled), Mode);
        }

        protected override List<TextMenu.Item> MakeUniqueMenuItems(bool inGame) {
            return [
                new BetterWidthSlider(
                    label  : Dialog.Clean(DialogIds.FastBubbleFrames),
                    values : (int val) => val.ToString(),
                    min    : 1,
                    max    : MaxFastBubbleFrames,
                    value  : Frames
                ).Change((int val) => Frames = val)
            ];
        }
    }

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

    // =================================================================================================================
    public class LintRules {
        public List<RecursiveSubMenuBase> MakeSubMenus(bool inGame, TextMenu topMenu, float rightWidth,
                                                       bool memorialTextEnabled, bool overrides) {
            return [
                JumpReleaseJump.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
                JumpReleaseDash.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
                JumpReleaseExit.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
                MoveAfterLand.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
                MoveAfterGainControl.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
                DashAfterUpEntry.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
                FastBubble.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
                ReleaseWBeforeDash.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
                FastfallGlitchBeforeDash.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
                TurnBeforeWallkick.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
                ShortWallboost.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
                BufferedUltra.MakeSubMenu(inGame, topMenu, rightWidth, memorialTextEnabled, overrides),
            ];
        }

        public JumpReleaseJumpSettings JumpReleaseJump { get; set; }                   = new();
        public JumpReleaseDashSettings JumpReleaseDash { get; set; }                   = new();
        public JumpReleaseExitSettings JumpReleaseExit { get; set; }                   = new();
        public MoveAfterLandSettings MoveAfterLand { get; set; }                       = new();
        public MoveAfterGainControlSettings MoveAfterGainControl { get; set; }         = new();
        public DashAfterUpEntrySettings DashAfterUpEntry { get; set; }                 = new();
        public FastBubbleSettings FastBubble { get; set; }                             = new();
        public ReleaseWBeforeDashSettings ReleaseWBeforeDash { get; set; }             = new();
        public FastfallGlitchBeforeDashSettings FastfallGlitchBeforeDash { get; set; } = new();
        public TurnBeforeWallkickSettings TurnBeforeWallkick { get; set; }             = new();
        public ShortWallboostSettings ShortWallboost { get; set; }                     = new();
        public BufferedUltraSettings BufferedUltra { get; set; }                       = new();
    };
    public LintRules BaseRules { get; set; } = new();

    // =================================================================================================================
    // level set (string) => chapter (string, AreaMode) => room (string) => rules
    public record struct ChapterTag {
        public ChapterTag() {}
        public string Name   = null;
        public AreaMode Side = AreaMode.Normal;
    }
    public Dictionary<string, Dictionary<ChapterTag, Dictionary<string, LintRules>>> Overrides { get; set; } = [];
    private LintRules activeOverride = null;

    // =================================================================================================================
    // Patches and hooks to apply the configured overrides
    public static void LoadOverrides(Level level) {
        MovementLinterModule.Settings.activeOverride                            = null;
        Dictionary<ChapterTag, Dictionary<string, LintRules>> levelSetOverrides = null;
        Dictionary<string, LintRules> chapterOverrides                          = null;
        MovementLinterModule.Settings.Overrides.TryGetValue(level.Session.Area.LevelSet, out levelSetOverrides);
        levelSetOverrides?.TryGetValue(new ChapterTag(){Name = AreaData.Areas[level.Session.Area.ID].Name,
                                                        Side = level.Session.Area.Mode},
                                       out chapterOverrides);
        chapterOverrides?.TryGetValue(level.Session.LevelData.Name, out MovementLinterModule.Settings.activeOverride);
    }

    public static void PatchLevelUpdate(ILContext il) {
        ILCursor cursor = new(il);

        // Reload overrides when we notice we've unpaused
        cursor.GotoNext(MoveType.After,
                        instr => instr.MatchCallvirt(typeof(Level), "EndPauseEffects"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Level>>(LoadOverrides);
    }

    // =================================================================================================================
    // Multiplex base rules and overrides
    [YamlIgnore]
    public JumpReleaseJumpSettings JumpReleaseJump { get {
        if (activeOverride != null && activeOverride.JumpReleaseJump.OverrideActive) {
            return activeOverride.JumpReleaseJump;
        } else {
            return BaseRules.JumpReleaseJump;
        }
    }}

    [YamlIgnore]
    public JumpReleaseDashSettings JumpReleaseDash { get {
        if (activeOverride != null && activeOverride.JumpReleaseDash.OverrideActive) {
            return activeOverride.JumpReleaseDash;
        } else {
            return BaseRules.JumpReleaseDash;
        }
    }}

    [YamlIgnore]
    public JumpReleaseExitSettings JumpReleaseExit { get {
        if (activeOverride != null && activeOverride.JumpReleaseExit.OverrideActive) {
            return activeOverride.JumpReleaseExit;
        } else {
            return BaseRules.JumpReleaseExit;
        }
    }}

    [YamlIgnore]
    public MoveAfterLandSettings MoveAfterLand { get {
        if (activeOverride != null && activeOverride.MoveAfterLand.OverrideActive) {
            return activeOverride.MoveAfterLand;
        } else {
            return BaseRules.MoveAfterLand;
        }
    }}

    [YamlIgnore]
    public MoveAfterGainControlSettings MoveAfterGainControl { get {
        if (activeOverride != null && activeOverride.MoveAfterGainControl.OverrideActive) {
            return activeOverride.MoveAfterGainControl;
        } else {
            return BaseRules.MoveAfterGainControl;
        }
    }}

    [YamlIgnore]
    public DashAfterUpEntrySettings DashAfterUpEntry { get {
        if (activeOverride != null && activeOverride.DashAfterUpEntry.OverrideActive) {
            return activeOverride.DashAfterUpEntry;
        } else {
            return BaseRules.DashAfterUpEntry;
        }
    }}

    [YamlIgnore]
    public FastBubbleSettings FastBubble { get {
        if (activeOverride != null && activeOverride.FastBubble.OverrideActive) {
            return activeOverride.FastBubble;
        } else {
            return BaseRules.FastBubble;
        }
    }}

    [YamlIgnore]
    public ReleaseWBeforeDashSettings ReleaseWBeforeDash { get {
        if (activeOverride != null && activeOverride.ReleaseWBeforeDash.OverrideActive) {
            return activeOverride.ReleaseWBeforeDash;
        } else {
            return BaseRules.ReleaseWBeforeDash;
        }
    }}

    [YamlIgnore]
    public FastfallGlitchBeforeDashSettings FastfallGlitchBeforeDash { get {
        if (activeOverride != null && activeOverride.FastfallGlitchBeforeDash.OverrideActive) {
            return activeOverride.FastfallGlitchBeforeDash;
        } else {
            return BaseRules.FastfallGlitchBeforeDash;
        }
    }}

    [YamlIgnore]
    public TurnBeforeWallkickSettings TurnBeforeWallkick { get {
        if (activeOverride != null && activeOverride.TurnBeforeWallkick.OverrideActive) {
            return activeOverride.TurnBeforeWallkick;
        } else {
            return BaseRules.TurnBeforeWallkick;
        }
    }}

    [YamlIgnore]
    public ShortWallboostSettings ShortWallboost { get {
        if (activeOverride != null && activeOverride.ShortWallboost.OverrideActive) {
            return activeOverride.ShortWallboost;
        } else {
            return BaseRules.ShortWallboost;
        }
    }}

    [YamlIgnore]
    public BufferedUltraSettings BufferedUltra { get {
        if (activeOverride != null && activeOverride.BufferedUltra.OverrideActive) {
            return activeOverride.BufferedUltra;
        } else {
            return BaseRules.BufferedUltra;
        }
    }}

    // =================================================================================================================
    public void CreateMenu(TextMenu menu, bool inGame) {
        BetterWidthOnOff mainEnable       = new(Dialog.Clean(DialogIds.Enabled), Enabled);
        RecursiveNakedSubMenu mainSubMenu = new(initiallyExpanded: Enabled, compactRightWidth: mainEnable.RightWidth());
        mainEnable.Change((bool val) => {
            Enabled = val;
            mainSubMenu.Expanded = val;
        });

        foreach (RecursiveSubMenuBase ruleMenu in BaseRules.MakeSubMenus(inGame, menu, mainEnable.RightWidth(),
                                                                         MemorialTextEnabled, false)) {
            mainSubMenu.AddItem(ruleMenu);
        }

        TextMenuPage overridesPage = MakeOverridesPage(menu, mainSubMenu, inGame);
        mainSubMenu.AddItem(new TextMenu.Button(Dialog.Clean(DialogIds.OverridesButton)).Pressed(overridesPage.Enter));

        menu.Add(mainEnable);
        menu.Add(mainSubMenu);
    }

    // =================================================================================================================
    // Helper for organizing metadata while discovering chapters
    private class LevelSetMetadata {
        public class ChapterMetadata {
            public AreaData Data;
            public List<AreaMode> Sides;
            public int SideSelection = 0;
        }
        public string Name;
        public List<ChapterMetadata> Chapters;
        public int ChapterSelection = 0;
    }

    // =================================================================================================================
    // Helper types for finding submenus by the canonical level set and chapter names
    // (rather than the Dialog.Clean'd ones)
    private class LevelSetTaggedSubmenu : RecursiveSubMenu {
        public readonly string LevelSetName;

        public LevelSetTaggedSubmenu(string levelSetName,
                                     string label              = "",
                                     bool autoEnterExit        = false,
                                     bool compactMode          = true,
                                     float compactRightWidth   = float.NaN,
                                     float itemSpacing         = 4f,
                                     float itemIndent          = 20f,
                                     List<TextMenu.Item> items = null,
                                     ISubMenuPreview preview   = null)
                : base (label:             label,
                        autoEnterExit:     autoEnterExit,
                        compactMode:       compactMode,
                        compactRightWidth: compactRightWidth,
                        itemSpacing:       itemSpacing,
                        itemIndent:        itemIndent,
                        items:             items,
                        preview:           preview) {
            this.LevelSetName = levelSetName;
        }
    }

    private class ChapterTaggedSubmenu : RecursiveSubMenu {
        public readonly ChapterTag ChapterTag;

        public ChapterTaggedSubmenu(ChapterTag chapterTag,
                                    string label              = "",
                                    bool autoEnterExit        = false,
                                    bool compactMode          = true,
                                    float compactRightWidth   = float.NaN,
                                    float itemSpacing         = 4f,
                                    float itemIndent          = 20f,
                                    List<TextMenu.Item> items = null,
                                    ISubMenuPreview preview   = null)
                : base (label:             label,
                        autoEnterExit:     autoEnterExit,
                        compactMode:       compactMode,
                        compactRightWidth: compactRightWidth,
                        itemSpacing:       itemSpacing,
                        itemIndent:        itemIndent,
                        items:             items,
                        preview:           preview) {
            this.ChapterTag = chapterTag;
        }
    }

    // =================================================================================================================
    // Menu page to handle deleting overrides / groups of overrides
    private class ConfirmOverridesDeletePage : TextMenuPage {
        private MovementLinterModuleSettings settings;
        private readonly bool inGame;
        private readonly Item lastNonOverrideItem;

        private SubHeader subHeader = new(""){ TopPadding = false };
        private string levelSet;
        private ChapterTag chapterTag;
        private string room;

        public ConfirmOverridesDeletePage(TextMenu parent, MovementLinterModuleSettings settings, bool inGame,
                                          Item lastNonOverrideItem)
                : base(parent, null) {
            this.settings            = settings;
            this.inGame              = inGame;
            this.lastNonOverrideItem = lastNonOverrideItem;
            Add(new Header(Dialog.Clean(DialogIds.DeleteOverridesHeader)));
            Add(subHeader);
            Add(new Button(Dialog.Clean(DialogIds.Confirm)).Pressed(AcceptDelete));
            Add(new Button(Dialog.Clean(DialogIds.Cancel)).Pressed(Return));
        }

        public void Enter(RecursiveSubMenuBase subMenuParent, string levelSet, ChapterTag chapterTag = default,
                          string room = null) {
            this.subMenuParent = subMenuParent;
            this.levelSet      = levelSet;
            this.chapterTag    = chapterTag;
            this.room          = room;
            // Build prompt
            if (chapterTag.Name == null) {
                subHeader.Title = string.Format(Dialog.Get(DialogIds.DeleteLevelSetPrompt),
                                                Dialog.CleanLevelSet(levelSet));
            } else if (room == null) {
                if (TotalSidesInChapter(chapterTag.Name) == 1) {
                    subHeader.Title = string.Format(Dialog.Get(DialogIds.DeleteChapterPromptNoSide),
                                                    Dialog.CleanLevelSet(levelSet), Dialog.Clean(chapterTag.Name));
                } else {
                    subHeader.Title = string.Format(Dialog.Get(DialogIds.DeleteChapterPromptWithSide),
                                                    Dialog.CleanLevelSet(levelSet), Dialog.Clean(chapterTag.Name),
                                                    Dialog.Clean(DialogIds.FullIdForSide(chapterTag.Side)));
                }
            } else {
                if (TotalSidesInChapter(chapterTag.Name) == 1) {
                    subHeader.Title = string.Format(Dialog.Get(DialogIds.DeleteRoomPromptNoSide),
                                                    Dialog.CleanLevelSet(levelSet), Dialog.Clean(chapterTag.Name),
                                                    room);
                } else {
                    subHeader.Title = string.Format(Dialog.Get(DialogIds.DeleteRoomPromptWithSide),
                                                    Dialog.CleanLevelSet(levelSet), Dialog.Clean(chapterTag.Name),
                                                    Dialog.Clean(DialogIds.FullIdForSide(chapterTag.Side)), room);
                }
            }
            Selection = 3;  // Select cancel button
            Enter();
        }

        private void AcceptDelete() {
            string nextSelectedLevelSet    = levelSet;
            ChapterTag nextSelectedChapter = chapterTag;
            string nextSelectedRoom        = room;
            if (chapterTag.Name != null) {
                if (room != null) {
                    nextSelectedRoom = NextSelection(settings.Overrides[levelSet][chapterTag].Keys.ToList(),
                                                     room, CompareRooms);
                    settings.Overrides[levelSet][chapterTag].Remove(room);
                    if (settings.Overrides[levelSet][chapterTag].Count == 0) {
                        nextSelectedChapter = NextSelection(settings.Overrides[levelSet].Keys.ToList(),
                                                            chapterTag, CompareChapters);
                        settings.Overrides[levelSet].Remove(chapterTag);
                    }
                } else {
                    nextSelectedChapter = NextSelection(settings.Overrides[levelSet].Keys.ToList(),
                                                        chapterTag, CompareChapters);
                    settings.Overrides[levelSet].Remove(chapterTag);
                }
                if (settings.Overrides[levelSet].Count == 0) {
                    nextSelectedLevelSet = NextSelection(settings.Overrides.Keys.ToList(), levelSet, CompareLevelSets);
                    settings.Overrides.Remove(levelSet);
                }
            } else {
                nextSelectedLevelSet = NextSelection(settings.Overrides.Keys.ToList(), levelSet, CompareLevelSets);
                settings.Overrides.Remove(levelSet);
            }
            Return();
            settings.RefreshOverridesMenus(inGame, parent, lastNonOverrideItem,
                                           nextSelectedLevelSet, nextSelectedChapter, nextSelectedRoom);
        }

        private T NextSelection<T>(List<T> existingList, T removingEntry, Comparison<T> compareFunc) {
            existingList.Sort(compareFunc);
            int removingIdx = existingList.IndexOf(removingEntry);
            if (removingIdx > 0) {
                return existingList[removingIdx - 1];
            } else if (existingList.Count > 1) {
                return existingList[1];
            } else {
                return default;
            }
        }
    }

    // =================================================================================================================
    private TextMenuPage MakeOverridesPage(TextMenu parent, RecursiveSubMenuBase subMenuParent, bool inGame) {
        TextMenuPage page = new(parent, subMenuParent){ InnerContent = TextMenu.InnerContentMode.TwoColumn };
        page.Add(new TextMenu.Header(Dialog.Clean(DialogIds.OverridesHeader)));

        // Making this subheader early since I want to reference it from a callback (it's the last item before all the
        // actual overrides, I look for it to know where things are when removing and re-adding the overrides list)
        TextMenu.SubHeader overridesListSubheader = new(Dialog.Clean(DialogIds.OverridesListSubheader));

        // Clear all button
        TextMenuPage clearAllPage            = new(page, null);
        TextMenu.Button clearAllCancelButton = new(Dialog.Clean(DialogIds.Cancel));
        clearAllCancelButton.Pressed(clearAllPage.Return);
        clearAllPage.Add(new TextMenu.Header(Dialog.Clean(DialogIds.OverridesClearAllHeader)))
                    .Add(new TextMenu.Button(Dialog.Clean(DialogIds.Confirm)).Pressed(delegate {
                        Overrides.Clear();
                        clearAllPage.Return();
                        RefreshOverridesMenus(inGame, page, overridesListSubheader);
                    }))
                    .Add(clearAllCancelButton);
        page.Add(new TextMenu.Button(Dialog.Clean(DialogIds.OverridesClearAllButton)).Pressed(delegate {
            clearAllPage.Selection = clearAllPage.Items.IndexOf(clearAllCancelButton);
            clearAllPage.Enter();
        }));

        // Add override subsection
        page.Add(new TextMenu.SubHeader(Dialog.Clean(DialogIds.AddOverrideSubheader)));
        page.Add(new TextMenu.Button(Dialog.Clean(DialogIds.CurrentRoomButton)){ Disabled = !inGame }.Pressed(delegate {
            Engine.Scene.OnEndOfFrame += delegate {
                Level level = Engine.Scene as Level;
                AddOverride(inGame, page, overridesListSubheader, level.Session.Area.LevelSet,
                            new ChapterTag(){Name = AreaData.Areas[level.Session.Area.ID].Name,
                                             Side = level.Session.Area.Mode},
                            level.Session.Level, false);
            };
        }));
        if (!inGame) {
            page.Add(new ParentAwareEaseInSubHeader(Dialog.Clean(DialogIds.CurrentRoomHint), !inGame, page, null){
                HeightExtra = 0f
            });
        }
        // Build sliders to select a levelset / chapter / side
        // Levelset menu / tracking
        List<LevelSetMetadata> levelSets    = [];
        int levelSetSelection               = 0;
        RecursiveOptionSubMenu levelSetMenu = new(label: Dialog.Clean(DialogIds.LevelSet),
                                                  autoEnterExit: true, itemIndent: 0);
        levelSetMenu.Change((int val) => levelSetSelection = val);
        // Chapter menu which we'll need to reference to add items
        RecursiveOptionSubMenu chapterMenu = null;
        // Go through all the areas (chapters), we'll find the levelsets along the way
        foreach (AreaData area in AreaData.Areas) {
            if (levelSets.Count == 0 || levelSets.Last().Name != area.LevelSet) {
                // We found a new levelset (chapters are sorted by levelset),
                // add it to the list and make an entry for it in the levelset slider
                levelSets.Add(new(){ Name = area.LevelSet, Chapters = [] });
                chapterMenu = new(label: Dialog.Clean(DialogIds.Chapter), autoEnterExit: true, itemIndent: 0);
                chapterMenu.Change((int val) => levelSets[levelSetSelection].ChapterSelection = val);
                levelSetMenu.AddMenu(Dialog.CleanLevelSet(area.LevelSet), [chapterMenu]);
            }
            // Write down this chapter's information in the current levelset's chapter list
            LevelSetMetadata.ChapterMetadata chapter = new(){ Data = area, Sides = [] };
            levelSets.Last().Chapters.Add(chapter);
            // Write down the list of sides for this chapter
            foreach (ModeProperties mode in area.Mode) {
                if (mode != null) {
                    AreaData.ParseName(mode.Path, out int? _, out AreaMode side, out string _);
                    levelSets.Last().Chapters.Last().Sides.Add(side);
                }
            }
            // Prepare a list for the contents of one entry in the chapter slider (representing one chapter).
            // This list will just have a slider for the sides in this chapter.
            List<TextMenu.Item> chapterMenuBody = [];
            int sidesCount = area.Mode.Count((ModeProperties mode) => mode != null);
            if (sidesCount > 1) {
                // Only add the slider if there are multiple sides, otherwise there's no choice to make
                BetterWidthSlider sideSlider = new(
                    label  : Dialog.Clean(DialogIds.Side),
                    values : (int val) => Dialog.Clean(DialogIds.ShortIdForSide(chapter.Sides[val])),
                    min    : 0,
                    max    : sidesCount - 1
                );
                sideSlider.Change((int val) => {
                    int chapterSelection = levelSets[levelSetSelection].ChapterSelection;
                    levelSets[levelSetSelection].Chapters[chapterSelection].SideSelection = val;
                });
                chapterMenuBody.Add(sideSlider);
            }
            chapterMenu.AddMenu(Dialog.Clean(area.Name), chapterMenuBody);
        }
        page.Add(levelSetMenu);
        // Button to choose a room from the selected chapter on a debug-map-like interface
        page.Add(new TextMenu.Button(Dialog.Clean(DialogIds.ChooseRoom)).Pressed(delegate {
            string levelSetName                          = levelSets[levelSetSelection].Name;
            int chapterSelection                         = levelSets[levelSetSelection].ChapterSelection;
            LevelSetMetadata.ChapterMetadata chapterMeta = levelSets[levelSetSelection].Chapters[chapterSelection];
            int sideSelection                            = chapterMeta.SideSelection;
            ChapterTag chapterTag                        = new(){Name = chapterMeta.Data.Name,
                                                                 Side = chapterMeta.Sides[sideSelection]};
            RoomChooserMap.EnterMap(chapterMeta.Data.ToKey(chapterMeta.Sides[sideSelection]), (string roomName) => {
                AddOverride(inGame, page, overridesListSubheader, levelSetName, chapterTag, roomName, true);
            });
        }));

        page.Add(overridesListSubheader);
        AddOverridesMenus(inGame, page, overridesListSubheader);
        return page;
    }

    // =================================================================================================================
    private void AddOverride(bool inGame, TextMenu page, TextMenu.Item lastNonOverrideItem, string levelSet,
                             ChapterTag chapterTag, string room, bool snapScroll) {
        if (!Overrides.ContainsKey(levelSet)) {
            Overrides[levelSet] = [];
        }
        if (!Overrides[levelSet].ContainsKey(chapterTag)) {
            Overrides[levelSet][chapterTag] = [];
        }
        if (!Overrides[levelSet][chapterTag].ContainsKey(room)) {
            Overrides[levelSet][chapterTag][room] = new();
        }
        RefreshOverridesMenus(inGame, page, lastNonOverrideItem, levelSet, chapterTag, room, true, snapScroll);
    }

    // =================================================================================================================
    private void AddOverridesMenus(bool inGame, TextMenu page, TextMenu.Item lastNonOverrideItem) {
        // I heard you like submenus so I
        ConfirmOverridesDeletePage confirmDeletePage = new(page, this, inGame, lastNonOverrideItem);
        List<string> levelSets = Overrides.Keys.ToList();
        levelSets.Sort(CompareLevelSets);
        foreach (string levelSet in levelSets) {
            // Make level set submenu if we have overrides in multiple level sets
            LevelSetTaggedSubmenu levelSetSubmenu =
                (levelSets.Count > 1) ? new(levelSet, Dialog.CleanLevelSet(levelSet)) : null;
            if (levelSetSubmenu != null) {
                page.Add(levelSetSubmenu);
            }
            // Add chapters to level set (or top level if no level set submenu)
            List<ChapterTag> chapters = Overrides[levelSet].Keys.ToList();
            chapters.Sort(CompareChapters);
            foreach (ChapterTag chapterTag in chapters) {
                // Make chapter + side submenu
                ChapterTaggedSubmenu chapterSubmenu = new(
                    chapterTag,
                    TotalSidesInChapter(chapterTag.Name) == 1
                        ? Dialog.Clean(chapterTag.Name)
                        : string.Format(Dialog.Get(DialogIds.ChapterWithSideFormat), Dialog.Clean(chapterTag.Name),
                                        Dialog.Clean(DialogIds.FullIdForSide(chapterTag.Side))));
                if (levelSetSubmenu != null) {
                    levelSetSubmenu.AddItem(chapterSubmenu);
                } else {
                    page.Add(chapterSubmenu);
                }
                // Add rooms to chapter
                List<string> rooms = Overrides[levelSet][chapterTag].Keys.ToList();
                rooms.Sort(CompareRooms);
                foreach (string room in rooms) {
                    // Make room submenu
                    RecursiveSubMenu roomSubmenu = new(room);
                    chapterSubmenu.AddItem(roomSubmenu);
                    // Add overrides to room
                    foreach (RecursiveSubMenuBase ruleMenu in Overrides[levelSet][chapterTag][room].MakeSubMenus(
                                inGame, page, float.NaN, MemorialTextEnabled, true)) {
                        roomSubmenu.AddItem(ruleMenu);
                    }
                    // Delete room overrides by journal or delete button
                    roomSubmenu.AltPressed(delegate {
                        confirmDeletePage.Enter(chapterSubmenu, levelSet, chapterTag, room);
                        Audio.Play(SFX.ui_main_button_select);
                    });
                    ParentAwareEaseInSubHeader deleteRoomHint = new(
                        levelSetSubmenu == null ? Dialog.Clean(DialogIds.DeleteOverridesHintNoLevelSet)
                                                : Dialog.Clean(DialogIds.DeleteOverridesHintWithLevelSet),
                        false, page, roomSubmenu){ HeightExtra = 0f };
                    TextMenu.Button deleteRoomButton = new(Dialog.Clean(DialogIds.DeleteOverridesButton));
                    deleteRoomButton.Pressed(delegate {
                        confirmDeletePage.Enter(roomSubmenu, levelSet, chapterTag, room);
                    }).Enter(delegate { deleteRoomHint.FadeVisible = true; })
                        .Leave(delegate { deleteRoomHint.FadeVisible = false; });
                    roomSubmenu.AddItem(deleteRoomButton)
                                .AddItem(deleteRoomHint);
                }
                // Delete chapter overrides by journal or delete button
                chapterSubmenu.AltPressed(delegate {
                    confirmDeletePage.Enter(levelSetSubmenu, levelSet, chapterTag);
                    Audio.Play(SFX.ui_main_button_select);
                });
                ParentAwareEaseInSubHeader deleteChapterHint = new(
                    levelSetSubmenu == null ? Dialog.Clean(DialogIds.DeleteOverridesHintNoLevelSet)
                                            : Dialog.Clean(DialogIds.DeleteOverridesHintWithLevelSet),
                    false, page, chapterSubmenu){ HeightExtra = 0f };
                TextMenu.Button deleteChapterButton = new(Dialog.Clean(DialogIds.DeleteOverridesButton));
                deleteChapterButton.Pressed(delegate {
                    confirmDeletePage.Enter(chapterSubmenu, levelSet, chapterTag);
                }).Enter(delegate { deleteChapterHint.FadeVisible = true; })
                    .Leave(delegate { deleteChapterHint.FadeVisible = false; });
                chapterSubmenu.AddItem(deleteChapterButton)
                            .AddItem(deleteChapterHint);
            }
            // Delete level set overrides by journal or delete button
            if (levelSetSubmenu != null) {
                levelSetSubmenu.AltPressed(delegate {
                    confirmDeletePage.Enter(null, levelSet);
                    Audio.Play(SFX.ui_main_button_select);
                });
                ParentAwareEaseInSubHeader deleteLevelSetHint = new(
                    Dialog.Clean(DialogIds.DeleteOverridesHintWithLevelSet),
                    false, page, levelSetSubmenu){ HeightExtra = 0f };
                TextMenu.Button deleteLevelSetButton = new(Dialog.Clean(DialogIds.DeleteOverridesButton));
                deleteLevelSetButton.Pressed(delegate {
                    confirmDeletePage.Enter(levelSetSubmenu, levelSet);
                }).Enter(delegate { deleteLevelSetHint.FadeVisible = true; })
                  .Leave(delegate { deleteLevelSetHint.FadeVisible = false; });
                levelSetSubmenu.AddItem(deleteLevelSetButton)
                               .AddItem(deleteLevelSetHint);
            }
        }
    }

    // =================================================================================================================
    private void RefreshOverridesMenus(bool inGame, TextMenu page, TextMenu.Item lastNonOverrideItem,
                                       string levelSet = null, ChapterTag chapterTag = default, string room = null,
                                       bool enterRoomSubmenu = false, bool snapScroll = true) {
        float prevPageHeight = page.Height;
        // Remove and re-add submenus
        while (page.Items.Last() != lastNonOverrideItem) {
            page.Remove(page.Items.Last());
        }
        page.Selection = page.LastPossibleSelection;
        AddOverridesMenus(inGame, page, lastNonOverrideItem);

        // Find the target submenu or closest parent
        int topIdx                            = page.IndexOf(lastNonOverrideItem) + 1;
        LevelSetTaggedSubmenu levelSetSubmenu = null;
        ChapterTaggedSubmenu chapterSubmenu   = null;
        RecursiveSubMenu roomSubmenu          = null;
        if (Overrides.Count > 1) {
            // Find level set submenu at top level
            for (; topIdx < page.Items.Count; ++topIdx) {
                if (page.Items[topIdx] is LevelSetTaggedSubmenu menu && menu.LevelSetName == levelSet) {
                    levelSetSubmenu = menu;
                    break;
                }
            }
            if (levelSetSubmenu != null) {
                // Find chapter submenu within level set
                for (int chapterMenuIdx = 0; chapterMenuIdx < levelSetSubmenu.CurrentMenu.Count; ++chapterMenuIdx) {
                    if (levelSetSubmenu.CurrentMenu[chapterMenuIdx] is ChapterTaggedSubmenu menu &&
                            menu.ChapterTag == chapterTag) {
                        chapterSubmenu = menu;
                        break;
                    }
                }
            }
        } else {
            // Find chapter submenu at top level
            for (; topIdx < page.Items.Count; ++topIdx) {
                if (page.Items[topIdx] is ChapterTaggedSubmenu menu && menu.ChapterTag == chapterTag) {
                    chapterSubmenu = menu;
                    break;
                }
            }
        }
        if (chapterSubmenu != null) {
            // Find room submenu within chapter
            for (int roomMenuIdx = 0; roomMenuIdx < chapterSubmenu.CurrentMenu.Count; ++roomMenuIdx) {
                if (chapterSubmenu.CurrentMenu[roomMenuIdx] is RecursiveSubMenu menu && menu.Label == room) {
                    roomSubmenu = menu;
                    break;
                }
            }
        }

        // Force selection to the closest thing we found
        if (roomSubmenu != null) {
            roomSubmenu.MoveSelectionTo(enterRoomSubmenu ? roomSubmenu.CurrentMenu[roomSubmenu.FirstPossibleSelection]
                                                         : null,
                                        true, snapScroll);
        } else if (chapterSubmenu != null) {
            chapterSubmenu.MoveSelectionTo(null, true, snapScroll);
        } else if (levelSetSubmenu != null) {
            levelSetSubmenu.MoveSelectionTo(null, true, snapScroll);
        } else {
            page.Focused    = true;
            page.AutoScroll = true;
            // Update position to avoid visual glitch from stale position
            // (this is handled by MoveSelectionTo in the other cases)
            page.Position.Y = (page.Height > page.ScrollableMinSize) ? page.ScrollTargetY : Engine.Height / 2f;
        }
        if (!snapScroll) {
            // Compensate for the menu height changing to keep the top of the menu in the same position
            page.Position.Y += (page.Height - prevPageHeight) / 2f;
        }
    }

    // =================================================================================================================
    // Sort functions and other helpers
    private static int CompareLevelSets(string x, string y) {
        // Sort Celeste first, otherwise lexicographical
        if (x == "Celeste") {
            return -1;
        } else if (y == "Celeste") {
            return 1;
        } else {
            return string.Compare(x, y);
        }
    }

    private static int CompareChapters(ChapterTag x, ChapterTag y) {
        if (x.Name == y.Name) {
            return (int) x.Side - (int) y.Side;
        } else {
            AreaData xData = AreaData.Areas.Find((AreaData area) => area.Name == x.Name);
            AreaData yData = AreaData.Areas.Find((AreaData area) => area.Name == y.Name);
            if (xData != null && yData != null) {
                return xData.ToKey().RelativeIndex - yData.ToKey().RelativeIndex;
            } else {
                // We didn't find one of the chapters (probably because they're both in a mod that's not loaded),
                // just sort them lexicographically by name
                return string.Compare(x.Name, y.Name);
            }
        }
    }

    private static int CompareRooms(string x, string y) {
        Match xMatch = ParseRoomNameRegex.Match(x);
        Match yMatch = ParseRoomNameRegex.Match(y);
        if (xMatch.Groups["number"].Success && yMatch.Groups["number"].Success) {
            // If both rooms start with numbers, sort by number, then the rest of the name
            int numberCompare = int.Parse(xMatch.Groups["number"].Value) -
                int.Parse(yMatch.Groups["number"].Value);
            return numberCompare != 0 ? numberCompare : string.Compare(xMatch.Groups["rest"].Value,
                                                                       yMatch.Groups["rest"].Value);
        } else if (xMatch.Groups["number"].Success) {
            // If only one room starts with a number, sort that first
            return -1;
        } else if (yMatch.Groups["number"].Success) {
            return 1;
        } else {
            // No numbers, just sort lexicographically
            return string.Compare(x, y);
        }
    }

    private static int TotalSidesInChapter(string chapterName) {
        AreaData chapterData = AreaData.Areas.Find((AreaData area) => area.Name == chapterName);
        if (chapterData == null) {
            // We didn't find the chapter we were looking for (because it's in a mod that's not loaded,
            // unless the settings file is just invalid). Just default to acting as if it has all the sides.
            return 3;
        } else {
            return chapterData.Mode.Count((ModeProperties mode) => mode != null);
        }
    }
}
