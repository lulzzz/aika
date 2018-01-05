# Aika


Aika ("time" in Finnish; pronounced *EYE-kuh*) is a time-based data historian service, written in C# using .NET Standard 2.0, suitable for use with IoT devices and services that use industrial process data.

Aika delegates the actual historian functionality to a back-end class implementing its `IHistorian` interface, allowing it to be used both with bespoke data-storage services, and with existing industrial plant historians (e.g. [OSIsoft PI](https://www.osisoft.com/pi-system/pi-capabilities/pi-server/)).  The repository includes an `IHistorian` implementation that uses in-memory data storage.

The repository also includes an ASP.NET Core 2.0 project that exposes Web API controllers for querying, writing to, and configuring historian tags; SignalR is used to manage real-time data subscriptions, so that subscribers can receive changes in value as soon as they are recorded.


## Getting Started

Look at the [sample app](https://github.com/wazzamatazz/aika/tree/master/src/Aika.SampleApp) for an example of how to host Aika using ASP.NET Core.

The sample app uses JWT bearer tokens for authentication.  This requires some additional setup steps.  For more information, [visit the wiki](https://github.com/wazzamatazz/aika/wiki/Configuring-JWT-Authentication).

If you want to use another form of authentication (e.g. Windows authentication), you will need to modify the authorization policies to perform role-based authorizaton.  The wiki contains [instructions](https://github.com/wazzamatazz/aika/wiki/Custom-Authorization) for how to do this.


## Data Aggregation

Aika includes built-in support for aggregation of raw data for historian implementations that do not support this capability natively (e.g. interpolation, minimum/maximum/average values calculated over a set interval).


## Exception and Compression Filtering

When values are written to Aika, exception and compression filters are applied to the incoming values, to reduce the volume of data that is actually recorded, while maintaining the shape of the data (and ensuring that meaningful value changes are recorded).

The wiki [describes this](https://github.com/wazzamatazz/aika/wiki/Exception-and-Compression-Filtering) in more detail. 