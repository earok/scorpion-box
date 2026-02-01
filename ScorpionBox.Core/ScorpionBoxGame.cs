#define Scorpio

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
    private Wrapper _retro;
    private SpriteBatch _spriteBatch;

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
    public ScorpionBoxGame(DllModule dll, string ext)
    {
        _graphics = new GraphicsDeviceManager(this);

        // Share GraphicsDeviceManager as a service.
        Services.AddService(typeof(GraphicsDeviceManager), _graphics);

        Content.RootDirectory = "Content";

        // Configure screen orientations.
        _graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;


        _retro = new Wrapper(".", dll, ext);
        if (_retro.StartGame("Cores", "genesis_plus_gx_wide", ".", "game") == false)
        {
            throw new Exception("Could not start game");
        }
    }

    /// <summary>
    /// Initializes the game, including setting up localization and adding the 
    /// initial screens to the ScreenManager.
    /// </summary>
    protected override void Initialize()
    {
        var width = _retro.Game.SystemAVInfo.geometry.base_width;
        var height = _retro.Game.SystemAVInfo.geometry.base_height;

        Window.AllowUserResizing = true;
        var screenWidths = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width / width;
        var screenHeights = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height / height;

        //Default to the largest native scale we can get away with
        var screenScale = (int)Math.Min(screenWidths, screenHeights);

        _graphics.PreferredBackBufferWidth = (int)width * screenScale;
        _graphics.PreferredBackBufferHeight = (int)height * screenScale;
        _graphics.ApplyChanges();

        PixelFormat = _retro.Game.PixelFormat switch
        {
            retro_pixel_format.RETRO_PIXEL_FORMAT_RGB565 => SurfaceFormat.Bgr565,
            retro_pixel_format.RETRO_PIXEL_FORMAT_0RGB1555 => SurfaceFormat.Bgra5551,
            retro_pixel_format.RETRO_PIXEL_FORMAT_XRGB8888 => SurfaceFormat.Bgra32,
            //            retro_pixel_format.RETRO_PIXEL_FORMAT_UNKNOWN => SurfaceFormat.Bgr565,
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

        _retro.Update();

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

        if (CurrentTexture == null)
        {
            return;
        }

        var texture = CurrentTexture;

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
        if (disposing)
        {
            //            _retro.Dispose();
        }

        base.Dispose(disposing);
    }

}