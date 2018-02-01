using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Tags {
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
        /// Gets the last value to pass the tag's exception filter.  Can be <see langword="null"/>.
        /// </summary>
        public TagValue LastExceptionValue { get; }

        /// <summary>
        /// Gets the minimum compression angle value, calculated when <see cref="LastExceptionValue"/> 
        /// was passed to the compression filter.
        /// </summary>
        public double CompressionAngleMinimum { get; }

        /// <summary>
        /// Gets the maximum compression angle value, calculated when <see cref="LastExceptionValue"/> 
        /// was passed to the compression filter.
        /// </summary>
        public double CompressionAngleMaximum { get; }



        /// <summary>
        /// Creates a new <see cref="InitialTagValues"/> object.
        /// </summary>
        /// <param name="snapshotValue">
        ///   The initial snapshot value of the tag.  Can be <see langword="null"/>.
        /// </param>
        /// <param name="lastArchivedValue">
        ///   The most-recent value to be permanently archived for the tag.  Can be <see langword="null"/>.
        /// </param>
        /// <param name="lastExceptionValue">
        ///   The last value that passed the tag data filter's exception filter.
        /// </param>
        /// <param name="compressionAngleMinimum">
        ///   The minimum compression angle value, calculated when <see cref="LastExceptionValue"/> was 
        ///   passed to the compression filter.
        /// </param>
        /// <param name="compressionAngleMaximum">
        ///   The maximum compression angle value, calculated when <see cref="LastExceptionValue"/> was 
        ///   passed to the compression filter.
        /// </param>
        public InitialTagValues(TagValue snapshotValue, TagValue lastArchivedValue, TagValue lastExceptionValue, double compressionAngleMinimum, double compressionAngleMaximum) {
            SnapshotValue = snapshotValue;
            LastArchivedValue = lastArchivedValue;
            LastExceptionValue = lastExceptionValue;
            CompressionAngleMinimum = compressionAngleMinimum;
            CompressionAngleMaximum = compressionAngleMaximum;
        }

    }
}
