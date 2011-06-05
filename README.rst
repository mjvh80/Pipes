
Some code to stitch together Begin and End asynchronous methods.

For example, perform an asynchronous request to some webservice, download and write the result asynchronously to another:::

 var pipe = Pipes.Create<Stream, Stream>(tRequest.BeginGetRequestStream, tRequest.EndGetRequestStream)
               .Connect(Pipes.CreateEnd<Stream, Stream>((str, cb, state) => str.BeginWrite(tRequestBuffer, 0, tRequestBuffer.Length, cb, state), (s, r) => s.EndWrite(r)).Dispose())
               .Connect(tRequest.BeginGetResponse, (r) => tRequest.EndGetResponse(r).GetResponseStream())
               .Connect(Pipes.ReadWrite<Stream, Int32>(new Byte[1024], (s) => s.BeginRead, s => s.EndRead, tResult.BeginWrite, tResult.EndWrite).Loop(i => i > 0).Dispose());

 pipe.EndFlow(pipe.BeginFlow(null, null));

On my MacBook this allows me to do 1000 requests to a local webservice (which blocks on threads itself) in about 3 seconds using no more than about 25 threads. This is in no way an accurate benchmark.