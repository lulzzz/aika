using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Tags;
using Aika.Tags.Security;

namespace Aika.Elasticsearch {

    /// <summary>
    /// Describes a tag on an <see cref="ElasticsearchHistorian"/>.
    /// </summary>
    public class ElasticsearchTagDefinition : TagDefinition {

        /// <summary>
        /// The owning historian.
        /// </summary>
        private readonly ElasticsearchHistorian _historian;

        /// <summary>
        /// The tag ID, converted to a <see cref="Guid"/>.
        /// </summary>
        internal Guid IdAsGuid { get; }

        /// <summary>
        /// Creates a new <see cref="ElasticsearchTagDefinition"/> object.
        /// </summary>
        /// <param name="historian">The owning historian.</param>
        /// <param name="id">The tag ID.</param>
        /// <param name="settings">The tag settings.</param>
        /// <param name="metadata">The tag metadata.</param>
        /// <param name="security">The tag security configuration.</param>
        /// <param name="initialTagValues">The initial tag values, to use with the exception and compression filters.</param>
        /// <param name="changeHistory">The change history information for the tag.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="security"/> is <see langword="null"/>.</exception>
        internal ElasticsearchTagDefinition(ElasticsearchHistorian historian, Guid id, TagSettings settings, TagMetadata metadata, TagSecurity security, InitialTagValues initialTagValues, IEnumerable<TagChangeHistoryEntry> changeHistory) : base(historian, id.ToString(), settings, metadata, security, initialTagValues, changeHistory) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            IdAsGuid = id;
        }


        /// <summary>
        /// Inserts tag values into the Elasticsearch archive.
        /// </summary>
        /// <param name="values">The values to write.</param>
        /// <param name="nextArchiveCandidate">The updated archive candidate value.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return a <see cref="WriteTagValuesResult"/> for the operation.  Due to 
        /// the way that Elasticsearch writesare batched and delegated to a background class to be 
        /// written in bulk, it is possible that write operations can fail after this method has 
        /// returned.
        /// </returns>
        protected override Task<WriteTagValuesResult> InsertArchiveValues(IEnumerable<TagValue> values, ArchiveCandidateValue nextArchiveCandidate, CancellationToken cancellationToken) {
            return _historian.InsertArchiveValues(this, values, nextArchiveCandidate, cancellationToken);
        }


        /// <summary>
        /// Saves the snapshot value for the tag to Elasticsearch.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="cancellationToken">The cancellation task for the operation.</param>
        /// <returns>
        /// A task that will perform the write.  Due to the way that Elasticsearch writes are batched 
        /// and delegated to a background class to be written in bulk, it is possible that write 
        /// operations can fail after this method has returned.
        /// </returns>
        /// </returns>
        protected override Task SaveSnapshotValue(TagValue value, CancellationToken cancellationToken) {
            return _historian.WriteSnapshotValue(this, value, cancellationToken);
        }
    }
}
