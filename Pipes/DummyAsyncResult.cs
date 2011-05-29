using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConsoleApplication1
{
   /// <summary>
   /// Provides a Dummy IAsyncResult implementation that has completed, with an option for synchronous completion.
   /// The WaitHandle will return a singaled handle (set), which is NOT disposed.
   /// </summary>
   public class _DummyAsyncResult : IAsyncResult
   {
      public static readonly IAsyncResult CompletedSynchronouslyResult = new _DummyAsyncResult(true, null);
      public static readonly IAsyncResult CompletedAsynchronouslyResult = new _DummyAsyncResult(false, null);

      protected readonly Object mState;
      protected readonly Boolean mSynchronous;

      public _DummyAsyncResult() : this(false, null) { }

      public _DummyAsyncResult(Boolean pCompletedSynchronously, Object pState)
      {
         mState = pState;
         mSynchronous = pCompletedSynchronously;
      }

      public object AsyncState
      {
         get { return mState; }
      }

      public System.Threading.WaitHandle AsyncWaitHandle
      {
         get { return new ManualResetEvent(true); }
      }

      public bool CompletedSynchronously
      {
         get { return mSynchronous; }
      }

      public bool IsCompleted
      {
         get { return true; }
      }
   }
}
