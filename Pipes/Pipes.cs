using System;
using System.IO;

namespace PipesCore
{
   public interface IPipe<in I, out T>
   {
      IAsyncResult BeginFlow(I input, AsyncCallback cb, Object state);
      T EndFlow(I input, IAsyncResult result);

      IAsyncResult BeginFlow(AsyncCallback cb, Object state);
      T EndFlow(IAsyncResult result);
   }

   // Redefinition of Action and Func delegates: allows usage in .NET 2.
   // Can be renamed to Func/Action in .NET 4 (or 3.5, it needs last one).
   public delegate void _Action();
   public delegate void _Action<T>(T item);
   public delegate void _Action<T1, T2>(T1 item1, T2 item2);
   public delegate T _Func<T>();
   public delegate T _Func<I, T>(I item);
   public delegate T _Func<I1, I2, T>(I1 item1, I2 item2);
   public delegate T _Func<I1, I2, I3, T>(I1 item1, I2 item2, I3 item3);
   public delegate T _Func<I1, I2, I3, I4, T>(I1 item1, I2 item2, I3 item3, I4 item4);
   public delegate T _Func<I1, I2, I3, I4, I5, T>(I1 item1, I2 item2, I3 item3, I4 item4, I5 item5); // you'll need this one in .net 3.5

   public static class Pipes
   {
      public static Pipe<I, Int32> ReadWrite<I, T>(Byte[] buffer, _Func<I, _Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult>> beginRead, _Func<I, _Func<IAsyncResult, Int32>> endRead,
        _Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult> beginWrite, _Action<IAsyncResult> endWrite)
      {
         return ReadWrite<I, T>(buffer, 0, buffer.Length, beginRead, endRead, beginWrite, endWrite);
      }

      public static Pipe<I, Int32> ReadWrite<I, T>(Byte[] buffer, Int32 offset, Int32 count, _Func<I, _Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult>> beginRead, _Func<I, _Func<IAsyncResult, Int32>> endRead,
        _Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult> beginWrite, _Action<IAsyncResult> endWrite)
      {
         return Create<I, Int32>((i, cb, s) => beginRead(i)(buffer, offset, count, cb, s), (i, r) => endRead(i)(r)).Connect((i, cb, s) => beginWrite(buffer, 0, i, cb, s), (i, r) => { endWrite(r); return i; });
      }

      // variant that adds a target

      public static Pipe<I, Int32> ReadWrite<I, T, S>(Byte[] buffer, S target,_Func<I, _Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult>> beginRead, _Func<I, _Func<IAsyncResult, Int32>> endRead,
        _Func<S, _Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult>> beginWrite, _Func<S, _Action<IAsyncResult>> endWrite)
      {
         return ReadWrite<I, T, S>(buffer, 0, buffer.Length, target, beginRead, endRead, beginWrite, endWrite);
      }

      public static Pipe<I, Int32> ReadWrite<I, T, S>(Byte[] buffer, Int32 offset, Int32 count, S target, _Func<I, _Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult>> beginRead, _Func<I, _Func<IAsyncResult, Int32>> endRead,
        _Func<S, _Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult>> beginWrite, _Func<S, _Action<IAsyncResult>> endWrite)
      {
         return Create<I, Int32>((i, cb, s) => beginRead(i)(buffer, offset, count, cb, s), (i, r) => endRead(i)(r)).Connect((i, cb, s) => beginWrite(target)(buffer, 0, i, cb, s), (i, r) => { endWrite(target)(r); return i; });
      }

      public static Pipe<I, T> Null<I, T>()
      {
         return new NullPipe<I, T>();
      }

      public static Pipe<I, T> Create<I, T>(_Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, _Func<I, IAsyncResult, T> endMethod)
      {
         return new DelegatePipe<I, T>(beginMethod, endMethod);
      }

      public static Pipe<I, T> Create<I, T>(_Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, _Func<IAsyncResult, T> endMethod)
      {
         return Create(beginMethod, (i, r) => endMethod(r));
      }

      public static Pipe<I, T> Create<I, T>(_Func<AsyncCallback, Object, IAsyncResult> beginMethod, _Func<IAsyncResult, T> endMethod)
      {
         return Create<I, T>((i, c, s) => beginMethod(c, s), (i, r) => endMethod(r)); // ignore input 
      }

      public static Pipe<I, T> CreateEnd<I, T>(_Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, _Action<I, IAsyncResult> endMethod)
      {
         return Create<I, T>((i, c, s) => beginMethod(i, c, s), (i, r) => { endMethod(i, r); return default(T); }); // ignore output 
      }

      public static Pipe<I, T> CreateEnd<I, T>(_Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, _Action<IAsyncResult> endMethod)
      {
         return Create<I, T>((i, c, s) => beginMethod(i, c, s), (i, r) => { endMethod(r); return default(T); }); // ignore output 
      }

      public static Pipe<I, T> CreateEnd<I, T>(_Func<AsyncCallback, Object, IAsyncResult> beginMethod, _Action<IAsyncResult> endMethod)
      {
         return Create<I, T>((i, c, s) => beginMethod(c, s), (i, r) => { endMethod(r); return default(T); }); // ignore output 
      }

      public static Pipe<I, T> ToPipe<I, T>(this _Func<I, T> del)
      {
         return Create<I, T>((i, cb, o) => del.BeginInvoke(i, cb, o), (r) => del.EndInvoke(r));
      }

      public static Pipe<T, T> ToPipe<T>(this _Func<T> del)
      {
         return Create<T, T>((cb, o) => del.BeginInvoke(null, null), r => del.EndInvoke(r));
      }

      public static Pipe<T, T> ToPipe<T>(this _Action<T> del)
      {
         return CreateEnd<T, T>((t, cb, s) => del.BeginInvoke(t, cb, s), r => del.EndInvoke(r));
      }
   }

   public abstract class Pipe<I, T> : IPipe<I, T>
   {
      protected _Action<I> mFinalAction = null;

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

      protected virtual void DoFinally(I input)
      {
         if (mFinalAction != null)
            mFinalAction(input);
      }

      #region Pipe Operations

      public Pipe<I, U> Map<U>(Func<T, U> map)
      {
         return this.Connect(new FunctionPipe<T, U>(map));
      }

      public Pipe<R, S> With<U, R, S>(U value, Func<U, Pipe<I, T>, Pipe<R, S>> map)
      {
         return map(value, this);
      }

      public Pipe<I, T> With<U>(U value, Func<U, Pipe<I, T>, Pipe<I, T>> map)
      {
         return With<U, I, T>(value, map);
      }

      public Pipe<I, R> WithResult<R>(Func<T, Pipe<I, T>, Pipe<T, R>> map)
      {
         AdapterPipe<T, R> tAdapter = new AdapterPipe<T, R>(null);

         return this.Connect<T>(new FunctionPipe<T, T>(t =>
         {
            tAdapter.InnerPipe = map(t, this);
            return t;
         }))
            .Connect<R>(tAdapter); // connect an empty adapter, which is created by map
      }

      public  Pipe<I, U> Connect<U>(_Func<T, AsyncCallback, Object, IAsyncResult> beginMethod, _Func<T, IAsyncResult, U> endMethod)
      {
         return this.Connect(Pipes.Create<T, U>(beginMethod, endMethod));
      }

      public  Pipe<I, U> Connect<U>(_Func<T, AsyncCallback, Object, IAsyncResult> beginMethod, _Func<IAsyncResult, U> endMethod)
      {
         return this.Connect(Pipes.Create<T, U>(beginMethod, endMethod));
      }

      public  Pipe<I, U> Connect<U>(_Func<AsyncCallback, Object, IAsyncResult> beginMethod, _Func<IAsyncResult, U> endMethod)
      {
         return this.Connect(Pipes.Create<T, U>(beginMethod, endMethod));
      }

      public Pipe<I, U> Connect<U>(Pipe<T, U> otherPipe)
      {
         return new ConnectedPipe<I, T, U>(this, otherPipe);
      }

      public  Pipe<I, U> ConnectEnd<U>(_Func<T, AsyncCallback, Object, IAsyncResult> beginMethod, _Action<T, IAsyncResult> endMethod)
      {
         return this.Connect(Pipes.CreateEnd<T, U>(beginMethod, endMethod));
      }

      public  Pipe<I, U> ConnectEnd<U>(_Func<T, AsyncCallback, Object, IAsyncResult> beginMethod, _Action<IAsyncResult> endMethod)
      {
         return this.Connect(Pipes.CreateEnd<T, U>(beginMethod, endMethod));
      }

      public  Pipe<I, U> ConnectEnd<U>(_Func<AsyncCallback, Object, IAsyncResult> beginMethod, _Action<IAsyncResult> endMethod)
      {
         return this.Connect(Pipes.CreateEnd<T, U>(beginMethod, endMethod));
      }

      public  Pipe<I, T> Loop(_Func<T, Boolean> predicate)
      {
         return Loop(t => t, predicate);
      }

      public  Pipe<I, T> Loop(_Func<T, T> next, _Func<T, Boolean> predicate)
      {
         return Loop(next.ToPipe(), predicate);
      }

      public  Pipe<I, T> Loop(Pipe<T, T> next, _Func<T, Boolean> predicate)
      {
         return Loop<T>(next, predicate);
      }

      public Pipe<I, R> Loop<R>(Pipe<T, R> nextPipe, _Func<T, Boolean> predicate)
      {
         return new LoopingPipe<I, T, R>(this, nextPipe, predicate);
      }
     
      public  Pipe<I, U> Branch<U>(Pipe<T, U> ifPipe, Pipe<T, U> elsePipe, _Func<T, Boolean> predicate)
      {
         return new ConditionalPipe<I, T, U>(this, ifPipe, elsePipe, predicate);
      }

      public  Pipe<I, T> Filter(_Func<T, T> filter)
      {
         return new DelegatePipe<I, T>(BeginFlow, (i, r) => filter(EndFlow(i, r)));
      }

      public virtual Pipe<I, T> Finally(_Action<I> finalAction)
      {
         mFinalAction = finalAction;
         return this;
      }

      public virtual Pipe<I, T> Dispose()
      {
         if (!typeof(IDisposable).IsAssignableFrom(typeof(I)))
            throw new InvalidOperationException(String.Format("Type {0} is not IDisposable.", typeof(I).Name));

         return Finally(i => ((IDisposable)i).Dispose());
      }

      // keep?
      public _Func<I, T> ToFunc()
      {
         return (i) => EndFlow(i, BeginFlow(i, null, null));
      }

      public static implicit operator _Func<I, T>(Pipe<I, T> pipe)
      {
         return pipe.ToFunc();
      }

      public static implicit operator _Func<T>(Pipe<I, T> pipe)
      {
         return () => pipe.EndFlow(pipe.BeginFlow(null, null));
      }

      public static implicit operator _Action<I>(Pipe<I, T> pipe)
      {
         return (i) => pipe.EndFlow(i, pipe.BeginFlow(i, null, null));
      }

      public static implicit operator _Action(Pipe<I, T> pipe)
      {
         return () => pipe.EndFlow(pipe.BeginFlow(null, null));
      }

      #endregion
   }

   internal class DelegatePipe<I, T> : Pipe<I, T>
   {
      protected _Func<I, AsyncCallback, Object, IAsyncResult> beginMethod;
      protected _Func<I, IAsyncResult, T> endMethod;

      public DelegatePipe(_Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, _Func<I, IAsyncResult, T> endMethod)
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

   internal class NullPipe<I, T> : FunctionPipe<I, T>
   {
      public NullPipe() : base(i => default(T)) { }
   }

   /// <summary>
   /// Synchronously completing pipe which simply applies a function on input to map to some value.
   /// </summary>
   internal class FunctionPipe<I, T> : Pipe<I, T>
   {
      protected Func<I, T> mFunction;

      public FunctionPipe(Func<I, T> function)
      {
         mFunction = function;
      }

      public override IAsyncResult BeginFlow(I input, AsyncCallback cb, object state)
      {
         IAsyncResult tResult = new _DummyAsyncResult(true, state);
         cb(tResult);
         return tResult;
      }

      public override T EndFlow(I input, IAsyncResult result)
      {
         try
         {
            return mFunction(input);
         }
         finally
         {
            DoFinally(input);
         }
      }
   }

   internal class IdentityPipe<I> : FunctionPipe<I, I>
   {
      public IdentityPipe() : base(i => i) { }
   }

   internal class FilteredPipe<I, T> : DelegatePipe<I, T>
   {
      protected _Func<T, T> mFilter;

      public FilteredPipe(Pipe<I, T> pipe, _Func<T, T> filter) : base(pipe.BeginFlow, pipe.EndFlow)
      {
         mFilter = filter;
      }

      public FilteredPipe(Pipe<I, T> pipe, Pipe<T, T> filterPipe) : this(pipe, filterPipe.ToFunc()) { }

      public override T EndFlow(I input, IAsyncResult result)
      {
         try
         {
            return mFilter(base.EndFlow(input, result));
         }
         finally
         {
            DoFinally(input);
         }
      }
   }

   internal class AdapterPipe<I, T> : Pipe<I, T>
   {
      public Pipe<I, T> InnerPipe { get; set; }

      public AdapterPipe(Pipe<I, T> pipe)
      {
         InnerPipe = pipe;
      }

      public override IAsyncResult BeginFlow(I input, AsyncCallback cb, object state)
      {
         return InnerPipe.BeginFlow(input, cb, state);
      }

      public override T EndFlow(I input, IAsyncResult result)
      {
         return InnerPipe.EndFlow(input, result);
      }

      public override Pipe<I, T> Finally(_Action<I> finalAction)
      {
         return InnerPipe.Finally(finalAction);
      }
   }

   // connects left to right
   internal class ConnectedPipe<I, R, T> : Pipe<I, T>
   {
      protected class ConnectedResult : AsyncResult
      {
         public ConnectedResult(AsyncCallback cb, Object state) : base(cb, null, state) { }
         public T Value;
      }

      protected Pipe<I, R> mLeft;
      protected Pipe<R, T> mRight;
      //protected T mValue;

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
                  ((ConnectedResult)completeResult).Value = pipe.EndFlow(value, r); 
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

      protected virtual ConnectedResult CreateResult(I input, AsyncCallback cb, object state)
      {
         return new ConnectedResult(cb, state);
      }

      public override IAsyncResult BeginFlow(I input, AsyncCallback cb, object state)
      {
         AsyncResult tCompleteResult = CreateResult(input, cb, state); // new AsyncResult(cb, null, state);
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
                  Console.WriteLine("Waiting on wait handle... retrying"); // todo: log to something else

            if (tCompleteResult.Exception != null)
               throw tCompleteResult.Exception;

            return ((ConnectedResult)result).Value;
         }
         finally
         {
            DoFinally(input);
         }
      }
   }

   internal class ConditionalPipe<I, R, T> : ConnectedPipe<I, R, T>
   {
      protected _Func<R, Boolean> mPredicate;
      protected Pipe<R, T> mOtherPipe;

      public ConditionalPipe(Pipe<I, R> inPipe, Pipe<R, T> ifPipe, Pipe<R,T> elsePipe, _Func<R, Boolean> predicate) : base(inPipe, ifPipe)
      {
         mPredicate = predicate;
         mOtherPipe = elsePipe;
      }

      protected virtual Pipe<R, T> GetThenPipe(R value, AsyncResult completeResult)
      {
         return mRight;
      }

      protected virtual Pipe<R, T> GetElsePipe(R value, AsyncResult completeResult)
      {
         return mOtherPipe;
      }

      protected override void Process(R value, AsyncResult completeResult)
      {
         Pipe<R,T> pipeToRun = null;

         if (mPredicate(value))
            pipeToRun = GetThenPipe(value, completeResult);
         else
            pipeToRun = GetElsePipe(value, completeResult);

         RunPipe(pipeToRun, value, completeResult);
      }
   }
   
   internal class LoopingPipe<I, R, T> : ConditionalPipe<I, R, T>
   {
      // Capture input state in the result, this way our pipe is fully threadsafe.
      protected class LoopingResult : ConnectedResult
      {
         public LoopingResult(AsyncCallback cb, Object state) : base(cb, state) { }
         public I Input;
      }

      public LoopingPipe(Pipe<I, R> inPipe, Pipe<R, T> elsePipe, _Func<R, Boolean> predicate) : base(inPipe, null, elsePipe, predicate) {      }

      protected override ConnectedPipe<I, R, T>.ConnectedResult CreateResult(I input, AsyncCallback cb, object state)
      {
         LoopingResult tResult = new LoopingResult(cb, state);
         tResult.Input = input;
         return tResult;
      }

      protected override Pipe<R, T> GetThenPipe(R value, AsyncResult completeResult)
      {
         LoopingResult tResult = (LoopingResult)completeResult;
         return Pipes.Create<R, T>((r, cb, s) => this.BeginFlow(tResult.Input, cb, s), (r, ar) => this.EndFlow(tResult.Input, ar));
      }
   }
}
