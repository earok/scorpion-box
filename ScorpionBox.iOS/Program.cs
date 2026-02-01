using System;
using Foundation;
using UIKit;
using ScorpionBox.Core;
using SK.Libretro.Utilities;

namespace ScorpionBox.iOS;
[Register("AppDelegate")]
internal class Program : UIApplicationDelegate
{
    internal static void RunGame()
    {
        var game = new ScorpionBoxGame("dylib",new DllModuleIOS());
        game.Run();
    }

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(Program));
    }

    public override void FinishedLaunching(UIApplication app)
    {
        RunGame();
    }
}
