using System;
using System.Threading;

namespace PipesCore
{
   /// <summary>
   /// General purpose thread safe IAsyncResult implementation with exception handling and (basic) support
   /// for caller state and callback handling.
   /// In the standard .NET begin* end* pattern, a state object must be passed through, which often means that
   /// an extra state object is desired. Thus the state object should be the one received by a (possible) invoker
   /// whereas the caller state is the actual state one may wish to pass along.
   /// </summary>
   public class AsyncResult : IAsyncResult, IDisposable
   {
      protected Object mState;
      protected ManualResetEvent mHandle;
      protected Boolean mIsCompleted;
      protected Boolean mCompletedSynchronously;
      protected AsyncCallback mUserCallback;
      protected Object mSyncRoot = new Object();
      protected Object mCallerState;

      /// <summary>
      /// Pass null for any parameters not used, including the callback (in which case it is expected the client
      /// uses the WaitHandle for completion notification).
      /// </summary>
      /// <param name="pCallback"></param>
      /// <param name="pCallerState"></param>
      /// <param name="pState"></param>
      public AsyncResult(AsyncCallback pCallback, Object pCallerState, Object pState)
      {
         mState = pState;
         mCallerState = pCallerState;
         mIsCompleted = false;
         mCompletedSynchronously = false;
         mUserCallback = pCallback;
      }

      public Object AsyncState { get { return mState; } }

      // If a wait handle is requested, then provide one. Notice that we set it in signaled state if we are completed.
      public WaitHandle AsyncWaitHandle
      {
         get { lock (mSyncRoot) return mHandle ?? (mHandle = new ManualResetEvent(mIsCompleted)); }
      }

      public Boolean CompletedSynchronously { get { lock (mSyncRoot) return mCompletedSynchronously; } }

      public Boolean IsCompleted
      {
         get { lock (mSyncRoot) return mIsCompleted; }
      }

      /// <summary>
      /// Call to mark that the asynchronous operation that this instance represents is completed.
      /// This will cause the user's callback to fire, if one is defined, or the waithandle to be signaled,
      /// if one was instantiated (ie someone is waiting for it).
      /// </summary>
      public void MarkComplete()
      {
         lock (mSyncRoot)
            if (!mIsCompleted)
            {
               mIsCompleted = true;

               if (mHandle != null) mHandle.Set();

               // If an exception happens in the callback, this thread won't be affected.
               if (mUserCallback != null)
                  //mUserCallback(this);
                  ThreadPool.QueueUserWorkItem(o => mUserCallback(this));
            }
      }

      /// <summary>
      /// Marks as completed synchronously, which is another method for signalling certain types (synchronous) of completion.
      /// </summary>
      public void MarkCompletedSynchronously()
      {
         lock (mSyncRoot)
         {
            if (!mIsCompleted) // if we're already completed, this makes no sense..
            {
               mCompletedSynchronously = true;
               MarkComplete();
            }
         }
      }

      /// <summary>
      /// Handle an exception with the following semantics:
      /// - the async operation should be aborted, thus marked complete
      /// - if an exception was previously handled, simply log the exception
      /// </summary>
      /// <param name="pEx"></param>
      public void HandleException(Exception pEx)
      {
         lock (mSyncRoot)
         {
            if (mException == null)
               this.mException = pEx;
           // else // only grab the first one, others get logged
             //  ExceptionLog.Log(pEx, "AsyncResult::HandleException: multiple requests for exception handling. Only logged.");
          
            MarkComplete();
         }
      }

      protected Exception mException;
      public Exception Exception { get { lock (mSyncRoot) return mException; } }

      /// <summary>
      /// Internally disposes of the reset event if one was created.
      /// </summary>
      public virtual void Dispose()
      {
         lock (mSyncRoot)
         {
            if (mHandle != null)
               ((IDisposable)mHandle).Dispose();
         }
      }

      /// <summary>
      /// Never throws an exception.
      /// </summary>
      public void SilentDispose()
      {
         try
         {
            Dispose();
         }
         catch
         {
            // log
         }
      }

      public Object CallerState
      {
         get
         {
            lock (mSyncRoot) return mCallerState;
         }
         set
         {
            lock (mSyncRoot) mCallerState = value;
         }
      }
   }
}
