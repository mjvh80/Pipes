using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aquabrowser.Core.Threading;
using System.Threading;
using System.IO;

namespace SomeNamespace
{
   public interface IPipe<in I, out T> { }

   public static class Pipes
   {
      /*
      public static Pipe<I, Int32> ReadWrite<I, T>(Byte[] buffer, Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult> beginRead, Func<IAsyncResult, Int32> endRead,
         Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult> beginWrite, Action<IAsyncResult> endWrite)
      {
         return ReadWrite<I, Int32>(buffer, 0, buffer.Length, beginRead, endRead, beginWrite, endWrite);
      }

      public static Pipe<I, Int32> ReadWrite<I, T>(Byte[] buffer, Int32 offset, Int32 count, Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult> beginRead, Func<IAsyncResult, Int32> endRead,
         Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult> beginWrite, Action<IAsyncResult> endWrite)
      {
         return Create<I, Int32>((i, cb, s) => beginRead(buffer, offset, count, cb, s), r => endRead(r)).Connect((i, cb, s) => beginWrite(buffer, 0, i, cb, s), (i, r) => { endWrite(r); return i; });
      }*/

      public static Pipe<I, Int32> ReadWrite<I, T>(Byte[] buffer, Func<I, Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult>> beginRead, Func<I, Func<IAsyncResult, Int32>> endRead,
        Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult> beginWrite, Action<IAsyncResult> endWrite)
      {
         return ReadWrite<I, T>(buffer, 0, buffer.Length, beginRead, endRead, beginWrite, endWrite);
      }

      public static Pipe<I, Int32> ReadWrite<I, T>(Byte[] buffer, Int32 offset, Int32 count, Func<I, Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult>> beginRead, Func<I, Func<IAsyncResult, Int32>> endRead,
        Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult> beginWrite, Action<IAsyncResult> endWrite)
      {
         return Create<I, Int32>((i, cb, s) => beginRead(i)(buffer, offset, count, cb, s), (i, r) => endRead(i)(r)).Connect((i, cb, s) => beginWrite(buffer, 0, i, cb, s), (i, r) => { endWrite(r); return i; });
      }

      public static Pipe<I, T> Create<I, T>(Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, Func<I, IAsyncResult, T> endMethod)
      {
         return new DelegatePipe<I, T>(beginMethod, endMethod);
      }

      public static Pipe<I, T> Create<I, T>(Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, Func<IAsyncResult, T> endMethod)
      {
         return Create(beginMethod, (i, r) => endMethod(r));
      }

      public static Pipe<I, T> Create<I, T>(Func<AsyncCallback, Object, IAsyncResult> beginMethod, Func<IAsyncResult, T> endMethod)
      {
         return Create<I, T>((i, c, s) => beginMethod(c, s), (i, r) => endMethod(r)); // ignore input 
      }

      public static Pipe<I, T> CreateEnd<I, T>(Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, Action<I, IAsyncResult> endMethod)
      {
         return Create<I, T>((i, c, s) => beginMethod(i, c, s), (i, r) => { endMethod(i, r); return default(T); }); // ignore output 
      }

      public static Pipe<I, T> CreateEnd<I, T>(Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod)
      {
         return Create<I, T>((i, c, s) => beginMethod(i, c, s), (i, r) => { endMethod(r); return default(T); }); // ignore output 
      }

      public static Pipe<I, T> CreateEnd<I, T>(Func<AsyncCallback, Object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod)
      {
         return Create<I, T>((i, c, s) => beginMethod(c, s), (i, r) => { endMethod(r); return default(T); }); // ignore output 
      }

      
      public static Pipe<I, U> Connect<I, T, U>(this Pipe<I, T> pipe, Func<T, AsyncCallback, Object, IAsyncResult> beginMethod, Func<T, IAsyncResult, U> endMethod)
      {
         return pipe.Connect(Pipes.Create<T, U>(beginMethod, endMethod));
      }

      public static Pipe<I, U> Connect<I, T, U>(this Pipe<I, T> pipe, Func<T, AsyncCallback, Object, IAsyncResult> beginMethod, Func<IAsyncResult, U> endMethod)
      {
         return pipe.Connect(Pipes.Create<T, U>(beginMethod, endMethod));
      }

      public static Pipe<I, U> Connect<I, T, U>(this Pipe<I, T> pipe, Func<AsyncCallback, Object, IAsyncResult> beginMethod, Func<IAsyncResult, U> endMethod)
      {
         return pipe.Connect(Pipes.Create<T, U>(beginMethod, endMethod));
      }

      public static Pipe<I, U> ConnectEnd<I, T, U>(this Pipe<I, T> pipe, Func<T, AsyncCallback, Object, IAsyncResult> beginMethod, Action<T, IAsyncResult> endMethod)
      {
         return pipe.Connect(Pipes.CreateEnd<T, U>(beginMethod, endMethod));
      }

      public static Pipe<I, U> ConnectEnd<I, T, U>(this Pipe<I, T> pipe, Func<T, AsyncCallback, Object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod)
      {
         return pipe.Connect(Pipes.CreateEnd<T, U>(beginMethod, endMethod));
      }

      public static Pipe<I, U> ConnectEnd<I, T, U>(this Pipe<I, T> pipe, Func<AsyncCallback, Object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod)
      {
         return pipe.Connect(Pipes.CreateEnd<T, U>(beginMethod, endMethod));
      }

      public static Pipe<I, T> Loop<I, T>(this Pipe<I, T> pipe, Func<T, T> next, Func<T, Boolean> predicate)
      {
         return pipe.Loop(next.ToPipe(), predicate);
      }

      //public static Pipe<I, U> Connect<I, T, U>(this Pipe<I, T> pipe, Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, Action<I, IAsyncResult> endMethod)
      //{
      //   return pipe.Connect(Pipes.Create<T, U>(beginMethod, endMethod));
      //}


      public static Func<I, T> ToFunc<I, T>(this Pipe<I, T> pipe)
      {
         return (i) => pipe.EndFlow(i, pipe.BeginFlow(i, null, null));
      }

      public static Pipe<T, T> ToPipe<T>(this Func<T, T> del)
      {
         return Create<T, T>((t, cb, s) => del.BeginInvoke(t, cb, s), (t, r) => del.EndInvoke(r));
      }

      public static Pipe<T, T> ToPipe<T>(this Action<T> del)
      {
         return CreateEnd<T, T>((t, cb, s) => del.BeginInvoke(t, cb, s), r => del.EndInvoke(r));
      }
   }

   public abstract class Pipe<I, T> : IPipe<I, T>
   {
      protected Action<I> mFinalAction = null;

      public abstract IAsyncResult BeginFlow(I input, AsyncCallback cb, Object state);

      public abstract T EndFlow(I input, IAsyncResult result);

      public IAsyncResult BeginFlow(AsyncCallback cb, Object state)
      {
         return BeginFlow(default(I), cb, state);
      }

      public T EndFlow(IAsyncResult result)
      {
         return EndFlow(default(I), result);
      }

      public virtual Pipe<I, T> Finally(Action<I> finalAction)
      {
         mFinalAction = finalAction;
         return this;
      }

      public Pipe<I, T> Dispose()
      {
         if (!typeof(IDisposable).IsAssignableFrom(typeof(I)))
            throw new InvalidOperationException(String.Format("Type {0} is not IDisposable.", typeof(I).Name));

         return Finally(i => ((IDisposable)i).Dispose());
      }

      protected virtual void DoFinally(I input)
      {
         if (mFinalAction != null)
            mFinalAction(input);
      }

      public Pipe<I, U> Connect<U>(Pipe<T, U> otherPipe)
      {
         return new ConnectedPipe<I, T, U>(this, otherPipe);
      }

      public Pipe<I, U> Branch<U>(Pipe<T, U> ifPipe, Pipe<T, U> elsePipe, Func<T, Boolean> predicate)
      {
         return new ConditionalPipe<I, T, U>(this, ifPipe, elsePipe, predicate);
      }

      public Pipe<I, R> Loop<R>(Pipe<T, R> nextPipe, Func<T, Boolean> predicate)
      {
         return new LoopingPipe<I, T, R>(this, nextPipe, predicate);
      }

      public Pipe<I, T> Filter(Func<T, T> filter)
      {
         return new DelegatePipe<I, T>(BeginFlow, (i, r) => filter(EndFlow(i, r)));
      }
   }

   internal class DelegatePipe<I, T> : Pipe<I, T>
   {
      protected Func<I, AsyncCallback, Object, IAsyncResult> beginMethod;
      protected Func<I, IAsyncResult, T> endMethod;

      public DelegatePipe(Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, Func<I, IAsyncResult, T> endMethod)
      {
         this.beginMethod = beginMethod;
         this.endMethod = endMethod;
      }

      public override IAsyncResult BeginFlow(I input, AsyncCallback cb, object state)
      {
         return beginMethod(input, cb, state);
      }

      public override T EndFlow(I input, IAsyncResult result)
      {
         try
         {
            return endMethod(input, result);
         }
         finally
         {
            DoFinally(input);
         }
      }
   }

   internal class FilteredPipe<I, T> : DelegatePipe<I, T>
   {
      protected Func<T, T> mFilter;

      public FilteredPipe(Pipe<I, T> pipe, Func<T, T> filter) : base(pipe.BeginFlow, pipe.EndFlow)
      {
         mFilter = filter;
      }

      public FilteredPipe(Pipe<I, T> pipe, Pipe<T, T> filterPipe) : this(pipe, filterPipe.ToFunc()) { }

      public override T EndFlow(I input, IAsyncResult result)
      {
         return mFilter(base.EndFlow(input, result));
      }
   }

   internal static class Counter { public static Int32 Count; }

   // connects left to right
   internal class ConnectedPipe<I, R, T> : Pipe<I, T>
   {
    //  private static int debug;// = 0;

      protected Pipe<I, R> mLeft;
      protected Pipe<R, T> mRight;
      //protected AsyncResult mResult; // todo: do we need to store?
      protected T mValue;

      public ConnectedPipe(Pipe<I, R> left, Pipe<R, T> right)
      {
         mLeft = left; mRight = right;
      }

      protected ConnectedPipe() { }

      protected virtual void Process(R value, AsyncResult completeResult)
      {
         RunPipe(mRight, value, completeResult);
      }

      protected virtual void RunPipe(Pipe<R, T> pipe, R value, AsyncResult completeResult)
      {
         try
         {
            pipe.BeginFlow(value, r =>
            {
               try
               {
                  mValue = pipe.EndFlow(value, r);
                  completeResult.MarkComplete();
               }
               catch (Exception e)
               {
                  completeResult.HandleException(e);
               }

               // Console.WriteLine(" - Completed result " + completeResult.CallerState);
            }, completeResult.AsyncState);
         }
         catch (Exception e2)
         {
            completeResult.HandleException(e2);
         }
      }

      public override IAsyncResult BeginFlow(I input, AsyncCallback cb, object state)
      {
         Int32 tmp = Interlocked.Increment(ref Counter.Count);
         AsyncResult tCompleteResult = new AsyncResult(cb, tmp, state);
         //Console.WriteLine(" + Starting flow in left (new result): " + tmp);
         mLeft.BeginFlow(input, r =>
            {
               try
               {
                  R tResult = mLeft.EndFlow(input, r);
                  Process(tResult, tCompleteResult); // close over tCompleteResult
               }
               catch (Exception e)
               {
                  tCompleteResult.HandleException(e);
               }
            }, state);
         return tCompleteResult;
      }

      public override T EndFlow(I input, IAsyncResult result)
      {
         try
         {
            AsyncResult tCompleteResult = (AsyncResult)result;
            if (!tCompleteResult.IsCompleted)
               while (!tCompleteResult.AsyncWaitHandle.WaitOne(1000))
                  Console.WriteLine("Waiting on wait handle... retrying " + tCompleteResult.CallerState); // todo: log to something else

            if (tCompleteResult.Exception != null)
               throw tCompleteResult.Exception;

            return mValue;
         }
         finally
         {
            DoFinally(input);
         }
      }
   }

   internal class ConditionalPipe<I, R, T> : ConnectedPipe<I, R, T>
   {
      protected Func<R, Boolean> mPredicate;
      protected Pipe<R, T> mOtherPipe;

      public ConditionalPipe(Pipe<I, R> inPipe, Pipe<R, T> ifPipe, Pipe<R,T> elsePipe, Func<R, Boolean> predicate) : base(inPipe, ifPipe)
      {
         mPredicate = predicate;
         mOtherPipe = elsePipe;
      }

      protected override void Process(R value, AsyncResult completeResult)
      {
         Pipe<R,T> pipeToRun = null;
         //Console.WriteLine("Running predicate on value " + value);
         if (mPredicate(value))
            pipeToRun = mRight;
         else
            pipeToRun = mOtherPipe;

         RunPipe(pipeToRun, value, completeResult);
      }
   }
   
   internal class LoopingPipe<I, R, T> : ConditionalPipe<I, R, T>
   {
      protected I mInput;

      public LoopingPipe(Pipe<I, R> inPipe, Pipe<R, T> elsePipe, Func<R, Boolean> predicate) : base(inPipe, null, elsePipe, predicate) 
      {
         mRight = BuildLoop();
      }

      public override IAsyncResult BeginFlow(I input, AsyncCallback cb, object state)
      {
         mInput = input; // capture input for looping
         return base.BeginFlow(input, cb, state);
      }

      protected virtual Pipe<R, T> BuildLoop()
      {
         return Pipes.Create<R, T>((i, cb, o) => this.BeginFlow(mInput, cb, o), (i, r) => this.EndFlow(mInput, r));
      }

      public override T EndFlow(I input, IAsyncResult result)
      {
         return base.EndFlow(input, result);
      }
   }


}
