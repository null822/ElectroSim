
using Microsoft.Xna.Framework;

namespace ElectroSim.Maths;

public class Range2D
{
    private readonly int _minX;
    private readonly int _minY;
    private readonly int _maxX;
    private readonly int _maxY;

    public Range2D(int minX, int minY, int maxX, int maxY)
    {
        _minX = minX;
        _minY = minY;
        _maxX = maxX;
        _maxY = maxY;
    }
    
    public Range2D(Vector2 tl, Vector2 br)
    {
        _minX = (int)tl.X;
        _minY = (int)tl.Y;
        _maxX = (int)br.X;
        _maxY = (int)br.Y;
    }

    public bool Intersects(Range2D range)
    {
        return this.ToRectangle().Intersects(range.ToRectangle());
    }
    
    public bool Contains(Range2D range)
    {
        return this.ToRectangle().Contains(range.ToRectangle());
    }
    
    public bool Contains(Vector2 point)
    {
        return this.ToRectangle().Contains(point);
    }

    private Rectangle ToRectangle()
    {
        return new Rectangle(_minX, _minY, _maxX - _minX, _maxY - _minY);
    }
}