
namespace ElectroSim.Maths;

public class Unit
{
    private readonly string _symbol;
    private readonly string _name;
    private readonly string _description;

    
    public Unit(string symbol, string displayName, string description)
    {
        _symbol = symbol;
        _name = displayName;
        _description = description;
    }
    

    public static Value operator *(Unit value1, Unit value2)
    {
        var values = new Unit2Orderless(value1, value2);

        if (values == Unit2Orderless.FromNames("Ampere", "Volt"))
            return new Value(3600, Units.Get("Joule"));
        
        if (values == Unit2Orderless.FromNames("Ampere", "Ohm"))
            return new Value(1, Units.Get("Volt"));
        
        if (values == Unit2Orderless.FromNames("Ampere", "Second"))
            return new Value(1, Units.Get("Coulomb"));


        return new Value(1, MaybeNullConcatenation(value1, value2, '*'));
    }
    public static Value operator /(Unit value1, Unit value2)
    {
        if (value1 == Units.Get("Ohm") && value2 == Units.Get("Volt"))
            return new Value(3600, Units.Get("Ampere"));
        
        if (value1 == Units.Get("Ampere") && value2 == Units.Get("Volt"))
            return new Value(1, Units.Get("Ohm"));
        
        if (value1 == Units.Get("Joule") && value2 == Units.Get("Second"))
            return new Value(1, Units.Get("JoulePerSecond"));
        
        return new Value(1, MaybeNullConcatenation(value1, value2, '/'));
    }

    public static bool operator ==(Unit value1, Unit value2)
    {
        return value1._symbol == value2._symbol;
    }
    public static bool operator !=(Unit value1, Unit value2)
    {
        return value1._symbol != value2._symbol;
    }


    private static Unit MaybeNullConcatenation(Unit value1, Unit value2, char sign)
    {
        var nullUnit = Units.Get("Null");

        var nonNullUnit = value1 == nullUnit ?
            value2 == nullUnit ?
                nullUnit :                  // both are Null
                value1                      // value2 != Null && value1 == Null
            : value2 == nullUnit ?
                value2 :                    // value1 != Null && value2 == Null
                null;                       // neither are null
        
        // neither are Null / one or more units are null
        return nonNullUnit ?? Concat(value1, sign, value2);
    }
    
    
    private static Unit Concat(Unit value1, char signChar, Unit value2)
    {
        // canceling
        if (value1 == value2 && signChar == '/')
            return Units.Get("Null");
        
        var square = value1 == value2 && signChar == '*';

        var sign = signChar switch
        {
            '*' => square ? "\u00b2" : "", // eh
            '/' => "/",
            
            _ => "?"
        };
        
        var name = signChar switch
        {
            '*' => "",
            '/' => "per",
            
            _ => "?"
        };
        
        if (sign == "?")
            Util.ConsoleWarn("Unknown Sign: " + signChar);
        
        
        var newUnit = new Unit(
            value1.GetSymbol() + sign + (square ? "" : value2.GetSymbol()),
            (square ? "Square" : "") + value1.GetName() + " " + name + (name == "" ? "" : " ")  + (square ? "" : value2.GetName()),
            GameConstants.DynamicallyGeneratedUnitMessage
        );
        
        Units.RegisterUnit(newUnit);
        return newUnit;
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

/// <summary>
/// Stores 2 separate units, in alphabetical order.
/// Useful for comparing 2 units to 2 other units where order does not matter.
/// </summary>
internal class Unit2Orderless
{
    private readonly Unit _unit1;
    private readonly Unit _unit2;

    public Unit2Orderless(Unit unit1, Unit unit2)
    {
        _unit1 = unit1;
        _unit2 = unit2;
    }
    
    public static Unit2Orderless FromNames(string unit1, string unit2)
    {
        return new Unit2Orderless(Units.Get(unit1), Units.Get(unit2));
    }
    
    public static bool operator ==(Unit2Orderless value1, Unit2Orderless value2)
    {
        return
            (value1!._unit1 == value2!._unit1 && value1._unit2 == value2._unit2) ||
            (value1._unit1 == value2._unit2 && value1._unit2 == value2._unit1);
    }
    public static bool operator !=(Unit2Orderless value1, Unit2Orderless value2)
    {
        return
            (value1!._unit1 != value2!._unit1 && value1._unit2 != value2._unit2) ||
            (value1._unit1 != value2._unit2 && value1._unit2 != value2._unit1);
    }
}


/// <summary>
/// Stores 2 separate units, in the order they are presented.
/// Useful for comparing 2 units to 2 other units where order matters.
/// </summary>
internal class Unit2Ordered
{
    private readonly Unit _unit1;
    private readonly Unit _unit2;

    public Unit2Ordered(Unit unit1, Unit unit2)
    {
        _unit1 = unit1;
        _unit2 = unit2;
    }
    
    public static Unit2Ordered FromNames(string unit1, string unit2)
    {
        return new Unit2Ordered(Units.Get(unit1), Units.Get(unit2));
    }

    public Unit Get1()
    {
        return _unit1;
    }
    
    public Unit Get2()
    {
        return _unit2;
    }
}