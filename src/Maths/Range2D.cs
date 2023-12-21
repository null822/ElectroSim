
using Microsoft.Xna.Framework;

namespace ElectroSim.Maths;

public class Range2D
{
    private readonly long _left;
    private readonly long _bottom;
    private readonly long _right;
    private readonly long _top;

    public Range2D(long left, long bottom, long right, long top)
    {
        _left = left;
        _bottom = bottom;
        _right = right;
        _top = top;
    }
    
    public Range2D(Vec2Long tl, Vec2Long br)
    {
        _left = tl.X;
        _bottom = tl.Y;
        _right = br.X;
        _top = br.Y;
    }
    
    public bool Overlaps(Range2D range)
    {
        return _left < range._right && _right > range._left && _top > range._bottom && _bottom < range._top;
    }
    
    // if (RectA.X1 < RectB.X2 && RectA.X2 > RectB.X1 && RectA.Y1 > RectB.Y2 && RectA.Y2 < RectB.Y1) 
    
    //  A<X1 or A1<X or B<Y1 or B1<Y


    public override string ToString()
    {
        return GameConstants.Range2DStringFormat ? $"({_left}, {_bottom})..({_right}, {_top})" : $"({_left}..{_right}, {_bottom}..{_top})";
    }
}