using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Tags.Security {

    /// <summary>
    /// Describes an access security policy on a tag.
    /// </summary>
    public class TagSecurityPolicy {

        /// <summary>
        /// Access policy for tag administrators.
        /// </summary>
        public const string Administrator = "Administrator";

        /// <summary>
        /// Access policy that allows reading data from the tag.
        /// </summary>
        public const string DataRead = "Read";

        /// <summary>
        /// Access policy that allows writing data to the tag.
        /// </summary>
        public const string DataWrite = "Write";


        /// <summary>
        /// Gets the entries that grant access to the policy.
        /// </summary>
        public TagSecurityEntry[] Allow { get; }

        /// <summary>
        /// Gets the entries that deny access to the policy.
        /// </summary>
        public TagSecurityEntry[] Deny { get; }


        /// <summary>
        /// Creates a new <see cref="TagSecurityPolicy"/> object.
        /// </summary>
        /// <param name="allow">The access control entries that allow access to the policy.</param>
        /// <param name="deny">The access control entries that deny access to the policy.</param>
        public TagSecurityPolicy(TagSecurityEntry[] allow, TagSecurityEntry[] deny) {
            Allow = allow ?? new TagSecurityEntry[0];
            Deny = deny ?? new TagSecurityEntry[0];
        }

    }
}
