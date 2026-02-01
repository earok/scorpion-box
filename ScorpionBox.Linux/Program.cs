using ScorpionBox.Core;
using SK.Libretro.Utilities;

using var game = new ScorpionBoxGame(new DllModuleLinux(), "so");
game.Run();
