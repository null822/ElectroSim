using System;
using ElectroSim.Content.ComponentTypes;
using ElectroSim.Maths;
using ElectroSim.Maths.BlockMatrix;
using ElectroSim.Registry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElectroSim.Content;

/// <summary>
/// The parent class of all components.
/// </summary>
public class Component : IBlockMatrixElement<Component>
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
    /// <param name="pos">the position to render the component at</param>
    /// <param name="tint">Tint colour (supports transparency) (optional)</param>
    public virtual void Render(SpriteBatch spriteBatch, Vec2Long pos, Color? tint = null)
    {
        var tintNonNull = tint ?? Color.White;
        var texture = Textures.GetTexture(_texture);
        
        var scaleVec = new Vec2Double(MainWindow.GetScale()) / 32;
        
        var screenPos = Util.GameToScreenCoords(pos);

        
        spriteBatch.Draw(
            texture,
            screenPos,
            null,
            tintNonNull,
            0f,
            new Vec2Float(0),
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
    public Vec2Long GetSize()
    {
        var texture = Textures.GetTexture(_texture);
        return new Vec2Long(texture.Width, texture.Height);
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
    
    static bool IBlockMatrixElement<Component>.operator ==(Component a, Component b)
    {
        return b != null && a != null && a.Details.ToString() == b.Details.ToString();
    }
    
    static bool IBlockMatrixElement<Component>.operator !=(Component a, Component b)
    {
        return b != null && a != null && a.Details.ToString() != b.Details.ToString();
    }
    
    private bool Equals(Component other)
    {
        return Equals(Details, other.Details);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Component)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Details);
    }

    public override string ToString()
    {
        return Details.ToString();
    }

    // block matrix
    
    public ReadOnlySpan<byte> Serialize()
    {
        return Details.GetName() == "Empty" ? BitConverter.GetBytes(0) : BitConverter.GetBytes(254);
    }
    
    public static Component Deserialize(ReadOnlySpan<byte> bytes)
    {
        var value = bytes[0];
        
        return value == 0 ? Components.Empty : Components.Capacitor.GetVariant(1e-6);
    }

    public static uint SerializeLength => 1;
}