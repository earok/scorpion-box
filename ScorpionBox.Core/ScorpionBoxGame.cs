using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.Utilities;
using NAudio.MediaFoundation;
using ScorpionBox.Core.Localization;
using ScorpionBox.Core.Processors;
using SK.Libretro;
using SK.Libretro.Utilities;
using static SK.Libretro.Wrapper;
//using SK.Libretro.Utilities;

namespace ScorpionBox.Core;
/// <summary>
/// The main class for the game, responsible for managing game components, settings, 
/// and platform-specific configurations.
/// </summary>
public class ScorpionBoxGame : Game
{
    public SurfaceFormat PixelFormat;
    public Texture2D CurrentTexture;

    // Resources for drawing.
    private GraphicsDeviceManager _graphics;
    private SurfaceFormat _surfaceFormat888;
    private Wrapper _retro;
    private SpriteBatch _spriteBatch;
    private SpriteFont _hudFont;
    private string _core;
    private string _error;
    private bool _gameStarted;
    private KeyboardState _previous;
    private KeyboardState _next;

    private Keys _keyFullscreen = Keys.F11;
    private Keys _keyQuit = Keys.Escape;

    private string _game;
    private bool _startFullScreen;

    public Dictionary<int, Dictionary<retro_device_id_joypad, Keys>> KeyDictionary = [];

    /// <summary>
    /// Indicates if the game is running on a mobile platform.
    /// </summary>
    public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    /// <summary>
    /// Indicates if the game is running on a desktop platform.
    /// </summary>
    public readonly static bool IsDesktop = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

    /// <summary>
    /// Initializes a new instance of the game. Configures platform-specific settings, 
    /// initializes services like settings and leaderboard managers, and sets up the 
    /// screen manager for screen transitions.
    /// </summary>
    public ScorpionBoxGame(DllModule dll,
        string ext,
        SurfaceFormat surfaceFormat888 = SurfaceFormat.Color)
    {
        ReadConfig();

        _graphics = new GraphicsDeviceManager(this);
        _surfaceFormat888 = surfaceFormat888;

        // Share GraphicsDeviceManager as a service.
        Services.AddService(typeof(GraphicsDeviceManager), _graphics);

        Content.RootDirectory = "Content";

        // Configure screen orientations.
        _graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;


        _retro = new Wrapper(".", dll, ext);
        try
        {
            _retro.StartGame("Cores", _core, ".", _game);
            _gameStarted = true;
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void ShowError(Exception v)
    {
        _error = v.Message;
    }

    private void ReadConfig()
    {
        var path = "config.txt";
        if (File.Exists(path) == false)
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();

            if (line.Length == 0)
                continue;

            if (line.StartsWith("#"))
                continue;

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            var key = line[..eq].Trim().ToLowerInvariant();
            var value = line[(eq + 1)..].Trim();

            switch (key)
            {
                case "title":
                    Window.Title = value;
                    break;

                case "core":
                    _core = value;
                    break;

                case "game":
                    _game = value;
                    break;

                case "key_fullscreen":
                    Enum.TryParse<Keys>(value, true, out _keyFullscreen);
                    break;

                case "key_quit":
                    Enum.TryParse<Keys>(value, true, out _keyQuit);
                    break;

                case "fullscreen":
                    bool.TryParse(value, out _startFullScreen);
                    break;

                default:
                    if (key.StartsWith("key_retro_device_id"))
                    {
                        key = key["key_".Length..];
                        var lastDash = key.LastIndexOf('_');

                        //This is for a specific controller
                        if (int.TryParse(key.AsSpan(lastDash + 1), out var controller))
                        {
                            key = key[0..lastDash];
                        }
                        if (Enum.TryParse<retro_device_id_joypad>(key, true, out var result)
                            && Enum.TryParse<Keys>(value, true, out var keyResult))
                        {
                            if (KeyDictionary.ContainsKey(controller) == false)
                            {
                                KeyDictionary[controller] = [];
                            }
                            KeyDictionary[controller][result] = keyResult;
                        }

                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Initializes the game, including setting up localization and adding the 
    /// initial screens to the ScreenManager.
    /// </summary>
    protected override void Initialize()
    {
        SetFullscreen(_startFullScreen);

        PixelFormat = _retro.Game.PixelFormat switch
        {
            retro_pixel_format.RETRO_PIXEL_FORMAT_RGB565 => SurfaceFormat.Bgr565,
            retro_pixel_format.RETRO_PIXEL_FORMAT_0RGB1555 => SurfaceFormat.Bgra5551,
            retro_pixel_format.RETRO_PIXEL_FORMAT_XRGB8888 => _surfaceFormat888,
            _ => SurfaceFormat.Bgr565,
        };

        _retro.ActivateGraphics(new ScorpionGraphicsProcessor(this));
        _retro.ActivateInput(new ScorpionInputProcessor(this));

        var processor = new NAudioAudioProcessor();
        processor.Init((int)_retro.Game.SystemAVInfo.timing.sample_rate);
        _retro.ActivateAudio(processor);

        // Load supported languages and set the default language.
        List<CultureInfo> cultures = LocalizationManager.GetSupportedCultures();
        var languages = new List<CultureInfo>();
        for (int i = 0; i < cultures.Count; i++)
        {
            languages.Add(cultures[i]);
        }

        // TODO You should load this from a settings file or similar,
        // based on what the user or operating system selected.
        var selectedLanguage = LocalizationManager.DEFAULT_CULTURE_CODE;
        LocalizationManager.SetCulture(selectedLanguage);

        base.Initialize();
    }

    private void SetFullscreen(bool v)
    {
        GetDimensions(out int width, out int height);

        var nativeWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        var nativeHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

        _graphics.IsFullScreen = v;
        if (v)
        {
            _graphics.PreferredBackBufferWidth = nativeWidth;
            _graphics.PreferredBackBufferHeight = nativeHeight;
        }
        else
        {
            Window.AllowUserResizing = true;
            var screenWidths = nativeWidth / width;
            var screenHeights = nativeHeight / height;

            //Default to the largest native scale we can get away with
            var screenScale = (int)Math.Min(screenWidths, screenHeights);
            _graphics.PreferredBackBufferWidth = (int)width * screenScale;
            _graphics.PreferredBackBufferHeight = (int)height * screenScale;
        }

        _graphics.ApplyChanges();
    }

    private void GetDimensions(out int width, out int height)
    {
        width = (int)_retro.Game.SystemAVInfo.geometry.base_width;
        height = (int)_retro.Game.SystemAVInfo.geometry.base_height;

        if (width <= 0 || height <= 0)
        {
            //Default dimensions
            width = 320;
            height = 224;
        }
    }

    /// <summary>
    /// Loads game content, such as textures and particle systems.
    /// </summary>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _hudFont = Content.Load<SpriteFont>("Fonts/Hud");
    }

    /// <summary>
    /// Updates the game's logic, called once per frame.
    /// </summary>
    /// <param name="gameTime">
    /// Provides a snapshot of timing values used for game updates.
    /// </param>
    protected override void Update(GameTime gameTime)
    {
        _retro.Update();

        _next = Keyboard.GetState();
        if (KeyPressed(_keyFullscreen))
        {
            SetFullscreen(!_graphics.IsFullScreen);
        }
        if (KeyPressed(_keyQuit))
        {
            Exit();
        }
        _previous = _next;

        base.Update(gameTime);
    }

    private bool KeyPressed(Keys key)
    {
        return _next.IsKeyDown(key) && _previous.IsKeyUp(key);
    }

    /// <summary>
    /// Draws the game's graphics, called once per frame.
    /// </summary>
    /// <param name="gameTime">
    /// Provides a snapshot of timing values used for rendering.
    /// </param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Transparent);

        if (CurrentTexture == null)
        {
            //Make sure we always have a texture ready
            GetDimensions(out var w, out var h);
            CurrentTexture = new Texture2D(GraphicsDevice, w, h);
        }

        _spriteBatch.Begin();
        var texture = CurrentTexture;

        var widthRatio = (double)Window.ClientBounds.Width / texture.Width;
        var heightRatio = (double)Window.ClientBounds.Height / texture.Height;

        var width = (widthRatio < heightRatio ? widthRatio : heightRatio) * texture.Width;
        var height = (widthRatio < heightRatio ? widthRatio : heightRatio) * texture.Height;

        var posX = (Window.ClientBounds.Width - width) / 2;
        var posY = (Window.ClientBounds.Height - height) / 2;

        _spriteBatch.Draw(texture, new Rectangle((int)posX, (int)posY, (int)width, (int)height), Color.White);

        if (string.IsNullOrWhiteSpace(_error) == false)
        {
            _spriteBatch.DrawString(_hudFont, _error, Vector2.Zero, Color.White);
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _retro.StopGame();
        }

        base.Dispose(disposing);
    }

}