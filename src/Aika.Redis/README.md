# Aika.Redis

An Aika historian implementation that uses [Redis](https://redis.io) as its back-end store.  [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) is used to query Redis.


## Getting Started

In your Startup.cs, configure Aika to use `RedisHistorian`:

```C#
services.AddSingleton(new Aika.Redis.RedisHistorianOptions() {
    RedisConfigurationOptions = "localhost:6379,ssl=false,password=..."
});
services.AddAikaHistorian<Aika.Redis.RedisHistorian>();
```


## Data Persistence

When using Redis as the back-end for the historian, you are at the mercy of the [persistence policies](https://redis.io/topics/persistence) of the Redis instance that you are using.  Make sure you use an appropriate persistence level!


## TODO

* State sets are not currenty implemented (they're not implemented properly in Aika yet, either).
* There is currently no native aggregation support (i.e. aggregation is performed by Aika based on raw data returned from Redis).  Need to look into using Lua to perform aggregation on the Redis instance.