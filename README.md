# Aika


Aika ("time" in Finnish; pronounced *EYE-kah*) is a time-based data historian service, written in C# using .NET Standard 2.0, suitable for use with IoT devices and services that use industrial process data.

Aika delegates the actual historian functionality to a back-end class implementing its `IHistorian` interface, allowing it to be used both with bespoke data-storage services, and with existing industrial plant historians (e.g. [OSIsoft PI](https://www.osisoft.com/pi-system/pi-capabilities/pi-server/)).  The repository includes an `IHistorian` implementation that uses in-memory data storage.

The repository also includes an ASP.NET Core 2.0 project that exposes Web API controllers for querying, writing to, and configuring historian tags; SignalR is used to manage real-time data subscriptions, so that subscribers can receive changes in value as soon as they are recorded.


## Data Aggregation

Aika includes built-in support for aggregation of raw data for historian implementations that do not support this capability natively (e.g. interpolation, minimum/maximum/average values calculated over a set interval).


## Exception and Compression Filtering

When values are written to Aika, exception and compression filters are applied to the incoming values, to reduce the volume of data that is actually recorded, while maintaining the shape of the data (and ensuring that meaningful value changes are recorded).

OSIsoft have an [excellent video](https://www.youtube.com/watch?v=89hg2mme7S0) describing how exception and compression filtering works.