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
            // Download a google search into a console stream, entirely asynchronously.

            WebRequest tRequest = WebRequest.Create("http://localhost/asptest/Foo.ashx");
            tRequest.Method = "POST";

            Byte[] tRequestBuffer = Encoding.UTF8.GetBytes("");

            MemoryStream tResult = new MemoryStream(); // for testing

             //Byte[] tBuffer = new Byte[1024];
            //var readWrite = Pipes.Create<Stream, Int32>((s, c, o) => s.BeginRead(tBuffer, 0, 1024, c, o), (s, r) => s.EndRead(r))
            //   .Connect(Pipes.Create<Int32, Int32>((i, c, o) => tResult.BeginWrite(tBuffer, 0, i, c, o), (i, r) => { tResult.EndWrite(r); return i; }));

            var pipe = Pipes.Create<Stream, Stream>(tRequest.BeginGetRequestStream, tRequest.EndGetRequestStream)
               .Connect(Pipes.CreateEnd<Stream, Stream>((str, cb, state) => str.BeginWrite(tRequestBuffer, 0, tRequestBuffer.Length, cb, state), (s, r) => s.EndWrite(r)))
               .Connect((cb, state) => tRequest.BeginGetResponse(cb, state), (r) => tRequest.EndGetResponse(r).GetResponseStream())
               .Connect(Pipes.ReadWrite<Stream, Int32>(new Byte[1024], (s) => s.BeginRead, s => s.EndRead, tResult.BeginWrite, tResult.EndWrite).Loop(t => 0, i => i > 0));

            // Start.
            IAsyncResult ar = pipe.BeginFlow(null, null);

            // Wait.
            pipe.EndFlow(ar);

            tResult.Position = 0;
            Console.WriteLine("Got: " + new StreamReader(tResult).ReadToEnd());


         }
         catch (Exception e)
         {
            Console.WriteLine("ERROR : " + e.Message);
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
