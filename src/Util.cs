using System;
using System.Linq;

namespace ElectroSim;

public static class Util
{
    public static void ConsoleWarn(string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[Warn]: {text}");
        Console.ForegroundColor = ConsoleColor.White;
    }
    
}