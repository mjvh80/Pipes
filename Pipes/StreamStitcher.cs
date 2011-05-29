//extern alias ActualSystem;

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Net;
//using System.IO;

//namespace ConsoleApplication1_disabled
//{


//   public class Foobar
//   {
//      public static void foo()
//      {
//         WebRequest tRequest = WebRequest.Create("http://www.google.com");

//         Byte[] tBuffer = new Byte[1024];
//         var tPipe = new Pipe<Stream, Byte[]>(tRequest.BeginGetRequestStream, (i, r) => tRequest.EndGetRequestStream(r));
     
//         Pipe.Create(tRequest.BeginGetRequestStream, tRequest.EndGetRequestStream)
//            .Connect(s => s.BeginWrite(tBuffer, 0, tBuffer.Length), s => s.EndWrite).While(i => i > 0)
//            .ContinueWith(tRequest.BeginGetResponse, tRequest.EndGetResponse)
//            .Connect(resp => resp.BeginRead(tBuffer, 0, tBuffer.Length), resp => resp.EndRead).While(i => i > 0)

//         //   .ConnectLoop((stream, cb, state) => stream.BeginRead(tBuffer, 0, tBuffer.Length, cb, state), 
//         //               (stream, r) => stream.EndRead(r), i => i > 0);

//        // var tReadWritePipe = new Pipe<Stream>()

//         //.Connect(stream.BeginRead, stream.EndRead).Connect(str2.BeginWrite, str2.EndWrite)
           

//         //tPipe = new Pipe<Stream>(tRequest.BeginGetRequestStream, tRequest.EndGetRequestStream)
//         //   .Connect((s, c) => s.BeginWrite, (s) => s.EndWrite)
//         //   .Connect((s, c) => s.BeginRead, (s, Int32) => s.EndRead)
//         //   .Loop(i => i > 0)

//      }
//   }

//   public interface IPipe<in I, out T> { }

//   public class Pipe<I, T> : IPipe<I, T>
//   {
//      public IAsyncResult BeginFlow(AsyncCallback cb, Object state)
//      {
//         return BeginFlow(default(I), cb, state);
//      }

//      public IAsyncResult BeginFlow(I input, AsyncCallback cb, Object state)
//      {
//         this.Input = input;
//         return Begin(input, cb, state);
//      }

//      public T EndFlow(IAsyncResult result)
//      {
//         return End(Input, result);
//      }

//      protected Func<I, AsyncCallback, Object, IAsyncResult> Begin;
//      protected Func<I, IAsyncResult, T> End;

//      protected I Input;
//      protected T Result;

//      public Pipe(Func<I, AsyncCallback, Object, IAsyncResult> beginMethod, Func<I, IAsyncResult, T> endMethod)
//      {
//         Begin = beginMethod;
//         End = endMethod;
//      }

//      public Pipe(Func<AsyncCallback, Object, IAsyncResult> beginMethod, Func<IAsyncResult, T> endMethod)
//      {
//         Begin = (i, a, o) => beginMethod(a, o);
//         End = (i, r) => endMethod(r);
//      }

//      protected Pipe() { } // internal use

//      // todo: overloads without state

//      // todo: Connect<R>(Pipe<R>)

//      public Pipe<I, R> Connect<R>(Func<T, AsyncCallback, Object, IAsyncResult> beginMethod, Func<IAsyncResult, R> endMethod)
//      {
//         return Connect(beginMethod, (t, r) => endMethod(r));
//      }

//      public Pipe<I, R> Connect<R>(Func<T, AsyncCallback, Object, IAsyncResult> beginMethod, Func<T, IAsyncResult, R> endMethod)
//      {
//         return this.Connect(new Pipe<T, R> { Begin = beginMethod, End = endMethod });
//      }

//      public Pipe<I,R> Connect<R>(Pipe<T, R> pipe)
//      {
//         return this.ConnectWhile(pipe, r => false, null);
//      }


//#if false
//      public Pipe<R> ConnectLoop<R>(Func<T, AsyncCallback, Object, IAsyncResult> beginMethod, Func<T, IAsyncResult, R> endMethod,
//         Func<R, Boolean> condition)
//      {
//         //return new Pipe<R>
//         //{
//         //   Begin = (callback, state) =>
//         //   {
//         //      return this.Begin(r =>
//         //      {
//         //         Result = this.End(r); // finish first part of pipe, until here

//         //         AsyncCallback loopCallback = null;
//         //         loopCallback = _r =>
//         //         {
//         //            R loopResult = endMethod(Result, _r);
//         //            if (condition(loopResult))
//         //               beginMethod(Result, loopCallback, _r);
//         //            else
//         //               callback(_r);
//         //         };

//         //         beginMethod(Result, loopCallback, r.AsyncState);
//         //      }, state);
//         //   },
//         //   End = r =>
//         //   {
//         //      return endMethod(Result, r);
//         //   }
//         //};
//      }

//      public Pipe<I, T2> Connect<T2>(Pipe<T, T2> pipe, Func<T2, Boolean> condition)
//      {
//         //return ConnectIf(pipe, condition, pipe, null); // loop with self

//         // 1. Connect pipe to itself:
//         var loopPipe = pipe.ConnectIf(condition, pipe, null);

//         // 2. Connect loop.
//         return this.ConnectIf(loopPipe, condition, loopPipe, null);

//         return new Pipe<I, T2>
//         {
//            Begin = (input, callback, state) =>
//            {
//               return this.Begin(input, r =>
//               {
//                  Result = this.End(input, r); // finish first part of pipe, until here

//                  // Build callback.
//                  AsyncCallback loopCallback = null;
//                  loopCallback = _r =>
//                  {
//                     T2 loopResult = pipe.EndFlow(_r);
//                     if (condition(loopResult))
//                        pipe.Begin(Result, loopCallback, _r.AsyncState);
//                     else
//                        callback(_r);
//                  };

//                  pipe.Begin(Result, loopCallback, r.AsyncState);
//               }, state);
//            },
//            End = (i, r) =>
//            {
//               //return pipe.End(Result, r);
//               return pipe.EndFlow(r);
//            }
//         };  
//      }

//#endif


//      // connect by reading into a buffer and writing..
//      public Pipe<I, T2> ConnectReadWrite<T2>(Pipe<T, T2> target, Int32 bufferSize)
//      {
//         return null;
//         //return this.ConnectReadWrite(target, new Byte[bufferSize], 0, bufferSize);
//      }


//      // read into buffer
//      // start new read
//      // write into target



//      public Pipe<I, T2> ConnectReadWrite<T2>(Func<Byte[], Int32, Int32, AsyncCallback, Object, IAsyncResult> beginWrite, Action<IAsyncResult> endWrite, Byte[] buffer, Int32 offset)
//      {
//         if (typeof(T) != typeof(Int32))
//            throw new InvalidOperationException();

//         // build a pipe: input > amount > write out using target

//         var readPipe = this;

//         // connect a pipe that wraps our target to ourselfs.
//         var resultPipe = this.Connect<Int32>(new Pipe<T, int>
//         {
//            Begin = (t, cb, state) => beginWrite(buffer, offset, (t as Int32?).Value, cb, state),
//            End = (t, r) => { endWrite(r); return (t as Int32?).Value; }
//         });

//      }
//     // connectWhile

//      public Pipe<I, T> If(Func<T, Boolean> condition, Pipe<T, T> ifPipe, Pipe<T, T> elsePipe)
//      {
//         return this.ConnectIf(this.EchoPipe(), condition, ifPipe, elsePipe);
//      }

//      public Pipe<I, T> While(Func<T, Boolean> condition, Pipe<T, T> otherPipe)
//      {
//         return this.ConnectWhile(this.EchoPipe(), condition, otherPipe);
//      }

//      protected Pipe<T, T> EchoPipe()
//      {
//         return new Pipe<T, T>
//         {
//            Begin = (input, callback, state) => 
//            { 
//               var tResult = new _DummyAsyncResult(true, state);
//               callback(tResult);
//               return tResult;
//            },
//            End = (input, result) => input
//         };
//      }

//      public Pipe<I, T2> ConnectWhile<T2>(Pipe<T, T2> pipe, Func<T2, Boolean> condition, Pipe<T, T2> otherPipe)
//      {
//         return new Pipe<I, T2>
//         {
//            Begin = (input, callback, state) =>
//            {
//               return this.Begin(input, r =>
//               {
//                  Result = this.End(input, r); // finish first part of pipe, until here

//                  // Build callback.
//                  AsyncCallback loopCallback = null;
//                  var activePipe = pipe;
//                  loopCallback = _r =>
//                  {
//                     T2 loopResult = activePipe.EndFlow(_r);
//                     if (condition(loopResult))
//                     {
//                        activePipe = otherPipe;
//                        activePipe.Begin(Result, loopCallback, _r.AsyncState);
//                     }
//                     else
//                        callback(r);
//                  };

//                  activePipe.Begin(Result, loopCallback, r.AsyncState);
//               }, state);
//            },
//            End = (i, r) =>
//         };
//      }


//      public Pipe<I, T2> ConnectIf<T2>(Pipe<T, T2> pipe, Func<T2, Boolean> condition, Pipe<T, T2> ifPipe, Pipe<T, T2> elsePipe)
//      {
//         return new Pipe<I, T2>
//         {
//            Begin = (input, callback, state) =>
//            {
//               return this.Begin(input, r =>
//               {
//                  Result = this.End(input, r); // finish first part of pipe, until here

//                  pipe.Begin(Result, _r =>
//                     {
//                        T2 pipeResult = pipe.End(Result, _r);
//                        Pipe<T, T2> nextPipe = null;
//                        if (condition(pipeResult))
//                           nextPipe = ifPipe;
//                        else if (elsePipe != null)
//                           nextPipe = elsePipe;
                        
//                        if (nextPipe == null)
//                           callback(r);
//                        else
//                           nextPipe.Begin(Result, __r => callback(r), _r.AsyncState);

//                     }, r.AsyncState);



//                  //// Build callback.
//                  //AsyncCallback loopCallback = null;
//                  //var activePipe = pipe;
//                  //loopCallback = _r =>
//                  //{
//                  //   T2 loopResult = activePipe.EndFlow(_r);
//                  //   if (condition(loopResult))
//                  //      activePipe = ifPipe;
//                  //   //ifPipe.Begin(Result, callback, _r.AsyncState);
//                  //   else if (elsePipe != null)
//                  //      activePipe = elsePipe;
//                  //   //elsePipe.Begin(Result, callback, _r.AsyncState);
//                  //   else
//                  //   {
//                  //      callback(_r);
//                  //      activePipe = null;
//                  //   }

//                  //   if (activePipe != null)
//                  //      activePipe.Begin(Result, loopCallback, _r.AsyncState);
//                  //};

//                  //activePipe.Begin(Result, loopCallback, r.AsyncState);
//               }, state);
//            },
//            End = (i, r) =>
//            {
//               //return pipe.End(Result, r);
//               return pipe.EndFlow(r);
//            }
//         };
//      }


//   }

//   //public class StreamStitcher
//   //{
//   //   protected class Step
//   //   {
//   //      public Func<AsyncCallback, IAsyncResult> Begin;
//   //      public Func<IAsyncResult, Object> End;
//   //   }

      

//   //   // BeginWrite -> request
//   //   // BeginRead -> resp.
//   //   // BeginWrite -> client

//   //   protected List<Step> mSteps = new List<Step>();

//   //   public void AddStep(Func<AsyncCallback, IAsyncResult> begin, Func<IAsyncResult, Object> end)
//   //   {
//   //      mSteps.Add(new Step { Begin = begin, End = end });
//   //   }

//   //   public Pipe<R> BuildPipe<R>(Func<AsyncCallback, IAsyncResult> beginMethod, Func<IAsyncResult, R> endMethod)
//   //   {
//   //      return new Pipe<R> { Begin = beginMethod, End = endMethod };
//   //   }

//   //   public void Run()
//   //   {

//   //   }

//   //   protected void RunStep(Int32 pIndex)
//   //   {
//   //      Step tStep = mSteps[pIndex];
//   //      tStep.Begin(r =>
//   //         {
//   //            tStep.End(r);
//   //         });
//   //   }

//   //}
//}
