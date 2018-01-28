using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Tags.Security {

    /// <summary>
    /// Describes an entry in a <see cref="TagSecurityPolicy"/>.
    /// </summary>
    public class TagSecurityEntry {

        /// <summary>
        /// The claim type that the entry applies to.
        /// </summary>
        public string ClaimType { get; }

        /// <summary>
        /// The claim value that the entry applies to.
        /// </summary>
        public string Value { get; }


        /// <summary>
        /// Creates a new <see cref="TagSecurityEntry"/> object.
        /// </summary>
        /// <param name="claimType">The claim type that the entry applies to.</param>
        /// <param name="value">The claim value that the entry applies to.</param>
        public TagSecurityEntry(string claimType, string value) {
            ClaimType = claimType;
            Value = value;
        }

    }
}
