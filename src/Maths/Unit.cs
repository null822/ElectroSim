using System.Collections.Generic;
using ElectroSim.Maths;

namespace ElectroSim.Content;

public class Unit
{
    private readonly string _symbol;
    private readonly string _name;
    private readonly string _description;

    private static readonly Dictionary<UnitOperatorUnit, Unit> UnitConversions = new()
    {
        // Ohm's Law
        {new UnitOperatorUnit(Units.Ampere, '*', Units.Ohm), Units.Volt},
        {new UnitOperatorUnit(Units.Volt, '/', Units.Ohm), Units.Ampere},
        {new UnitOperatorUnit(Units.Volt, '/', Units.Ampere), Units.Ohm},
        
        
        {new UnitOperatorUnit(Units.Volt, '*', Units.Ampere), Units.Watt},

    };
    
    public Unit(string symbol, string displayName, string description)
    {
        _symbol = symbol;
        _name = displayName;
        _description = description;
    }
    
    
    public static Unit operator *(Unit value1, Unit value2)
    {
        var op = new UnitOperatorUnit(value1, '*', value2);
        
        return UnitConversions.TryGetValue(op, out var value) ? value : value1;
    }
    public static Unit operator /(Unit value1, Unit value2)
    {
        var op = new UnitOperatorUnit(value1, '/', value2);
        
        // division and canceling units
        return UnitConversions.TryGetValue(op, out var value) ? value : value1 == value2 ? Units.Null : value1;
    }

    public string GetSymbol()
    {
        return _symbol;
    }

    public string GetName()
    {
        return _name;
    }

    public string GetDescription()
    {
        return _description;
    }

    public override string ToString()
    {
        return GetSymbol();
    }

}

internal class UnitOperatorUnit
{
    private Unit _unit1;
    private Unit _unit2;
    private char _operator;

    public UnitOperatorUnit(Unit unit1, char @operator, Unit unit2)
    {
        _unit1 = unit1;
        _unit2 = unit2;
        _operator = @operator;
    }
}