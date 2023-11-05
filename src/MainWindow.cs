using System;
using System.Collections.Generic;
using System.Linq;
using ElectroSim.Maths;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Component = ElectroSim.Content.Component;

namespace ElectroSim;

public class MainWindow : Game
{
    // Debug/Dev
    private readonly Component _activeBrush = ElectroSim.Components.Capacitor.GetVariant(1e-6);
    
    // rendering
    private readonly GraphicsDeviceManager _graphics;
    private static SpriteBatch _spriteBatch;

    // game logic
    private readonly Dictionary<Vector2,List<Component>> _components = new();
    
    
    private static readonly List<Component> Brush = new();
    private static bool _isOverlapping;
    private static Vector2 _initialMousePos = Vector2.Zero;
    
    private static double _scale = 1;
    private static Vector2 _translation = Vector2.Zero;
    private static Vector2 _prevTranslation = Vector2.Zero;

    private static Vector2 _dimensions = Vector2.One;
    


    // controls
    private readonly bool[] _prevMouseButtons = new bool[5];
    private Vector2 _middleMouseCords = Vector2.Zero;
    private int _scrollWheelOffset = -1200;
    

    public MainWindow()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "assets";
        IsMouseVisible = true;
        
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnResize;
        
        Console.WriteLine(Prefixes.FormatNumber(3.1e-6, Units.Farad));
        
    }

    protected override void Initialize()
    {
        Brush.Add(_activeBrush.Copy());
        
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        RegisterTextures(new[]
        {
            "missing",
            "chip",
            "component"
        });
    }

    /// <summary>
    /// The game logic loop.
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

        var mouseScreenCords = new Vector2(mouseState.X, mouseState.Y);

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
        
        
        _isOverlapping = Brush.Any(ComponentIntersect);
        
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
            }
        }
        
        // !lMouse || !lShift
        if ((mouseState.LeftButton == ButtonState.Released && !_prevMouseButtons[0]) || !keyboardState.IsKeyDown(Keys.LeftShift))
        {
            Brush[0].SetPos(mousePos);
        }
        
        _isOverlapping = Brush.Any(ComponentIntersect);

        // first tick of lMouse
        if (mouseState.LeftButton == ButtonState.Pressed && !_prevMouseButtons[0])
        {
            _initialMousePos = mousePos;

            // & !overlap & !lShift
            if (!_isOverlapping && !keyboardState.IsKeyDown(Keys.LeftShift))
            {
                AddComponent(Brush[0]);
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
                    AddComponent(brushComponent);
                }
            }
            
            Brush.Clear();
            Brush.Add(_activeBrush.Copy());
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
                _translation = _prevTranslation + (mouseScreenCords - _middleMouseCords) / (float)_scale;
            }
        }
        
        // rMouse
        if (mouseState.RightButton == ButtonState.Pressed && !_prevMouseButtons[1])
        {
            _translation = Vector2.Zero;
            _scrollWheelOffset = mouseState.ScrollWheelValue - 1200;
            
            _scale = Math.Pow(Math.Min(Math.Max((mouseState.ScrollWheelValue - _scrollWheelOffset) / 1024f, 0e-4), 0e4), 4);

        }
        
        
        _prevMouseButtons[0] = mouseState.LeftButton == ButtonState.Pressed;
        _prevMouseButtons[1] = mouseState.RightButton == ButtonState.Pressed;
        _prevMouseButtons[2] = mouseState.MiddleButton == ButtonState.Pressed;
        _prevMouseButtons[3] = mouseState.XButton1 == ButtonState.Pressed;
        _prevMouseButtons[4] = mouseState.XButton2 == ButtonState.Pressed;

        
        base.Update(gameTime);
    }

    /// <summary>
    /// The draw loop.
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        // only run when focused
        if (!IsActive)
            return;
        
        GraphicsDevice.Clear(Colors.CircuitBackground);
        
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        #region Culling
        
        var center = ScreenToGameCoords(_dimensions / 2f);
        
        var gridPos = center / Constants.CollisionGridSize;
        gridPos.Ceiling();
         
        var gridWidth = (int)(_dimensions.X / _scale / Constants.CollisionGridSize);
        var gridHeight = (int)(_dimensions.Y / _scale / Constants.CollisionGridSize);
         
        var visibleComponents = new List<List<Component>>();
        
        for (var x = -(int)Math.Floor(gridWidth/2f)-2; x <= (int)Math.Ceiling(gridWidth/2f)+2; x++)
        {
            for (var y = -(int)Math.Floor(gridHeight/2f)-2; y <= (int)Math.Ceiling(gridHeight/2f)+2; y++)
            {
                if (!_components.TryGetValue(new Vector2(x + gridPos.X, y + gridPos.Y), out var collisionSquare))
                    continue;
                
                visibleComponents.Add(collisionSquare);
            }
        }
        
        #endregion
        
        foreach (var component in visibleComponents.SelectMany(a => a))
        {
            component.Render(_spriteBatch);
        }
        visibleComponents.Clear();
        
        foreach (var brushComponent in Brush)
        {
            brushComponent.Render(_spriteBatch, (_isOverlapping ? Color.Red : Color.White) * 0.25f);
        }

        // TODO: Menus / Brush Selection
        
        _spriteBatch.End();
        
        
        base.Draw(gameTime);
    }

    /// <summary>
    /// Returns true if the specified component intersects with another component.
    /// </summary>
    /// <param name="component">The component to check for an intersection</param>
    private bool ComponentIntersect(Component component)
    {
        var pos = component.GetPos();
        var size = component.GetSize();
        
        var componentRectangle = new Rectangle(
            (int)(pos.X - size.X / 2f),
            (int)(pos.Y - size.X / 2f),
            (int)size.X,
            (int)size.Y);

        var collisionRectangles = GetCollisionRectangles(component.GetPos());
        return collisionRectangles.Any(rect => rect.Intersects(componentRectangle));
    }
    
    
    /// <summary>
    /// Returns the rectangles of all components around the specified point.
    /// </summary>
    /// <param name="pos">The point within the center square of a 3x3 grid in which to return all the contained collision boxes</param>
    private IEnumerable<Rectangle> GetCollisionRectangles(Vector2 pos)
    {
        var gridPos = pos / Constants.CollisionGridSize;
        gridPos.Ceiling();

        var collisionComponents = new List<List<Component>>();

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                collisionComponents.Add(
                    _components.TryGetValue(
                        new Vector2(x + gridPos.X, y + gridPos.Y),
                        out var collisionSquare)
                        ? collisionSquare : new List<Component>()
                    );
            }
        }

        return from component in collisionComponents.SelectMany(collisionBoxes => collisionBoxes)
            let componentPos = component.GetPos()
            let componentSize = component.GetSize()
            select new Rectangle(
                (int)(componentPos.X - componentSize.X / 2f),
                (int)(componentPos.Y - componentSize.Y / 2f),
                (int)componentSize.X,
                (int)componentSize.Y
                );
    }

    private void AddComponent(Component component)
    {
        var gridPos = component.GetPos() / Constants.CollisionGridSize;
        gridPos.Ceiling();
        
        if (!_components.ContainsKey(gridPos))
            _components.Add(gridPos, new List<Component>());
        
        _components[gridPos].Add(component.Copy());
    }
    
    /// <summary>
    /// Converts coords from the screen (like mouse pos) into game coords (like positions of components).
    /// </summary>
    /// <param name="screenCords">The coords from the screen to convert</param>
    private static Vector2 ScreenToGameCoords(Vector2 screenCords)
    {
        var center = _dimensions / 2f;
        return (screenCords - center) / (float)_scale - _translation + center;
    }
    
    /// <summary>
    /// Converts coords from the game (like positions of components) into screen coords (like mouse pos).
    /// </summary>
    /// <param name="gameCoords">The coords from the game to convert</param>
    private static Vector2 GameToScreenCoords(Vector2 gameCoords)
    {
        var center = _dimensions / 2f;
        return new Vector2((float)_scale) * (gameCoords + _translation - center) + center;
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
    /// Register a single texture and store it in Textures.
    /// </summary>
    /// <param name="name">The name of the texture</param>
    private void RegisterTexture(string name)
    {
        Textures.RegisterTexture(name, Content.Load<Texture2D>("textures/" + name));
    }
    

    /// <summary>
    /// Register multiple textures and store them in Textures.
    /// </summary>
    /// <param name="names">An array of the names of the textures to load</param>
    private void RegisterTextures(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            RegisterTexture(name);
        }
    }
    
    
    // public methods

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
    public static Vector2 GetTranslation()
    {
        return _translation;
    }

    /// <summary>
    /// Returns the screen size.
    /// </summary>
    /// <returns></returns>
    public static Vector2 GetScreenSize()
    {
        return _dimensions;
    }
}