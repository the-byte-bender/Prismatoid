using System.Runtime.InteropServices;

namespace Prismatoid.NativeInterop
{
    [StructLayout(LayoutKind.Sequential)]
    public partial struct PrismConfig
    {
        public byte version;
    }
}
