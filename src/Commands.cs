using Celeste.Mod.TasTestSuite;
using Monocle;

namespace Celeste.Mod.MovementLinter;

public static class Commands {
    [Command("linter_selftest", "Run self-test suite for MovementLinter")]
    public static void LinterSelftest(bool verbose = false, bool fastForward = true) {
        TestSuite.StartTestSuite(MovementLinterModule.Instance, "test", verbose, fastForward);
    }
}
