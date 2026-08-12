using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Prismatoid.NativeInterop
{
    public partial struct PrismRegistry { }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct PrismLogHandlerNative
    {
        public delegate* unmanaged[Cdecl]<void*, PrismLogLevel, sbyte*, sbyte*, void> fn;
        public void* userdata;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct PrismConfig
    {
        public byte version;
        public PrismRegistry* registry;
        public delegate* unmanaged[Cdecl]<void*, ulong, sbyte*, byte, void> availability_callback;
        public void* availability_userdata;
        public uint availability_poll_interval_ms;
        public uint availability_debounce_samples;
        public uint availability_backoff_max_ms;
        public byte availability_auto_power_manage;
    }
}
