using ElectroSim.Content;

namespace ElectroSim.Maths;

public static class Units
{
    public static readonly Unit Ohm = new("Ω", "Unit of resistance");
    public static readonly Unit Volt = new("V", "Unit of voltage");
    public static readonly Unit Ampere = new("A", "Unit of current");
    public static readonly Unit Farad = new("F", "Unit of capacitance");
    public static readonly Unit Henry = new("H", "Unit of inductance");
    
    public static readonly Unit Joule = new("J", "Unit of energy");
    public static readonly Unit JoulePerSecond = new("J/s", "Unit of energy/time (1 J/s = 1 Watt)");

}