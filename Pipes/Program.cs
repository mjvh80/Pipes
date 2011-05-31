using System;
using System.IO;
using System.Net;
using System.Text;

namespace PipesCore
{
   class Program
   {
      public static void Main(String[] args)
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
