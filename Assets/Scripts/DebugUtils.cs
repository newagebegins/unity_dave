using System;
using System.Diagnostics;

public class DebugUtils
{
    [Conditional("DEBUG")]
    public static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new Exception();
        }
    }
}
