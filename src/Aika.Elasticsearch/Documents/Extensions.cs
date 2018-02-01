using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aika.StateSets;
using Aika.Tags;

namespace Aika.Elasticsearch.Documents {

    /// <summary>
    /// Extension methods for Elasticsearch document classes.
    /// </summary>
    public static class Extensions {

        /// <summary>
        /// Converts an <see cref="ElasticsearchTagDefinition"/> into a <see cref="TagDocument"/> that 
        /// will be stored in Elasticsearch.
        /// </summary>
        /// <param name="tag">The tag definition.</param>
        /// <returns>
        /// The equivalent <see cref="TagDocument"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> is <see langword="null"/>.</exception>
        public static TagDocument ToTagDocument(this ElasticsearchTagDefinition tag) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

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


        /// <summary>
        /// Converts an Elasticsearch <see cref="TagDocument"/> into the equivalent <see cref="TagSettings"/> object.
        /// </summary>
        /// <param name="tag">The Elasticsearch tag document.</param>
        /// <returns>
        /// An equivalent <see cref="TagSettings"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> is <see langword="null"/>.</exception>
        public static TagSettings ToTagSettings(this TagDocument tag) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

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


        /// <summary>
        /// Creates a <see cref="TagMetadata"/> from the specified <see cref="TagDocument"/>.
        /// </summary>
        /// <param name="tag">The Elasticsearch tag document.</param>
        /// <returns>
        /// A <see cref="TagMetadata"/> that is populated using the metadata in the tag document.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> is <see langword="null"/>.</exception>
        public static TagMetadata ToTagMetadata(this TagDocument tag) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            return new TagMetadata(tag.Metadata?.UtcCreatedAt ?? DateTime.MinValue, tag.Metadata?.Creator, tag.Metadata?.UtcLastModifiedAt, tag.Metadata?.LastModifiedBy);
        }


        /// <summary>
        /// Creates an Aika <see cref="Tags.Security.TagSecurity"/> object from an Elasticsearch <see cref="TagDocument.TagSecurity"/> object.
        /// </summary>
        /// <param name="tagSecurity">The Elasticsearch tag security document.</param>
        /// <returns>
        /// An equivalent <see cref="Tags.Security.TagSecurity"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="tagSecurity"/> is <see langword="null"/>.</exception>
        public static Tags.Security.TagSecurity ToTagSecurity(this TagDocument.TagSecurity tagSecurity) {
            if (tagSecurity == null) {
                throw new ArgumentNullException(nameof(tagSecurity));
            }
            return new Tags.Security.TagSecurity(tagSecurity.Owner,
                                            tagSecurity.Policies?
                                                       .ToDictionary(x => x.Key,
                                                                     x => new Tags.Security.TagSecurityPolicy(x.Value.Allow.Select(p => new Tags.Security.TagSecurityEntry(p.ClaimType, p.Value)).ToArray(),
                                                                                                         x.Value.Deny.Select(p => new Tags.Security.TagSecurityEntry(p.ClaimType, p.Value)).ToArray())));
        }


        /// <summary>
        /// Creates an Elasticsearch <see cref="TagDocument.TagSecurity"/> document from an Aika <see cref="Tags.Security.TagSecurity"/> object.
        /// </summary>
        /// <param name="tagSecurity">The Aika tag security object.</param>
        /// <returns>
        /// An equivalent <see cref="TagDocument.TagSecurity"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="tagSecurity"/> is <see langword="null"/>.</exception>
        public static TagDocument.TagSecurity ToTagDocumentSecurity(this Tags.Security.TagSecurity tagSecurity) {
            if (tagSecurity == null) {
                throw new ArgumentNullException(nameof(tagSecurity));
            }

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


        /// <summary>
        /// Creates an Elasticsearch <see cref="StateSetDocument"/> from the specified Aika <see cref="StateSet"/>.
        /// </summary>
        /// <param name="stateSet">The Aika state set.</param>
        /// <returns>
        /// An equivalent Elasticsearch <see cref="StateSetDocument"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="stateSet"/> is <see langword="null"/>.</exception>
        public static StateSetDocument ToStateSetDocument(this StateSet stateSet) {
            if (stateSet == null) {
                throw new ArgumentNullException(nameof(stateSet));
            }

            return new StateSetDocument() {
                Name = stateSet.Name,
                Description = stateSet.Description,
                States = stateSet.StateNames.Select(x => new StateSetEntryDocument() {
                    Name = x,
                    Value = stateSet[x].Value
                }).ToArray()
            };
        }


        /// <summary>
        /// Creates an Aika <see cref="StateSet"/> from the specified Elasticsearch <see cref="StateSetDocument"/>.
        /// </summary>
        /// <param name="stateSet">The Elasticsearch state set document.</param>
        /// <returns>
        /// An equivalent Aika <see cref="StateSet"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="stateSet"/> is <see langword="null"/>.</exception>
        public static StateSet ToStateSet(this StateSetDocument stateSet) {
            if (stateSet == null) {
                throw new ArgumentNullException(nameof(stateSet));
            }

            return new StateSet(stateSet.Name, stateSet.Description, stateSet.States.Select(x => new StateSetItem(x.Name, x.Value)).ToArray());
        }


        /// <summary>
        /// Converts an Aika <see cref="TagValue"/> into an Elasticsearch <see cref="TagValueDocument"/>.
        /// </summary>
        /// <param name="value">The Aika tag value.</param>
        /// <param name="tag">The tag that the value is for.</param>
        /// <param name="documentId">
        ///   The optional document ID to assign to the <see cref="TagValueDocument"/>.  Specify 
        ///   <see langword="null"/> for archive values, and the tag ID for snapshot and archive 
        ///   candidate values (so that the existing snapshot or archive candidate document for 
        ///   the tag will be replaced).
        /// </param>
        /// <returns>
        /// An equivalent <see cref="TagValueDocument"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> is <see langword="null"/>.</exception>
        public static TagValueDocument ToTagValueDocument(this TagValue value, ElasticsearchTagDefinition tag, Guid? documentId) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

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


        /// <summary>
        /// Converts an Aika <see cref="ArchiveCandidateValue"/> into an Elasticsearch <see cref="TagValueDocument"/>.
        /// </summary>
        /// <param name="value">The Aika archive candidate value.</param>
        /// <param name="tag">The tag that the value is for.</param>
        /// <param name="documentId">
        ///   The optional document ID to assign to the <see cref="TagValueDocument"/>.  Specify 
        ///   <see langword="null"/> for archive values, and the tag ID for snapshot and archive 
        ///   candidate values (so that the existing snapshot or archive candidate document for 
        ///   the tag will be replaced).
        /// </param>
        /// <returns>
        /// An equivalent <see cref="TagValueDocument"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> is <see langword="null"/>.</exception>
        public static TagValueDocument ToTagValueDocument(this ArchiveCandidateValue value, ElasticsearchTagDefinition tag, Guid? documentId) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            var val = value?.Value.ToTagValueDocument(tag, documentId);
            val.Properties = new Dictionary<string, object>() {
                { "CompressionAngleMinimum", value.CompressionAngleMinimum },
                { "CompressionAngleMaximum", value.CompressionAngleMaximum }
            };

            return val;
        }


        /// <summary>
        /// Converts an Elasticsearch <see cref="TagValueDocument"/> into an Aika <see cref="TagValue"/>, 
        /// optionally overriding one or more of the tag value document's fields.
        /// </summary>
        /// <param name="value">The value document.</param>
        /// <param name="utcSampleTime">Overrides the UTC sample time from the <paramref name="value"/>.</param>
        /// <param name="numericValue">Overrides the numeric value from the <paramref name="value"/>.</param>
        /// <param name="textValue">Overrides the text value from the <paramref name="value"/>.</param>
        /// <param name="quality">Overrides the quality status from the <paramref name="value"/>.</param>
        /// <param name="units">Specifies the tag units for the <see cref="TagValue"/>.</param>
        /// <returns>
        /// An equivalent <see cref="TagValue"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static TagValue ToTagValue(this TagValueDocument value, DateTime? utcSampleTime = null, double? numericValue = null, string textValue = null, TagValueQuality? quality = null, string units = null) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            return new TagValue(utcSampleTime ?? value.UtcSampleTime, 
                                numericValue ?? value.NumericValue, 
                                textValue ?? value.TextValue, 
                                quality ?? value.Quality, 
                                units);
        }


        /// <summary>
        /// Converts an Elasticsearch <see cref="TagValueDocument"/> into an Aika <see cref="ArchiveCandidateValue"/>.
        /// </summary>
        /// <param name="value">The value document.</param>
        /// <param name="units">Specifies the tag units for the <see cref="TagValue"/> on the <see cref="ArchiveCandidateValue"/>.</param>
        /// <returns>
        /// An equivalent <see cref="ArchiveCandidateValue"/>.
        /// </returns>
        public static ArchiveCandidateValue ToArchiveCandidateValue(this TagValueDocument value, string units) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            var val = value.ToTagValue(units: units);
            var min = Double.NaN;
            var max = Double.NaN;

            object o = null;
            if (value.Properties?.TryGetValue("CompressionAngleMinimum", out o) ?? false) {
                min = Convert.ToDouble(o);
            }
            if (value.Properties?.TryGetValue("CompressionAngleMaximum", out o) ?? false) {
                max = Convert.ToDouble(o);
            }

            return new ArchiveCandidateValue(val, min, max);
        }

    }
}
