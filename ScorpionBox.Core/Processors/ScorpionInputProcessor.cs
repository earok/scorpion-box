using Microsoft.Xna.Framework.Input;
using SK.Libretro;
using static SK.Libretro.Wrapper;

namespace ScorpionBox.Core.Processors;
internal class ScorpionInputProcessor : IInputProcessor
{
    private ScorpionBoxGame _box;
    private float DeadZone = 0.1f;

    public ScorpionInputProcessor(ScorpionBoxGame scorpionBoxGame)
    {
        _box = scorpionBoxGame;
    }

    public bool JoypadButton(int port, int button)
    {
        var state = Keyboard.GetState();
        if (_box.KeyDictionary.TryGetValue(port, out var portData))
        {
            if (portData.TryGetValue((retro_device_id_joypad)button, out var key))
            {
                if (state.IsKeyDown(key))
                {
                    return true;
                }
            }
        }

        //If we get to here, we should try the gamepad instead
        var gpState = GamePad.GetState(port);
        if (gpState.IsConnected)
        {
            switch ((retro_device_id_joypad)button)
            {
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_LEFT:
                    return gpState.DPad.Left == ButtonState.Pressed || gpState.ThumbSticks.Left.X < -DeadZone;
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_RIGHT:
                    return gpState.DPad.Right == ButtonState.Pressed || gpState.ThumbSticks.Left.X > DeadZone;
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_DOWN:
                    return gpState.DPad.Down == ButtonState.Pressed || gpState.ThumbSticks.Left.Y < -DeadZone;
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_UP:
                    return gpState.DPad.Up == ButtonState.Pressed || gpState.ThumbSticks.Left.Y > DeadZone;
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_A:
                    return gpState.IsButtonDown(Buttons.A);
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_B:
                    return gpState.IsButtonDown(Buttons.B);
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_X:
                    return gpState.IsButtonDown(Buttons.X);
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_Y:
                    return gpState.IsButtonDown(Buttons.Y);
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_START:
                    return gpState.IsButtonDown(Buttons.Start);
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_SELECT:
                    return gpState.IsButtonDown(Buttons.Back);
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_L:
                    return gpState.IsButtonDown(Buttons.LeftShoulder);
                case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_R:
                    return gpState.IsButtonDown(Buttons.RightShoulder);
            }
        }

        return false;
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
