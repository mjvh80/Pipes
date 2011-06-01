using System;
using System.IO;
using System.Net;
using System.Text;

namespace PipesCore
{
   class Program
   {
      public static void Test()
      {
         try {
            Int32 sBufferSize = 2048;
            Byte[] tRequestBuffer = new Byte[sBufferSize];
            MemoryStream tResult = new MemoryStream();

            WebRequest pRequest = WebRequest.Create("http://localhost/asptest/Foo.ashx");
            pRequest.Method = "POST";

            MemoryStream pSourceOrNull = new MemoryStream();
            MemoryStream tempStream = new MemoryStream();
            using (StreamWriter tWriter = new StreamWriter(tempStream))
            {
               tWriter.Write("Hello World");
               tWriter.Flush();
               tempStream.Position = 0;
               tempStream.WriteTo(pSourceOrNull);
            }
            pSourceOrNull.Position = 0;


            var requestPipe = Pipes.Create<Stream, Stream>(pRequest.BeginGetRequestStream, pRequest.EndGetRequestStream)
               .WithResult((s, pipe) => 
                  pipe.Connect(Pipes.ReadWrite<Stream, Int32>(new Byte[sBufferSize], __s => pSourceOrNull.BeginRead, __s => pSourceOrNull.EndRead, s.BeginWrite, s.EndWrite).Loop(i => i > 0).Dispose()));

            var finalPipe = (pSourceOrNull == null ? Pipes.Null<Stream, Int32>() : requestPipe)
               .Connect(pRequest.BeginGetResponse, pRequest.EndGetResponse)
               .Map(webres => webres.GetResponseStream())
               .Connect(Pipes.ReadWrite<Stream, Int32>(new Byte[1024], s => s.BeginRead, s => s.EndRead, tResult.BeginWrite, tResult.EndWrite).Loop(i => i > 0).Dispose())
               .Map(i => tResult);

            MemoryStream tOut = finalPipe.EndFlow(finalPipe.BeginFlow(null, null));
            tOut.Position = 0;
            Console.WriteLine("Got: " + new StreamReader(tResult).ReadToEnd());
         }
         catch (Exception e)
         {
            Console.WriteLine("ERROR : " + e.Message + Environment.NewLine + e.StackTrace);
            Console.Read();
         }
         Console.Read();
      }

      public static void Main(String[] args) { Test(); }

      public static void Main2(String[] args)
      {
         try
         {
            /* Example program that uses a local service which echoes the request. */
            WebRequest tRequest = WebRequest.Create("http://localhost/asptest/Foo.ashx");
            tRequest.Method = "POST";

            /* Random, 9000 a's followed by a B. */
            Byte[] tRequestBuffer = Encoding.UTF8.GetBytes(new String('a', 9000) + "B"); // something here
            MemoryStream tResult = new MemoryStream(); // usually this would be a client, say, with us acting as proxy

            var pipe = Pipes.Create<Stream, Stream>(tRequest.BeginGetRequestStream, tRequest.EndGetRequestStream)
               .Connect(Pipes.CreateEnd<Stream, Stream>((str, cb, state) => str.BeginWrite(tRequestBuffer, 0, tRequestBuffer.Length, cb, state), (s, r) => s.EndWrite(r)).Dispose())
               .Connect(tRequest.BeginGetResponse, tRequest.EndGetResponse) // (r) => tRequest.EndGetResponse(r).GetResponseStream())
               .Map(webres => webres.GetResponseStream()) // can also do this above of course, as in comment
               .Connect(Pipes.ReadWrite<Stream, Int32>(new Byte[1024], (s) => s.BeginRead, s => s.EndRead, tResult.BeginWrite, tResult.EndWrite).Loop(i => i > 0).Dispose());

            // Start.
            IAsyncResult ar = pipe.BeginFlow(null, 7);

            if ((Int32)ar.AsyncState != 7)
               throw new Exception("bad state");

            // Wait here.
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
   }
}
