FastCgiNet
==========
A FastCgi library written in C#. FastCgi is a protocol that allows traditional CGI applications or console applications to run smoothlessly (without changingtheir code) side by side with a Web Server, responding to user's HTTP requests. It can also be used to run other types of web applications (PHP, RoR and Owin applications) and is a very good hosting choice for many reasons (see [the official site](http://www.fastcgi.com)). It should be noted that FastCgiNet **does not intend** to enable console .NET applications to run without any code changes: FastCgiNet does not redirect stdin, stdout or stderr. It provides other mechanisms for applications to function as a CGI application.

API
---
The API is still changing between commits. That said, here are some docs on how to use this library:

The Requests API
----------------
There are two APIs in FastCgiNet: the Records API and the Requests API. The Requests API is the recommended API and should fit most users' needs. This section describes it.

The webserver's point of view:
1. When the browser requests a page/url, the webserver must request an answer from the application.  
2. The application will then write the HTTP Response Status, HTTP Response Headers and Standard Output (it may also write to the Standard Error Output) and the webserver will finally send these to the actual visitor.  

So if you are writing a Web Server, you might want to use a *WebServerSocketRequest* per visitor's request, like this:
```
using FastCgiNet;
using FastCgiNet.Streams;
using FastCgiNet.Requests;

...

// Let's simulate a GET request to http://github.com/mzabani/FastCgiNet
var requestedUrl = new Uri("http://github.com/mzabani/FastCgiNet");
string requestMethod = "GET";

// Suppose the FastCgi application is listening on 127.0.0.1, port 9000
var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
sock.Connect(new IPEndPoint(IPAddress.Loopback, 9000));

// There must be no two concurrent requests with the same requestid, even if in different sockets. For simplicity, this request will have request id equal to 1
ushort requestId = 1;
using (var request = new WebServerSocketRequest(sock, requestId))
{
	// The BeginRequest Record defines how the application should respond. To know more read FastCgi's docs.
	request.SendBeginRequest(Role.Responder, true);

	// The Request Headers are sent with Params Records. You don't have to worry about the mechanisms, though: just write to the Params stream.
	using (var nvpWriter = new NvpWriter(request.Params))
	{
		// The WriteParamsFromUri is a helper method that writes the following Name-Value Pairs:
		// HTTP_HOST, HTTPS, SCRIPT_NAME, DOCUMENT_URI, REQUEST_METHOD, SERVER_NAME, QUERY_STRING, REQUEST_URI, SERVER_PROTOCOL, GATEWAY_INTERFACE
		nvpWriter.WriteParamsFromUri(requestedUrl, requestMethod);

		// The other http request headers, e.g. User-Agent
		nvpWriter.Write("HTTP_USER_AGENT", "Super cool Browser v1.0");
	}

	// If there is any request body, send it through the Stdin stream. If there is nothing to send, send an End-Of-Request Record (an empty record)
	request.SendEmptyStdin();

	// At this point, the application is processing the request and cooking up a response for us, so let's welcome the incoming data until the response is over
	int bytesRead;
	byte[] buf = new byte[4096];
	while (!request.ResponseComplete)
	{
		bytesRead = sock.Receive(buf, SocketFlags.None);
		request.FeedBytes(buf, 0, bytesRead);
	}

	// All the application's response will be in the Stdout and/or Stderr streams
	// Don't forget that the very first line of the output is ASCII encoded text with the response status, such as "Status: 200 OK"
	using (var reader = new StreamReader(request.Stdout))
	{
		Console.Write(reader.ReadToEnd());
	}
}
// The socket and all other resources are automatically disposed at this point.
// This implies that WebServerSocketRequest still doesn't multiplex requests 
// (not for long, hopefully - also, you can inherit from this class and make
// *Dispose()* not call *CloseSocket()* if you want to multiplex requests)

```

The code sample above is a pretty good demonstration of how easy it is to use FastCgiNet! Don't forget to handle all sorts of errors, though. For instance, the socket can be closed abruptly by the application any time and an evil application could hold you in an infinite loop if it never sends an EndRequest Record.
Now let's look at it from the application's point of view, with the *ApplicationSocketRequest* class:

```
using FastCgi;
using FastCgi.Streams;
using FastCgi.Requests;

...

using (var listenSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
{
	listenSock.Bind(new IPEndPoint(IPAddress.Loopback, 9000));
	listenSock.Listen(1);

	// For simplicity, let's accept only one connection
	var sock = listenSock.Accept();
	using (var request = new ApplicationSocketRequest(sock))
	{
		// Now let's wait until we have received the Params and Stdin streams completely
		int bytesRead;
		byte[] buf = new byte[4096];
		while (!request.Params.IsComplete || !request.Stdin.IsComplete)
		{
			bytesRead = sock.Receive(buf, SocketFlags.None);
			request.FeedBytes(buf, 0, bytesRead);
		}

		// Let's look for the requested path and ignore everything else
		string requestedPath = null;
		using (var nvpReader = new NvpReader(request.Params))
		{
			NameValuePair nvp;
			while ((nvp = nvpReader.Read()) != null)
			{
				if (nvp.Name == "DOCUMENT_URI")
					requestedPath = nvp.Value;
			}
		}

		// Let's write a classic response
		using (var writer = new StreamWriter(request.Stdout))
		{
			// The headers first
			writer.NewLine = "\r\n";
			writer.Write("Status: 200 OK");
			writer.WriteLine("Content-Type: text/html");
			writer.WriteLine();

			// Now the body
			writer.Write("<html><head><title>Hello World</title></head><body><h1>Hello FastCgiNet!</h1>The requested path was {0}</body></html>", requestedPath);
		}

		// Our application status and end of request. The FastCgi Standard defines that returning 0 indicates there were no errors
		request.SendEndRequest(0, ProtocolStatus.RequestComplete);
	}

	// The connection socket and all other resources (except for the 
	// listen socket) are automatically disposed at this point. This
	// implies that ApplicationSocketRequest still doesn't multiplex
	// requests (not for long, hopefully - also, you can inherit from
	// this class and make *Dispose()* not call *CloseSocket()* if 
	// you want to multiplex requests).
}


```

Once again, don't forget to handle all sorts of socket and evil web server errors.

The Records API
--------------
This is a lower level API that allows you to build Records and send them yourself. It is _highly_ recommended that you don't use this API, for a couple reasons:

- A record holds at most 65535 bytes of content. This means that you have to break your data into several records to get it through, or things can go badly wrong.
- There is no notion of a Request in this API. You have to deal with that on your own.
- The Requests API makes several sanity checks that make it very helpful.

This API is public because in conjunction with the Requests API, a power user might make good use of it (although I see very few use cases myself).
As such, I will not take as much effort in documenting this API as I will take documenting the Requests API. The code is very well documented and intuitive, so if you need it, just explore the API and you'll probably be fine.

TODO
----
- Multiplexing Requests
- Records of Type GetValues, GetValuesResult, AbortRequest, Data and UnknownType
- More attention to roles Filter and Authorizer
