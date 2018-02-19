﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Aika {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Aika.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify at least one state..
        /// </summary>
        internal static string Error_AtLeastOneStateIsRequired {
            get {
                return ResourceManager.GetString("Error_AtLeastOneStateIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify at least one tag name..
        /// </summary>
        internal static string Error_AtLeastOneTagNameRequired {
            get {
                return ResourceManager.GetString("Error_AtLeastOneTagNameRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Tag operations cannot be performed on tags owned by another historian..
        /// </summary>
        internal static string Error_CannotOperateOnTagsOwnedByAnotherHistorian {
            get {
                return ResourceManager.GetString("Error_CannotOperateOnTagsOwnedByAnotherHistorian", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify a change description..
        /// </summary>
        internal static string Error_ChangeDescriptionIsRequired {
            get {
                return ResourceManager.GetString("Error_ChangeDescriptionIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Last-archived value cannot be more recent than last-received value..
        /// </summary>
        internal static string Error_CompressionFilter_LastArchivedValueCannotBeNewerThanLastReceivedValue {
            get {
                return ResourceManager.GetString("Error_CompressionFilter_LastArchivedValueCannotBeNewerThanLastReceivedValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify a data function name..
        /// </summary>
        internal static string Error_DataFunctionNameIsRequired {
            get {
                return ResourceManager.GetString("Error_DataFunctionNameIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The historian is not running.  Call the {0} method..
        /// </summary>
        internal static string Error_HistorianNotRunning {
            get {
                return ResourceManager.GetString("Error_HistorianNotRunning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The historian is still initializing..
        /// </summary>
        internal static string Error_HistorianStillInitializing {
            get {
                return ResourceManager.GetString("Error_HistorianStillInitializing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An identity is required..
        /// </summary>
        internal static string Error_IdentityIsRequired {
            get {
                return ResourceManager.GetString("Error_IdentityIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify a point count greater than zero..
        /// </summary>
        internal static string Error_PositivePointCountIsRequired {
            get {
                return ResourceManager.GetString("Error_PositivePointCountIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify a positive sample interval..
        /// </summary>
        internal static string Error_SampleIntervalMustBeGreaterThanZero {
            get {
                return ResourceManager.GetString("Error_SampleIntervalMustBeGreaterThanZero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Start time cannot be greater than end time..
        /// </summary>
        internal static string Error_StartTimeCannotBeLaterThanEndTime {
            get {
                return ResourceManager.GetString("Error_StartTimeCannotBeLaterThanEndTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified state set already exists..
        /// </summary>
        internal static string Error_StateSetAlreadyExists {
            get {
                return ResourceManager.GetString("Error_StateSetAlreadyExists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified state set does not exist..
        /// </summary>
        internal static string Error_StateSetDoesNotExist {
            get {
                return ResourceManager.GetString("Error_StateSetDoesNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify a state set name..
        /// </summary>
        internal static string Error_StateSetNameIsRequired {
            get {
                return ResourceManager.GetString("Error_StateSetNameIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify a tag ID..
        /// </summary>
        internal static string Error_TagIdIsRequired {
            get {
                return ResourceManager.GetString("Error_TagIdIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify a tag ID or name..
        /// </summary>
        internal static string Error_TagIdOrNameIsRequired {
            get {
                return ResourceManager.GetString("Error_TagIdOrNameIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify a tag name..
        /// </summary>
        internal static string Error_TagNameIsRequired {
            get {
                return ResourceManager.GetString("Error_TagNameIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified tag was not found..
        /// </summary>
        internal static string Error_TagNotFound {
            get {
                return ResourceManager.GetString("Error_TagNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The tag&apos;s state set is required..
        /// </summary>
        internal static string Error_TagStateSetIsRequired {
            get {
                return ResourceManager.GetString("Error_TagStateSetIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unsupported data function..
        /// </summary>
        internal static string Error_UnsupportedDataFunction {
            get {
                return ResourceManager.GetString("Error_UnsupportedDataFunction", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Created..
        /// </summary>
        internal static string TagModification_Created {
            get {
                return ResourceManager.GetString("TagModification_Created", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unauthorized.
        /// </summary>
        internal static string TagValue_Unauthorized {
            get {
                return ResourceManager.GetString("TagValue_Unauthorized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} invalid values were specified..
        /// </summary>
        internal static string WriteTagValuesResult_InvalidValuesSpecified {
            get {
                return ResourceManager.GetString("WriteTagValuesResult_InvalidValuesSpecified", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No values specified..
        /// </summary>
        internal static string WriteTagValuesResult_NoValuesSpecified {
            get {
                return ResourceManager.GetString("WriteTagValuesResult_NoValuesSpecified", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unauthorized.
        /// </summary>
        internal static string WriteTagValuesResult_Unauthorized {
            get {
                return ResourceManager.GetString("WriteTagValuesResult_Unauthorized", resourceCulture);
            }
        }
    }
}
