#nullable enable
using System;

namespace ElectroSim.Maths.Text;

public static class Prefixes
{
    private static readonly char[] PrefixList = 
    {
        'q', // 1e-30
        'r', // 1e-27
        'y', // 1e-24
        'z', // 1e-21
        'a', // 1e-18
        'f', // 1e-15
        'p', // 1e-12
        'n', // 1e-09
        'µ', // 1e-06
        'm', // 1e-03
        
        ' ', // 1e+00
        
        'k', // 1e+03
        'M', // 1e+06
        'G', // 1e+09
        'T', // 1e+12
        'P', // 1e+15
        'E', // 1e+18
        'Z', // 1e+21
        'Y', // 1e+24
        'R', // 1e+27
        'Q', // 1e+30
    };
    
    public static string FormatNumber(double value, Unit unit)
    {
        if (Math.Abs(value) is < 1000 and > 1)
            return value.ToString("0.00").Replace(".00", "") + unit.GetSymbol();
        
        var prefix = GetPrefix(value);
        var mag10 = Math.Clamp(Math.Floor(Math.Log10(value)), -30, 30);
        
        var mantissa = value / Math.Pow(10, mag10);
        var exponent = (int)(mag10 % 3 + 3);
        
        if (exponent == 3) exponent = 0;
        if (Math.Abs(value) >= 0) exponent %= 3;

        var simplifiedValue = (mantissa * Math.Pow(10, exponent)).ToString("0.00").Replace(".00", "");
        if (simplifiedValue == "0") return "0" + unit.GetSymbol();
        
        return simplifiedValue + (prefix == ' ' ? "" : prefix) + unit.GetSymbol();
    }
    
    private static char GetPrefix(double value)
    {
        var log = Math.Floor(Math.Log10(value));
        
        return PrefixList[LogToPrefixIndex(log)];
    }


    private static int LogToPrefixIndex(double log)
    {
        return Math.Clamp((int)Math.Ceiling((log + 1) / 3) + 9, 0, PrefixList.Length - 1);
    }
    
    private static int LogToPrefixIndexUnclamped(double log)
    {
        return (int)Math.Ceiling((log + 1) / 3) + 9;
    }
}