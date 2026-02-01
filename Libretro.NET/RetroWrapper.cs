using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using Libretro.NET.Bindings;

namespace Libretro.NET
{
    /// <summary>
    /// Wraps all (most? (necessary?)) libretro mechanisms used to run a core and a game.
    /// After creation, <see cref="LoadCore(string)"/> and then <see cref="LoadGame(string)"/> must be called before anything else.
    /// </summary>
    public unsafe class RetroWrapper : IDisposable
    {
        private const float _floatScale = 0.000030517578125f;
        private IRetro _interop;

        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public double FPS { get; private set; }
        public double SampleRate { get; private set; }
        public retro_pixel_format PixelFormat { get; private set; }

        //Ensures that 8888 works
        public int BytesPerPixel = 4;
        public int PerformanceLevel;
        private const int RETRO_API_VERSION = 1;

        public delegate void OnFrameDelegate(byte[] frame, uint width, uint height);
        public OnFrameDelegate OnFrame { get; set; }

        public delegate void OnSampleDelegate(float[] sample);
        public OnSampleDelegate OnSample { get; set; }

        public delegate bool OnCheckInputDelegate(uint port, uint device, uint index, uint id);
        public OnCheckInputDelegate OnCheckInput { get; set; }

        public void LoadCore(string corePath)
        {
            _interop = NativeDispatchProxy.Create<IRetro>(corePath, "retro_");

            _interop.set_environment(Environment);
            _interop.set_video_refresh(VideoRefresh);
            _interop.set_audio_sample(AudioSample);
            _interop.set_audio_sample_batch(AudioSampleBatch);
            _interop.set_input_poll(InputPoll);
            _interop.set_input_state(InputState);
            _interop.init();

            var x = _interop.api_version();
        }

        public bool LoadGame(string gamePath)
        {
            var game = new retro_game_info
            {
                path = (sbyte*)Marshal.StringToHGlobalAuto(gamePath),
                size = (UIntPtr)new FileInfo(gamePath).Length
            };

            var system = new retro_system_info();
            _interop.get_system_info(ref system);

            if (!system.need_fullpath)
            {
                game.data = (void*)Marshal.AllocHGlobal((int)game.size);
                Marshal.Copy(File.ReadAllBytes(gamePath), 0, (IntPtr)game.data, (int)game.size);
            }

            var result = _interop.load_game(ref game);

            var av = new retro_system_av_info();
            _interop.get_system_av_info(ref av);

            Width = av.geometry.base_width > 0 ? av.geometry.base_width : av.geometry.max_width;
            Height = av.geometry.base_height > 0 ? av.geometry.base_height : av.geometry.max_height;
            FPS = av.timing.fps;
            SampleRate = av.timing.sample_rate;

            return result == 1;
        }

        public void Run()
        {
            _interop.run();
        }

        private bool Environment(uint cmd, void* data)
        {
            //Expanded from https://raw.githubusercontent.com/humbertodias/RetroUnityFE/e08e9c46cd7279ded55815d761f7d9d3944278b8/Assets/Libretro/Scripts/Wrapper/LibretroEnvironment.cs
            //To support additional options

            Debug.WriteLine("Cmd " + cmd);

            switch (cmd)
            {
                case RetroBindings.RETRO_ENVIRONMENT_GET_SYSTEM_DIRECTORY:
                    {
                        char** cb = (char**)data;
                        *cb = (char*)Marshal.StringToHGlobalAuto(".");
                    }
                    break;

                case RetroBindings.RETRO_ENVIRONMENT_SET_PIXEL_FORMAT:
                    {
                        PixelFormat = (retro_pixel_format)(*(byte*)data);
                        switch (PixelFormat)
                        {
                            case retro_pixel_format.RETRO_PIXEL_FORMAT_XRGB8888:
                                BytesPerPixel = 4;
                                break;

                            default:
                                BytesPerPixel = 2;
                                break;
                        }
                    }
                    break;

                case RetroBindings.RETRO_ENVIRONMENT_GET_LOG_INTERFACE:
                    {
                        retro_log_callback* cb = (retro_log_callback*)data;
                        cb->log = NativeDispatchProxy.Register<retro_log_printf_t>(Log);
                    }
                    break;

                case RetroBindings.RETRO_ENVIRONMENT_GET_CAN_DUPE:
                    {
                        return *(bool*)data = true;
                    }

                case RetroBindings.RETRO_ENVIRONMENT_SET_FRAME_TIME_CALLBACK:
                    {
                        retro_frame_time_callback* cb = (retro_frame_time_callback*)data;
                        cb->callback = NativeDispatchProxy.Register<retro_frame_time_callback_t>(Time);
                    }
                    break;

                case RetroBindings.RETRO_ENVIRONMENT_GET_VARIABLE_UPDATE:
                    {
                        bool* outVariableUpdate = (bool*)data;
                        *outVariableUpdate = false;
                    }
                    break;

                case RetroBindings.RETRO_ENVIRONMENT_GET_CORE_OPTIONS_VERSION:
                    {
                        uint* outVersion = (uint*)data;
                        *outVersion = RETRO_API_VERSION;
                    }
                    break;

                case RetroBindings.RETRO_ENVIRONMENT_SET_PERFORMANCE_LEVEL:
                    {
                        int* inPerformanceLevel = (int*)data;
                        PerformanceLevel = *inPerformanceLevel;
                    }
                    break;


                case RetroBindings.RETRO_ENVIRONMENT_GET_VARIABLE:
                    retro_variable* outVariable = (retro_variable*)data;
                    string key = CharsToString(outVariable->key);
                    Debug.WriteLine(key);
                    return false;

                //Various methods not implemented at all
                case RetroBindings.RETRO_ENVIRONMENT_GET_LANGUAGE:
                case RetroBindings.RETRO_ENVIRONMENT_SET_CORE_OPTIONS_INTL:
                case RetroBindings.RETRO_ENVIRONMENT_GET_DISK_CONTROL_INTERFACE_VERSION:
                case RetroBindings.RETRO_ENVIRONMENT_SET_DISK_CONTROL_INTERFACE:
                    return false;

                default:
                    Debug.WriteLine("Unimplemented cmd " + cmd);
                    return false;
            }
            return true;
        }

        private void VideoRefresh(void* data, uint width, uint height, UIntPtr pitch)
        {
            int totalWidth = (int)width * BytesPerPixel;

            byte[] raw = new byte[(uint)pitch * height];
            Marshal.Copy((IntPtr)data, raw, 0, (int)pitch * (int)height);

            byte[] result = new byte[totalWidth * height];
            var destinationIndex = 0;
            for (var sourceIndex = 0; sourceIndex < (uint)pitch * height; sourceIndex += (int)pitch)
            {
                Array.Copy(raw, sourceIndex, result, destinationIndex, totalWidth);
                destinationIndex += totalWidth;
            }

            OnFrame?.Invoke(result, width, height);
        }

        private void InputPoll()
        {
            //Am I supposed to do something?
        }

        private short InputState(uint port, uint device, uint index, uint id)
        {
            return OnCheckInput?.Invoke(port, device, index, id) ?? false ? (short)1 : (short)0;
        }

        private void AudioSample(short left, short right)
        {
            var audio = new float[2];
            audio[0] = ClampAudio(left);
            audio[1] = ClampAudio(right);
            OnSample?.Invoke(audio);
        }

        private UIntPtr AudioSampleBatch(short* data, UIntPtr frames)
        {
            var floatBuffer = new float[(int)frames * 2];
            for (var i = 0; i < floatBuffer.Length; ++i)
            {
                floatBuffer[i] = ClampAudio(data[i]);
            }
            OnSample?.Invoke(floatBuffer);
            return frames;
        }

        private float ClampAudio(float v)
        {
            v *= _floatScale;
            if (v < -1)
            {
                return -1;
            }
            if (v > 1)
            {
                return 1;
            }
            return v;
        }

        private void Log(retro_log_level level, sbyte* fmt)
        {
            //Hard to log anything relevant without varargs support.
        }

        private void Time(long usec)
        {
            //Nothing relevant to do yet...
        }

        public void Dispose()
        {
            _interop.deinit();
            NativeDispatchProxy.Dispose(_interop);
        }

        public unsafe static string CharsToString(sbyte* str)
        {
            return Marshal.PtrToStringAnsi((IntPtr)str);
        }

    }
}
