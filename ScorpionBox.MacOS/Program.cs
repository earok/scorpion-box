using ScorpionBox.Core;
using SK.Libretro.Utilities;

using var game = new ScorpionBoxGame(new DllModuleMacOS(), ".dylib");
game.Run();
