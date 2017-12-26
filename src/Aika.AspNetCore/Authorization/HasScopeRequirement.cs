using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace Aika.AspNetCore.Authorization {

    /// <summary>
    /// An authorization requirement that specifies that a calling identity must have a given scope.
    /// </summary>
    public class HasScopeRequirement : IAuthorizationRequirement {

        /// <summary>
        /// Gets the scope issuer that must be matched.
        /// </summary>
        public string Issuer { get; }

        /// <summary>
        /// Gets the scope name that must be matched.
        /// </summary>
        public string Scope { get; }


        /// <summary>
        /// Creates a new <see cref="HasScopeRequirement"/> object.
        /// </summary>
        /// <param name="scope">The scope name.</param>
        /// <param name="issuer">The issuer name.</param>
        public HasScopeRequirement(string scope, string issuer) {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }

    }
}
