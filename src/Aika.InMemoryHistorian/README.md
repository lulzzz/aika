# Aika In-Memory Historian

The `Aika.InMemoryHistorian` project contains a very basic implementation of the Aika `IHistorian` interface, that stores tag definitions and raw tag data in-memory.  It does not provide and sort of persistence i.e. if your application restarts, you lose all configuration and data. 

The in-memory historian does not natively support any aggregation functions, meaning that it relies on Aika's own built-in aggregation.  Since it only works with in-memory tag values anyway, this should mean that performance remains fast regardless.