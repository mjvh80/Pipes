using System;
using System.Collections.Generic;
using System.Text;

namespace PipesCore
{
   internal class WrappingAsyncResult<A> : IAsyncResult where A : IAsyncResult
   {
      protected A mInnerResult;

      public A InnerResult { get { return mInnerResult; } }

      public WrappingAsyncResult(A innerResult)
      {
         mInnerResult = innerResult;
      }

      public object AsyncState
      {
         get { return mInnerResult.AsyncState; }
      }

      public System.Threading.WaitHandle AsyncWaitHandle
      {
         get { return mInnerResult.AsyncWaitHandle; }
      }

      public bool CompletedSynchronously
      {
         get { return mInnerResult.CompletedSynchronously; }
      }

      public bool IsCompleted
      {
         get { return mInnerResult.IsCompleted; }
      }

      public static implicit operator A(WrappingAsyncResult<A> result)
      {
         return (A)result.mInnerResult;
      }
   }
}
