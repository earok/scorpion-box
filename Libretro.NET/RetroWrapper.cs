using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Libretro.NET.Bindings;

namespace Libretro.NET
{
    /// <summary>
    /// Wraps all (most? (necessary?)) libretro mechanisms used to run a core and a game.
    /// After creation, <see cref="LoadCore(string)"/> and then <see cref="LoadGame(string)"/> must be called before anything else.
    /// </summary>
    public unsafe class RetroWrapper : IDisposable
    {
        private IRetro _interop;

        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public double FPS { get; private set; }
        public double SampleRate { get; private set; }
        public retro_pixel_format PixelFormat { get; private set; }

        //Ensures that 8888 works
        public int BytesPerPixel = 4;

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
            _interop.set_input_poll(InputPoll);
            _interop.set_input_state(InputState);
            _interop.set_audio_sample(AudioSample);
            _interop.set_audio_sample_batch(AudioSampleBatch);
            _interop.init();
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

            Width = av.geometry.base_width;
            Height = av.geometry.base_height;
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
            switch (cmd)
            {
                case RetroBindings.RETRO_ENVIRONMENT_GET_SYSTEM_DIRECTORY:
                    {
                        char** cb = (char**)data;
                        *cb = (char*)Marshal.StringToHGlobalAuto(".");
                        return true;
                    }
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
                        return true;
                    }
                case RetroBindings.RETRO_ENVIRONMENT_GET_LOG_INTERFACE:
                    {
                        retro_log_callback* cb = (retro_log_callback*)data;
                        cb->log = NativeDispatchProxy.Register<retro_log_printf_t>(Log);
                        return true;
                    }
                case RetroBindings.RETRO_ENVIRONMENT_GET_CAN_DUPE:
                    {
                        return *(bool*)data = true;
                    }
                case RetroBindings.RETRO_ENVIRONMENT_SET_FRAME_TIME_CALLBACK:
                    {
                        retro_frame_time_callback* cb = (retro_frame_time_callback*)data;
                        cb->callback = NativeDispatchProxy.Register<retro_frame_time_callback_t>(Time);
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
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
            //To implement this
            throw new NotImplementedException();

            var count = 2;
            var audio = new float[count*2];
            var data = Marshal.AllocHGlobal(count * 2);

            Marshal.Copy(new[] { left, right }, 0, data, 0);
            Marshal.Copy(data, audio, 0, count * 2);

            OnSample?.Invoke(audio);
        }

        private UIntPtr AudioSampleBatch(short* data, UIntPtr frames)
        {
            var count = (int)frames * 2;
            float[] floatBuffer = new float[count];

            for (int i = 0; i < floatBuffer.Length; ++i)
            {
                var f = data[i] * 0.000030517578125f;
                //todo implement math clamp function
                if(f < -1)
                {
                    floatBuffer[i] = -1;
                }
                else if(f > 1)
                {
                    floatBuffer[i] = 1;
                }
                else
                {
                    floatBuffer[i] = f;
                }
            }

            OnSample?.Invoke(floatBuffer);
            return frames;
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
            NativeDispatchProxy.Dispose(_interop);
        }
    }
}
