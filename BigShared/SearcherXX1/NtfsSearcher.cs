using SearcherXX1;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

public static class NtfsSearcher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MFT_ENUM_DATA_V0
    {
        public ulong StartFileReferenceNumber;
        public long LowUsn;
        public long HighUsn;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct USN_RECORD_V2
    {
        public uint RecordLength;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ulong FileReferenceNumber;
        public ulong ParentFileReferenceNumber;
        public long Usn;
        public long TimeStamp;
        public uint Reason;
        public uint SourceInfo;
        public uint SecurityId;
        public uint FileAttributes;
        public ushort FileNameLength;
        public ushort FileNameOffset;
    }

    public const uint FSCTL_ENUM_USN_DATA = 0x000900b3;
    public const int ERROR_HANDLE_EOF = 38;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        ref MFT_ENUM_DATA_V0 lpInBuffer,
        uint nInBufferSize,
        byte[] lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    public static void SearchDeepFull(string drivePath, string containsName, ConcurrentDictionary<string, byte> bag, bool ignore)
    {
        const int bufferSize = 65536;
        var frnMap = new Dictionary<ulong, (ulong parentFrn, string name)>();
        var volumeHandle = CreateFile(
            @"\\.\" + drivePath.TrimEnd('\\'),
            0x80000000, 1 | 2, IntPtr.Zero, 3, 0, IntPtr.Zero);

        if (volumeHandle == IntPtr.Zero || volumeHandle == new IntPtr(-1))
            throw new IOException("Failed to open volume handle.");

        var input = new MFT_ENUM_DATA_V0
        {
            StartFileReferenceNumber = 0,
            LowUsn = 0,
            HighUsn = long.MaxValue
        };

        var outputBuffer = new byte[bufferSize];
        bool doneReading = false;

        while (!doneReading)
        {
            bool success = DeviceIoControl(
                volumeHandle, FSCTL_ENUM_USN_DATA,
                ref input, (uint)Marshal.SizeOf(input),
                outputBuffer, (uint)outputBuffer.Length,
                out uint bytesReturned, IntPtr.Zero);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == ERROR_HANDLE_EOF) break;
                throw new IOException("DeviceIoControl failed with error: " + error);
            }

            var stream = new MemoryStream(outputBuffer);
            var reader = new BinaryReader(stream, Encoding.Unicode);
            input.StartFileReferenceNumber = reader.ReadUInt64();

            while (stream.Position < bytesReturned)
            {
                long recordStart = stream.Position;
                var record = new USN_RECORD_V2
                {
                    RecordLength = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    FileReferenceNumber = reader.ReadUInt64(),
                    ParentFileReferenceNumber = reader.ReadUInt64(),
                    Usn = reader.ReadInt64(),
                    TimeStamp = reader.ReadInt64(),
                    Reason = reader.ReadUInt32(),
                    SourceInfo = reader.ReadUInt32(),
                    SecurityId = reader.ReadUInt32(),
                    FileAttributes = reader.ReadUInt32(),
                    FileNameLength = reader.ReadUInt16(),
                    FileNameOffset = reader.ReadUInt16()
                };

                stream.Position = recordStart + record.FileNameOffset;
                byte[] nameBytes = reader.ReadBytes(record.FileNameLength);
                string fileName = Encoding.Unicode.GetString(nameBytes);

                frnMap[record.FileReferenceNumber] = (record.ParentFileReferenceNumber, fileName);

                if (fileName.myContains(containsName, ignore))
                {
                    string fullPath = ReconstructPath(record.FileReferenceNumber, frnMap, drivePath);
                    bag[fullPath] = 0;
                }

                stream.Position = recordStart + record.RecordLength;
            }
        }
    }
    private static string ReconstructPath(ulong frn, Dictionary<ulong, (ulong parentFrn, string name)> frnMap, string root)
    {
        var stack = new Stack<string>();
        while (frnMap.TryGetValue(frn, out var entry))
        {
            stack.Push(entry.name);
            if (entry.parentFrn == frn) break; // root
            frn = entry.parentFrn;
        }
        return Path.Combine(root.TrimEnd('\\') + "\\", Path.Combine(stack.ToArray()));
    }
    public static void SearchDeep(string drivePath, string containsName, ConcurrentBag<string> bag)
    {
        const int bufferSize = 65536;
        var volumeHandle = CreateFile(
            @"\\.\" + drivePath.TrimEnd('\\'),
            0x80000000, // GENERIC_READ
            1 | 2,      // FILE_SHARE_READ | FILE_SHARE_WRITE
            IntPtr.Zero,
            3,          // OPEN_EXISTING
            0,
            IntPtr.Zero);

        if (volumeHandle == IntPtr.Zero || volumeHandle == new IntPtr(-1))
            throw new IOException("Failed to open volume handle.");

        var input = new MFT_ENUM_DATA_V0
        {
            StartFileReferenceNumber = 0,
            LowUsn = 0,
            HighUsn = long.MaxValue
        };

        var outputBuffer = new byte[bufferSize];
        bool doneReading = false;

        while (!doneReading)
        {
            bool success = DeviceIoControl(
                volumeHandle,
                FSCTL_ENUM_USN_DATA,
                ref input,
                (uint)Marshal.SizeOf(input),
                outputBuffer,
                (uint)outputBuffer.Length,
                out uint bytesReturned,
                IntPtr.Zero);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == ERROR_HANDLE_EOF)
                {
                    doneReading = true;
                    break;
                }
                else
                {
                    throw new IOException("DeviceIoControl failed with error: " + error);
                }
            }

            var stream = new MemoryStream(outputBuffer);
            var reader = new BinaryReader(stream, Encoding.Unicode);

            input.StartFileReferenceNumber = reader.ReadUInt64(); // next StartFileReferenceNumber

            while (stream.Position < bytesReturned)
            {
                long recordStart = stream.Position;
                var record = new USN_RECORD_V2
                {
                    RecordLength = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    FileReferenceNumber = reader.ReadUInt64(),
                    ParentFileReferenceNumber = reader.ReadUInt64(),
                    Usn = reader.ReadInt64(),
                    TimeStamp = reader.ReadInt64(),
                    Reason = reader.ReadUInt32(),
                    SourceInfo = reader.ReadUInt32(),
                    SecurityId = reader.ReadUInt32(),
                    FileAttributes = reader.ReadUInt32(),
                    FileNameLength = reader.ReadUInt16(),
                    FileNameOffset = reader.ReadUInt16()
                };

                stream.Position = recordStart + record.FileNameOffset;
                byte[] nameBytes = reader.ReadBytes(record.FileNameLength);
                string fileName = Encoding.Unicode.GetString(nameBytes);

                if (fileName.Contains(containsName/*, StringComparison.OrdinalIgnoreCase*/))
                {
                    bag.Add(fileName);
                }

                stream.Position = recordStart + record.RecordLength;
            }
        }
    }
}
