using Libretro.NET.Bindings;

namespace Libretro.NET
{
    /// <summary>
    /// Represents native function bindings.
    /// Names and parameters must match the ones found in <see cref="RetroBindings"/>.
    /// </summary>
    public interface IRetro
    {
        void set_environment(retro_environment_t param0);
        void set_video_refresh(retro_video_refresh_t param0);
        void set_input_poll(retro_input_poll_t param0);
        void set_input_state(retro_input_state_t param0);
        void set_audio_sample(retro_audio_sample_t param0);
        void set_audio_sample_batch(retro_audio_sample_batch_t param0);
        void set_controller_port_device(uint port, uint device);



        /*
        private retro_log_printf_t _logPrintfCallback;
        private retro_perf_get_time_usec_t _perfGetTimeUsecCallback;
        private retro_perf_get_counter_t _perfGetCounterCallback;
        private retro_get_cpu_features_t _getCPUFeaturesCallback;
        private retro_perf_log_t _perfLogCallback;
        private retro_perf_register_t _perfRegisterCallback;
        private retro_perf_start_t _perfStartCallback;
        private retro_perf_stop_t _perfStopCallback;         
        */

        void init();

        void get_system_info(ref retro_system_info param0);
        byte load_game(ref retro_game_info param0);
        void get_system_av_info(ref retro_system_av_info param0);

        void run();
        void deinit();

        uint api_version();

    }
}
