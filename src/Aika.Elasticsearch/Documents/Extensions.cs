using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aika.StateSets;
using Aika.Tags;

namespace Aika.Elasticsearch.Documents {
    public static class Extensions {

        public static TagDocument ToTagDocument(this ElasticsearchTagDefinition tag) {
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
                Security = tag.Security.ToTagDocumentSecurity(),
                Metadata = new TagDocument.TagMetadata() {
                    UtcCreatedAt = tag.Metadata.UtcCreatedAt,
                    Creator = tag.Metadata.Creator,
                    UtcLastModifiedAt = tag.Metadata.UtcLastModifiedAt,
                    LastModifiedBy = tag.Metadata.LastModifiedBy
                }
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


        public static TagMetadata ToTagMetadata(this TagDocument tag) {
            return new TagMetadata(tag.Metadata?.UtcCreatedAt ?? DateTime.MinValue, tag.Metadata?.Creator, tag.Metadata?.UtcLastModifiedAt, tag.Metadata?.LastModifiedBy);
        }


        public static Tags.Security.TagSecurity ToTagSecurity(this TagDocument.TagSecurity tagSecurity) {
            return new Tags.Security.TagSecurity(tagSecurity.Owner,
                                            tagSecurity.Policies?
                                                       .ToDictionary(x => x.Key,
                                                                     x => new Tags.Security.TagSecurityPolicy(x.Value.Allow.Select(p => new Tags.Security.TagSecurityEntry(p.ClaimType, p.Value)).ToArray(),
                                                                                                         x.Value.Deny.Select(p => new Tags.Security.TagSecurityEntry(p.ClaimType, p.Value)).ToArray())));
        }


        public static TagDocument.TagSecurity ToTagDocumentSecurity(this Tags.Security.TagSecurity tagSecurity) {
            return new TagDocument.TagSecurity() {
                Owner = tagSecurity.Owner,
                Policies = tagSecurity.Policies.ToDictionary(x => x.Key,
                                                             x => new TagDocument.TagSecurityPolicy() {
                                                                 Allow = x.Value.Allow.Select(p => new TagDocument.TagSecurityEntry() {
                                                                     ClaimType = p.ClaimType,
                                                                     Value = p.Value
                                                                 }).ToArray(),
                                                                 Deny = x.Value.Deny.Select(p => new TagDocument.TagSecurityEntry() {
                                                                     ClaimType = p.ClaimType,
                                                                     Value = p.Value
                                                                 }).ToArray()
                                                             })
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


        public static TagValueDocument ToTagValueDocument(this TagValue value, ElasticsearchTagDefinition tag, Guid? documentId) {
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


        public static TagValue ToTagValue(this TagValueDocument value, DateTime? utcSampleTime = null, double? numericValue = null, string textValue = null, TagValueQuality? quality = null, string units = null) {
            return new TagValue(utcSampleTime ?? value.UtcSampleTime, 
                                numericValue ?? value.NumericValue, 
                                textValue ?? value.TextValue, 
                                quality ?? value.Quality, 
                                units);
        }

    }
}
