using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using EasyHook;

namespace AdapteveDLL
{
    public class GraphicsCardDX9Hook : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate UInt32 GetAdapterIdentifierDelegate(UInt32 Adapter, UInt64 Flags, [In][Out] IntPtr pIdentifier);
        private GetAdapterIdentifierDelegate GetAdapterIdentifierOriginal;

        [DllImport("d3d9.dll")]
        private static extern IntPtr Direct3DCreate9(uint sdkVersion);

        private string _name;
        private LocalHook _hook;
        private Settings _settings;

        public GraphicsCardDX9Hook(Settings settings)
        {
            this._settings = settings;

            IntPtr direct3D = Direct3DCreate9(32);
            if (direct3D == IntPtr.Zero)
                throw new Exception("Failed to create D3D.");

            IntPtr adapterIdentPtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(direct3D), 20);

            GetAdapterIdentifierOriginal = (GetAdapterIdentifierDelegate)Marshal.GetDelegateForFunctionPointer(adapterIdentPtr, typeof(GetAdapterIdentifierDelegate));

            _name = string.Format("GetAdapterIdentHook_{0:X}", adapterIdentPtr.ToInt32());
            _hook = LocalHook.Create(adapterIdentPtr, new GetAdapterIdentifierDelegate(GetAdapterIdentifierDetour), this);
            _hook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
        }

        private UInt32 GetAdapterIdentifierDetour(UInt32 Adapter, UInt64 Flags, [In][Out] IntPtr pIdentifier)
        {
            var result = GetAdapterIdentifierOriginal(Adapter, Flags, pIdentifier);

            D3DADAPTER_IDENTIFIER9 newStruct = (D3DADAPTER_IDENTIFIER9)Marshal.PtrToStructure(pIdentifier, typeof(D3DADAPTER_IDENTIFIER9));
            newStruct.Description = _settings.GpuDescription;
            newStruct.DeviceId = _settings.GpuDeviceId;
            newStruct.DeviceIdentifier = Guid.Parse(_settings.GpuIdentifier);
            newStruct.DriverVersion.QuadPart = _settings.GpuDriverversion;
            newStruct.Revision = _settings.GpuRevision;
            newStruct.VendorId = _settings.GpuVendorId;
            Marshal.StructureToPtr(newStruct, pIdentifier, true);
            return 0;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct _GUID
        {
            public Int32 Data1;
            public Int16 Data2;
            public Int16 Data3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Data4;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct D3DADAPTER_IDENTIFIER9
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
            public string Driver;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
            public string Description;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.Struct)]
            public LARGE_INTEGER DriverVersion;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 VendorId;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 DeviceId;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 SubSysId;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 Revision;
            [MarshalAs(UnmanagedType.Struct)]
            public Guid DeviceIdentifier;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 WHQLLevel;
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        public struct LARGE_INTEGER
        {
            [FieldOffset(0)]
            public Int64 QuadPart;
            [FieldOffset(0)]
            public UInt32 LowPart;
            [FieldOffset(4)]
            public UInt32 HighPart;
        }

        public void Dispose()
        {
            if (_hook == null)
                return;

            _hook.Dispose();
            _hook = null;
        }
    }
}
