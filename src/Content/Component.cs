using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElectroSim.Content;

/// <summary>
/// The parent class of all components.
/// </summary>
public class Component
{
    private Vector2 _pos;
    private readonly string _texture;

    protected readonly ComponentDetails Details;
    
    protected Component(ComponentDetails details, string texture = "missing", Vector2? pos = null)
    {
        var posNonNull = pos ?? Vector2.Zero;

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
        
        var scale = (float)MainWindow.GetScale();
        var scaleVec = new Vector2(scale);

        var center = Vector2.Divide(MainWindow.GetScreenSize(), 2);

        var texture = Textures.GetTexture(_texture);
        
        spriteBatch.Draw(
            texture,
            (_pos - center + MainWindow.GetTranslation()) * scaleVec + center,
            null,
            tintNonNull,
            0f,
            new Vector2(texture.Width / 2f, texture.Height / 2f),
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
    private static Vector2 AlignPos(Vector2 pos)
    {
        pos /= GameConstants.MinComponentSize;
        pos.Round();
        pos *= GameConstants.MinComponentSize;

        return pos;
    }
    
    /// <summary>
    /// Sets the position of the component.
    /// </summary>
    /// <param name="pos">The new position</param>
    public void SetPos(Vector2 pos)
    {
        _pos = AlignPos(pos);
    }
    
    /// <summary>
    /// Adds to the position of the component.
    /// </summary>
    /// <param name="pos">The position to add</param>
    public void MovePos(Vector2 pos)
    {
        _pos += AlignPos(pos);
    }
    
    /// <summary>
    /// Returns the position of the component.
    /// </summary>
    public Vector2 GetPos()
    {
        return _pos;
    }
    
    /// <summary>
    /// Returns the size of the component.
    /// </summary>
    public Vector2 GetSize()
    {
        var texture = Textures.GetTexture(_texture);
        return new Vector2(texture.Width, texture.Height);
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

}