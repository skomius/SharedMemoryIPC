using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;

namespace Utility
{
    public unsafe struct Handles
    {
        public fixed int handles[3];
    }

    public class SharedMemoryIPC : SharedMemory
    {
        const int NONE = 0;
        const int SERVER = 1;
        const int CLIENT = 2;

        static int _instanceCount = 0;

        int role = NONE;

        EventWaitHandle serverEvent;

        EventWaitHandle clientEvent;

        SharedMemory sharedMemory;

        public string SharedMemoryIPCName { private set { } get { return sharedMemory.SharedMemoryName; } }

        SharedMemoryIPC(SharedMemory sharedMemory, EventWaitHandle serverEvent, EventWaitHandle clientEvent)
        {
            this.sharedMemory = sharedMemory;
            this.serverEvent = serverEvent;
            this.clientEvent = clientEvent;

            role = CLIENT;
        }

        public SharedMemoryIPC(string name, uint size, FileMapProtection fileMapProtection)
        {

            string countInstance = Interlocked.Increment(ref _instanceCount).ToString();
            string serverEventName = name + "." + "server" + countInstance;
            string clientEventName = name + "." + "client" + countInstance;

            initializeKernelObjects(name, size, fileMapProtection, serverEventName, clientEventName);

            WriteString(serverEventName, 0);
            WriteString(clientEventName, serverEventName.Length + 1);

            role = SERVER;
        }

        /// <summary>
        /// To connect with this shared memory IPC server use hconnect method exclusively. SharedMemoryIPC object are
        /// created without named kernel object, thus is safer.
        /// </summary>
        /// <param name="sharedMemoryIPCName"></param>
        /// <returns></returns>
        public SharedMemoryIPC(uint size, FileMapProtection fileMapProtection, out Handles handles)
        {

        }


        public SharedMemoryIPC(string name, uint size, FileMapProtection fileMapProtection, string serverEventName, string clientEventName)
        {
            initializeKernelObjects(name, size, fileMapProtection, serverEventName, clientEventName);
            role = SERVER;
        }

        public static SharedMemoryIPC Connect(string name, string serverEventName, string clientEventName)
        {
            return new SharedMemoryIPC(Open(name, FileMapAccess.FileMapAllAccess),
                EventWaitHandle.OpenExisting(serverEventName), EventWaitHandle.OpenExisting(clientEventName));
        }

        public static SharedMemoryIPC Connect(string sharedMemoryIPCName, FileMapAccess fileMapAccess)
        {
            SharedMemory sharedMemory = Open(sharedMemoryIPCName, fileMapAccess);
            EventWaitHandle serverEvent;
            EventWaitHandle clientEvent;


            string serverEventName = sharedMemory.ReadString(0);

            try
            {
                serverEvent = EventWaitHandle.OpenExisting(serverEventName);
                clientEvent = EventWaitHandle.OpenExisting(sharedMemory.ReadString(serverEventName.Length + 1));
            }
            catch (Win32Exception ex)
            {
                if (ex.ErrorCode == 0x000010d8)
                    throw new NoServerException();
                else
                    throw;
            }

            clientEvent.Set();

            return new SharedMemoryIPC(sharedMemory, serverEvent, clientEvent);
        }

        void initializeKernelObjects(string name, uint size, FileMapProtection fileMapProtection, string serverEventName, string clientEventName)
        {
            bool newCreatedSrEvent;
            bool newCreatedClEvent;
            sharedMemory = Create(name, size, FileMapProtection.PageExecuteReadWrite);

            serverEvent = new EventWaitHandle(false, EventResetMode.ManualReset, serverEventName, out newCreatedSrEvent);
            if (newCreatedSrEvent != true)
                throw new ObjectAlreadyExistException(serverEventName);

            clientEvent = new EventWaitHandle(false, EventResetMode.ManualReset, clientEventName, out newCreatedClEvent);
            if (newCreatedClEvent != true)
                throw new ObjectAlreadyExistException(clientEventName);
        }

        public void WaitForConnection()
        {
            clientEvent.WaitOne();
        }

        public void Replay()
        {
            if (role == SERVER)
                serverEvent.Set();
            else
                clientEvent.Set();
        }

        public void WaitForReplay()
        {
            if (role == CLIENT)
                serverEvent.WaitOne();
            else
                clientEvent.WaitOne();
        }

        public new void Dispose()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                serverEvent.Dispose();
                clientEvent.Dispose();
                sharedMemory.Dispose();
            }
        }
    }

    public class ObjectAlreadyExistException : Exception
    {
        string eventName;

        public ObjectAlreadyExistException(string eventName)
        {
            this.eventName = eventName;
        }

        public override string Message { get { return " Shared memory with such name" + eventName + " doesn't exist"; } }
    }

    public class NoServerException : Exception
    {
        public NoServerException() { }

        public override string Message
        {
            get
            {
                return "Server with such name doesn't exist";
            }
        }
    }

    public class EventWaitHandleMod : WaitHandle
    {
        const uint INFINITE = 0xFFFFFFFF;
        const uint WAIT_ABANDONED = 0x00000080;
        const uint WAIT_OBJECT_0 = 0x00000000;
        const uint WAIT_TIMEOUT = 0x00000102;
        const uint WAIT_FAILED = 0xFFFFFFFF;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenEvent(uint dwDesiredAccess, bool bInheritHandle, string lpName);

        [DllImport("kernel32.dll")]
        static extern bool SetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        public EventWaitHandleMod() { }

        public EventWaitHandleMod(IntPtr handle)
        {
            this.SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(handle, true);
        }

        public void Set()
        {
            if (SetEvent(SafeWaitHandle.DangerousGetHandle()) == false)
            {
                throw new Win32Exception();
            }
        }
    }

    public static class PipeLineEx
    {

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

        [DllImport("c:/Users/Skomantas/OneDrive/visual studio/SharedMemoryIPC/Debug/DublicateHandleMod.dll", CallingConvention = CallingConvention.Cdecl, 
            SetLastError = true, CharSet = CharSet.Unicode)]
        static extern int PassHandles(
            IntPtr namedPipe,
            IntPtr handle,
            uint dwDesiredAccess,
            uint dwOptions
            );

        [DllImport("c:/Users/Skomantas/OneDrive/visual studio/SharedMemoryIPC/Debug/DublicateHandleMod.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Unicode)]
        static extern int GetHandle(IntPtr namedPipe);

        //       public static SharedMemoryIPC ConnectH(this NamedPipeServerStream namedPipe,  )
        //       {
        //           return new SharedMemoryIPC
        //      }


        public unsafe static bool DublicateHandles(this NamedPipeServerStream namedPipe, IntPtr handle,
          uint dwOptions, uint dwDesiredAccess)
        {
            dwDesiredAccess = 0x100002;
            dwOptions = 0x00000002;

            int index = PassHandles(namedPipe.SafePipeHandle.DangerousGetHandle(), handle, dwDesiredAccess, dwOptions);

            Console.WriteLine( index + "\n");

            if (index > 0)
                return true;
            else
                return false;
        }

        public unsafe static int GetHandle(this NamedPipeServerStream namedPipe)
        {
            return GetHandle(namedPipe.SafePipeHandle.DangerousGetHandle());
        }

        public static bool DublicateHandles(this NamedPipeClientStream namedPipe, IntPtr handle,
          uint dwOptions, uint dwDesiredAccess)
        {
            dwDesiredAccess = 0x100002;
            dwOptions = 0x00000002;

            int index = PassHandles(namedPipe.SafePipeHandle.DangerousGetHandle(), handle, dwDesiredAccess, dwOptions);

            Console.WriteLine(index + "\n");

            if (index > 0)
                return true;
            else
                return false;
        }

        public static int GetHandle(this NamedPipeClientStream namedPipe)
        {
            return GetHandle(namedPipe.SafePipeHandle.DangerousGetHandle());
        }

    }
}

