using System;
using Microsoft.Xna.Framework;

namespace ElectroSim.Maths;

public class ScalableValue2
{
    private readonly ScalableValue _x;
    private readonly ScalableValue _y;


    public ScalableValue2(Vector2 scalablePoint,
        Vector2? minValue = null, Vector2? maxValue = null,
        AxisBind bindX = AxisBind.X, AxisBind bindY = AxisBind.Y)
    {
        
        var useMin = minValue != null;
        var useMax = maxValue != null;

        float? minValueX = useMin ? minValue.Value.X : null;
        float? minValueY = useMin ? minValue.Value.Y : null;
        
        float? maxValueX = useMax ? maxValue.Value.X : null;
        float? maxValueY = useMax ? maxValue.Value.Y : null;

        
        _x = new ScalableValue(scalablePoint.X, bindX, minValueX, maxValueX);
        _y = new ScalableValue(scalablePoint.Y, bindY, minValueY, maxValueY);
    }
    
    public float GetY()
    {
        return _y;
    }
    
    public float GetX()
    {
        return _x;
    }

    public Vector2 Get()
    {
        return new Vector2(_x, _y);
    }

    public static implicit operator Vector2(ScalableValue2 value)
    {
        return value.Get();
    }
    
}