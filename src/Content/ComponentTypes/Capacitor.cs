using ElectroSim.Maths;
using Microsoft.Xna.Framework;

namespace ElectroSim.Content.ComponentTypes;

public class Capacitor : Component
{
    private const string DefaultTexture = "component";

    private Value _charge = new Value(1, Units.Get("Joule")) * 3600;
    
    
    public Capacitor(ComponentDetails componentDetails, string texture = DefaultTexture, Vector2? pos = null)
        : base(componentDetails, texture, pos)
    {
        
    }
    
    public Capacitor(ComponentDetails componentDetails)
        : base(componentDetails, DefaultTexture)
    {
        
    }
    
    
}