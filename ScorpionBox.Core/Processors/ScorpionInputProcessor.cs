using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using SK.Libretro;
using static SK.Libretro.Wrapper;

namespace ScorpionBox.Core.Processors;
internal class ScorpionInputProcessor : IInputProcessor
{
    private ScorpionBoxGame _scorpionBoxGame;

    public ScorpionInputProcessor(ScorpionBoxGame scorpionBoxGame)
    {
        _scorpionBoxGame = scorpionBoxGame;
    }

    public bool JoypadButton(int port, int button)
    {
        KeyboardState state = Keyboard.GetState();
        var id = (retro_device_id_joypad)button;

        return id switch
        {
            retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_A => state.IsKeyDown(Keys.X),
            retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_B => state.IsKeyDown(Keys.C),
            retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_L => state.IsKeyDown(Keys.A),
            retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_R => state.IsKeyDown(Keys.Z),
            retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_UP => state.IsKeyDown(Keys.Up),
            retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_DOWN => state.IsKeyDown(Keys.Down),
            retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_LEFT => state.IsKeyDown(Keys.Left),
            retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_RIGHT => state.IsKeyDown(Keys.Right),
            retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_START => state.IsKeyDown(Keys.Enter),
            retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_SELECT => state.IsKeyDown(Keys.RightShift),
            _ => false
        };
    }

    public bool MouseButton(int port, int button)
    {
        return false;
    }

    public float MouseDelta(int port, int axis)
    {
        return 0;
    }

    public float MouseWheelDelta(int port, int axis)
    {
        return 0;
    }
}
