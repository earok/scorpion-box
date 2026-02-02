using Microsoft.Xna.Framework.Graphics;
using SK.Libretro.Utilities;

namespace ScorpionBox.Core.Windows
{
    public static class WindowsCore
    {
        internal static void Start(SurfaceFormat surfaceFormat)
        {
            using var game = new ScorpionBoxGame(new DllModuleWindows(), ".dll", surfaceFormat);
            game.Run();
        }
    }
}
