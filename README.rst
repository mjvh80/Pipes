
Some code to stitch together Begin and End asynchronous methods.

For example, perform an asynchronous request to some webservice (requestPipe is a pipe doing the request), download and write the result asynchronously to another:::

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
 
 finalPipe.EndFlow(finalPipe.BeginFlow(null, null));

On my MacBook this allows me to do 1000 requests to a local webservice (which blocks on threads itself) in about 3 seconds using no more than about 25 threads. This is in no way an accurate benchmark.

License
=======

(The MIT License)

Copyright (c) 2011 Marcus van Houdt

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 'Software'), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.