using Godot;
using System;

namespace SFS
{
    internal static class Debug
    {
        internal static void Assert(bool condition, string msg)
#if DEBUG
        {
            if (condition) 
                return;
            GD.PushError(msg);            
            throw new ApplicationException("Assert Failed: " + msg);
        }
#else
        {}
#endif

        internal static void Print(string msg)
#if DEBUG
        { GD.Print(msg); }
#else
        {}
#endif    

        internal static void PrintWarning(string msg)
#if DEBUG
        { GD.PushWarning(msg); }
#else
        {}
#endif

        internal static void PrintError(string msg)
#if DEBUG
        { GD.PushError(msg); }
#else
        {}
#endif
    }
}
