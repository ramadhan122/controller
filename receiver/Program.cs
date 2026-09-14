using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class Program
{
    // ============================================================
    // AOA
    // ============================================================

    const byte AOA_GET_PROTOCOL = 51;
    const byte AOA_SEND_STRING = 52;
    const byte AOA_START_ACCESSORY = 53;

    const ushort AOA_STRING_MANUFACTURER = 0;
    const ushort AOA_STRING_MODEL = 1;
    const ushort AOA_STRING_DESCRIPTION = 2;
    const ushort AOA_STRING_VERSION = 3;
    const ushort AOA_STRING_URI = 4;
    const ushort AOA_STRING_SERIAL = 5;

    const string NORMAL_TARGET = "VID_18D1&PID_4EE7";
    const string AOA_TARGET = "VID_18D1&PID_2D01&MI_00";

    // ============================================================
    // FILE / DEVICE
    // ============================================================

    const uint FILE_FLAG_OVERLAPPED = 0x40000000;

    const uint DIGCF_PRESENT = 0x00000002;
    const uint DIGCF_DEVICEINTERFACE = 0x00000010;

    const uint GENERIC_READ = 0x80000000;
    const uint GENERIC_WRITE = 0x40000000;

    const uint FILE_SHARE_READ = 0x00000001;
    const uint FILE_SHARE_WRITE = 0x00000002;

    const uint OPEN_EXISTING = 3;

    const int ERROR_NO_MORE_ITEMS = 259;

    static readonly nint INVALID_HANDLE_VALUE = -1;

    // ============================================================
    // MAIN
    // ============================================================

    static void Main()
    {
        Console.WriteLine("ZZZ Controller - AOA Receiver");
        Console.WriteLine("=============================");
        Console.WriteLine();

        // --------------------------------------------------------
        // STEP 1
        // Pastikan HP normal terdeteksi
        // --------------------------------------------------------

        Console.WriteLine("Mencari POCO USB / ADB interface...");

        string? normalDevicePath = FindInterfacePath(
            NORMAL_TARGET,
            "Normal USB / ADB"
        );

        if (normalDevicePath == null)
        {
            Console.WriteLine();
            Console.WriteLine(
                "Interface normal VID_2717&PID_FF48&MI_01 tidak ditemukan."
            );

            Console.WriteLine();
            Console.WriteLine(
                "Pastikan HP terhubung melalui USB."
            );

            return;
        }

        Console.WriteLine();
        Console.WriteLine("Normal USB interface ditemukan:");
        Console.WriteLine(normalDevicePath);
        Console.WriteLine();

        // --------------------------------------------------------
        // STEP 2
        // Buka interface ADB yang menggunakan WinUSB
        // --------------------------------------------------------

        nint normalDeviceHandle = CreateFile(
            normalDevicePath,
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_OVERLAPPED,
            IntPtr.Zero
        );

        if (normalDeviceHandle == INVALID_HANDLE_VALUE)
        {
            Console.WriteLine(
                $"CreateFile normal USB gagal: {Marshal.GetLastWin32Error()}"
            );

            return;
        }

        Console.WriteLine(
            "Normal USB interface berhasil dibuka."
        );

        // --------------------------------------------------------
        // STEP 3
        // Inisialisasi WinUSB
        // --------------------------------------------------------

        if (!WinUsb_Initialize(
                normalDeviceHandle,
                out nint normalInterfaceHandle))
        {
            Console.WriteLine(
                $"WinUsb_Initialize normal gagal: {Marshal.GetLastWin32Error()}"
            );

            CloseHandle(normalDeviceHandle);
            return;
        }

        Console.WriteLine(
            "WinUSB normal berhasil diinisialisasi."
        );

        // --------------------------------------------------------
        // STEP 4
        // AOA GET_PROTOCOL
        // --------------------------------------------------------

        if (!GetAOAProtocol(
                normalInterfaceHandle,
                out ushort protocol))
        {
            WinUsb_Free(normalInterfaceHandle);
            CloseHandle(normalDeviceHandle);
            return;
        }

        Console.WriteLine();
        Console.WriteLine(
            $"AOA protocol version: {protocol}"
        );

        if (protocol < 1)
        {
            Console.WriteLine();
            Console.WriteLine(
                "AOA protocol tidak didukung."
            );

            WinUsb_Free(normalInterfaceHandle);
            CloseHandle(normalDeviceHandle);
            return;
        }

        // --------------------------------------------------------
        // STEP 5
        // Kirim accessory strings
        // --------------------------------------------------------

        Console.WriteLine();
        Console.WriteLine("Mengirim AOA accessory information...");

        if (!SendAOAString(
                normalInterfaceHandle,
                AOA_STRING_MANUFACTURER,
                "Ramadhan"))
        {
            CleanupNormal(
                normalInterfaceHandle,
                normalDeviceHandle
            );

            return;
        }

        if (!SendAOAString(
                normalInterfaceHandle,
                AOA_STRING_MODEL,
                "ZZZ Controller"))
        {
            CleanupNormal(
                normalInterfaceHandle,
                normalDeviceHandle
            );

            return;
        }

        if (!SendAOAString(
                normalInterfaceHandle,
                AOA_STRING_DESCRIPTION,
                "USB Xbox-style controller"))
        {
            CleanupNormal(
                normalInterfaceHandle,
                normalDeviceHandle
            );

            return;
        }

        if (!SendAOAString(
                normalInterfaceHandle,
                AOA_STRING_VERSION,
                "1.0"))
        {
            CleanupNormal(
                normalInterfaceHandle,
                normalDeviceHandle
            );

            return;
        }

        if (!SendAOAString(
                normalInterfaceHandle,
                AOA_STRING_URI,
                "https://example.com"))
        {
            CleanupNormal(
                normalInterfaceHandle,
                normalDeviceHandle
            );

            return;
        }

        if (!SendAOAString(
                normalInterfaceHandle,
                AOA_STRING_SERIAL,
                "ZZC-001"))
        {
            CleanupNormal(
                normalInterfaceHandle,
                normalDeviceHandle
            );

            return;
        }

        Console.WriteLine(
            "Accessory information berhasil dikirim."
        );

        // --------------------------------------------------------
        // STEP 6
        // START ACCESSORY
        // --------------------------------------------------------

        Console.WriteLine();
        Console.WriteLine(
            "Memulai Android Open Accessory mode..."
        );

        if (!StartAOA(
                normalInterfaceHandle))
        {
            CleanupNormal(
                normalInterfaceHandle,
                normalDeviceHandle
            );

            return;
        }

        Console.WriteLine();
        Console.WriteLine(
            "START_ACCESSORY berhasil dikirim."
        );

        Console.WriteLine(
            "Menunggu USB reconnect..."
        );

        CleanupNormal(
            normalInterfaceHandle,
            normalDeviceHandle
        );

        // --------------------------------------------------------
        // STEP 7
        // Tunggu device berubah menjadi VID_18D1
        // --------------------------------------------------------

        string? aoaDevicePath = null;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            Thread.Sleep(500);

            aoaDevicePath = FindInterfacePath(
                AOA_TARGET,
                "AOA MI_00"
            );

            if (aoaDevicePath != null)
                break;

            Console.WriteLine(
                $"Menunggu AOA... {attempt + 1}/20"
            );
        }

        if (aoaDevicePath == null)
        {
            Console.WriteLine();
            Console.WriteLine(
                "AOA MI_00 interface tidak ditemukan setelah START_ACCESSORY."
            );

            Console.WriteLine();
            Console.WriteLine(
                "Cek kembali apakah HP sudah melakukan USB re-enumeration."
            );

            return;
        }

        // --------------------------------------------------------
        // STEP 8
        // Buka AOA MI_00
        // --------------------------------------------------------

        Console.WriteLine();
        Console.WriteLine(
            "AOA MI_00 interface ditemukan!"
        );

        Console.WriteLine(
            aoaDevicePath
        );

        Console.WriteLine();

        nint aoaDeviceHandle = CreateFile(
            aoaDevicePath,
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_OVERLAPPED,
            IntPtr.Zero
        );

        if (aoaDeviceHandle == INVALID_HANDLE_VALUE)
        {
            Console.WriteLine(
                $"CreateFile AOA gagal: {Marshal.GetLastWin32Error()}"
            );

            return;
        }

        Console.WriteLine(
            "AOA MI_00 berhasil dibuka."
        );

        // --------------------------------------------------------
        // STEP 9
        // WinUSB AOA
        // --------------------------------------------------------

        if (!WinUsb_Initialize(
                aoaDeviceHandle,
                out nint aoaInterfaceHandle))
        {
            Console.WriteLine(
                $"WinUsb_Initialize gagal: {Marshal.GetLastWin32Error()}"
            );

            CloseHandle(aoaDeviceHandle);
            return;
        }

        Console.WriteLine(
            "WinUSB berhasil diinisialisasi."
        );

        // --------------------------------------------------------
        // STEP 10
        // Query interface
        // --------------------------------------------------------

        if (!WinUsb_QueryInterfaceSettings(
                aoaInterfaceHandle,
                0,
                out USB_INTERFACE_DESCRIPTOR descriptor))
        {
            Console.WriteLine(
                $"Query interface gagal: {Marshal.GetLastWin32Error()}"
            );

            WinUsb_Free(aoaInterfaceHandle);
            CloseHandle(aoaDeviceHandle);

            return;
        }

        Console.WriteLine();
        Console.WriteLine("=============================");
        Console.WriteLine("USB INTERFACE");
        Console.WriteLine("=============================");

        Console.WriteLine(
            $"Interface Number : {descriptor.bInterfaceNumber}"
        );

        Console.WriteLine(
            $"Alternate Setting: {descriptor.bAlternateSetting}"
        );

        Console.WriteLine(
            $"Endpoint Count   : {descriptor.bNumEndpoints}"
        );

        // --------------------------------------------------------
        // STEP 11
        // Cari bulk endpoint
        // --------------------------------------------------------

        byte bulkIn = 0;
        byte bulkOut = 0;

        Console.WriteLine();

        for (byte i = 0; i < descriptor.bNumEndpoints; i++)
        {
            if (!WinUsb_QueryPipe(
                    aoaInterfaceHandle,
                    0,
                    i,
                    out WINUSB_PIPE_INFORMATION pipe))
            {
                Console.WriteLine(
                    $"QueryPipe gagal: {Marshal.GetLastWin32Error()}"
                );

                continue;
            }

            Console.WriteLine(
                $"Endpoint: 0x{pipe.PipeId:X2} | " +
                $"Type: {pipe.PipeType} | " +
                $"MaxPacket: {pipe.MaximumPacketSize}"
            );

            // UsbdPipeTypeBulk = 2
            if (pipe.PipeType == 2)
            {
                if ((pipe.PipeId & 0x80) != 0)
                {
                    bulkIn = pipe.PipeId;
                }
                else
                {
                    bulkOut = pipe.PipeId;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("=============================");
        Console.WriteLine("ENDPOINT");
        Console.WriteLine("=============================");

        Console.WriteLine(
            $"Bulk IN : 0x{bulkIn:X2}"
        );

        Console.WriteLine(
            $"Bulk OUT: 0x{bulkOut:X2}"
        );

        if (bulkIn == 0 || bulkOut == 0)
        {
            Console.WriteLine();
            Console.WriteLine(
                "Bulk endpoint tidak lengkap."
            );

            WinUsb_Free(aoaInterfaceHandle);
            CloseHandle(aoaDeviceHandle);

            return;
        }

        // --------------------------------------------------------
        // STEP 12
        // DATA CHANNEL
        // --------------------------------------------------------

        Console.WriteLine();
        Console.WriteLine("=============================");
        Console.WriteLine("AOA DATA CHANNEL READY.");
        Console.WriteLine("Menunggu data dari HP...");
        Console.WriteLine("=============================");
        Console.WriteLine();

        ReadLoop(
            aoaInterfaceHandle,
            bulkIn
        );

        WinUsb_Free(aoaInterfaceHandle);
        CloseHandle(aoaDeviceHandle);
    }

    // ============================================================
    // AOA GET PROTOCOL
    // ============================================================

    static bool GetAOAProtocol(
        nint interfaceHandle,
        out ushort protocol)
    {
        protocol = 0;

        WINUSB_SETUP_PACKET setupPacket =
            new WINUSB_SETUP_PACKET
            {
                RequestType = 0xC0,
                Request = AOA_GET_PROTOCOL,
                Value = 0,
                Index = 0,
                Length = 2
            };

        byte[] buffer = new byte[2];

        bool result = WinUsb_ControlTransfer(
            interfaceHandle,
            setupPacket,
            buffer,
            2,
            out uint transferred,
            IntPtr.Zero
        );

        if (!result)
        {
            Console.WriteLine(
                $"AOA GET_PROTOCOL gagal: {Marshal.GetLastWin32Error()}"
            );

            return false;
        }

        if (transferred != 2)
        {
            Console.WriteLine(
                $"GET_PROTOCOL response tidak lengkap: {transferred} byte"
            );

            return false;
        }

        protocol = BitConverter.ToUInt16(
            buffer,
            0
        );

        return true;
    }

    // ============================================================
    // AOA SEND STRING
    // ============================================================

    static bool SendAOAString(
        nint interfaceHandle,
        ushort stringIndex,
        string value)
    {
        byte[] data = Encoding.UTF8.GetBytes(
            value + "\0"
        );

        WINUSB_SETUP_PACKET setupPacket =
            new WINUSB_SETUP_PACKET
            {
                RequestType = 0x40,
                Request = AOA_SEND_STRING,
                Value = 0,
                Index = stringIndex,
                Length = (ushort)data.Length
            };

        bool result = WinUsb_ControlTransfer(
            interfaceHandle,
            setupPacket,
            data,
            (uint)data.Length,
            out uint transferred,
            IntPtr.Zero
        );

        if (!result)
        {
            Console.WriteLine(
                $"SEND_STRING [{stringIndex}] gagal: " +
                $"{Marshal.GetLastWin32Error()}"
            );

            return false;
        }

        Console.WriteLine(
            $"String {stringIndex}: {value}"
        );

        return true;
    }

    // ============================================================
    // AOA START ACCESSORY
    // ============================================================

    static bool StartAOA(
        nint interfaceHandle)
    {
        WINUSB_SETUP_PACKET setupPacket =
            new WINUSB_SETUP_PACKET
            {
                RequestType = 0x40,
                Request = AOA_START_ACCESSORY,
                Value = 0,
                Index = 0,
                Length = 0
            };

        byte[] empty = Array.Empty<byte>();

        bool result = WinUsb_ControlTransfer(
            interfaceHandle,
            setupPacket,
            empty,
            0,
            out _,
            IntPtr.Zero
        );

        if (!result)
        {
            Console.WriteLine(
                $"START_ACCESSORY gagal: " +
                $"{Marshal.GetLastWin32Error()}"
            );

            return false;
        }

        return true;
    }

    // ============================================================
    // READ DATA
    // ============================================================

    static void ReadLoop(
        nint interfaceHandle,
        byte bulkIn)
    {
        byte[] buffer = new byte[512];

        while (true)
        {
            bool result = WinUsb_ReadPipe(
                interfaceHandle,
                bulkIn,
                buffer,
                (uint)buffer.Length,
                out uint transferred,
                IntPtr.Zero
            );

            if (!result)
            {
                int error = Marshal.GetLastWin32Error();

                Console.WriteLine(
                    $"WinUsb_ReadPipe gagal: {error}"
                );

                break;
            }

            if (transferred == 0)
                continue;

            string data = Encoding.UTF8.GetString(
                buffer,
                0,
                (int)transferred
            );

            Console.WriteLine(
                $"RECV: {data.Replace("\r", "\\r").Replace("\n", "\\n")}"
            );
        }
    }

    // ============================================================
    // FIND USB INTERFACE
    // ============================================================

    static string? FindInterfacePath(
        string target,
        string label)
    {
        Console.WriteLine(
            $"Mencari {label}..."
        );

        Guid winUsbGuid =
            new Guid(
                "DEE824EF-729B-4A0E-9C14-B7117D33A817"
            );

        nint deviceInfoSet = SetupDiGetClassDevs(
            ref winUsbGuid,
            IntPtr.Zero,
            IntPtr.Zero,
            DIGCF_PRESENT | DIGCF_DEVICEINTERFACE
        );

        if (deviceInfoSet == INVALID_HANDLE_VALUE)
        {
            Console.WriteLine(
                $"SetupDiGetClassDevs gagal: " +
                $"{Marshal.GetLastWin32Error()}"
            );

            return null;
        }

        try
        {
            for (uint index = 0; ; index++)
            {
                SP_DEVICE_INTERFACE_DATA interfaceData =
                    new SP_DEVICE_INTERFACE_DATA();

                interfaceData.cbSize =
                    Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>();

                bool result =
                    SetupDiEnumDeviceInterfaces(
                        deviceInfoSet,
                        IntPtr.Zero,
                        ref winUsbGuid,
                        index,
                        ref interfaceData
                    );

                if (!result)
                {
                    int error =
                        Marshal.GetLastWin32Error();

                    if (error == ERROR_NO_MORE_ITEMS)
                        break;

                    Console.WriteLine(
                        $"SetupDiEnumDeviceInterfaces gagal: {error}"
                    );

                    break;
                }

                uint requiredSize = 0;

                SetupDiGetDeviceInterfaceDetail(
                    deviceInfoSet,
                    ref interfaceData,
                    IntPtr.Zero,
                    0,
                    ref requiredSize,
                    IntPtr.Zero
                );

                if (requiredSize == 0)
                    continue;

                nint detailBuffer =
                    Marshal.AllocHGlobal(
                        (int)requiredSize
                    );

                try
                {
                    // x64 = 8
                    int cbSize =
                        IntPtr.Size == 8 ? 8 : 6;

                    Marshal.WriteInt32(
                        detailBuffer,
                        cbSize
                    );

                    bool detailResult =
                        SetupDiGetDeviceInterfaceDetail(
                            deviceInfoSet,
                            ref interfaceData,
                            detailBuffer,
                            requiredSize,
                            ref requiredSize,
                            IntPtr.Zero
                        );

                    if (!detailResult)
                    {
                        Console.WriteLine(
                            $"GetDeviceInterfaceDetail gagal: " +
                            $"{Marshal.GetLastWin32Error()}"
                        );

                        continue;
                    }

                    // DevicePath dimulai offset 4
                    int pathOffset = 4;

                    nint pathPtr =
                        IntPtr.Add(
                            detailBuffer,
                            pathOffset
                        );

                    string? path =
                        Marshal.PtrToStringUni(
                            pathPtr
                        );

                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    Console.WriteLine(
                        $"WinUSB Interface {index}: {path}"
                    );

                    if (path.IndexOf(
                            target,
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0)
                    {
                        return path;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(
                        detailBuffer
                    );
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(
                deviceInfoSet
            );
        }

        return null;
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    static void CleanupNormal(
        nint interfaceHandle,
        nint deviceHandle)
    {
        WinUsb_Free(interfaceHandle);
        CloseHandle(deviceHandle);
    }

    // ============================================================
    // STRUCTURES
    // ============================================================

    [StructLayout(LayoutKind.Sequential)]
    struct WINUSB_SETUP_PACKET
    {
        public byte RequestType;
        public byte Request;
        public ushort Value;
        public ushort Index;
        public ushort Length;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct USB_INTERFACE_DESCRIPTOR
    {
        public byte bLength;
        public byte bDescriptorType;
        public byte bInterfaceNumber;
        public byte bAlternateSetting;
        public byte bNumEndpoints;
        public byte bInterfaceClass;
        public byte bInterfaceSubClass;
        public byte bInterfaceProtocol;
        public byte iInterface;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WINUSB_PIPE_INFORMATION
    {
        public int PipeType;
        public byte PipeId;
        public byte Interval;
        public ushort MaximumPacketSize;
        public uint MaximumTransferSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVICE_INTERFACE_DATA
    {
        public int cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public nint Reserved;
    }

    // ============================================================
    // SETUPAPI
    // ============================================================

    [DllImport(
        "setupapi.dll",
        SetLastError = true,
        CharSet = CharSet.Unicode
    )]
    static extern nint SetupDiGetClassDevs(
        ref Guid ClassGuid,
        nint Enumerator,
        nint hwndParent,
        uint Flags
    );

    [DllImport(
        "setupapi.dll",
        SetLastError = true
    )]
    static extern bool SetupDiEnumDeviceInterfaces(
        nint DeviceInfoSet,
        nint DeviceInfoData,
        ref Guid InterfaceClassGuid,
        uint MemberIndex,
        ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData
    );

    [DllImport(
        "setupapi.dll",
        SetLastError = true,
        CharSet = CharSet.Unicode
    )]
    static extern bool SetupDiGetDeviceInterfaceDetail(
        nint DeviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
        nint DeviceInterfaceDetailData,
        uint DeviceInterfaceDetailDataSize,
        ref uint RequiredSize,
        nint DeviceInfoData
    );

    [DllImport(
        "setupapi.dll",
        SetLastError = true
    )]
    static extern bool SetupDiDestroyDeviceInfoList(
        nint DeviceInfoSet
    );

    // ============================================================
    // WINUSB
    // ============================================================

    [DllImport(
        "winusb.dll",
        SetLastError = true
    )]
    static extern bool WinUsb_Initialize(
        nint DeviceHandle,
        out nint InterfaceHandle
    );

    [DllImport(
        "winusb.dll",
        SetLastError = true
    )]
    static extern bool WinUsb_Free(
        nint InterfaceHandle
    );

    [DllImport(
        "winusb.dll",
        SetLastError = true
    )]
    static extern bool WinUsb_QueryInterfaceSettings(
        nint InterfaceHandle,
        byte AlternateInterfaceNumber,
        out USB_INTERFACE_DESCRIPTOR UsbAltInterfaceDescriptor
    );

    [DllImport(
        "winusb.dll",
        SetLastError = true
    )]
    static extern bool WinUsb_QueryPipe(
        nint InterfaceHandle,
        byte AlternateInterfaceNumber,
        byte PipeIndex,
        out WINUSB_PIPE_INFORMATION PipeInformation
    );

    [DllImport(
        "winusb.dll",
        SetLastError = true
    )]
    static extern bool WinUsb_ReadPipe(
        nint InterfaceHandle,
        byte PipeID,
        [Out] byte[] Buffer,
        uint BufferLength,
        out uint LengthTransferred,
        nint Overlapped
    );

    [DllImport(
        "winusb.dll",
        SetLastError = true
    )]
    static extern bool WinUsb_ControlTransfer(
        nint InterfaceHandle,
        WINUSB_SETUP_PACKET SetupPacket,
        [Out] byte[] Buffer,
        uint BufferLength,
        out uint LengthTransferred,
        nint Overlapped
    );

    // ============================================================
    // KERNEL32
    // ============================================================

    [DllImport(
        "kernel32.dll",
        SetLastError = true,
        CharSet = CharSet.Unicode
    )]
    static extern nint CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        nint lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        nint hTemplateFile
    );

    [DllImport(
        "kernel32.dll",
        SetLastError = true
    )]
    static extern bool CloseHandle(
        nint hObject
    );
}