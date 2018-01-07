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


## Redis Keys

All Redis keys used by the historian are prefixed with `aika` by default; this can be overridden via the `RedisHistorianOptions.KeyPrefix` property.  The historian uses the following keys and structures to store its data:

* `aika:tags:tagIds` - a list that contains the unique identifiers for all tag definitions.  On startup, the historian loads all tag definitions by reading from this list and then reading individual tag definitions into memory.
* `aika:tags:{tagId}:definition` - a hash that contains the tag definition properties for the specified `{tagId}` (name, description, and so on).
* `aika:tags:{tagId}:snapshot` - a hash that contains the snapshot value for the tag (i.e. the tag's current value).  This is updated every time a new snapshot value is written to the tag.
* `aika:tags:{tagId}:archive` - a sorted set that contains the archive values for the tag.  The score for an item is the UTC time stamp of the sample, in [ticks](https://docs.microsoft.com/en-gb/dotnet/api/system.datetime.ticks).  Since a sorted set can only contain string elements, the values themselves are instances of [RedisTagValue](https://github.com/wazzamatazz/aika/blob/master/src/Aika.Redis/RedisTagValue.cs) that are serialized as JSON before being written.
* `aika:stateSets:names` - a list that contains the names of the available state sets for state-based tags.
* `aika:stateSets:{name}:definition` - a hash that defines the state set with the specified `{name}`.  Each key is the name of a state, and each value is the numeric state value.


## Data Persistence

When using Redis as the back-end for the historian, you are at the mercy of the [persistence policies](https://redis.io/topics/persistence) of the Redis instance that you are using.  Make sure you use an appropriate persistence level!


## TODO

* There is currently no native aggregation support (i.e. aggregation is performed by Aika based on raw data returned from Redis).  Need to look into using Lua to perform aggregation on the Redis instance.