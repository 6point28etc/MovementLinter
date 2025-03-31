namespace Celeste.Mod.MovementLinter;

[SettingName(DialogIds.MovementLinter)]
public class MovementLinterModuleSettings : EverestModuleSettings {
    // =================================================================================================================
    public enum LintAction {
        Kill,
    };
    public enum TransitionDirection {
        UpOnly,
        NotDown,
        Any,
    };
    public enum MoveAfterLandMode {
        Disabled,
        DashOnly,
        DashOrJump,
        JumpOnly,
    };
    public enum BufferedUltraMode {
        Disabled,
        OnlyWhenMattered,
        Always
    };
    public const int MaxShortDurationFrames  = 99;
    public const int MaxShortWallboostFrames = 11;

    // =================================================================================================================
    [SettingName(DialogIds.Enabled)]
    public bool Enabled { get; set; } = true;

    // =================================================================================================================
    [SettingSubMenu]
    public class JumpReleaseJumpSubMenu {
        [SettingName(DialogIds.Enabled)]
        public bool Enabled { get; set; } = true;

        public LintAction Action { get; set; } = LintAction.Kill;
        public void CreateActionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ActionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.LintAction),
                values: LintActionToString,
                min: (int) LintAction.Kill,
                max: (int) LintAction.Kill,
                value: (int) Action
            );
            ActionEntry.Change(newValue => Action = (LintAction) newValue);
            subMenu.Add(ActionEntry);
        }

        [SettingRange(1, MaxShortDurationFrames)]
        [SettingName(DialogIds.JumpReleaseJumpFrames)]
        public int Frames { get; set; } = 2;
    }
    [SettingName(DialogIds.JumpReleaseJump)]
    [SettingSubText(DialogIds.JumpReleaseJumpHint)]
    public JumpReleaseJumpSubMenu JumpReleaseJump { get; set; } = new();

    // =================================================================================================================
    [SettingSubMenu]
    public class JumpReleaseDashSubMenu {
        [SettingName(DialogIds.Enabled)]
        public bool Enabled { get; set; } = true;

        public LintAction Action { get; set; } = LintAction.Kill;
        public void CreateActionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ActionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.LintAction),
                values: LintActionToString,
                min: (int) LintAction.Kill,
                max: (int) LintAction.Kill,
                value: (int) Action
            );
            ActionEntry.Change(newValue => Action = (LintAction) newValue);
            subMenu.Add(ActionEntry);
        }

        [SettingRange(1, MaxShortDurationFrames)]
        [SettingName(DialogIds.JumpReleaseDashFrames)]
        public int Frames { get; set; } = 2;
    }
    [SettingName(DialogIds.JumpReleaseDash)]
    [SettingSubText(DialogIds.JumpReleaseDashHint)]
    public JumpReleaseDashSubMenu JumpReleaseDash { get; set; } = new();

    // =================================================================================================================
    [SettingSubMenu]
    public class JumpReleaseExitSubMenu {
        [SettingName(DialogIds.Enabled)]
        public bool Enabled { get; set; } = true;

        public LintAction Action { get; set; } = LintAction.Kill;
        public void CreateActionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ActionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.LintAction),
                values: LintActionToString,
                min: (int) LintAction.Kill,
                max: (int) LintAction.Kill,
                value: (int) Action
            );
            ActionEntry.Change(newValue => Action = (LintAction) newValue);
            subMenu.Add(ActionEntry);
        }

        public TransitionDirection Direction { get; set; } = TransitionDirection.UpOnly;
        public void CreateDirectionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider DirectionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.JumpReleaseExitDirection),
                values: TransitionDirectionToString,
                min: (int) TransitionDirection.UpOnly,
                max: (int) TransitionDirection.Any,
                value: (int) Direction
            );
            DirectionEntry.Change(newValue => Direction = (TransitionDirection) newValue);
            subMenu.Add(DirectionEntry);
        }

        [SettingRange(1, MaxShortDurationFrames)]
        [SettingName(DialogIds.JumpReleaseExitFrames)]
        public int Frames { get; set; } = 2;
    }
    [SettingName(DialogIds.JumpReleaseExit)]
    [SettingSubText(DialogIds.JumpReleaseExitHint)]
    public JumpReleaseExitSubMenu JumpReleaseExit { get; set; } = new();

    // =================================================================================================================
    [SettingSubMenu]
    public class MoveAfterLandSubMenu {
        public MoveAfterLandMode Mode { get; set; } = MoveAfterLandMode.DashOnly;
        public void CreateModeEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ModeEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.Mode),
                values: MoveAfterLandModeToString,
                min: (int) MoveAfterLandMode.Disabled,
                max: (int) MoveAfterLandMode.JumpOnly,
                value: (int) Mode
            );
            ModeEntry.Change(newValue => Mode = (MoveAfterLandMode) newValue);
            subMenu.Add(ModeEntry);
        }

        [SettingName(DialogIds.MoveAfterLandIgnoreUltras)]
        public bool IgnoreUltras { get; set; } = true;

        public LintAction Action { get; set; } = LintAction.Kill;
        public void CreateActionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ActionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.LintAction),
                values: LintActionToString,
                min: (int) LintAction.Kill,
                max: (int) LintAction.Kill,
                value: (int) Action
            );
            ActionEntry.Change(newValue => Action = (LintAction) newValue);
            subMenu.Add(ActionEntry);
        }

        [SettingRange(1, MaxShortDurationFrames)]
        [SettingName(DialogIds.MoveAfterLandFrames)]
        public int Frames { get; set; } = 3;
    }
    [SettingName(DialogIds.MoveAfterLand)]
    [SettingSubText(DialogIds.MoveAfterLandHint)]
    public MoveAfterLandSubMenu MoveAfterLand { get; set; } = new();

    // =================================================================================================================
    [SettingSubMenu]
    public class MoveAfterGainControlSubMenu {
        [SettingName(DialogIds.Enabled)]
        public bool Enabled { get; set; } = true;

        public LintAction Action { get; set; }
        public void CreateActionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ActionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.LintAction),
                values: LintActionToString,
                min: (int) LintAction.Kill,
                max: (int) LintAction.Kill,
                value: (int) Action
            );
            ActionEntry.Change(newValue => Action = (LintAction) newValue);
            subMenu.Add(ActionEntry);
        }

        [SettingRange(1, MaxShortDurationFrames)]
        [SettingName(DialogIds.MoveAfterGainControlFrames)]
        public int Frames { get; set; } = 3;
    }
    [SettingName(DialogIds.MoveAfterGainControl)]
    [SettingSubText(DialogIds.MoveAfterGainControlHint)]
    public MoveAfterGainControlSubMenu MoveAfterGainControl { get; set; } = new();

    // =================================================================================================================
    [SettingSubMenu]
    public class DashAfterUpEntrySubMenu {
        [SettingName(DialogIds.Enabled)]
        public bool Enabled { get; set; } = true;

        public LintAction Action { get; set; }
        public void CreateActionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ActionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.LintAction),
                values: LintActionToString,
                min: (int) LintAction.Kill,
                max: (int) LintAction.Kill,
                value: (int) Action
            );
            ActionEntry.Change(newValue => Action = (LintAction) newValue);
            subMenu.Add(ActionEntry);
        }

        [SettingRange(1, MaxShortDurationFrames)]
        [SettingName(DialogIds.DashAfterUpEntryFrames)]
        public int Frames { get; set; } = 3;
    }
    [SettingName(DialogIds.DashAfterUpEntry)]
    [SettingSubText(DialogIds.DashAfterUpEntryHint)]
    public DashAfterUpEntrySubMenu DashAfterUpEntry { get; set; } = new();

    // =================================================================================================================
    [SettingSubMenu]
    public class ReleaseWBeforeDashSubMenu {
        [SettingName(DialogIds.Enabled)]
        public bool Enabled { get; set; } = true;

        public LintAction Action { get; set; } = LintAction.Kill;
        public void CreateActionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ActionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.LintAction),
                values: LintActionToString,
                min: (int) LintAction.Kill,
                max: (int) LintAction.Kill,
                value: (int) Action
            );
            ActionEntry.Change(newValue => Action = (LintAction) newValue);
            subMenu.Add(ActionEntry);
        }

        [SettingRange(1, MaxShortDurationFrames)]
        [SettingName(DialogIds.ReleaseWBeforeDashFrames)]
        public int Frames { get; set; } = 4;
    }
    [SettingName(DialogIds.ReleaseWBeforeDash)]
    [SettingSubText(DialogIds.ReleaseWBeforeDashHint)]
    public ReleaseWBeforeDashSubMenu ReleaseWBeforeDash { get; set; } = new();

    // =================================================================================================================
    [SettingSubMenu]
    public class FastfallGlitchBeforeDashSubMenu {
        [SettingName(DialogIds.Enabled)]
        public bool Enabled { get; set; } = true;

        public LintAction Action { get; set; } = LintAction.Kill;
        public void CreateActionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ActionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.LintAction),
                values: LintActionToString,
                min: (int) LintAction.Kill,
                max: (int) LintAction.Kill,
                value: (int) Action
            );
            ActionEntry.Change(newValue => Action = (LintAction) newValue);
            subMenu.Add(ActionEntry);
        }

        [SettingRange(1, MaxShortDurationFrames)]
        [SettingName(DialogIds.FastfallGlitchBeforeDashFrames)]
        public int Frames { get; set; } = 4;
    }
    [SettingName(DialogIds.FastfallGlitchBeforeDash)]
    [SettingSubText(DialogIds.FastfallGlitchBeforeDashHint)]
    public FastfallGlitchBeforeDashSubMenu FastfallGlitchBeforeDash { get; set; } = new();

    // =================================================================================================================
    [SettingSubMenu]
    public class TurnBeforeWallkickSubMenu {
        [SettingName(DialogIds.Enabled)]
        public bool Enabled { get; set; } = true;

        public LintAction Action { get; set; } = LintAction.Kill;
        public void CreateActionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ActionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.LintAction),
                values: LintActionToString,
                min: (int) LintAction.Kill,
                max: (int) LintAction.Kill,
                value: (int) Action
            );
            ActionEntry.Change(newValue => Action = (LintAction) newValue);
            subMenu.Add(ActionEntry);
        }

        [SettingRange(1, MaxShortDurationFrames)]
        [SettingName(DialogIds.TurnBeforeWallkickFrames)]
        public int Frames { get; set; } = 4;
    }
    [SettingName(DialogIds.TurnBeforeWallkick)]
    [SettingSubText(DialogIds.TurnBeforeWallkickHint)]
    public TurnBeforeWallkickSubMenu TurnBeforeWallkick { get; set; } = new();

    // =================================================================================================================
    [SettingSubMenu]
    public class ShortWallboostSubMenu {
        [SettingName(DialogIds.Enabled)]
        public bool Enabled { get; set; } = true;

        public LintAction Action { get; set; } = LintAction.Kill;
        public void CreateActionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ActionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.LintAction),
                values: LintActionToString,
                min: (int) LintAction.Kill,
                max: (int) LintAction.Kill,
                value: (int) Action
            );
            ActionEntry.Change(newValue => Action = (LintAction) newValue);
            subMenu.Add(ActionEntry);
        }

        [SettingRange(1, MaxShortWallboostFrames)]
        [SettingName(DialogIds.ShortWallboostFrames)]
        public int Frames { get; set; } = 2;
    }
    [SettingName(DialogIds.ShortWallboost)]
    [SettingSubText(DialogIds.ShortWallboostHint)]
    public ShortWallboostSubMenu ShortWallboost { get; set; } = new();

    // =================================================================================================================
    [SettingSubMenu]
    public class BufferedUltraSubMenu {
        public BufferedUltraMode Mode { get; set; } = BufferedUltraMode.OnlyWhenMattered;
        public void CreateModeEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ModeEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.Mode),
                values: BufferedUltraModeToString,
                min: (int) BufferedUltraMode.Disabled,
                max: (int) BufferedUltraMode.Always,
                value: (int) Mode
            );
            ModeEntry.Change(newValue => Mode = (BufferedUltraMode) newValue);
            subMenu.Add(ModeEntry);
        }

        public LintAction Action { get; set; } = LintAction.Kill;
        public void CreateActionEntry(TextMenuExt.SubMenu subMenu, bool inGame) {
            TextMenu.Slider ActionEntry = new TextMenu.Slider(
                label: Dialog.Clean(DialogIds.LintAction),
                values: LintActionToString,
                min: (int) LintAction.Kill,
                max: (int) LintAction.Kill,
                value: (int) Action
            );
            ActionEntry.Change(newValue => Action = (LintAction) newValue);
            subMenu.Add(ActionEntry);
        }
    }
    [SettingName(DialogIds.BufferedUltra)]
    [SettingSubText(DialogIds.BufferedUltraHint)]
    public BufferedUltraSubMenu BufferedUltra { get; set; } = new();

    // =================================================================================================================
    public static string LintActionToString(int action) {
        switch ((LintAction) action) {
        case LintAction.Kill:
            return Dialog.Clean(DialogIds.LintActionKill);
        }
        return "";
    }
    public static string TransitionDirectionToString(int direction) {
        switch ((TransitionDirection) direction) {
        case TransitionDirection.UpOnly:
            return Dialog.Clean(DialogIds.JumpReleaseExitUp);
        case TransitionDirection.NotDown:
            return Dialog.Clean(DialogIds.JumpReleaseExitNotDown);
        case TransitionDirection.Any:
            return Dialog.Clean(DialogIds.JumpReleaseExitAny);
        }
        return "";
    }
    public static string MoveAfterLandModeToString(int mode) {
        switch ((MoveAfterLandMode) mode) {
        case MoveAfterLandMode.Disabled:
            return Dialog.Clean(DialogIds.Off);
        case MoveAfterLandMode.DashOnly:
            return Dialog.Clean(DialogIds.MoveAfterLandDashOnly);
        case MoveAfterLandMode.DashOrJump:
            return Dialog.Clean(DialogIds.MoveAfterLandDashOrJump);
        case MoveAfterLandMode.JumpOnly:
            return Dialog.Clean(DialogIds.MoveAfterLandJumpOnly);
        }
        return "";
    }
    public static string BufferedUltraModeToString(int mode) {
        switch ((BufferedUltraMode) mode) {
        case BufferedUltraMode.Disabled:
            return Dialog.Clean(DialogIds.Off);
        case BufferedUltraMode.OnlyWhenMattered:
            return Dialog.Clean(DialogIds.BufferedUltraOnlyWhenMattered);
        case BufferedUltraMode.Always:
            return Dialog.Clean(DialogIds.BufferedUltraAlways);
        }
        return "";
    }
}
