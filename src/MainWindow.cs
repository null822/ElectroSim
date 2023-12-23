using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ElectroSim.Content;
using ElectroSim.Gui;
using ElectroSim.Gui.MenuElements;
using ElectroSim.Maths;
using ElectroSim.Maths.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using Component = ElectroSim.Content.Component;
using static ElectroSim.Util;

namespace ElectroSim;

public class MainWindow : Game
{
    
    // rendering
    private readonly GraphicsDeviceManager _graphics;
    private static SpriteBatch _spriteBatch;

    // world/ui
    // private readonly BlockMatrix<Component> _components = new(Registry.Components.Empty, new Vec2Long(4611686018427387904, 4611686018427387904));
    private readonly BlockMatrix<Component> _components = new(Registry.Components.Empty, new Vec2Long(65536, 65536));
    private readonly List<Menu> _menus = new();
    
    // world editing
    private Component _activeBrush = Registry.Components.Capacitor.GetVariant(1e-6);
    private static Range2D _brushRange;
    private static bool _isOverlapping;
    private static Vec2Long _initialMousePos = Vector2.Zero;
    
    // camera position
    private static double _scale = 1;
    private static Vec2Double _translation = Vector2.Zero;
    private static Vec2Double _prevTranslation = Vector2.Zero;
    private static Vec2Long _gridSize;
    
    // output/screen
    private static Vec2Int _screenSize = Vector2.One;
    
    
    
    // controls
    // private readonly bool[] _prevMouseButtons = new bool[5];
    private MouseState _prevMouseState;
    private KeyboardState _prevKeyboardState;
    private Vec2Int _middleMouseCords = Vector2.Zero;
    private int _scrollWheelOffset = -1200;
    
    
    public MainWindow()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "assets";
        IsMouseVisible = true;
        
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnResize;
        
        // "System Checks"
        
        Debug("===============[SYSTEM CHECKS]===============");
        
        Log("log text");
        Debug("debug text");
        Warn("warn text");
        Error("error text");

        Debug(Prefixes.FormatNumber(3.1e-6, Units.Get("Farad")));
        
        var voltage1 = Value.Parse("1e+28 V");
        var current1 = Value.Parse("1e-30 A");
        
        var voltage2 = Value.Parse("1.1 V");
        var current2 = Value.Parse("45 A");
        
        Debug(voltage1 + " * " + current1 + " = " + voltage1 * current1);
        Debug(current2 + " / " + voltage2 + " = " + current2 / voltage2);

        var r1 = new Range2D(-2, -2, 2, 4);
        var r2 = new Range2D(0, 1, 3, 3);
        
        Debug(r1.Overlap(r2));
        
        Debug("===============[BEGIN PROGRAM]===============");
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        Console.WriteLine("Initialized");
    }

    /// <summary>
    /// Loads all of the games resources/fonts and initializes some variables
    /// </summary>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        RegisterTextures(new[]
        {
            "missing",
            "chip",
            "component",
            
            "gui/center",
            "gui/corner",
            "gui/edge"
        });
        
        RegisterFonts(new[]
        {
            "consolas"
        });

        var brushTypes = new List<Component>
        {
            Registry.Components.Capacitor.GetVariant(1e-6),
            Registry.Components.Resistor.GetVariant(1e3)
        };

        var brushTypeMenuElements = new MenuElement[brushTypes.Count];

        var i = 0;
        foreach (var brushType in brushTypes)
        {
            brushTypeMenuElements[i] = 
                new ImageElement(
                    new ScalableValue2(
                        new ScalableValue(0, AxisBind.X, 8, 8),
                        new ScalableValue(0.1f * (i+1), AxisBind.Y)
                    ),
                    new ScalableValue2(new Vector2(0), new Vector2(48), new Vector2(48)),
                    brushType.GetTexture(),
                    () =>
                    {
                        _activeBrush = brushType;
                        Console.WriteLine(brushType.GetDetails());
                    }
                );

            i++;
        }
        
        // debug / testing
        _menus.Add(new Menu(
            new ScalableValue2(new Vector2(0, 0.15f)),
            new ScalableValue2(
                new ScalableValue(0, AxisBind.X, 56, 56),
                new ScalableValue(0.7f, AxisBind.Y)
                ),
            brushTypeMenuElements
            )
        );


    }

    /// <summary>
    /// The game logic loop
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        // only run when focused
        if (!IsActive)
            return;
        
        // Logic
        _screenSize = new Vector2(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        
        
        // Controls
        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        if (keyboardState.IsKeyDown(Keys.Enter) && !_prevKeyboardState.IsKeyDown(Keys.Enter))
        {
            
            var svgMap = _components.GetSvgMap().ToString();
            
            var file = File.Create("BlockMatrixMap.svg");
            file.Write(Encoding.ASCII.GetBytes(svgMap));
            file.Close();
            
            Log("BlockMatrix Dumped");
        }

        var mouseScreenCords = new Vec2Int(mouseState.X, mouseState.Y);

        const int min = 1000;
        const int max = 4000;

        _scrollWheelOffset = (mouseState.ScrollWheelValue - _scrollWheelOffset) switch
        {
            > max => mouseState.ScrollWheelValue - max,
            < min => mouseState.ScrollWheelValue - min,
            _ => _scrollWheelOffset
        };
        
        // only run controls logic when hovered
        if (mouseScreenCords.X < 0 || mouseScreenCords.X > _screenSize.X || mouseScreenCords.Y < 0 || mouseScreenCords.Y > _screenSize.Y)
            return;
        
        var mousePos = Util.ScreenToGameCoords(mouseScreenCords);
        _scale = Math.Pow((mouseState.ScrollWheelValue - _scrollWheelOffset) / 1024f, 4);
        
        foreach (var menu in _menus)
        {
            menu.CheckHover(mouseScreenCords);
        }

        switch (mouseState.LeftButton)
        {
            // lMouse first tick
            case ButtonState.Pressed when _prevMouseState.LeftButton == ButtonState.Released && _menus.Any(menu => menu.Click()):
                UpdatePrevMouseState(mouseState);
                return;
            // !lMouse
            case ButtonState.Released when _prevMouseState.LeftButton == ButtonState.Pressed:
                break;
        }

        // !lShift
        if (!keyboardState.IsKeyDown(Keys.LeftShift))
        {
            if (_brushRange.GetArea() > 1)
            {
                _brushRange = new Range2D(mousePos.X, mousePos.Y, mousePos.X + 1, mousePos.Y + 1);
            }
        }
        
        // tick after lMouse || !lShift
        if ((mouseState.LeftButton == ButtonState.Released && _prevMouseState.LeftButton == ButtonState.Released) || !keyboardState.IsKeyDown(Keys.LeftShift))
        {
            _brushRange = new Range2D(mousePos.X, mousePos.Y, mousePos.X + 1, mousePos.Y + 1);
        }
        
        // update _isOverlapping by checking if _brushRange intersects with any component on screen
        _isOverlapping = ComponentIntersect(_brushRange);

        // first tick of lMouse
        if (mouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
        {
            _initialMousePos = mousePos;

            // & !overlap & !lShift
            if (!_isOverlapping && !keyboardState.IsKeyDown(Keys.LeftShift))
            {
                // _components[Brush[0].GetPos()] = Brush[0];

                _components[_brushRange] = _activeBrush;

                // _components.Set(Brush[0].GetPos(), Brush[0]);
                // AddComponent(Brush[0]);
            }
        }
        
        // tick after lMouse
        if (mouseState.LeftButton == ButtonState.Released && _prevMouseState.LeftButton == ButtonState.Pressed)
        {
            // & lShift
            if (keyboardState.IsKeyDown(Keys.LeftShift) && !_isOverlapping)
            {
                // create the contents of the brush in the world (add to _components)
                _components.Set(_brushRange, _activeBrush);
            }
            
            _brushRange = new Range2D(mousePos.X, mousePos.Y, mousePos.X + 1, mousePos.Y + 1);
        }

        // lMouse
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            // & lShift
            if (keyboardState.IsKeyDown(Keys.LeftShift))
            {
                _brushRange = new Range2D(
                    (int)_initialMousePos.X,
                    (int)_initialMousePos.Y,
                    (int)mousePos.X + 1,
                    (int)mousePos.Y + 1);
            }
        }
        
        // mMouse
        if (mouseState.MiddleButton == ButtonState.Pressed)
        {
            if (_prevMouseState.MiddleButton == ButtonState.Released)
            {
                _middleMouseCords = mouseScreenCords;
                _prevTranslation = _translation;
            }
            else
            {
                _translation = _prevTranslation + (Vec2Double)(mouseScreenCords - _middleMouseCords) / _scale;
            }
        }
        
        // rMouse
        if (mouseState.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released)
        {
            _translation = new Vec2Double(0);
            _scrollWheelOffset = mouseState.ScrollWheelValue - 1200;
            
            _scale = Math.Pow(Math.Min(Math.Max((mouseState.ScrollWheelValue - _scrollWheelOffset) / 1024f, 0e-4), 0e4), 4);

        }

        UpdatePrevMouseState(mouseState);
        UpdatePrevKeyboardState(keyboardState);

        _gridSize = Util.GameToScreenCoords(new Vec2Long(0, 0)) - Util.GameToScreenCoords(new Vec2Long(1, 1));

        base.Update(gameTime);
    }

    /// <summary>
    /// The draw loop
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        // only run when focused
        if (!IsActive)
            return;
        
        GraphicsDevice.Clear(Colors.CircuitBackground);
        
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        // render components (with off-screen culling)
        
        // game coords of the top left and bottom right corners of the screen, with a small buffer to prevent culling things still partially within the frame
        var tlScreen = Util.ScreenToGameCoords(new Vector2(0, 0) - new Vector2(64));
        var brScreen = Util.ScreenToGameCoords(_screenSize + new Vec2Int(64));
        
        _components.InvokeRanged(new Range2D(tlScreen, brScreen), (component, pos) =>
        {
            component.Render(_spriteBatch, pos);
            return true;
        }, ResultComparisons.Or, true);
        
        // render brush outline

        var brushScreenCoordsBl = Util.GameToScreenCoords(new Vec2Long(_brushRange.MinX, _brushRange.MinY));
        var brushScreenCoordsTr = Util.GameToScreenCoords(new Vec2Long(_brushRange.MaxX, _brushRange.MaxY));

        var brushScreenSize = brushScreenCoordsTr - brushScreenCoordsBl;
        
        _spriteBatch.DrawRectangle(brushScreenCoordsBl.X, brushScreenCoordsBl.Y, brushScreenSize.X, brushScreenSize.Y,
            Color.White, 2f);

        foreach (var menu in _menus)
        {
            menu.Render(_spriteBatch);
        }
        
        _spriteBatch.End();
        
        
        base.Draw(gameTime);
    }

    /// <summary>
    /// Returns true if the specified rectangle intersects with any component.
    /// </summary>
    /// <param name="rectangle">The rectangle to check for an intersection</param>
    private bool ComponentIntersect(Range2D rectangle)
    {
        var retValue = _components.InvokeRanged(rectangle,
            (c, _) => c != null && GetCollisionRectangle(c).Overlaps(rectangle), ResultComparisons.Or, true);
        return retValue;
    }

    /// <summary>
    /// Returns a rectangle representing the collision of a component
    /// </summary>
    /// <param name="component">the component to get the collision of</param>
    private static Range2D GetCollisionRectangle(Component component)
    {
        var componentPos = component.GetPos();
        var componentSize = component.GetSize();
        
        return new Range2D(
            (long)(componentPos.X - componentSize.X / 2f),
            (long)(componentPos.Y - componentSize.Y / 2f),
            (long)(componentPos.X + componentSize.X / 2f),
            (long)(componentPos.Y + componentSize.Y / 2f)
        );
    }
    
    private void UpdatePrevMouseState(MouseState state)
    {
        _prevMouseState = state;
    }
    
    private void UpdatePrevKeyboardState(KeyboardState state)
    {
        _prevKeyboardState = state;
    }
    
    /// <summary>
    /// Update the program when the size changes.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnResize(object sender, EventArgs e)
    {
        if (_graphics.PreferredBackBufferWidth == _graphics.GraphicsDevice.Viewport.Width &&
            _graphics.PreferredBackBufferHeight == _graphics.GraphicsDevice.Viewport.Height)
            return;
        
        _graphics.PreferredBackBufferWidth = _graphics.GraphicsDevice.Viewport.Width;
        _graphics.PreferredBackBufferHeight = _graphics.GraphicsDevice.Viewport.Height;
        
        _graphics.ApplyChanges();
    }
    
    
    /// <summary>
    /// Register multiple textures and store them in Textures.
    /// </summary>
    /// <param name="names">An array of the names of the textures to load</param>
    private void RegisterTextures(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            Textures.RegisterTexture(name, Content.Load<Texture2D>("textures/" + name));
        }
    }
    
    
    /// <summary>
    /// Register multiple fonts and store them in Fonts.
    /// </summary>
    /// <param name="names">An array of the names of the fonts to load</param>
    private void RegisterFonts(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            var fontName = name[(name.LastIndexOf('/') + 1)..];
        
            Fonts.RegisterFont(fontName, Content.Load<BitmapFont>("fonts/" + name));
        }
    }
    
    
    // public getters

    /// <summary>
    /// Returns zoom scale multiplier.
    /// </summary>
    public static double GetScale()
    {
        return _scale;
    }

    /// <summary>
    /// Returns the translation (pan) of the world.
    /// </summary>
    public static Vec2Double GetTranslation()
    {
        return _translation;
    }

    /// <summary>
    /// Returns the screen size.
    /// </summary>
    public static Vec2Int GetScreenSize()
    {
        return _screenSize;
    }
    
    /// <summary>
    /// Returns the screen size.
    /// </summary>
    public static Vec2Long GetGridSize()
    {
        return _gridSize;
    }
    
}
