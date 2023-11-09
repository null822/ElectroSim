using ElectroSim.Maths;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElectroSim.Gui;

public class Menu
{
    private readonly MenuBackground _background;
    private readonly MenuElement[] _elements;


    public Menu(ScalableValue2 pos, ScalableValue2 size,
        MenuElement[] elements
        )
    {
        _background = new MenuBackground(pos, size);

        _elements = elements;
    }

    public void Render(SpriteBatch spriteBatch)
    {
        _background.Render(spriteBatch);
        
        foreach (var element in _elements)
        {
            element.Render(spriteBatch, _background.GetPos(), _background.GetSize());
        }
    }

    public void CheckHover(Vector2 mousePos)
    {
        foreach (var element in _elements)
        {
            element.CheckHover(mousePos);
        }
    }


}