using System.Runtime.CompilerServices;

namespace Shared;

public static class TraceUtils
{
    public static (string filepath, int lineno, string function)
        CodeInfo(
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineno = -1,
            [CallerMemberName] string function = "")
        => (filePath, lineno, function);
}