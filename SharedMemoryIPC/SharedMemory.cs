//Shared memory IPC 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Utility
{
    [Flags]
    public enum FileMapProtection : uint
    {
        PageReadonly = 0x02,
        PageReadWrite = 0x04,
        PageWriteCopy = 0x08,
        PageExecuteRead = 0x20,
        PageExecuteReadWrite = 0x40,
        SectionCommit = 0x8000000,
        SectionImage = 0x1000000,
        SectionNoCache = 0x10000000,
        SectionReserve = 0x4000000,
    }

    [Flags]
    public enum FileMapAccess : uint
    {
        FileMapCopy = 0x0001,
        FileMapWrite = 0x0002,
        FileMapRead = 0x0004,
        FileMapAllAccess = 0x001f,
        FileMapExecute = 0x0020,
        FileMapReadWrite = FileMapWrite | FileMapRead
    }

    [Flags]
    public enum HandleFlags : uint
    {
        DuplicateCloseSource = 0x00000001,
        DuplicateSameAccess = 0x00000002,
        Both = DuplicateSameAccess | DuplicateCloseSource,
        None = 0
    }

    public class SharedMemory : IDisposable
    {
        public uint InitialSharedMemorySize { private set; get; }

        IntPtr sharedMemoryHandle;

        IntPtr shmBase;

        public string SharedMemoryName { private set; get; }

        public int Offset { private set; get; }

        static readonly IntPtr invalidHandleValue = new IntPtr(-1);

        //Opens named file mapping object and returns handle  
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr OpenFileMapping(
            FileMapAccess dwDesiredAccess,
            bool bInheritHandle,
            string lpName);

        //Creates and opens named file mapping object and returns handle 
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr CreateFileMapping(
            IntPtr hFile,
            IntPtr lpFileMappingAttributes,
            FileMapProtection flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            string lpName);

        //Maps file to the process virtual adrress space
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr MapViewOfFile(
            IntPtr hFileMappingObject,
            FileMapAccess dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            uint dwNumberOfBytesToMap);

        //Unmaps a mapped view of a file from the calling proces address space.
        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern bool UnmapViewOfFile(IntPtr map);

        //Closes an open object handle
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            out IntPtr lpTargetHandle,
            uint dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            uint dwOptions);

        internal SharedMemory() { }

        internal SharedMemory(IntPtr fmHandle, IntPtr Base, uint initialSize)
        {
            sharedMemoryHandle = fmHandle;
            shmBase = Base;
            InitialSharedMemorySize = initialSize;
        }

        public static SharedMemory Create(string name, uint initialSize, FileMapProtection protection)
        {
            IntPtr fmHandle = CreateFileMapping(invalidHandleValue, IntPtr.Zero, protection, 0, initialSize, name);
            if (Marshal.GetLastWin32Error() == 183)
                throw new ObjectAlreadyExistException(name);

            if (fmHandle == IntPtr.Zero)
                throw new Win32Exception();

            IntPtr Base = MapViewOfFile(fmHandle, FileMapAccess.FileMapReadWrite, 0, 0, 0);
            if (Base != IntPtr.Zero)
                return new SharedMemory(fmHandle, Base, initialSize);
            else
                throw new Win32Exception();
        }

        public static SharedMemory Open(IntPtr fmHandle)
        {
            IntPtr Base = MapViewOfFile(fmHandle, FileMapAccess.FileMapReadWrite, 0, 0, 0);
            if (Base != IntPtr.Zero)
            {
                return new SharedMemory(fmHandle, Base, 0);
            }
            else
                throw new Win32Exception();
        }

        public static SharedMemory Open(string name, FileMapAccess fileMapAccess)
        {
            IntPtr fmHandle = OpenFileMapping(fileMapAccess, true, name);
            if (fmHandle == IntPtr.Zero)
                throw new Win32Exception();

            IntPtr Base = MapViewOfFile(fmHandle, fileMapAccess, 0, 0, 0);
            if (Base != IntPtr.Zero)
            {
                return new SharedMemory(fmHandle, Base, 0);
            }
            else
                throw new Win32Exception();
        }

        public unsafe void WriteByte(byte b, int offset)
        {
            byte* ptr = (byte*)shmBase.ToPointer() + offset;
            *ptr = b;
        }

        public unsafe byte ReadByte(int offset)
        {
            byte* ptr = (byte*)shmBase.ToPointer() + offset;
            return *ptr;
        }

        public unsafe void WriteInt(int integer, int offset)
        {
            int* ptr = (int*)shmBase.ToPointer() + offset;
            *ptr = integer;
        }

        public unsafe int ReadInt(int offset)
        {
            int* ptr = (int*)shmBase.ToPointer() + offset;
            return *ptr;
        }

        public unsafe void WriteString(string str, int offset)
        {
            char* ptr = (char*)shmBase.ToPointer() + offset;
            for (int i = 0; i < str.Length; i++)
            {
                *ptr = str[i];
                ptr++;
            }
            *ptr = '\0';
        }

        public unsafe string ReadString(int offset)
        {
            char* ptr = (char*)shmBase.ToPointer() + offset;
            return new string(ptr);
        }

        public static unsafe bool DublicateHandle(
            IntPtr handle, string targetProcessName, IntPtr ptrToCopyHandle, uint HandleFlags,
         uint dwDesiredAccess)
        {
            Process currentProcess = Process.GetCurrentProcess();
            if (true != DuplicateHandle(currentProcess.Handle, handle, Process.GetProcessesByName(targetProcessName)[0].Handle, out ptrToCopyHandle,
                 dwDesiredAccess, true, HandleFlags))
                return true;
            else
                throw new Win32Exception();
        }

        /// <summary>
        /// such as the desktop's interactive user account.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose( bool disposing)
        {
            if (shmBase != IntPtr.Zero)
                UnmapViewOfFile(shmBase);

            if (sharedMemoryHandle != IntPtr.Zero)
                CloseHandle(sharedMemoryHandle);
        }

        ~SharedMemory()
        {
            Dispose(false);
        }
    }

    public class OutOfMappedViewRangeException : Exception
    {
        public OutOfMappedViewRangeException() { }

        public string Massege { get { return "Out of mapped view range"; } }
    }

    public sealed class FileMappingSecurity : NativeObjectSecurity
    {

        public FileMappingSecurity(SafeHandle handle) : base(true, ResourceType.KernelObject, handle, AccessControlSections.All)
        {

        }

        public override Type AccessRightType
        {
            get { return typeof(FileMapProtection); }
        }

        public override Type AccessRuleType
        {
            get { return typeof(FileSystemAccessRule); }
        }

        public override Type AuditRuleType
        {
            get { return typeof(FileMappingAuditRule); }
        }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask,
            bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
        {
            return new FileMappingAccessRule(identityReference, accessMask, isInherited,
            inheritanceFlags, propagationFlags, type);
        }

        public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask,
            bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags auditFlags)
        {
            return new FileMappingAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, auditFlags);
        }
    }

    public class FileMappingAccessRule : AccessRule
    {
        public FileMappingAccessRule(IdentityReference identityReference, int accessMask, bool isInherited,
            InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : base(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type)
        {

        }
    }

    public class FileMappingAuditRule : AuditRule
    {
        public FileMappingAuditRule(IdentityReference identityReference, int accessMask, bool isInherited, 
            InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags auditFlags
            ) : base(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, auditFlags)
        {

        }
    }

    
}
    

