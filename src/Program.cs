using Microsoft.Xna.Framework;

namespace ElectroSim;

public static class Program
{

    private static Game _game;

    private static void Main()
    {
        _game = new MainWindow();
        _game.Run();
    }
    
}