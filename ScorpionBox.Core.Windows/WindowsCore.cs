using Microsoft.Xna.Framework.Graphics;
using SK.Libretro.Utilities;

namespace ScorpionBox.Core.Windows;
public static class WindowsCore
{
    internal static void Start(SurfaceFormat surfaceFormat, bool useXInput)
    {
        using var game = new ScorpionBoxGame(new DllModuleWindows(), ".dll", surfaceFormat, useXInput);
        game.Run();
    }
}
