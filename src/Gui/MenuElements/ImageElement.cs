using System;
using System.Text;
using ElectroSim.Content;
using ElectroSim.Maths;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace ElectroSim.Gui.MenuElements;

public class ImageElement : MenuElement
{
    private readonly string _image;
    private readonly ScalableValue2 _scale;

    public ImageElement(ScalableValue2 pos, string image, ScalableValue2 scale, Action click = null)
        : base(pos, new ScalableValue2(new Vector2(1), new Vector2(1), new Vector2(1)), click)
    {
        _image = image;
        _scale = scale;
    }

    protected override void RenderContents(SpriteBatch spriteBatch, Vector2 pos, Vector2 size)
    {
        spriteBatch.Draw(
            Textures.GetTexture(_image),
            pos,
            null,
            Hover ? Color.White : Color.LightGray,
            0,
            new Vector2(0),
            _scale.Get(),
            SpriteEffects.None,
            0f
            );
    }
    
}