using System;
using System.Collections.Generic;
using ElectroSim.Content.ComponentTypes;
using ElectroSim.Maths;

namespace ElectroSim.Content;

public class ComponentVariations<T> where T : Component
{
    private readonly Dictionary<double, Component> _components = new();
    private readonly PropertyType _variablePropertyType;
    private readonly Unit _variableUnit;
    
    public ComponentVariations(string displayNameBase, string description, IDictionary<PropertyType, Value> fixedProperties,
        PropertyType variablePropertyType, Unit variableUnit, IReadOnlyList<double> variableValue
        )
    {
        _variablePropertyType = variablePropertyType;
        _variableUnit = variableUnit;
        
        foreach (var value in variableValue)
        {
            var properties = new Dictionary<PropertyType, Value>(fixedProperties)
            {
                { variablePropertyType, new Value(value, variableUnit) }
            };
            
            _components.Add(value, (T)Activator.CreateInstance(typeof(T), new ComponentDetails(
                        displayNameBase + " " + new Value(value, variableUnit),
                        description,
                        properties
                    )
                )
            );
        }
    }
    
    /// <summary>
    /// Returns the variants with the specified variable value. Returns null if no component was found.
    /// </summary>
    public Component GetVariant(double variableValue)
    {
        return _components.TryGetValue(variableValue, out var component) ? component : null;
    }
    
    /// <summary>
    /// Returns the variable values of all the variants.
    /// </summary>
    public double[] GetVariantValues()
    {
        var variations = new double[_components.Count];

        var i = 0;
        foreach (var componentKvP in _components)
        {
            variations[i] = componentKvP.Key;
            i++;
        }

        return variations;
    }
    
    /// <summary>
    /// Returns the names of all the variants.
    /// </summary>
    public string[] GetVariantNames()
    {
        var variations = new string[_components.Count];

        var i = 0;
        foreach (var componentKvP in _components)
        {
            variations[i] = componentKvP.Value.GetDetails().GetName();
            i++;
        }

        return variations;
    }

    public PropertyType GetVariablePropertyType()
    {
        return _variablePropertyType;
    }

    public Unit GetVariableUnit()
    {
        return _variableUnit;
    }
}