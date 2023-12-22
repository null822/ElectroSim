using System;
using System.Collections.Generic;
using ElectroSim.Maths;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElectroSim.Content;

/// <summary>
/// The parent class of all components.
/// </summary>
public class Component : EqualityComparer<Component>
{
    private Vec2Long _pos;
    private readonly string _texture;

    protected readonly ComponentDetails Details;
    
    protected Component(ComponentDetails details, string texture = "missing", Vec2Long? pos = null)
    {
        var posNonNull = pos ?? new Vec2Long(0);

        Details = details;
        _texture = texture;
        _pos = AlignPos(posNonNull);
    }


    /// <summary>
    /// Renders the component.
    /// </summary>
    /// <param name="spriteBatch">The spritebatch to render to</param>
    /// <param name="tint">Tint colour (supports transparency) (optional)</param>
    public void Render(SpriteBatch spriteBatch, Color? tint = null)
    {
        var tintNonNull = tint ?? Color.White;
        var texture = Textures.GetTexture(_texture);

        var scaleVec = new Vec2Double(MainWindow.GetScale());
        var screenCenter = (Vec2Long)MainWindow.GetScreenSize() / 2;
        
        var screenPos = (_pos - screenCenter + MainWindow.GetTranslation()) * scaleVec + screenCenter;
        
        // Console.WriteLine(screenCenter);
        // Console.WriteLine(_pos + " => " + screenPos);
        
        spriteBatch.Draw(
            texture,
            screenPos,
            null,
            tintNonNull,
            0f,
            new Vec2Float(texture.Width / 2f, texture.Height / 2f),
            scaleVec,
            SpriteEffects.None,
            0f
        );
    }

    /// <summary>
    /// Aligns the coordinate to a component-sized grid.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private static Vec2Long AlignPos(Vec2Long pos)
    {
        pos /= GameConstants.MinComponentSize;
        // as an integer, it gets rounded automatically
        pos *= GameConstants.MinComponentSize;

        return pos;
    }
    
    /// <summary>
    /// Sets the position of the component.
    /// </summary>
    /// <param name="pos">The new position</param>
    public void SetPos(Vec2Long pos)
    {
        _pos = AlignPos(pos);
    }
    
    /// <summary>
    /// Adds to the position of the component.
    /// </summary>
    /// <param name="pos">The position to add</param>
    public void MovePos(Vec2Long pos)
    {
        _pos += AlignPos(pos);
    }
    
    /// <summary>
    /// Returns the position of the component.
    /// </summary>
    public Vec2Long GetPos()
    {
        return _pos;
    }
    
    /// <summary>
    /// Returns the size of the component.
    /// </summary>
    public Vec2Float GetSize()
    {
        var texture = Textures.GetTexture(_texture);
        return new Vec2Float(texture.Width, texture.Height);
    }
    
    
    /// <summary>
    /// Returns the details of the component.
    /// </summary>
    public ComponentDetails GetDetails()
    {
        return Details;
    }
    
    /// <summary>
    /// Returns the texture name of the component.
    /// </summary>
    public string GetTexture()
    {
        return _texture;
    }

    /// <summary>
    /// Creates a copy of the component
    /// </summary>
    public Component Copy()
    {
        return new Component(Details, _texture, _pos);
    }
    
    // overrides
    
    public static bool operator ==(Component a, Component b)
    {
        if (object.Equals(a, null) || object.Equals(b, null))
            return false;

        return a.Details == b.Details;
    }
    
    public static bool operator !=(Component a, Component b)
    {
        if (object.Equals(a, null) || object.Equals(b, null))
            return true;

        return a.Details != b.Details;
    }

    public bool Equals(Component other)
    {
        if (Equals(other, null)) return false;
        return Equals(Details, other.Details);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Component)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Details);
    }

    public override bool Equals(Component a, Component b)
    {
        return a == b;
    }

    public override int GetHashCode(Component obj)
    {
        return HashCode.Combine(obj.Details);
    }
}