using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Client.Dto {
    /// <summary>
    /// Describes a modification to a tag.
    /// </summary>
    public class TagChangeHistoryEntryDto {
        /// <summary>
        /// Gets the ID of the modification.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets the UTC time stamp that the modification was made at.
        /// </summary>
        public DateTime UtcTime { get; set; }

        /// <summary>
        /// Gets the name of the user who made the modification.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Gets the description of the modification.
        /// </summary>
        public string Description { get; set; }
    }
}
