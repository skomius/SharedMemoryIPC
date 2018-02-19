//Shared memory IPC 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace csharpConsole
{

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct SharedData
    {
        public int msType;
        public int sharedMemorySize;
        public int _token;
        public fixed char message[2000];
    }

    [Flags]
    enum FileProtection : uint
    {
        PAGE_READONLY = 0x00000002,
        PAGE_READWRITE = 0x00000004,
        PAGE_WRITECOPY = 0x00000008,
        PAGE_EXECUTE_READ = 0x00000020,
        PAGE_EXECUTE_READWRITE = 0x00000040,
        PAGE_EXECUTE_WRITECOPY = 0x00000080,
    }

    [Flags]
    enum FileRights : uint
    {
        GENERIC_READ = 0x80000000,
        GENERIC_WRITE = 0x40000000,
        GENERIC_RW = GENERIC_READ | GENERIC_WRITE
    }



    public sealed class SharedMemory : IDisposable
    {

        string Message { set { } }

        int sharedMemorySize;

        enum MassegeType : int
        {
            TEXT = 1,
            BINARY = 2,
        }

        enum Token : int
        {
            NOT_LOCKED = 0,
            LOCKED = 1
        }

        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        //Opens named file mapping object and returns handle  
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenFileMapping(FileRights dwDesiredAccess,
                                             bool bInheritHandle,
                                             string lpName);

        //Creates and opens named file mapping object and returns handle 
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFileMapping(IntPtr hFile,
                                               int lpAttributes,
                                               FileProtection flProtect,
                                               uint dwMaximumSizeHigh,
                                               uint dwMaximumSizeLow,
                                               string lpName);

        //Maps file to the process virtual adrress space
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject,
                                           FileRights dwDesiredAccess,
                                           uint dwFileOffsetHigh,
                                           uint dwFileOffsetLow,
                                           uint dwNumberOfBytesToMap);

        //Unmaps a mapped view of a file from the calling proces address space.
        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern bool UnmapViewOfFile(IntPtr map);

        //Closes an open object handle
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int CloseHandle(IntPtr hObject);

        public unsafe SharedMemory()
        {
            System.Security.Cryptography.X509Certificates 


        }

        public void Dispose()
        {

        }

    }
}
