using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Diagnostics;
using System.Threading.Tasks;
using SomeNamespace;
using System.Net;
using System.IO;

namespace ConsoleApplication1
{
   class Program
   {
      public static void Main(String[] args)
      {
         try
         {
            WebRequest tRequest = WebRequest.Create("http://localhost/asptest/Foo.ashx");
            tRequest.Method = "POST";

            Byte[] tRequestBuffer = Encoding.UTF8.GetBytes(""); // something here
            MemoryStream tResult = new MemoryStream(); // for testing

            var pipe = Pipes.Create<Stream, Stream>(tRequest.BeginGetRequestStream, tRequest.EndGetRequestStream)
               .Connect(Pipes.CreateEnd<Stream, Stream>((str, cb, state) => str.BeginWrite(tRequestBuffer, 0, tRequestBuffer.Length, cb, state), (s, r) => s.EndWrite(r)).Dispose()) // should probably flush as well.
               .Connect(tRequest.BeginGetResponse, (r) => tRequest.EndGetResponse(r).GetResponseStream())
               .Connect(Pipes.ReadWrite<Stream, Int32>(new Byte[1024], (s) => s.BeginRead, s => s.EndRead, tResult.BeginWrite, tResult.EndWrite).Loop(t => 0, i => i > 0).Dispose());

            // Start.
            IAsyncResult ar = pipe.BeginFlow(null, null);

            // Wait.
            pipe.EndFlow(ar);

            tResult.Position = 0;
            Console.WriteLine("Got: " + new StreamReader(tResult).ReadToEnd());


         }
         catch (Exception e)
         {
            Console.WriteLine("ERROR : " + e.Message + Environment.NewLine + e.StackTrace);
            Console.Read();
         }
         Console.Read();
         Console.Read();
      }


      static Int64 Time(Action a)
      {
         Stopwatch tTimer = Stopwatch.StartNew();
         a();
         return tTimer.ElapsedMilliseconds;
      }
   }
}
