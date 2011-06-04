using System;
using System.IO;
using System.Net;
using System.Text;
using PipesCore;
using System.Threading;

namespace PipesCore
{
   // Example, playing with closing over the async result... which is a bit hard.

   public class SimpleDownloaderWithPing
   {
      private static Int32 sBufferSize = 2048;

      #region Overloads

      public IAsyncResult BeginDownload(HttpWebRequest pRequest, AsyncCallback pCallback, Object pState)
      {
         return BeginDownload(pRequest, (Stream)null, pCallback, pState);
      }

      public IAsyncResult BeginDownload(HttpWebRequest pRequest, String pBody, AsyncCallback pCallback, Object pState)
      {
         return BeginDownload(pRequest, pBody, null, pCallback, null, pState);
      }

      public IAsyncResult BeginDownload(HttpWebRequest pRequest, Stream pDestination, AsyncCallback pCallback, Object pState)
      {
         return BeginDownload(pRequest, pDestination, pCallback, null, pState);
      }

      public IAsyncResult BeginDownload(HttpWebRequest pRequest, Stream pDestination, AsyncCallback pCallback, AsyncCallback pPingCallback, Object pState)
      {
         return BeginDownload(pRequest, (Stream)null, pDestination, pCallback, pPingCallback, pState);
      }

      public IAsyncResult BeginDownload(HttpWebRequest pRequest, String pBody, Stream pDestinationOrNull, AsyncCallback pCallback, AsyncCallback pPingCallback, Object pState)
      {
         MemoryStream tStringStream = null;
         if (pBody != null)
         {
            tStringStream = new MemoryStream(Encoding.UTF8.GetBytes(pBody));
            tStringStream.Position = 0;
         }
         return BeginDownload(pRequest, tStringStream, pDestinationOrNull, pCallback, pPingCallback, pState);
      }

      public IAsyncResult BeginDownload(String pHttpUrl, AsyncCallback pCallback, Object pState)
      {
         HttpWebRequest tRequest = (HttpWebRequest)WebRequest.Create(pHttpUrl);
         return BeginDownload(tRequest, pCallback, pState);
      }

      public IAsyncResult BeginDownload(HttpWebRequest pRequest, Stream pSourceOrNull, Stream pDestinationOrNull, AsyncCallback pCallback, AsyncCallback pPingCallback, Object pState)
      {
         return BeginDownload(pRequest, pSourceOrNull, false, pDestinationOrNull, pCallback, pPingCallback, pState);
      }

      #endregion

      // Example with callback which needs to capture closure state so to speak.
      public IAsyncResult BeginDownload(HttpWebRequest pRequest, Stream pSourceOrNull, Boolean pCloseSourceStream, Stream pDestinationOrNull, AsyncCallback pCallback, AsyncCallback pPingCallback, Object pState)
      {
         // Build our pipe, save it and start it.
         DownloadResult tActualResult = null;
         Object tLock = new Object();

         // Below pipe uses "WaitOnPoolIf". Another method is
         AsyncCallback tActualPingCallback = r => ThreadPool.QueueUserWorkItem(o =>
            {
               lock (tLock)
                  pCallback(r);
            });

         // Pipe for doing request. todo don't create if not needed..
         var requestPipe =
             Pipes.Create<Stream, Stream>(pRequest.BeginGetRequestStream, pRequest.EndGetRequestStream)
                  .WithResult((s, pipe) =>
                      Pipes.ReadWrite<Stream, Int32>(new Byte[sBufferSize], __s => pSourceOrNull.BeginRead, __s => pSourceOrNull.EndRead, s.BeginWrite, s.EndWrite)
                           .DoIf(pPingCallback != null, () => tActualPingCallback(tActualResult)) // todo: crap, must wrap here too MUST be sure it's assigned here..
                           .Loop(i => i > 0)
                           .Dispose());

         // Response pipe.
         var finalPipe = (pSourceOrNull == null ? Pipes.Null<Stream, Int32>() : requestPipe)
            .Connect(pRequest.BeginGetResponse, pRequest.EndGetResponse)
            .WithResult((wr, pipe) =>
            {
               Stream tTargetStream = wr.GetResponseStream(); // don't need to dispose -> webresponse will do this
               Stream tResultStream = pDestinationOrNull == null ? new MemoryStream((Int32)(wr.ContentLength <= 0 ? 1024 : wr.ContentLength)) : pDestinationOrNull; // todo: may want to max our stream size here, or we'll run out of mem.
               return
                  Pipes.WaitOnPoolIf<WebResponse>(tActualResult == null, tLock)
                       .Connect(
                           Pipes.ReadWrite<WebResponse, Int32>(new Byte[1024], s => tTargetStream.BeginRead, s => tTargetStream.EndRead, tResultStream.BeginWrite, tResultStream.EndWrite)
                                .DoIf(pPingCallback != null, () => pPingCallback(tActualResult))
                                .Loop(i => i > 0)
                                .Dispose() // get rid of webresponse
                                .Map(i => tResultStream)
                             );
            });

         // Start download, don't forget to wrap the callback as we're wrapping the result.
         // todo: must check actual result

         // We must queue the callback on a different thread in order to be sure our assignment happened.
         // todo: this sucks, we're losing synchronously completing code completely.

#if false

         lock(tLock)
            tActualResult = new DownloadResult(finalPipe.BeginFlow(r =>
            {
               if (tActualResult == null) // not yet assigned, must queue async
                  ThreadPool.QueueUserWorkItem((o) =>
                  {
                     lock (tLock)
                        pCallback(tActualResult);
                  });
               else
                  pCallback(tActualResult);
            } , pState), pRequest, finalPipe);
         return tActualResult;
#else

         // Better, I think: in case of the assignment not having happened, simply create a new wrapper. Note that we DO have the correct IAsyncResult to wrap
         // at this point, through the callback. todo: is it better simply to always create a new wrapper? It avoids the closure, so may be cheaper (?).
         // I suspect we'll need to ILDasm this for a bit.
         lock(tLock)
            tActualResult = new DownloadResult(finalPipe.BeginFlow(r => pCallback(tActualResult ?? new DownloadResult(r, pRequest, finalPipe)), pState), pRequest, finalPipe);
         return tActualResult;
#endif
      }

    

      protected class DownloadResult : WrappingAsyncResult<IAsyncResult>
      {
         public volatile HttpWebRequest Request;
         public volatile Pipe<Stream, Stream> Pipe;

         public DownloadResult(IAsyncResult innerResult, HttpWebRequest request, Pipe<Stream, Stream> pipe) : base(innerResult)
         {
            Request = request;
            Pipe = pipe;
         }
      }

      /// <summary>
      /// Note: after cancellation, it is likely that enddownload will throw exceptions.
      /// </summary>
      /// <param name="pResult"></param>
      public void CancelDownload(IAsyncResult pResult)
      {
         try {
             ((DownloadResult)pResult).Request.Abort();
         }
         catch (WebException e)
         {
            if (e.Status != WebExceptionStatus.RequestCanceled)
               throw;
         }
      }

      /// <summary>
      /// Ends the download, blocking if the download has not yet finished.
      /// Throws an exception if any exception occurred during the download process.
      /// Caller should attempt to dispose of the IAsyncResult object.
      /// </summary>
      public Stream EndDownload(IAsyncResult pResult)
      {
         DownloadResult tDlRes = (DownloadResult)pResult; // todo: hide bad casts
         return tDlRes.Pipe.EndFlow(tDlRes.InnerResult);
      }
   }
}
