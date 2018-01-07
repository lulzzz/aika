using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;

namespace Aika.AspNetCore {
    internal static class DtoExtensions {

        internal static async Task<HistorianInfoDto> ToHistorianInfoDto(this AikaHistorian historian, CancellationToken cancellationToken) {
            if (historian == null) {
                return null;
            }

            var supportedFunctions = await historian.GetAvailableDataQueryFunctions(cancellationToken).ConfigureAwait(false);

            return new HistorianInfoDto() {
                TypeName = historian.Historian.GetType().FullName,
                Version = historian.Historian.GetType().Assembly?.GetName()?.Version.ToString(),
                Description = historian.Historian.Description,
                SupportedFunctions = supportedFunctions.Select(x => x.ToHistorianDataFunctionDto()).ToArray(),
                Properties = historian.Historian.Properties == null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(historian.Historian.Properties)
            };
        }


        internal static HistorianDataFunctionDto ToHistorianDataFunctionDto(this DataQueryFunction func) {
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            return new HistorianDataFunctionDto() {
                Name = func.Name,
                Description = func.Description,
                IsNativeFunction = func.IsNativeFuction
            };
        }


        internal static HistoricalTagValuesDto ToHistoricalTagValuesDto(this TagValueCollection values) {
            if (values == null) {
                return null;
            }

            return new HistoricalTagValuesDto() {
                Values = values.Values.Select(x => x.ToTagValueDto()).ToArray(),
                VisualizationHint = values.VisualizationHint.ToString()
            };
        }


        internal static StateSetFilter ToStateSetFilter(this StateSetSearchRequest filter) {
            if (filter == null) {
                throw new ArgumentNullException(nameof(filter));
            }

            return new StateSetFilter() {
                PageSize = filter.PageSize,
                Page = filter.Page,
                Filter = filter.Name
            };
        }


        /// <summary>
        /// Creates a new <see cref="StateSetDto"/> from an existing <see cref="StateSet"/> object.
        /// </summary>
        /// <param name="stateSet">The state set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stateSet"/> is <see langword="null"/>.</exception>
        internal static StateSetDto ToStateSetDto(this StateSet stateSet) {
            if (stateSet == null) {
                throw new ArgumentNullException(nameof(stateSet));
            }

            return new StateSetDto() {
                Name = stateSet.Name,
                States = stateSet.Select(x => x.ToStateSetItemDto()).ToArray()
            };
        }


        /// <summary>
        /// Creates a new <see cref="StateSetItemDto"/> object from the specified <see cref="StateSetItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="StateSetItem"/> to copy the configuration from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
        internal static StateSetItemDto ToStateSetItemDto(this StateSetItem item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            return new StateSetItemDto() {
                Name = item.Name,
                Value = item.Value
            };
        }


        /// <summary>
        /// Converts the object to an equivalent <see cref="StateSetItem"/>.
        /// </summary>
        /// <param name="item">The item to convert.</param>
        /// <returns>The equivalent <see cref="StateSetItem"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
        internal static StateSetItem ToStateSetItem(this StateSetItemDto item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            return new StateSetItem(item.Name, item.Value);
        }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionDto"/> object from the specified <see cref="TagDefinition"/>.
        /// </summary>
        /// <param name="tagDefinition">The tag definition to copy from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tagDefinition"/> is <see langword="null"/>.</exception>
        internal static TagDefinitionDto ToTagDefinitionDto(this TagDefinition tagDefinition) {
            if (tagDefinition == null) {
                throw new ArgumentNullException(nameof(tagDefinition));
            }

            return new TagDefinitionDto() {
                Id = tagDefinition.Id,
                Name = tagDefinition.Name,
                Description = tagDefinition.Description,
                Units = tagDefinition.Units,
                DataType = tagDefinition.DataType.ToString(),
                StateSet = tagDefinition.DataType == TagDataType.State
                    ? tagDefinition.StateSet
                    : null
            };
        }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionExtendedDto"/> object from the specified tag definition.
        /// </summary>
        /// <param name="tagDefinition">The tag definition.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tagDefinition"/> is <see langword="null"/>.</exception>
        internal static TagDefinitionExtendedDto ToTagDefinitionExtendedDto(this TagDefinition tagDefinition) {
            return new TagDefinitionExtendedDto() {
                Id = tagDefinition.Id,
                Name = tagDefinition.Name,
                Description = tagDefinition.Description,
                Units = tagDefinition.Units,
                DataType = tagDefinition.DataType.ToString(),
                StateSet = tagDefinition.DataType == TagDataType.State
                    ? tagDefinition.StateSet
                    : null,
                ExceptionFilterSettings = tagDefinition.DataFilter.ExceptionFilter.Settings.ToTagValueFilterSettingsDto(),
                CompressionFilterSettings = tagDefinition.DataFilter.CompressionFilter.Settings.ToTagValueFilterSettingsDto(),
                UtcCreatedAt = tagDefinition.UtcCreatedAt,
                UtcLastModifiedAt = tagDefinition.UtcLastModifiedAt,
                Properties = new Dictionary<string, object>(tagDefinition.GetProperties())
            };
        }


        /// <summary>
        /// Converts the object into a <see cref="TagSettings"/> object.
        /// </summary>
        /// <returns>
        /// An equivalent <see cref="TagSettings"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
        internal static TagSettings ToTagDefinitionUpdate(this TagDefinitionUpdateDto item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            return new TagSettings() {
                Name = item.Name,
                Description = item.Description,
                Units = item.Units,
                DataType = Enum.TryParse<TagDataType>(item.DataType, out var dt) ? dt : TagDataType.FloatingPoint,
                StateSet = item.StateSet,
                ExceptionFilterSettings = item.ExceptionFilterSettings?.ToTagValueFilterSettingsUpdate(),
                CompressionFilterSettings = item.CompressionFilterSettings?.ToTagValueFilterSettingsUpdate()
            };
        }


        /// <summary>
        /// Converts the <see cref="TagSearchRequest"/> into a <see cref="TagDefinitionFilter"/> used internally by Aika.
        /// </summary>
        /// <returns>
        /// The equivalent <see cref="TagDefinitionFilter"/>.
        /// </returns>
        internal static TagDefinitionFilter ToTagDefinitionFilter(this TagSearchRequest item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            var clauses = new List<TagDefinitionFilterClause>();

            if (!String.IsNullOrWhiteSpace(item.Name)) {
                clauses.Add(new TagDefinitionFilterClause() { Field = TagDefinitionFilterField.Name, Value = item.Name });
            }
            if (!String.IsNullOrWhiteSpace(item.Description)) {
                clauses.Add(new TagDefinitionFilterClause() { Field = TagDefinitionFilterField.Description, Value = item.Description });
            }
            if (!String.IsNullOrWhiteSpace(item.Units)) {
                clauses.Add(new TagDefinitionFilterClause() { Field = TagDefinitionFilterField.Units, Value = item.Units });
            }

            return new TagDefinitionFilter() {
                PageSize = item.PageSize,
                Page = item.Page,
                FilterType = Enum.TryParse<TagDefinitionFilterJoinType>(item.Type, out var t) ? t : TagDefinitionFilterJoinType.And,
                FilterClauses = clauses
            };
        }


        internal static TagCompressionFilterResultDto ToTagCompressionFilterResultDto(this CompressionFilterResult result, TagDefinition tag) {
            if (result == null) {
                return null;
            }

            return new TagCompressionFilterResultDto() {
                TagId = tag?.Id,
                TagName = tag?.Name,
                UtcReceivedAt = result.UtcReceivedAt,
                Value = result.Value.ToTagValueDto(),
                Notes = result.Notes,
                Rejected = result.Result.Rejected,
                Reason = result.Result.Reason,
                LastArchivedValue = result.Result.LastArchivedValue.ToTagValueDto(),
                LastReceivedValue = result.Result.LastReceivedValue.ToTagValueDto(),
                Settings = result.Result.Settings.ToTagValueFilterSettingsDto(),
                Limits = new TagCompressionLimitsDto() {
                    Base = result.Result.Limits.Base == null
                        ? null
                        : new TagCompressionLimitSetDto() {
                            UtcSampleTime = result.Result.Limits.Base.UtcSampleTime,
                            Minimum = result.Result.Limits.Base.Minimum,
                            Maximum = result.Result.Limits.Base.Maximum
                        },
                    Incoming = result.Result.Limits.Incoming == null
                        ? null
                        : new TagCompressionLimitSetDto() {
                            UtcSampleTime = result.Result.Limits.Incoming.UtcSampleTime,
                            Minimum = result.Result.Limits.Incoming.Minimum,
                            Maximum = result.Result.Limits.Incoming.Maximum
                        },
                    Updated = result.Result.Limits.Updated == null
                        ? null
                        : new TagCompressionLimitSetDto() {
                            UtcSampleTime = result.Result.Limits.Updated.UtcSampleTime,
                            Minimum = result.Result.Limits.Updated.Minimum,
                            Maximum = result.Result.Limits.Updated.Maximum
                        }
                }
            };
        }


        internal static TagExceptionFilterResultDto ToTagExceptionFilterResultDto(this ExceptionFilterResult result, TagDefinition tag) {
            if (result == null) {
                return null;
            }

            return new TagExceptionFilterResultDto() {
                TagId = tag?.Id,
                TagName = tag?.Name,
                UtcReceivedAt = result.UtcReceivedAt,
                Value = result.Value.ToTagValueDto(),
                Notes = result.Notes,
                Rejected = result.Result.Rejected,
                Reason = result.Result.Reason,
                LastExceptionValue = result.Result.LastExceptionValue.ToTagValueDto(),
                Settings = result.Result.Settings.ToTagValueFilterSettingsDto(),
                Limits = result.Result.Limits == null
                    ? null
                    : new TagExceptionLimitSetDto() {
                        Minimum = result.Result.Limits.Minimum,
                        Maximum = result.Result.Limits.Maximum
                    }
            };
        }


        internal static TagValueDto ToTagValueDto(this TagValue value) {
            if (value == null) {
                return null;
            }

            return new TagValueDto() {
                UtcSampleTime = value.UtcSampleTime,
                NumericValue = value.NumericValue,
                TextValue = value.TextValue,
                Quality = value.Quality.ToString(),
            };
        }


        internal static TagValue ToTagValue(this TagValueDto value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            return new TagValue(value.UtcSampleTime, value.NumericValue.HasValue ? value.NumericValue.Value : Double.NaN, value.TextValue, Enum.TryParse<TagValueQuality>(value.Quality, out var q) ? q : TagValueQuality.Uncertain, value.Units);
        }


        /// <summary>
        /// Creates a new <see cref="TagValueFilterSettingsDto"/> object from an equivalent <see cref="TagValueFilterSettings"/> object.
        /// </summary>
        /// <param name="settings">The equivalent <see cref="TagValueFilterSettings"/> object.</param>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
        internal static TagValueFilterSettingsDto ToTagValueFilterSettingsDto(this TagValueFilterSettings settings) {
            if (settings == null) {
                throw new ArgumentNullException(nameof(settings));
            }

            return new TagValueFilterSettingsDto() {
                IsEnabled = settings.IsEnabled,
                LimitType = settings.LimitType.ToString(),
                Limit = settings.Limit,
                WindowSize = settings.WindowSize,
            };
        }


        /// <summary>
        /// Converts the object into the equivalent <see cref="TagValueFilterSettingsUpdate"/> object.
        /// </summary>
        /// <returns>
        /// An equivalent <see cref="TagValueFilterSettingsUpdate"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
        internal static TagValueFilterSettingsUpdate ToTagValueFilterSettingsUpdate(this TagValueFilterSettingsDto settings) {
            if (settings == null) {
                throw new ArgumentNullException(nameof(settings));
            }

            return new TagValueFilterSettingsUpdate() {
                IsEnabled = settings.IsEnabled,
                LimitType = String.IsNullOrWhiteSpace(settings.LimitType) 
                    ? (TagValueFilterDeviationType?) null 
                    : Enum.TryParse<TagValueFilterDeviationType>(settings.LimitType, out var lt) 
                        ? lt 
                        : TagValueFilterDeviationType.Absolute,
                Limit = settings.Limit,
                WindowSize = settings.WindowSize
            };
        }


        /// <summary>
        /// Converts the object into the format required for an <see cref="ITagDataWriter"/> write call.
        /// </summary>
        /// <param name="writeRequest">The rwquest to convert.</param>
        /// <returns>
        /// A dictionary that maps from case-insensitive tag name to tag value.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="writeRequest"/> is <see langword="null"/>.</exception>
        internal static IDictionary<string, IEnumerable<TagValue>> ToTagValueDictionary(this WriteTagValuesRequest writeRequest) {
            if (writeRequest == null) {
                throw new ArgumentNullException(nameof(writeRequest));
            }

            if (writeRequest.Data == null) {
                return new Dictionary<string, IEnumerable<TagValue>>();
            }

            return writeRequest.Data
                               .ToLookup(x => x.Key,
                                         x => x.Value,
                                         StringComparer.OrdinalIgnoreCase)
                               .ToDictionary(x => x.Key,
                                             x => x.SelectMany(y => y.Select(dto => dto.ToTagValue()))
                                                   .ToArray()
                                                   .AsEnumerable());
        }


        /// <summary>
        /// Creates a new <see cref="WriteTagValuesResultDto"/> object using the specified <see cref="WriteTagValuesResult"/>.
        /// </summary>
        /// <param name="result">The <see cref="WriteTagValuesResult"/> to copy from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
        internal static WriteTagValuesResultDto ToWriteTagValuesResultDto(this WriteTagValuesResult result) {
            if (result == null) {
                throw new ArgumentNullException(nameof(result));
            }

            return new WriteTagValuesResultDto() {
                Success = result.Success,
                SampleCount = result.SampleCount,
                UtcEarliestSampleTime = result.UtcEarliestSampleTime,
                UtcLatestSampleTime = result.UtcLatestSampleTime,
                Notes = result.Notes?.ToArray()
            };
        }

    }
}
