using System;
using System.Collections.Generic;
using System.Linq;
using LibRetroFE_WrapperOnly.Compatibility;
using Microsoft.Xna.Framework.Input;
using SK.Libretro;
using static SK.Libretro.Wrapper;

namespace ScorpionBox.Core.Processors;
internal class ScorpionInputProcessor : IInputProcessor
{
    private ScorpionBoxGame _box;
    private bool _useXInput;
    private float DeadZone = 0.1f;
    private int AxisDeadZone = (int)(32767 * 0.5f);
    private HashSet<Tuple<int, int>> StableJoystickAxes = new HashSet<Tuple<int, int>>();

    public ScorpionInputProcessor(ScorpionBoxGame scorpionBoxGame, bool useXInput)
    {
        _box = scorpionBoxGame;
        _useXInput = useXInput;
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

        if (_useXInput)
        {
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
        }
        else
        {
            var joystick = Joystick.GetState(port);
            if (joystick.IsConnected)
            {
                switch ((retro_device_id_joypad)button)
                {
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_LEFT:
                        return TestJoystickAxis(joystick, port, 0) < 0;
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_RIGHT:
                        return TestJoystickAxis(joystick, port, 0) > 0;
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_DOWN:
                        return TestJoystickAxis(joystick, port, 1) > 0;
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_UP:
                        return TestJoystickAxis(joystick, port, 1) > 0;
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_A:
                        return TestJoystickButton(joystick, 0);
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_B:
                        return TestJoystickButton(joystick, 1);
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_X:
                        return TestJoystickButton(joystick, 2);
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_Y:
                        return TestJoystickButton(joystick, 3);
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_L:
                        return TestJoystickButton(joystick, 4);
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_R:
                        return TestJoystickButton(joystick, 5);
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_START:
                        return TestJoystickButton(joystick, 6);
                    case retro_device_id_joypad.RETRO_DEVICE_ID_JOYPAD_SELECT:
                        return TestJoystickButton(joystick, 7);
                }
            }
        }

        return false;
    }

    private int TestJoystickAxis(JoystickState joystick, int port, int axis)
    {
        //Check the "hats" (dpads)
        foreach (var hat in joystick.Hats)
        {
            if (axis == 0)
            {
                if (hat.Left == ButtonState.Pressed)
                {
                    return -1;
                }
                if (hat.Right == ButtonState.Pressed)
                {
                    return 1;
                }
            }
            else
            {
                if (hat.Up == ButtonState.Pressed)
                {
                    return 1;
                }
                if (hat.Down == ButtonState.Pressed)
                {
                    return -1;
                }
            }
        }

        while (axis < joystick.Axes.Length)
        {
            var result = joystick.Axes[axis];
            var index = new Tuple<int, int>(port, axis);

            //We'll consider an axis stable if it's at zero
            if (StableJoystickAxes.Contains(index) == false)
            {
                //Not stable yet
                if (result > AxisDeadZone || result < -AxisDeadZone)
                {
                    return 0;
                }
                StableJoystickAxes.Add(index);
            }

            if (result > AxisDeadZone)
            {
                return 1;
            }
            if (result < -AxisDeadZone)
            {
                return -1;
            }
            axis += 2;
        }

        return 0;
    }

    private bool TestJoystickButton(JoystickState joystick, int index)
    {
        if (index < joystick.Buttons.Count()
            && joystick.Buttons[index] == ButtonState.Pressed)
        {
            return true;
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
