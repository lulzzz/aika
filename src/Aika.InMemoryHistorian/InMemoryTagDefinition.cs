using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika;
using Aika.Tags;
using Aika.Tags.Security;

namespace Aika.Implementations.InMemory {

    /// <summary>
    /// Aika tag definition for an <see cref="InMemoryHistorian"/>.
    /// </summary>
    public class InMemoryTagDefinition : TagDefinition {

        /// <summary>
        /// The historian that owns the tag.
        /// </summary>
        private readonly InMemoryHistorian _historian;

        /// <summary>
        /// Custom tag properties.
        /// </summary>
        internal ConcurrentDictionary<string, object> Properties { get; } = new ConcurrentDictionary<string, object>();


        /// <summary>
        /// Creates a new <see cref="InMemoryTagDefinition"/> object.
        /// </summary>
        /// <param name="historian">The owning historian.</param>
        /// <param name="id">The tag ID.</param>
        /// <param name="tagSettings">The tag settings.</param>
        /// <param name="metadata">The tag metadata.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="tagSettings"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <see langword="null"/>.</exception>
        /// <exception cref="ValidationException"><paramref name="tagSettings"/> is not valid.</exception>
        internal InMemoryTagDefinition(InMemoryHistorian historian, string id, TagSettings tagSettings, TagMetadata metadata) : base(historian, id, tagSettings, metadata, CreateTagSecurity(), null, null) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        /// <summary>
        /// Configures default tag security.
        /// </summary>
        /// <returns>
        /// A <see cref="TagSecurity"/> object that grants read, write and administrator permissions 
        /// on the tag for all users.
        /// </returns>
        private static TagSecurity CreateTagSecurity() {
            return new TagSecurity(null, new Dictionary<string, TagSecurityPolicy>() {
                {
                    TagSecurityPolicy.Administrator,
                    new TagSecurityPolicy(new [] { new TagSecurityEntry(null, "*") }, null)
                },
                {
                    TagSecurityPolicy.DataRead,
                    new TagSecurityPolicy(new [] { new TagSecurityEntry(null, "*") }, null)
                },
                {
                    TagSecurityPolicy.DataWrite,
                    new TagSecurityPolicy(new [] { new TagSecurityEntry(null, "*") }, null)
                }
            });
        }


        /// <summary>
        /// Gets the custom tag properties.
        /// </summary>
        /// <returns>
        /// The tag properties.
        /// </returns>
        public override IDictionary<string, object> GetProperties() {
            return Properties.ToDictionary(x => x.Key, x => x.Value);
        }


        /// <summary>
        /// Saves the specified snapshot value for the tag.
        /// </summary>
        /// <param name="value">The new snapshot value.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will save the snapshot value.
        /// </returns>
        protected override Task SaveSnapshotValue(TagValue value, CancellationToken cancellationToken) {
            // Do nothing; snapshot values are held in-memory on the tag.
            return Task.CompletedTask;
        }


        /// <summary>
        /// Inserts values into the historian archive for the tag.
        /// </summary>
        /// <param name="values">The values to archive.</param>
        /// <param name="nextArchiveCandidate">
        ///   The current archive candidate value for the tag (i.e. the next value that might 
        ///   potentially be written to the archive).
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The result of the write.
        /// </returns>
        protected override Task<WriteTagValuesResult> InsertArchiveValues(IEnumerable<TagValue> values, ArchiveCandidateValue nextArchiveCandidate, CancellationToken cancellationToken) {
            _historian.SaveArchiveCandidateValue(Id, nextArchiveCandidate);
            var result = !(values?.Any() ?? false) 
                ? WriteTagValuesResult.CreateEmptyResult() 
                : _historian.InsertArchiveValues(Id, values);
            return Task.FromResult(result);
        }

        
        /// <summary>
        /// Allows the <see cref="TagDefinition.OnDeleted"/> method to be called internally.
        /// </summary>
        internal new void OnDeleted() {
            base.OnDeleted();
        }
    }
}
