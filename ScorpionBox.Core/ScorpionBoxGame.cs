using System;
using System.Collections.Generic;
using System.Globalization;
using Libretro.NET;
using Libretro.NET.Bindings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ScorpionBox.Core.Localization;
using SK.Libretro;

namespace ScorpionBox.Core;
/// <summary>
/// The main class for the game, responsible for managing game components, settings, 
/// and platform-specific configurations.
/// </summary>
public class ScorpionBoxGame : Game
{
    // Resources for drawing.
    private GraphicsDeviceManager _graphics;
    private RetroWrapper _retro;
    private SurfaceFormat _pixelFormat;
    private Texture2D _currentTexture;
    private SpriteBatch _spriteBatch;
    private bool _isRunning = true;

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
    public ScorpionBoxGame()
    {
        _graphics = new GraphicsDeviceManager(this);

        // Share GraphicsDeviceManager as a service.
        Services.AddService(typeof(GraphicsDeviceManager), _graphics);

        Content.RootDirectory = "Content";

        // Configure screen orientations.
        _graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;

        _retro = new RetroWrapper();
        _retro.LoadCore("blastem_libretro.dll");
        _retro.LoadGame("game.bin");
    }

    /// <summary>
    /// Initializes the game, including setting up localization and adding the 
    /// initial screens to the ScreenManager.
    /// </summary>
    protected override void Initialize()
    {
        Window.AllowUserResizing = true;

        _graphics.PreferredBackBufferWidth = (int)_retro.Width * 4;
        _graphics.PreferredBackBufferHeight = (int)_retro.Height * 4;
        _graphics.ApplyChanges();

        _pixelFormat = _retro.PixelFormat switch
        {
            retro_pixel_format.RETRO_PIXEL_FORMAT_RGB565 => SurfaceFormat.Bgr565,
            retro_pixel_format.RETRO_PIXEL_FORMAT_0RGB1555 => SurfaceFormat.Bgra5551,
            retro_pixel_format.RETRO_PIXEL_FORMAT_XRGB8888 => SurfaceFormat.Bgra32,
            retro_pixel_format.RETRO_PIXEL_FORMAT_UNKNOWN => SurfaceFormat.Bgr565,
            _ => SurfaceFormat.Bgr565,
        };

        _retro.OnFrame = OnFrame;
        _retro.OnCheckInput = OnCheckInput;

        var processor = new NAudioAudioProcessor();
        processor.Init((int)_retro.SampleRate);
        _retro.OnSample = processor.ProcessSamples;

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

    private bool OnCheckInput(uint port, uint device, uint index, uint id)
    {
        KeyboardState state = Keyboard.GetState();

        return id switch
        {
            RetroBindings.RETRO_DEVICE_ID_JOYPAD_A => state.IsKeyDown(Keys.X),
            RetroBindings.RETRO_DEVICE_ID_JOYPAD_B => state.IsKeyDown(Keys.C),
            RetroBindings.RETRO_DEVICE_ID_JOYPAD_L => state.IsKeyDown(Keys.A),
            RetroBindings.RETRO_DEVICE_ID_JOYPAD_R => state.IsKeyDown(Keys.Z),
            RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP => state.IsKeyDown(Keys.Up),
            RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN => state.IsKeyDown(Keys.Down),
            RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT => state.IsKeyDown(Keys.Left),
            RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT => state.IsKeyDown(Keys.Right),
            RetroBindings.RETRO_DEVICE_ID_JOYPAD_START => state.IsKeyDown(Keys.Enter),
            RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT => state.IsKeyDown(Keys.RightShift),
            _ => false
        };
    }

    private void OnSample(byte[] sample)
    {
        //To implement
    }

    private void OnFrame(byte[] frame, uint width, uint height)
    {
        if (_currentTexture != null) _currentTexture.Dispose();
        _currentTexture = new Texture2D(GraphicsDevice, (int)width, (int)height, false, _pixelFormat);
        _currentTexture.SetData(frame);
    }

    /// <summary>
    /// Loads game content, such as textures and particle systems.
    /// </summary>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    /// <summary>
    /// Updates the game's logic, called once per frame.
    /// </summary>
    /// <param name="gameTime">
    /// Provides a snapshot of timing values used for game updates.
    /// </param>
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        _retro.Run();

        base.Update(gameTime);
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

        var texture = _currentTexture;

        var widthRatio = (double)Window.ClientBounds.Width / texture.Width;
        var heightRatio = (double)Window.ClientBounds.Height / texture.Height;

        var width = (widthRatio < heightRatio ? widthRatio : heightRatio) * texture.Width;
        var height = (widthRatio < heightRatio ? widthRatio : heightRatio) * texture.Height;

        var posX = (Window.ClientBounds.Width - width) / 2;
        var posY = (Window.ClientBounds.Height - height) / 2;

        _spriteBatch.Begin();
        _spriteBatch.Draw(texture, new Rectangle((int)posX, (int)posY, (int)width, (int)height), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        _isRunning = false;
        if (disposing)
        {
            _retro.Dispose();
        }

        base.Dispose(disposing);
    }

}