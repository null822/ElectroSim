using System;
using ElectroSim.Maths;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElectroSim.Gui;

public class MenuElement
{
    protected readonly ScalableValue2 Pos;
    protected readonly ScalableValue2 Size;
    protected bool Hover;
    private readonly Action _click;
    
    protected MenuElement(ScalableValue2 pos, ScalableValue2 size, Action click = null)
    {
        Pos = pos;
        Size = size;
        _click = click;
        
        Hover = false;
    }

    public void Render(SpriteBatch spriteBatch, Vector2 pos, Vector2 size)
    {
        RenderContents(spriteBatch, pos + Pos, Size);
    }
    
    public void Click()
    {
        if (_click == null || !Hover)
            return;

        _click();
    }
    
    
    public bool CheckHover(Vector2 mousePos)
    {
        var pos = Pos.Get();
        var size = Size.Get();

        var collision = new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
        
        Hover = collision.Contains(mousePos);
        return Hover;
    }

    protected virtual void RenderContents(SpriteBatch spriteBatch, Vector2 pos, Vector2 size)
    {
    }

    public ScalableValue2 GetPos()
    {
        return Pos;
    }
    
    public ScalableValue2 GetSize()
    {
        return Size;
    }
    
}