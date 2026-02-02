using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ScorpionBox.Core.Localization;
using ScorpionBox.Core.Processors;
using SK.Libretro;
using SK.Libretro.Utilities;
using static SK.Libretro.Wrapper;

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
    private StreamWriter _log;

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
        var portDevices = new Dictionary<uint, uint>();
        var coreOptionsList = new CoreOptionsList();
        ReadConfig(coreOptionsList, portDevices);

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
            if(string.IsNullOrEmpty(_core))
            {
                throw new Exception("No core defined, check config.txt");
            }

            _retro.StartGame("Cores", _core, "Games", _game, coreOptionsList, portDevices);
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
        Log.Error(v.ToString());
    }

    private void ReadConfig(CoreOptionsList coreOptionsList, Dictionary<uint, uint> portDevices)
    {
        var path = "config.txt";
        if (File.Exists(path) == false)
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if(line.StartsWith('#'))
            {
                continue;
            }

            var com = line.IndexOf('#');
            if (com > -1)
            {
                line = line[..(com - 1)].Trim();
                if (line.Length == 0)
                    continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            line = line.Trim();
            if (line.Length == 0)
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

                    if (key.StartsWith("c_"))
                    {
                        key = key["c_".Length..];
                        var core = coreOptionsList.Cores.FirstOrDefault(p => p.CoreName == key);
                        if (core == null)
                        {
                            core = new CoreOptions() { CoreName = key, Options = new List<string>() };
                            coreOptionsList.Cores.Add(core);
                        }
                        core.Options.Add(value);
                    }
                    else if (key.StartsWith("key_retro_device_id"))
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
                    else if (key.StartsWith("port_"))
                    {
                        key = key["port_".Length..];
                        if (uint.TryParse(key, out var port))
                        {
                            uint device = (uint)retro_device.RETRO_DEVICE_NONE;
                            if (Enum.TryParse<retro_device>(value, out var retro))
                            {
                                //Used enum
                                device = (uint)retro;
                            }
                            else
                            {
                                //Try to read it as an exact device id
                                uint.TryParse(value, out var number);
                            }

                            portDevices[port] = device;
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


        IsFixedTimeStep = true;

        if (_retro.Game.SystemAVInfo.timing.fps > 0)
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / _retro.Game.SystemAVInfo.timing.fps);
        }

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
        Window.AllowUserResizing = true;

        _graphics.IsFullScreen = v;
        FixScreen(true);
    }

    private void FixScreen(bool isDirty)
    {
        var targetWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        var targetHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

        if (_graphics.IsFullScreen == false)
        {
            GetDimensions(out int width, out int height);
            var screenWidths = targetWidth / width;
            var screenHeights = targetHeight / height;

            //Default to the largest native scale we can get away with
            var screenScale = (int)Math.Min(screenWidths, screenHeights);
            targetWidth = (int)width * screenScale;
            targetHeight = (int)height * screenScale;
        }

        if (_graphics.PreferredBackBufferWidth != targetWidth
            && _graphics.PreferredBackBufferHeight != targetHeight)
        {
            _graphics.PreferredBackBufferWidth = targetWidth;
            _graphics.PreferredBackBufferHeight = targetHeight;
            isDirty = true;
        }

        if (isDirty)
        {
            _graphics.ApplyChanges();
        }
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
        FixScreen(false);
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