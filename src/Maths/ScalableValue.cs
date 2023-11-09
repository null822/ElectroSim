using System;
using Microsoft.Xna.Framework;

namespace ElectroSim.Maths;

public class ScalableValue
{
    private readonly float _minValue;
    private readonly float _maxValue;
    private readonly float _scalablePoint;

    private readonly bool _useMin;
    private readonly bool _useMax;
    private readonly AxisBind _axisBind;


    /// <summary>
    /// Creates a value that will scale depending on the size of the screen.
    /// </summary>
    /// <param name="scalablePoint">The amount to scale the value by, depending on screen size {0-1, 0-1}</param>
    /// <param name="axisBind">The axis to bind to</param>
    /// <param name="minValue">The minimum value (optional)</param>
    /// <param name="maxValue">The maximum value (optional)</param>
    public ScalableValue(float scalablePoint, AxisBind axisBind, float? minValue = null, float? maxValue = null)
    {
        _useMin = minValue != null;
        _useMax = maxValue != null;

        var minPixelsNonNull = minValue ?? 0;
        var maxPixelsNonNull = maxValue ?? 0;
        
        _minValue = minPixelsNonNull;
        _maxValue = maxPixelsNonNull;
        _scalablePoint = scalablePoint;
        _axisBind = axisBind;
    }

    /// <summary>
    /// Gets the value, with the current screen size.
    /// </summary>
    public float Get()
    {
        var scaleValue = _axisBind switch
        {
            AxisBind.X => MainWindow.GetScreenSize().X,
            AxisBind.Y => MainWindow.GetScreenSize().Y,
            AxisBind.Average => (MainWindow.GetScreenSize().X + MainWindow.GetScreenSize().Y) / 2f,
            _ => 0f
        };
        
        
        var point = _scalablePoint * scaleValue;

        if (_useMax)
            point = Math.Min(point, _maxValue);
        
        if (_useMin)
            point = Math.Max(point, _minValue);
        
        return point;
    }
    
    public static implicit operator float(ScalableValue value)
    {
        return value.Get();
    }
}

public enum AxisBind
{
    X,
    Y,
    Average
}
