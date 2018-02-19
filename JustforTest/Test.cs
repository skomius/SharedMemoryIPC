using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;
using System.Security.AccessControl;
using System.IO.Pipes;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;

namespace JustforTest
{
    class Test
    {
        static unsafe void Main(string[] args)
        {
            using (var namedPipeClient = new NamedPipeClientStream("pipepipe"))
            {
                namedPipeClient.Connect();
                namedPipeClient.ReadMode = PipeTransmissionMode.Message;

                IntPtr handle = new IntPtr(namedPipeClient.GetHandle());

                Console.WriteLine(handle.ToInt32() + "\n");

                EventWaitHandleMod eventHandle = new EventWaitHandleMod(handle);
                eventHandle.Set();
            }


                Console.ReadKey();
            }
        }
    }

