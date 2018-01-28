# Aika.Elasticsearch

An Aika historian implementation that uses [Elasticsearch](https://elasticsearch.co) as its back-end store.  [NEST](https://github.com/elastic/elasticsearch-net) is used to query Elasticsearch.


## Getting Started

In your Startup.cs, configure Aika to use `ElasticsearchHistorian`:

```C#
services.AddSingleton(x => {
    var settings = new Nest.ConnectionSettings(new Uri("http://localhost:9200")).ThrowExceptions(true);
    return new Nest.ElasticClient(settings);
});
services.AddSingleton(new Aika.Elasticsearch.Options());
services.AddAikaHistorian<Aika.Elasticsearch.ElasticsearchHistorian>();
```


## Features

The `ElasticsearchHistorian` supports the following data aggregation functions:

* AVG
* MIN
* MAX
* INTERP


## Indices

By default, the following Elasticsearch indices are used by the historian:

* `aika-tags` - holds tag definitions.
* `aika-tag-config-history` - holds old versions of tag definitions.
* `aika-state-sets` - holds state set definitions.
* `aika-snapshot` - snapshot values.
* `aika-archive-temporary` - archive candidate values.
* `aika-archive-permanent-{SUFFIX}` - permanently-archived data.  By default, the suffix used is `YYYY-MM`, i.e. the UTC year and month for the data held in the index.  An alternative suffix can be generated using the `GetArchiveIndexSuffix` property on the `Options` class passed to the historian's constructor e.g. to specify an index-per-day, or an index-per-tag.

The `Aika.Elasticsearch.Documents` namespace defines the document models for the indices.  The `aika-` prefix can be changed via the `Options` class passed to the historian's constructor.