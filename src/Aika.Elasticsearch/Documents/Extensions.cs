using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aika.Elasticsearch.Documents {
    public static class Extensions {

        public static TagDocument ToTagDocument(this ElasticTagDefinition tag) {
            return new TagDocument() {
                Id = tag.IdAsGuid,
                Name = tag.Name,
                Description = tag.Description,
                Units = tag.Units,
                DataType = tag.DataType,
                StateSet = tag.StateSet,
                ExceptionFilter = new TagValueFilterSettingsUpdate() {
                    IsEnabled = tag.DataFilter.ExceptionFilter.Settings.IsEnabled,
                    LimitType = tag.DataFilter.ExceptionFilter.Settings.LimitType,
                    Limit = tag.DataFilter.ExceptionFilter.Settings.Limit,
                    WindowSize = tag.DataFilter.ExceptionFilter.Settings.WindowSize
                },
                CompressionFilter = new TagValueFilterSettingsUpdate() {
                    IsEnabled = tag.DataFilter.CompressionFilter.Settings.IsEnabled,
                    LimitType = tag.DataFilter.CompressionFilter.Settings.LimitType,
                    Limit = tag.DataFilter.CompressionFilter.Settings.Limit,
                    WindowSize = tag.DataFilter.CompressionFilter.Settings.WindowSize
                },
                Security = tag.Security?.ToArray()
            };
        }


        public static TagSettings ToTagSettings(this TagDocument tag) {
            return new TagSettings() {
                Name = tag.Name,
                Description = tag.Description,
                Units = tag.Units,
                DataType = tag.DataType,
                StateSet = tag.StateSet,
                ExceptionFilterSettings = new TagValueFilterSettingsUpdate() {
                    IsEnabled = tag.ExceptionFilter.IsEnabled,
                    LimitType = tag.ExceptionFilter.LimitType,
                    Limit = tag.ExceptionFilter.Limit,
                    WindowSize = tag.ExceptionFilter.WindowSize
                },
                CompressionFilterSettings = new TagValueFilterSettingsUpdate() {
                    IsEnabled = tag.CompressionFilter.IsEnabled,
                    LimitType = tag.CompressionFilter.LimitType,
                    Limit = tag.CompressionFilter.Limit,
                    WindowSize = tag.CompressionFilter.WindowSize
                }
            };
        }


        public static StateSetDocument ToStateSetDocument(this StateSet stateSet) {
            return new StateSetDocument() {
                Name = stateSet.Name,
                Description = stateSet.Description,
                States = stateSet.StateNames.Select(x => new StateSetEntryDocument() {
                    Name = x,
                    Value = stateSet[x].Value
                }).ToArray()
            };
        }


        public static StateSet ToStateSet(this StateSetDocument doc) {
            return new StateSet(doc.Name, doc.Description, doc.States.Select(x => new StateSetItem(x.Name, x.Value)).ToArray());
        }


        public static TagValueDocument ToTagValueDocument(this TagValue value, ElasticTagDefinition tag, Guid? documentId) {
            return new TagValueDocument() {
                Id = documentId.HasValue 
                    ? documentId.Value 
                    : Guid.NewGuid(),
                TagId = tag.IdAsGuid,
                UtcSampleTime = value.UtcSampleTime,
                NumericValue = tag.DataType == TagDataType.Text
                    ? Double.NaN
                    : value.NumericValue,
                TextValue = tag.DataType == TagDataType.FloatingPoint || tag.DataType == TagDataType.Integer
                    ? null
                    : value.TextValue,
                Quality = value.Quality
            };
        }


        public static TagValue ToTagValue(this TagValueDocument value, string units) {
            return new TagValue(value.UtcSampleTime, 
                                value.NumericValue, 
                                value.TextValue, 
                                value.Quality, 
                                units);
        }

    }
}
