using System.Collections.Generic;
using ElectroSim.Content;
using ElectroSim.Maths;

namespace ElectroSim;

/// <summary>
/// Stores all components for access anywhere in the program.
/// </summary>
public static class Components
{
    public static readonly ComponentVariations Capacitor = new(
        "Capacitor",
        "Stores Energy",
        new Dictionary<PropertyType, Value> 
        { 
            { PropertyType.Resistance, new Value(1e3, Units.Ohm) } 
        },
        PropertyType.Capacitance, 
        Units.Farad, 
        new[]
        {
            1e-6
        });
    

}