
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