using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElectroSim.Content;

public class Capacitor : Component
{

    private double _charge = 0;
    
    
    public Capacitor(ComponentDetails componentDetails, string texNameName = "component", Vector2? pos = null)
        : base(componentDetails, texNameName, pos)
    {
        
    }
}