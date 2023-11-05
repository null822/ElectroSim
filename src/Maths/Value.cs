using ElectroSim.Content;

namespace ElectroSim.Maths;

public class Value
{
    private readonly Unit _unit;
    private double _value;
    
    
    public Value(double value, Unit unit)
    {
        _unit = unit;
        _value = value;
    }

    public override string ToString()
    {
        return Prefixes.FormatNumber(_value, _unit);
    }

    public Unit GetUnit()
    {
        return _unit;
    }
    
    public static Value operator *(Value value1, Value value2)
    {
        value1._value *= value2._value;
        return value1;
    }
    public static Value operator /(Value value1, Value value2)
    {
        value1._value /= value2._value;
        return value1;
    }

    public static Value operator +(Value value1, Value value2)
    {
        value1._value += value2._value;
        return value1;
    }
    public static Value operator -(Value value1, Value value2)
    {
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
    
    
    public void SetValue(double value)
    {
        _value = value;
    }
}