using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utility
{
    class Program
    {
       static unsafe void Main(string[] args)
        {
            using (var namedPipeServer = new NamedPipeServerStream("pipepipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message))
            {
                EventWaitHandle eventas = new EventWaitHandle(false, EventResetMode.ManualReset);
                namedPipeServer.WaitForConnection();
                namedPipeServer.DublicateHandles(eventas.SafeWaitHandle.DangerousGetHandle(), 0x00000002, 0x100002);
                eventas.WaitOne();
                System.IO.FileStream fs3 = File.Create("jkjl");
                Console.Write("Signalas");
            }
            Console.ReadKey();

        }
    }
}
