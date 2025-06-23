using System.Collections.Generic;
using Celeste.Mod.TextMenuLib;

namespace Celeste.Mod.MovementLinter;

[SettingName(DialogIds.MovementLinter)]
public class MovementLinterModuleSettings : EverestModuleSettings {
    // =================================================================================================================
    public enum LintResponse {
        Tooltip,
        Kill,
        SFX,
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

    // =================================================================================================================
    public bool Enabled { get; set; } = true;

    // =================================================================================================================
    /// <summary>
    ///     Base class for all the settings associated with a single heuristic rule / check
    /// </summary>
    /// <typeparam name="ModeT">
    ///     Type of the "mode" setting for this rule
    ///     (bool for a simple on/off toggle, or some enum for a richer set of mode options)
    /// </typeparam>
    public abstract class LintRuleSettings<ModeT> {
        private readonly string titleId;
        private readonly string hintId;

        public ModeT Mode { get; set; }
        public LintResponse Response { get; set; } = LintResponse.Tooltip;
        public SFXOption SFX { get; set; }         = SFXOption.Caw;

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
        public RecursiveSubMenu MakeSubMenu(bool inGame, TextMenu topMenu, float compactRightWidth) {
            BetterWidthOption<ModeT> modeItem = MakeModeMenuItem();
            modeItem.Change((ModeT val) => Mode = val);
            OptionPreview<ModeT> preview = new(modeItem);

            BetterWidthOption<SFXOption> SFXSlider = new(Dialog.Clean(DialogIds.LintResponseSFX));
            SFXSlider.Add(Dialog.Clean(DialogIds.SFXCaw),         SFXOption.Caw,         SFX == SFXOption.Caw)
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

            List<TextMenu.Item> items = [
                modeItem,
                new RecursiveOptionSubMenu(
                    label: Dialog.Clean(DialogIds.LintResponse),
                    initialMenuSelection: (int) Response,
                    compactRightWidth: compactRightWidth,
                    menus: [
                        new(Dialog.Clean(DialogIds.LintResponseTooltip), []),
                        new(Dialog.Clean(DialogIds.LintResponseKill), []),
                        new(Dialog.Clean(DialogIds.LintResponseSFX), [SFXSlider]),
                    ]
                ).Change((int val) => Response = (LintResponse) val)
            ];
            items.AddRange(MakeUniqueMenuItems(inGame));
            items.Add(new TextMenuExt.EaseInSubHeaderExt(Dialog.Clean(hintId), true, topMenu){ HeightExtra = 0f });
            return new RecursiveSubMenu(label: Dialog.Clean(titleId), compactRightWidth: compactRightWidth,
                                        items: items, preview: preview);
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

        mainSubMenu.AddItem(JumpReleaseJump.MakeSubMenu(inGame, menu, mainEnable.RightWidth()))
                   .AddItem(JumpReleaseDash.MakeSubMenu(inGame, menu, mainEnable.RightWidth()))
                   .AddItem(JumpReleaseExit.MakeSubMenu(inGame, menu, mainEnable.RightWidth()))
                   .AddItem(MoveAfterLand.MakeSubMenu(inGame, menu, mainEnable.RightWidth()))
                   .AddItem(MoveAfterGainControl.MakeSubMenu(inGame, menu, mainEnable.RightWidth()))
                   .AddItem(DashAfterUpEntry.MakeSubMenu(inGame, menu, mainEnable.RightWidth()))
                   .AddItem(ReleaseWBeforeDash.MakeSubMenu(inGame, menu, mainEnable.RightWidth()))
                   .AddItem(FastfallGlitchBeforeDash.MakeSubMenu(inGame, menu, mainEnable.RightWidth()))
                   .AddItem(TurnBeforeWallkick.MakeSubMenu(inGame, menu, mainEnable.RightWidth()))
                   .AddItem(ShortWallboost.MakeSubMenu(inGame, menu, mainEnable.RightWidth()))
                   .AddItem(BufferedUltra.MakeSubMenu(inGame, menu, mainEnable.RightWidth()));

        menu.Add(mainEnable);
        menu.Add(mainSubMenu);
    }
}
