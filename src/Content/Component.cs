﻿using System;
using ElectroSim.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElectroSim.Content;

/// <summary>
/// The parent class of all components.
/// </summary>
public class Component
{
    private Vector2 _pos;
    private readonly string _texName;

    protected readonly ComponentDetails Details;

    protected Component(ComponentDetails details, string texNameName = "chip", Vector2? pos = null)
    {
        var posNonNull = pos ?? Vector2.Zero;

        Details = details;
        _texName = texNameName;
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

        var texture = Textures.GetTexture(_texName);
        
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
    private Vector2 AlignPos(Vector2 pos)
    {
        pos /= Constants.MinComponentSize;
        pos.Round();
        pos *= Constants.MinComponentSize;

        return pos;
        
        
        return new Vector2((float)(Math.Round(pos.X / Constants.MinComponentSize) * Constants.MinComponentSize), (float)(Math.Round(pos.Y / Constants.MinComponentSize) * Constants.MinComponentSize));
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
        var texture = Textures.GetTexture(_texName);
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
    /// Creates a copy of the component
    /// </summary>
    public Component Copy()
    {
        return new Component(Details, _texName, _pos);
    }

}