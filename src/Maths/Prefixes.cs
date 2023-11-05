#nullable enable
using System;
using System.Collections.Generic;
using ElectroSim.Content;

namespace ElectroSim.Maths;

public static class Prefixes
{
    private static readonly KeyValuePair<string, double>[] PrefixDictionary = 
    {
        new("a", 1e-18),
        new("f", 1e-15),
        new("p", 1e-12),
        new("n", 1e-09),
        new("µ", 1e-06),
        new("m", 1e-03),
        
        new("",  1e+00),
        
        new("k", 1e+03),
        new("M", 1e+06),
        new("G", 1e+09),
        new("T", 1e+12),
        new("P", 1e+15),
        new("E", 1e+18)
    };

    private static string GetPrefix(double value)
    {
        var log = Math.Floor(Math.Log10(value));
        
        return PrefixDictionary[(int)Math.Ceiling((log + 1) / 3) + 5].Key;
    }
    
    public static string FormatNumber(double value, Unit unit)
    {
        if (Math.Abs(value) is < 1000 and > 1)
            return value.ToString("0.00") + unit.GetName();
        
        var prefix = GetPrefix(value);
        var mag10 = Math.Floor(Math.Log10(value));

        var mantissa = value / Math.Pow(10, mag10);
        var exponent = (int)(mag10 % 3 + 3);

        if (exponent == 3) exponent = 0;
        if (Math.Abs(value) >= 0) exponent %= 3;
        
        var simplifiedValue = (mantissa * Math.Pow(10, exponent)).ToString("0.00");
        return simplifiedValue + prefix + unit.GetName();
    }
}