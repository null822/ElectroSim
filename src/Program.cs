using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace ElectroSim;

public static class Program
{

    private static Game _game;

    private static void Main(string[] args)
    {
        _game = new MainWindow();
        _game.Run();
    }
    
    public static Game Get()
    {
        _game ??= new MainWindow();

        return _game;
    }
    
    public static Texture2D LoadTexture(string name)
    {
        _game ??= new MainWindow();
        
        return _game.Content.Load<Texture2D>("textures/" + name);
    }
    
    public static BitmapFont LoadFont(string name)
    {
        _game ??= new MainWindow();
        
        return _game.Content.Load<BitmapFont>("fonts/" + name);
    }

}