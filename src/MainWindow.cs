﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ElectroSim.Content;
using ElectroSim.Gui;
using ElectroSim.Gui.MenuElements;
using ElectroSim.Maths;
using ElectroSim.Maths.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using Component = ElectroSim.Content.Component;

namespace ElectroSim;

public class MainWindow : Game
{
    // debug/testing
    private Component _activeBrush = Registry.Components.Capacitor.GetVariant(1e-6);
    
    
    // rendering
    private readonly GraphicsDeviceManager _graphics;
    private static SpriteBatch _spriteBatch;

    // game logic
    private readonly BlockMatrix<Component> _components = new(null, new Vec2Long(4611686018427387904, 4611686018427387904));
    private readonly List<Menu> _menus = new();
    
    private static readonly List<Component> Brush = new();
    private static Rectangle _brushRectangle;
    private static bool _isOverlapping;
    private static Vec2Long _initialMousePos = Vector2.Zero;
    
    private static double _scale = 1;
    private static Vec2Double _translation = Vector2.Zero;
    private static Vec2Double _prevTranslation = Vector2.Zero;

    private static Vec2Int _dimensions = Vector2.One;
    


    // controls
    private readonly bool[] _prevMouseButtons = new bool[5];
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
        
        Console.WriteLine("===============[SYSTEM CHECKS]===============");

        Console.WriteLine(Prefixes.FormatNumber(3.1e-6, Units.Get("Farad")));
        
        var voltage1 = Value.Parse("1e+28 V");
        var current1 = Value.Parse("1e-30 A");
        
        var voltage2 = Value.Parse("1.1 V");
        var current2 = Value.Parse("45 A");
        
        Console.WriteLine(voltage1 + " * " + current1 + " = " + voltage1 * current1);
        Console.WriteLine(current2 + " / " + voltage2 + " = " + current2 / voltage2);

        // write / creation test
        var matrix = new BlockMatrix<int>(0, new Vec2Long(65, 65))
        {
            [-23, -23] = 1
        };
        matrix.All((element, pos) => { Console.WriteLine(element + " @ " + pos); return true; });
        
        // rewrite test
        matrix[-23, -23] = 99;
        
        matrix.All((element, pos) => { Console.WriteLine(element + " @ " + pos); return true; });
        
        Console.WriteLine("===============[BEGIN PROGRAM]===============");
    }

    protected override void Initialize()
    {
        Brush.Add(_activeBrush.Copy());
        
        base.Initialize();
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
        _dimensions = new Vector2(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        
        
        // Controls
        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        var mouseScreenCords = new Vec2Int(mouseState.X, mouseState.Y);

        const int min = 450;
        const int max = 2400;

        _scrollWheelOffset = (mouseState.ScrollWheelValue - _scrollWheelOffset) switch
        {
            > max => mouseState.ScrollWheelValue - max,
            < min => mouseState.ScrollWheelValue - min,
            _ => _scrollWheelOffset
        };
        
        // only run controls logic when hovered
        if (mouseScreenCords.X < 0 || mouseScreenCords.X > _dimensions.X || mouseScreenCords.Y < 0 || mouseScreenCords.Y > _dimensions.Y)
            return;
        
        var mousePos = ScreenToGameCoords(mouseScreenCords);
        _scale = Math.Pow((mouseState.ScrollWheelValue - _scrollWheelOffset) / 1024f, 4);
        
        
        // _isOverlapping = Brush.Any(ComponentIntersect);

        
        foreach (var menu in _menus)
        {
            menu.CheckHover(mouseScreenCords);
        }

        // lMouse first tick
        if (mouseState.LeftButton == ButtonState.Pressed && !_prevMouseButtons[0])
        {
            if (_menus.Any(menu => menu.Click()))
            {
                UpdatePrevMouseButtons(mouseState);
                return;
            }
        }

        // !lMouse
        if (mouseState.LeftButton == ButtonState.Released && !_prevMouseButtons[0])
        {
            
        }
        
        // !lShift
        if (!keyboardState.IsKeyDown(Keys.LeftShift))
        {
            if (Brush.Count != 1)
            {
                Brush.Clear();
                var newBrush = _activeBrush.Copy();
                newBrush.SetPos(mousePos);
                Brush.Add(newBrush);
                
                _brushRectangle = GetCollisionRectangle(Brush[0]);
            }
        }
        
        // !lMouse || !lShift
        if ((mouseState.LeftButton == ButtonState.Released && !_prevMouseButtons[0]) || !keyboardState.IsKeyDown(Keys.LeftShift))
        {
            Brush[0].SetPos(mousePos);
            _brushRectangle = GetCollisionRectangle(Brush[0]);

        }
        
        // update _isOverlapping by checking if _brushRectangle intersects with any component on screen
        _isOverlapping = ComponentIntersect(_brushRectangle);

        // first tick of lMouse
        if (mouseState.LeftButton == ButtonState.Pressed && !_prevMouseButtons[0])
        {
            _initialMousePos = mousePos;

            // & !overlap & !lShift
            if (!_isOverlapping && !keyboardState.IsKeyDown(Keys.LeftShift))
            {
                _components[Brush[0].GetPos()] = Brush[0];
                
                // _components.Add(Brush[0].GetPos(), Brush[0]);
                // AddComponent(Brush[0]);
            }
        }
        
        // tick after lMouse
        if (mouseState.LeftButton == ButtonState.Released && _prevMouseButtons[0])
        {
            // & lShift
            if (keyboardState.IsKeyDown(Keys.LeftShift) && !_isOverlapping)
            {
                foreach (var brushComponent in Brush)
                {
                    _components[brushComponent.GetPos()] = brushComponent;
                    
                    // _components.Add(brushComponent.GetPos(), brushComponent);
                    // AddComponent(brushComponent);
                }
            }
            
            Brush.Clear();
            Brush.Add(_activeBrush.Copy());
            _brushRectangle = GetCollisionRectangle(Brush[0]);

        }

        // lMouse
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            
            // & lShift
            if (keyboardState.IsKeyDown(Keys.LeftShift))
            {
                Brush.Clear();

                var dist = mousePos - _initialMousePos;

                var brushSize = _activeBrush.GetSize();
                
                var signX = dist.X < 0;
                var signY = dist.Y < 0;
                var sign = new Vector2(signX ? -1 : 1, signY ? -1 : 1);
                
                
                for (var x = 0; signX ? x > dist.X : x < dist.X; x+= (int)(brushSize.X * sign.X))
                {
                    for (var y = 0; signY ? y > dist.Y : y < dist.Y; y+=(int)(brushSize.Y * sign.Y))
                    {
                        var brushComponent = _activeBrush.Copy();
                        brushComponent.SetPos(new Vector2(x + _initialMousePos.X, y +_initialMousePos.Y));

                        Brush.Add(brushComponent);
                    }
                }

                _brushRectangle = new Rectangle(
                    (int)_initialMousePos.X, 
                    (int)_initialMousePos.Y,
                    (int)(dist.X / brushSize.X), 
                    (int)(dist.Y / brushSize.Y));

                // & !lShift & !overlap
            }

        }
        
        // mMouse
        if (mouseState.MiddleButton == ButtonState.Pressed)
        {
            if (!_prevMouseButtons[2])
            {
                _middleMouseCords = mouseScreenCords;
                _prevTranslation = _translation;
            }
            else
            {
                _translation = _prevTranslation + (mouseScreenCords - _middleMouseCords) / _scale;
            }
        }
        
        // rMouse
        if (mouseState.RightButton == ButtonState.Pressed && !_prevMouseButtons[1])
        {
            _translation = Vector2.Zero;
            _scrollWheelOffset = mouseState.ScrollWheelValue - 1200;
            
            _scale = Math.Pow(Math.Min(Math.Max((mouseState.ScrollWheelValue - _scrollWheelOffset) / 1024f, 0e-4), 0e4), 4);

        }

        UpdatePrevMouseButtons(mouseState);
        
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
        
        // game coords of the top left and bottom right corners of the screen, with a small buffer to prevent culling things still within the frame
        var tlScreen = ScreenToGameCoords(new Vector2(0, 0) - new Vector2(64));
        var brScreen = ScreenToGameCoords(_dimensions + new Vec2Int(64));
        
        _components.InvokeRanged(new Range2D(tlScreen, brScreen), (component, _) =>
        {
            component.Render(_spriteBatch);
            return true;
        }, ResultComparison.Or, false);
        
        
        foreach (var brushComponent in Brush)
        {
            brushComponent.Render(_spriteBatch, (_isOverlapping ? Color.Red : Color.White) * 0.25f);
        }

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
    private bool ComponentIntersect(Rectangle rectangle)
    {
        
        var retValue = _components.InvokeRanged(
            new Range2D(rectangle.X, rectangle.Y, rectangle.Width + rectangle.X, rectangle.Height + rectangle.Y),
            (c, _) => c != null && GetCollisionRectangle(c).Intersects(rectangle), ResultComparison.Or, false);

        if (retValue == null) return false;
        return (bool)retValue;
    }


    /// <summary>
    /// Returns a rectangle representing the collision of a component
    /// </summary>
    /// <param name="component">the component to get the collision of</param>
    private static Rectangle GetCollisionRectangle(Component component)
    {
        var componentPos = component.GetPos();
        var componentSize = component.GetSize();
        
        return new Rectangle(
            (int)(componentPos.X - componentSize.X / 2f),
            (int)(componentPos.Y - componentSize.Y / 2f),
            (int)componentSize.X,
            (int)componentSize.Y
        );
    }
    
    private void UpdatePrevMouseButtons(MouseState mouseState)
    {
        _prevMouseButtons[0] = mouseState.LeftButton == ButtonState.Pressed;
        _prevMouseButtons[1] = mouseState.RightButton == ButtonState.Pressed;
        _prevMouseButtons[2] = mouseState.MiddleButton == ButtonState.Pressed;
        _prevMouseButtons[3] = mouseState.XButton1 == ButtonState.Pressed;
        _prevMouseButtons[4] = mouseState.XButton2 == ButtonState.Pressed;
    }

    
    /// <summary>
    /// Converts coords from the screen (like mouse pos) into game coords (like positions of components).
    /// </summary>
    /// <param name="screenCords">The coords from the screen to convert</param>
    private static Vec2Long ScreenToGameCoords(Vec2Int screenCords)
    {
        var center = _dimensions / 2;
        return (Vec2Long)((screenCords - center) / _scale - _translation + center);
    }
    
    /// <summary>
    /// Converts coords from the game (like positions of components) into screen coords (like mouse pos).
    /// </summary>
    /// <param name="gameCoords">The coords from the game to convert</param>
    private static Vec2Long GameToScreenCoords(Vec2Long gameCoords)
    {
        var center = _dimensions / 2f;
        return (Vec2Long)((gameCoords + _translation - center) * _scale + center);
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
    /// <returns></returns>
    public static double GetScale()
    {
        return _scale;
    }

    /// <summary>
    /// Returns the translation (pan) of the world.
    /// </summary>
    /// <returns></returns>
    public static Vec2Double GetTranslation()
    {
        return _translation;
    }

    /// <summary>
    /// Returns the screen size.
    /// </summary>
    /// <returns></returns>
    public static Vec2Int GetScreenSize()
    {
        return _dimensions;
    }


}