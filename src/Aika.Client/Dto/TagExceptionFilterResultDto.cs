using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes the processing result for a value sent to a tag's exception filter.
    /// </summary>
    public class TagExceptionFilterResultDto : TagDataFilterResultDto {

        /// <summary>
        /// Gets or sets the last value to pass through the exception filter.  The 
        /// <see cref="Limits"/> used by the filter are calculated based on deviation from this value.
        /// </summary>
        public TagValueDto LastExceptionValue { get; set; }

        /// <summary>
        /// Gets or sets the limits that were calculated during the operation.
        /// </summary>
        public TagExceptionLimitSetDto Limits { get; set; }

        /// <summary>
        /// Gets or sets the exception filter settings at the time of processing.
        /// </summary>
        public TagValueFilterSettingsDto Settings { get; set; }

    }


    /// <summary>
    /// Describes the minimum and maximum limits calculated for an exception filter operation.
    /// </summary>
    public class TagExceptionLimitSetDto {
        
        /// <summary>
        /// Gets or sets the minumum limit.
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        /// Gets or sets the maximum limit.
        /// </summary>
        public double Maximum { get; set; }

    }
}
