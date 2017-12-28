using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.AspNetCore.Authorization {

    /// <summary>
    /// Describes policies used to authorize API access.
    /// </summary>
    public static class Policies {

        /// <summary>
        /// Namespace for policies.
        /// </summary>
        private const string PolicyNamespace = "aika";

        /// <summary>
        /// Separator between namespace and name.
        /// </summary>
        private const string Separator = ":";

        /// <summary>
        /// Prefix to prepend to policy names.
        /// </summary>
        private const string PolicyNamePrefix = PolicyNamespace + Separator;

        /// <summary>
        /// The policy required to be able to perform tag searches and read tag data.  Note that 
        /// individual tags can apply additional authorization constraints.
        /// </summary>
        public const string ReadTagData = PolicyNamePrefix + "readtagdata";

        /// <summary>
        /// The policy required to be able to write tag data.  Note that individual tags can apply 
        /// additional authorization constraints.
        /// </summary>
        public const string WriteTagData = PolicyNamePrefix + "writetagdata";

        /// <summary>
        /// The policy required to be able to manage tag configuration (i.e. create, update, and delete 
        /// tags).  Note that individual tags can apply additional authorization constraints.
        /// </summary>
        public const string ManageTags = PolicyNamePrefix + "managetags";

        /// <summary>
        /// The policy required to be able to perform general-purpose administrative duties.
        /// </summary>
        public const string Administrator = PolicyNamePrefix + "administrator";

    }
}
