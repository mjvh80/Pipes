using System;
using System.IO;
using System.Net;
using System.Text;
using PipesCore;
using System.Threading;

namespace PipesCore
{
   public class SimpleDowloader
   {
      private static Int32 sBufferSize = 2048;

      #region Overloads

      public IAsyncResult BeginDownload(HttpWebRequest pRequest, AsyncCallback pCallback, Object pState)
      {
         return BeginDownload(pRequest, (Stream)null, pCallback, pState);
      }

      public IAsyncResult BeginDownload(HttpWebRequest pRequest, String pBody, AsyncCallback pCallback, Object pState)
      {
         return BeginDownload(pRequest, pBody, null, pCallback, pState);
      }

      public IAsyncResult BeginDownload(HttpWebRequest pRequest, Stream pDestination, AsyncCallback pCallback, Object pState)
      {
         return BeginDownload(pRequest, (Stream)null, pDestination, pCallback, pState);
      }

      public IAsyncResult BeginDownload(HttpWebRequest pRequest, String pBody, Stream pDestinationOrNull, AsyncCallback pCallback, Object pState)
      {
         MemoryStream tStringStream = null;
         if (pBody != null)
         {
            tStringStream = new MemoryStream(Encoding.UTF8.GetBytes(pBody));
            tStringStream.Position = 0;
         }
         return BeginDownload(pRequest, tStringStream, pDestinationOrNull, pCallback, pState);
      }

      public IAsyncResult BeginDownload(String pHttpUrl, AsyncCallback pCallback, Object pState)
      {
         HttpWebRequest tRequest = (HttpWebRequest)WebRequest.Create(pHttpUrl);
         return BeginDownload(tRequest, pCallback, pState);
      }

      public IAsyncResult BeginDownload(HttpWebRequest pRequest, Stream pSourceOrNull, Stream pDestinationOrNull, AsyncCallback pCallback, Object pState)
      {
         return BeginDownload(pRequest, pSourceOrNull, false, pDestinationOrNull, pCallback, pState);
      }

      #endregion

      // Example with callback which needs to capture closure state so to speak.
      public IAsyncResult BeginDownload(HttpWebRequest pRequest, Stream pSourceOrNull, Boolean pCloseSourceStream, Stream pDestinationOrNull, AsyncCallback pCallback, Object pState)
      {
         // Pipe for doing request. todo don't create if not needed..
         var requestPipe = pSourceOrNull == null ? Pipes.Null<Stream, Int32>() : 
             Pipes.Create<Stream, Stream>(pRequest.BeginGetRequestStream, pRequest.EndGetRequestStream)
                  .WithResult((s, pipe) =>
                      Pipes.ReadWrite<Stream, Int32>(new Byte[sBufferSize], __s => pSourceOrNull.BeginRead, __s => pSourceOrNull.EndRead, s.BeginWrite, s.EndWrite)
                           .Loop(i => i > 0)
                           .Dispose());

         // Connect up.
         var finalPipe =
            requestPipe
               .Connect(pRequest.BeginGetResponse, pRequest.EndGetResponse)
               .WithResult((wr, pipe) =>
               {
                  Stream tTargetStream = wr.GetResponseStream(); // don't need to dispose -> webresponse will do this
                  Stream tResultStream = pDestinationOrNull ?? new MemoryStream((Int32)(wr.ContentLength <= 0 ? sBufferSize : wr.ContentLength)); // probably want to max this here..
                  return Pipes.ReadWrite<WebResponse, Int32>(new Byte[sBufferSize], s => tTargetStream.BeginRead, s => tTargetStream.EndRead, tResultStream.BeginWrite, tResultStream.EndWrite)
                              .Loop(i => i > 0)
                              .Dispose() // get rid of webresponse
                              .Map(i => tResultStream);
               });

         IAsyncResult tActualResult = null;

         // Note that this callback *can* be called on this thread (ie synchronously)!
         AsyncCallback tActualCallback = r =>
            {
               Interlocked.CompareExchange(ref tActualResult, new DownloadResult(r, pRequest, finalPipe), null);
               pCallback(tActualResult);
            };

         Interlocked.CompareExchange(ref tActualResult, new DownloadResult(finalPipe.BeginFlow(tActualCallback, pState), pRequest, finalPipe), null);
         return tActualResult;
      }

      internal class DownloadResult : WrappingAsyncResult<IAsyncResult>
      {
         public HttpWebRequest Request;
         public Pipe<Stream, Stream> Pipe;

         public DownloadResult(IAsyncResult innerResult, HttpWebRequest request, Pipe<Stream, Stream> pipe)
            : base(innerResult)
         {
            Request = request;
            Pipe = pipe;
         }
      }

      public void CancelDownload(IAsyncResult pResult)
      {
         DownloadResult tDlRes = pResult as DownloadResult;
         if (tDlRes == null)
            throw new InvalidOperationException("Invalid async result passed.");

         try
         {
            tDlRes.Request.Abort();
         }
         catch (WebException e)
         {
            if (e.Status != WebExceptionStatus.RequestCanceled)
               throw;
         }
      }

      public Stream EndDownload(IAsyncResult pResult)
      {
         DownloadResult tDlRes = pResult as DownloadResult;
         if (tDlRes == null)
            throw new InvalidOperationException("Invalid async result passed.");
         return tDlRes.Pipe.EndFlow(tDlRes.InnerResult);
      }
   }
}
