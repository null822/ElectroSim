using System.Collections.Generic;
using ElectroSim.Maths;

namespace ElectroSim.Content;

public class ComponentDetails
{

    private readonly string _displayName;
    private readonly string _description;

    private readonly Dictionary<PropertyType, Value> _properties;
    
    /// <summary>
    /// Details for a component.
    /// </summary>
    /// <param name="displayName">The name of the component</param>
    /// <param name="description">The description of the component</param>
    /// <param name="properties">The properties of the component (PropertyType:Value)</param>
    public ComponentDetails(string displayName, string description, Dictionary<PropertyType, Value> properties)
    {
        _displayName = displayName;
        _description = description;
        _properties = properties;
    }

    /// <summary>
    /// Returns the name of the component.
    /// </summary>
    public string GetName()
    {
        return _displayName;
    }

    /// <summary>
    /// Returns the description of the component.
    /// </summary>
    public string GetDescription()
    {
        return _description;
    }

    /// <summary>
    /// Returns the value of a property, by name. Returns null if no property was found.
    /// </summary>
    /// <param name="propertyName">The property to get the value of</param>
    public Value GetPropertyValue(PropertyType propertyName)
    {
        return _properties.TryGetValue(propertyName, out var value) ? value : null;
    }
}