using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes the processing result for a value sent to a tag's compression filter.
    /// </summary>

    public class TagCompressionFilterResultDto : TagDataFilterResultDto {

        /// <summary>
        /// Gets or sets the last-archived value held by the filter at the time of processing.
        /// </summary>
        public TagValueDto LastArchivedValue { get; set; }

        /// <summary>
        /// Gets or sets the value that was received by the compression filter prior to the 
        /// incoming <see cref="TagDataFilterResultDto.Value"/>.
        /// </summary>
        public TagValueDto LastReceivedValue { get; set; }

        /// <summary>
        /// Gets or sets the limits that were calculated during the operation.
        /// </summary>
        public TagCompressionLimitsDto Limits { get; set; }

        /// <summary>
        /// Gets or sets the compression filter settings at the time of processing.
        /// </summary>
        public TagValueFilterSettingsDto Settings { get; set; }

    }


    /// <summary>
    /// Describes the limits associated with a compression filter operation.
    /// </summary>
    public class TagCompressionLimitsDto {

        /// <summary>
        /// Gets or sets the base compression limits at the time of processing.  Combined with the 
        /// last-archived value and the last-received value, these are used to calculate the 
        /// minimum and maximum limits for the incoming value.
        /// </summary>
        public TagCompressionLimitSetDto Base { get; set; }

        /// <summary>
        /// Gets or sets the limits that were calculated for the incoming value.
        /// </summary>
        public TagCompressionLimitSetDto Incoming { get; set; }

        /// <summary>
        /// Gets or sets the updated limits that will be used as the base limits for the next incoming 
        /// value.
        /// </summary>
        public TagCompressionLimitSetDto Updated { get; set; }

    }


    /// <summary>
    /// Describes a set of compression filter limits associated with a <see cref="TagCompressionLimitsDto"/> instance.
    /// </summary>
    public class TagCompressionLimitSetDto {

        /// <summary>
        /// Gets or sets the UTC sample time that was used to calculate the limits.
        /// </summary>
        public DateTime UtcSampleTime { get; set; }

        /// <summary>
        /// Gets or sets the minimum value for the <see cref="UtcSampleTime"/>.
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        /// Gets or sets the maximum value for the <see cref="UtcSampleTime"/>.
        /// </summary>
        public double Maximum { get; set; }

    }

}
