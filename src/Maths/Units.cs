using System;
using System.Collections.Generic;

namespace ElectroSim.Maths;

public static class Units
{
    
    /// <summary>
    /// A Dictionary containing all units (name:unit).
    /// </summary>
    private static readonly Dictionary<string, Unit> UnitDictionary = new()
    {
        { "Null", new Unit("", "", "A Number") },

        { "Ohm",  new Unit("Ω", "Ohm", "Unit of Resistance") },
        { "Volt",  new Unit("V", "Volt", "Unit of Voltage") },
        { "Ampere",  new Unit("A", "Ampere", "Unit of Current") },
        { "Farad",  new Unit("F", "Farad", "Unit of Capacitance") },
        { "Henry",  new Unit("H", "Henry", "Unit of Inductance") },
        { "Coulomb",  new Unit("C", "Coulomb", "Unit of Electric Charge") },

        { "Joule",  new Unit("J", "Joule", "Unit of Energy") },
        { "JoulePerSecond",  new Unit("J/s", "Joule Per Second", "Unit of Energy over Time") },

        { "Second",  new Unit("s", "Second", "Unit of Time") },
        { "Meter",  new Unit("m", "Meter", "Unit of Distance") },
        { "Gram",  new Unit("g", "Gram", "Unit of Weight") },

    };

    /// <summary>
    /// Register a unit.
    /// </summary>
    /// <param name="unit">The unit</param>
    public static bool RegisterUnit(Unit unit)
    {
        return UnitDictionary.TryAdd(unit.GetName().Replace(" ", ""), unit);
    }
    
    /// <summary>
    /// Get a unit, by name. Returns a null unit none was not found.
    /// </summary>
    /// <param name="name">The name of the unit to return</param>
    public static Unit Get(string name)
    {
        var unit = UnitDictionary.TryGetValue(name, out var unit1) ? unit1 : UnitDictionary["null"];
        return unit;
    }



    
}