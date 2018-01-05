# Aika.Client

The `Aika.Client` project contains model classes for Aika API requests and responses, and a client for querying Aika via HTTP.


## ApiClient

The [ApiClient](https://github.com/wazzamatazz/aika/blob/master/src/Aika.Client/ApiClient.cs) class provides an easy way of making Aika API calls.  Simply create a new instance and specify the base URL for the Aika system that you want to query, e.g.

```C#
var client = new ApiClient("https://historian.myorg.com/aika", logFactory /* A Microsoft.Extensions.Logging.ILogFactory instance */) {
    Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "<JTW>")
};
```


## ApiMessageHandler

The [ApiMessageHandler](https://github.com/wazzamatazz/aika/blob/master/src/Aika.Client/ApiMessageHandler.cs) class defines `BeforeSend` and `AfterResponse` events, that respectively allow you to inspect or modify an HTTP request immediately prior to sending, or to inspect a response immediately after a response has been received.  Although the [ApiClient](https://github.com/wazzamatazz/aika/blob/master/src/Aika.Client/ApiClient.cs) does not directly expose its underlying `ApiMessageHandler` instance, it own `BeforeSend` and `AfterResponse` events hook directly into those on the `ApiMessageHandler`.