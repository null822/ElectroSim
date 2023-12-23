using ElectroSim.Maths;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElectroSim.Content.ComponentTypes;

public class Empty : Component
{
    private const string DefaultTexture = "component";

    private Value _charge = new Value(1, Units.Get("Joule")) * 3600;
    
    
    public Empty(ComponentDetails componentDetails, string texture = DefaultTexture, Vector2? pos = null)
        : base(componentDetails, texture, pos)
    {
        
    }
    
    public Empty(ComponentDetails componentDetails)
        : base(componentDetails, DefaultTexture)
    {
        
    }

    public override void Render(SpriteBatch spriteBatch, Vec2Long pos, Color? tint = null)
    {
        
    }
}