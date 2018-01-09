using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    /// <summary>
    /// Describes the initial values to use when loading a <see cref="TagDefinition"/> into memory.
    /// </summary>
    public class InitialTagValues {

        /// <summary>
        /// Gets the initial snapshot value of the tag.  Can be <see langword="null"/>.
        /// </summary>
        public TagValue SnapshotValue { get; }

        /// <summary>
        /// Gets the most-recent value to be permanently archived for the tag.  Can be <see langword="null"/>.
        /// </summary>
        public TagValue LastArchivedValue { get; }

        /// <summary>
        /// Gets the value that will be sent for permanent archiving the next time an incoming tag 
        /// value passes the tag's exception and compression filter tests.
        /// </summary>
        public TagValue NextArchiveCandidateValue { get; }


        /// <summary>
        /// Creates a new <see cref="InitialTagValues"/> object.
        /// </summary>
        /// <param name="snapshotValue">
        ///   The initial snapshot value of the tag.  Can be <see langword="null"/>.
        /// </param>
        /// <param name="lastArchivedValue">
        ///   The most-recent value to be permanently archived for the tag.  Can be <see langword="null"/>.
        /// </param>
        /// <param name="nextArchiveCandidateValue">T
        ///   The value that will be sent for permanent archiving the next time an incoming tag value 
        ///   passes the tag's exception and compression filter tests.
        /// </param>
        public InitialTagValues(TagValue snapshotValue, TagValue lastArchivedValue, TagValue nextArchiveCandidateValue) {
            SnapshotValue = snapshotValue;
            LastArchivedValue = lastArchivedValue;
            NextArchiveCandidateValue = nextArchiveCandidateValue;
        }

    }
}
