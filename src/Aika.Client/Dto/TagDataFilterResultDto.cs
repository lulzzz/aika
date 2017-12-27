using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Client.Dto
{
    public abstract class TagDataFilterResultDto
    {

        public string TagId { get; set; }

        public string TagName { get; set; }

        /// <summary>
        /// Gets or sets the UTC time stamp that the value was received at.
        /// </summary>
        public DateTime UtcReceivedAt { get; set; }

        /// <summary>
        /// Gets or sets the tag value.
        /// </summary>
        public TagValueDto Value { get; set; }

        /// <summary>
        /// Gets or sets additional notes about the result.
        /// </summary>
        public string Notes { get; set; }

    }
}
