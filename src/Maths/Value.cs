using System;
using ElectroSim.Maths.Text;

namespace ElectroSim.Maths;

public class Value
{
    private readonly Unit _unit;
    private double _value;
    
    
    public Value(double value, Unit unit)
    {
        _value = value;
        _unit = unit;
    }

    public static Value operator *(Value value1, Value value2)
    {
        var unitValue = value1._unit * value2._unit;
        
        return new Value(value1._value * value2._value * unitValue._value, unitValue._unit);
    }
    public static Value operator /(Value value1, Value value2)
    {
        var unitValue = value1._unit / value2._unit;
        
        return new Value(value1._value / value2._value * unitValue._value, unitValue._unit);
    }

    public static Value operator +(Value value1, Value value2)
    {
        if (value1._unit != value2._unit)
            Util.ConsoleWarn($"Unit {value1._unit} and {value2._unit} are being added, but are not the same");
        
        value1._value += value2._value;
        return value1;
    }
    public static Value operator -(Value value1, Value value2)
    {
        if (value1._unit != value2._unit)
            Util.ConsoleWarn($"Unit {value1._unit} and {value2._unit} are being added, but are not the same");

        value1._value -= value2._value;
        return value1;
    }
    public static Value operator -(Value value1)
    {
        value1._value = -value1._value;
        return value1;
    }

    public static Value operator %(Value value1, Value value2)
    {
        value1._value %= value2._value;
        return value1;
    }
    
    public static bool operator >(Value value1, Value value2)
    {
        return value1._value > value2._value;
    }
    public static bool operator <(Value value1, Value value2)
    {
        return value1._value < value2._value;
    }
    
    public static bool operator >=(Value value1, Value value2)
    {
        return value1._value >= value2._value;
    }
    public static bool operator <=(Value value1, Value value2)
    {
        return value1._value <= value2._value;
    }
    
    public static bool operator ==(Value value1, Value value2)
    {
        return Math.Abs(value1!._value - value2!._value) < 1e-9 && value1._unit == value2._unit;
    }
    public static bool operator !=(Value value1, Value value2)
    {
        return Math.Abs(value1!._value - value2!._value) > 1e-9 || value1._unit != value2._unit;
    }

    
    public override string ToString()
    {
        return Prefixes.FormatNumber(_value, _unit);
    }

    public Unit GetUnit()
    {
        return _unit;
    }
    
    public string GetUnitName()
    {
        return _unit.GetName();
    }

    
    public void SetValue(double value)
    {
        _value = value;
    }

    public static implicit operator Value(double value)
    {
        return new Value(value, Units.Get("Null"));
    }

    public static Value Parse(string value)
    {
        var spaceIndex = value.IndexOf(' ');

        var number = double.TryParse(value[..spaceIndex], out var n) ? n : 0;

        var unitString = value[(spaceIndex + 1)..];
        
        var unit = Units.Get(unitString);

        if (unit != Units.Get("Null")) return new Value(number, unit);

        const string dynMsg = GameConstants.DynamicallyGeneratedUnitMessage;
        
        unit = new Unit(unitString, dynMsg, dynMsg);
        Units.RegisterUnit(unit);

        return new Value(number, unit);

    }
    
}
