﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    /// <summary>
    /// Defines common role names used for authorizing access to historian features.
    /// </summary>
    public static class Roles {

        /// <summary>
        /// Namespace for common role names.
        /// </summary>
        private const string RoleNamespace = "aika";

        /// <summary>
        /// Separator between namespace and name.
        /// </summary>
        private const string Separator = ":";

        /// <summary>
        /// Prefix to prepend to role names.
        /// </summary>
        private const string RoleNamePrefix = RoleNamespace + Separator;

        /// <summary>
        /// The role required to be able to perform tag searches and read tag data.  Note that 
        /// individual tags can apply additional authorization constraints.
        /// </summary>
        public const string ReadTagData = RoleNamePrefix + "readtagdata";

        /// <summary>
        /// The role required to be able to write tag data.  Note that individual tags can apply 
        /// additional authorization constraints.
        /// </summary>
        public const string WriteTagData = RoleNamePrefix + "writetagdata";

        /// <summary>
        /// The role required to be able to manage tag configuration (i.e. create, update, and delete 
        /// tags).  Note that individual tags can apply additional authorization constraints.
        /// </summary>
        public const string ManageTags = RoleNamePrefix + "managetags";

        /// <summary>
        /// The role required to be able to perform general-purpose administrative duties.
        /// </summary>
        public const string Administrator = RoleNamePrefix + "administrator";

    }
}
