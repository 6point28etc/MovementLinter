using System.Runtime.CompilerServices;

// Access to internal and private members without needing to use reflection
// (because apparently c# is so oop brainrotted they check this shit at runtime)
[assembly: IgnoresAccessChecksTo("Celeste")]
