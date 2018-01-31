using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Tests {

    /// <summary>
    /// Utility class for providing <see cref="ClaimsPrincipal"/> objects for use in tests.
    /// </summary>
    internal static class Identities {

        /// <summary>
        /// Gets an internal identity that represents an Aika test user account.
        /// </summary>
        /// <returns>
        /// A <see cref="System.Security.Claims.ClaimsPrincipal"/> that represents an Aika test 
        /// user account.
        /// </returns>
        internal static System.Security.Claims.ClaimsPrincipal GetTestIdentity() {
            var identity = new System.Security.Claims.ClaimsIdentity("AikaTestUser");

            identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.Empty.ToString()));
            identity.AddClaim(new System.Security.Claims.Claim(identity.NameClaimType, "AikaTestUser"));

            return new System.Security.Claims.ClaimsPrincipal(identity);
        }

    }
}
