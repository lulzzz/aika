using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Aika.Tags {
    /// <summary>
    /// Describes create/update staus for a tag.
    /// </summary>
    public class TagChangeHistoryEntry {

        /// <summary>
        /// Gets the ID of the modification.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the UTC time stamp that the modification was made at.
        /// </summary>
        public DateTime UtcTime { get; }

        /// <summary>
        /// Gets the name of the user who made the modification.
        /// </summary>
        public string User { get; }

        /// <summary>
        /// Gets the description of the modification.
        /// </summary>
        public string Description { get; }


        /// <summary>
        /// Creates a new <see cref="TagChangeHistoryEntry"/> object.
        /// </summary>
        /// <param name="id">The ID of the change.</param>
        /// <param name="utcTime">The UTC time stamp for the change.</param>
        /// <param name="user">The name of the user making the change.</param>
        /// <param name="description">The change description.</param>
        public TagChangeHistoryEntry(Guid id, DateTime utcTime, string user, string description) {
            Id = id;
            UtcTime = utcTime;
            User = user ?? "<UNKNOWN>";
            Description = description;
        }


        /// <summary>
        /// Creates a new "created" information item.
        /// </summary>
        /// <param name="creator">The tag's creator.</param>
        /// <returns>
        /// A new <see cref="TagChangeHistoryEntry"/> object.
        /// </returns>
        public static TagChangeHistoryEntry Created(ClaimsPrincipal creator) {
            return new TagChangeHistoryEntry(Guid.NewGuid(), DateTime.UtcNow, creator?.Identity?.Name, Resources.TagModification_Created);
        }


        /// <summary>
        /// Creates a new "updated" information item.
        /// </summary>
        /// <param name="modifier">The tag's modifier.</param>
        /// <param name="description">The description of the modification.</param>
        /// <returns>
        /// A new <see cref="TagChangeHistoryEntry"/> object.
        /// </returns>
        public static TagChangeHistoryEntry Updated(ClaimsPrincipal modifier, string description) {
            return new TagChangeHistoryEntry(Guid.NewGuid(), DateTime.UtcNow, modifier?.Identity?.Name, description);
        }

    }
}
