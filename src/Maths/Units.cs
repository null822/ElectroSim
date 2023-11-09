using ElectroSim.Content;

namespace ElectroSim.Maths;

public static class Units
{
    public static readonly Unit Null = new("", "", "A number");

    
    public static readonly Unit Ohm = new("Ω", "Ohm", "Unit of resistance");
    public static readonly Unit Volt = new("V", "Volt", "Unit of voltage");
    public static readonly Unit Ampere = new("A", "Ampere", "Unit of current");
    public static readonly Unit Farad = new("F", "Farad", "Unit of capacitance");
    public static readonly Unit Henry = new("H", "Henry", "Unit of inductance");
    public static readonly Unit Coulomb = new("C", "Coulomb", "Unit of electrical charge");
    
    
    // up for debate
    public static readonly Unit WattHour = new("Wh", "Watt Hour", "Unit of energy");
    public static readonly Unit Watt = new("W", "Watt", "Unit of energy/time");

    public static readonly Unit Joule = new("J", "Joule", "Unit of energy");
    public static readonly Unit JoulePerSecond = new("J/s", "Joule per Second", "Unit of energy/time (1 J/s = 1 Watt)");

}