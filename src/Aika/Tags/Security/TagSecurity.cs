using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Aika.Tags.Security {

    /// <summary>
    /// Describes security on a tag.
    /// </summary>
    public class TagSecurity {

        /// <summary>
        /// Gets the ID of the tag's owner.
        /// </summary>
        public string Owner { get; }

        /// <summary>
        /// Gets the access policies for tha tag.
        /// </summary>
        public IReadOnlyDictionary<string, TagSecurityPolicy> Policies { get; }


        /// <summary>
        /// Creates a new <see cref="TagSecurity"/> object.
        /// </summary>
        /// <param name="owner">The tag owner.</param>
        /// <param name="policies">The tag's access policies.</param>
        public TagSecurity(string owner, IDictionary<string, TagSecurityPolicy> policies) {
            Owner = owner;
            Policies = new ReadOnlyDictionary<string, TagSecurityPolicy>(policies ?? new Dictionary<string, TagSecurityPolicy>());
        }

    }
}
