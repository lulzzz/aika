using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes the result of an exception filter or compression filter operation on a single 
    /// incoming value.
    /// </summary>
    public abstract class TagDataFilterResultDto {

        /// <summary>
        /// Gets or sets the ID of the tag that the result applies to.
        /// </summary>
        public string TagId { get; set; }

        /// <summary>
        /// Gets or sets the name of the tag that the result applies to.
        /// </summary>
        public string TagName { get; set; }

        /// <summary>
        /// Gets or sets the UTC time stamp that the value was received at.
        /// </summary>
        public DateTime UtcReceivedAt { get; set; }

        /// <summary>
        /// Gets or sets the incoming tag value that was processed by the filter.
        /// </summary>
        public TagValueDto Value { get; set; }

        /// <summary>
        /// Gets or sets additional notes about the result.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Gets or sets a flag that indicates if the filter rejected the value or not.
        /// </summary>
        public bool Rejected { get; set; }

        /// <summary>
        /// Gets or sets the reason that the filter rejected (or did not reject) the <see cref="Value"/>.
        /// </summary>
        public string Reason { get; set; }

    }
}
