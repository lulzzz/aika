using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Aika.AspNetCore.Authorization {

    /// <summary>
    /// Authorization handler that processes <see cref="HasScopeRequirement"/> instances.
    /// </summary>
    public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement> {

        /// <summary>
        /// Scope claim type to match.
        /// </summary>
        private const string ScopeClaimType = "scope";


        /// <summary>
        /// Tests if the specified requirement has been met.
        /// </summary>
        /// <param name="context">The authorization context.</param>
        /// <param name="requirement">The scope requirement to process.</param>
        /// <returns>
        /// A task that will process the requirement.  If the requirement is met, the <paramref name="context"/> 
        /// will be updated to indicate success.
        /// </returns>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement) {
            var claim = context.User.FindFirst(c => String.Equals(c.Type, ScopeClaimType) && String.Equals(c.Issuer, requirement.Issuer));

            // The the user does not have a scope claim, there's no point in continuing.
            if (claim == null) {
                return Task.CompletedTask;
            }

            // Check for the required scope.
            if (claim.Value.Split(' ').Any(s => s == requirement.Scope)) {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
