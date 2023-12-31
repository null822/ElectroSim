﻿using System;
using ElectroSim.Content;
using ElectroSim.Maths;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElectroSim.Gui.MenuElements;

public class ImageElement : MenuElement
{
    private readonly string _image;

    public ImageElement(ScalableValue2 pos, ScalableValue2 size, string image, Action clickAction = null)
        : base(pos, size, clickAction)
    {
        _image = image;
    }

    protected override void RenderContents(SpriteBatch spriteBatch, Vector2 pos, Vector2 size)
    {
        var texture = Textures.GetTexture(_image);

        var scale = size / new Vector2(texture.Width, texture.Height);
        
        spriteBatch.Draw(
            Textures.GetTexture(_image),
            pos,
            null, 
            ClickAction == null ? Color.White : Hover ? Color.White : Color.LightGray,
            0,
            new Vector2(0),
            scale,
            SpriteEffects.None,
            0f
            );
    }
    
}