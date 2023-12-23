using System.Collections.Generic;
using ElectroSim.Content;
using ElectroSim.Content.ComponentTypes;
using ElectroSim.Maths;

namespace ElectroSim.Registry;

/// <summary>
/// Stores all components for access anywhere in the program.
/// </summary>
public static class Components
{
    public static readonly Empty Empty =
        new(new ComponentDetails("Empty", "There is nothing here.", new Dictionary<PropertyType, Value>()));
    
    public static readonly ComponentVariations<Capacitor> Capacitor = new(
        "Capacitor",
        "Stores Energy",
        new Dictionary<PropertyType, Value> 
        { 
            { PropertyType.Resistance, new Value(1e3, Units.Get("Ohm")) } 
        },
        PropertyType.Capacitance, 
        Units.Get("Farad"), 
        new[]
        {
            1e-6
        });
    
    public static readonly ComponentVariations<Capacitor> Resistor = new(
        "Resistor",
        "Resists",
        new Dictionary<PropertyType, Value> 
        {
        },
        PropertyType.Resistance, 
        Units.Get("Ohm"), 
        new[]
        {
            1e0,
            1e1,
            1e2,
            1e3,
            1e4,
            1e5,
            1e6,
            1e7,
            1e9,
        });
    
}