namespace ElectroSim.Content;

public class Unit
{
    private readonly string _displayName;
    private readonly string _description;
    
    public Unit(string displayName, string description)
    {
        _displayName = displayName;
        _description = description;
    }

    public string GetName()
    {
        return _displayName;
    }

    public string GetDescription()
    {
        return _description;
    }
}