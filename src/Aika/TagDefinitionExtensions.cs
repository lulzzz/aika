using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Aika {

    /// <summary>
    /// Extension methods for <see cref="TagDefinition"/> objects.
    /// </summary>
    internal static class TagDefinitionExtensions {

        /// <summary>
        /// Attempts to validate an incoming tag value.
        /// </summary>
        /// <param name="tag">The tag that the value is for.</param>
        /// <param name="incoming">The incoming tag value.</param>
        /// <param name="stateSet">
        ///   The state set for the tag.  Can be <see langword="null"/> when the tag's data type is not 
        ///   <see cref="TagDataType.State"/>.
        /// </param>
        /// <param name="validatedValue">The validated tag value.</param>
        /// <returns>
        /// <see langword="true"/> if the value could be validated, or <see langword="false"/> otherwise.  
        /// When the result is <see langword="false"/>, <paramref name="validatedValue"/> will be 
        /// <see langword="null"/>.
        /// </returns>
        internal static bool TryValidateIncomingTagValue(this TagDefinition tag, TagValue incoming, StateSet stateSet, out TagValue validatedValue) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (incoming == null) {
                throw new ArgumentNullException(nameof(incoming));
            }

            bool result;

            switch (tag.DataType) {
                case TagDataType.FloatingPoint:
                    result = TryValidateDoubleValue(tag, incoming, out validatedValue);
                    break;
                case TagDataType.Integer:
                    result = TryValidateInt32Value(tag, incoming, out validatedValue);
                    break;
                case TagDataType.Text:
                    result = TryValidateStringValue(tag, incoming, out validatedValue);
                    break;
                case TagDataType.State:
                    result = TryValidateStateValue(tag, incoming, stateSet, out validatedValue);
                    break;
                default:
                    validatedValue = null;
                    result = false;
                    break;
            }

            return result;
        }


        /// <summary>
        /// Attempts to validate an incoming floating-point tag value.
        /// </summary>
        /// <param name="tag">The tag for the value.</param>
        /// <param name="originalValue">The incoming value.</param>
        /// <param name="validatedValue">The validated value.</param>
        /// <returns>
        /// A flag that indicates if the value was validated.
        /// </returns>
        private static bool TryValidateDoubleValue(TagDefinition tag, TagValue originalValue, out TagValue validatedValue) {
            validatedValue = new TagValue(originalValue.UtcSampleTime, originalValue.NumericValue, Convert.ToString(originalValue.NumericValue, CultureInfo.InvariantCulture), originalValue.Quality, tag.Units);
            return true;
        }


        /// <summary>
        /// Attempts to validate an incoming integer tag value.
        /// </summary>
        /// <param name="tag">The tag for the value.</param>
        /// <param name="originalValue">The incoming value.</param>
        /// <param name="validatedValue">The validated value.</param>
        /// <returns>
        /// A flag that indicates if the value was validated.
        /// </returns>
        private static bool TryValidateInt32Value(TagDefinition tag, TagValue originalValue, out TagValue validatedValue) {
            var intValue = (int) originalValue.NumericValue;
            validatedValue = new TagValue(originalValue.UtcSampleTime, intValue, Convert.ToString(intValue, CultureInfo.InvariantCulture), originalValue.Quality, tag.Units);
            return true;
        }


        /// <summary>
        /// Attempts to validate an incoming string tag value.
        /// </summary>
        /// <param name="tag">The tag for the value.</param>
        /// <param name="originalValue">The incoming value.</param>
        /// <param name="validatedValue">The validated value.</param>
        /// <returns>
        /// A flag that indicates if the value was validated.
        /// </returns>
        private static bool TryValidateStringValue(TagDefinition tag, TagValue originalValue, out TagValue validatedValue) {
            validatedValue = new TagValue(originalValue.UtcSampleTime, Double.NaN, originalValue.TextValue, originalValue.Quality, null);
            return true;
        }


        /// <summary>
        /// Attempts to validate an incoming floating-point tag value.
        /// </summary>
        /// <param name="tag">The tag for the value.</param>
        /// <param name="originalValue">The incoming value.</param>
        /// <param name="stateSet">The state set for the <paramref name="tag"/>.</param>
        /// <param name="validatedValue">The validated value.</param>
        /// <returns>
        /// A flag that indicates if the value was validated.  If <paramref name="stateSet"/> 
        /// is <see langword="null"/>, or if it does not match the name specified in the 
        /// <see cref="TagDefinition.StateSet"/> property of the <paramref name="tag"/>, the 
        /// result will be <see langword="false"/>.
        /// </returns>
        private static bool TryValidateStateValue(TagDefinition tag, TagValue originalValue, StateSet stateSet, out TagValue validatedValue) {
            if (stateSet == null || !stateSet.Name.Equals(tag.StateSet, StringComparison.OrdinalIgnoreCase)) {
                validatedValue = null;
                return false;
            }

            StateSetItem state = null;
            if (!String.IsNullOrWhiteSpace(originalValue.TextValue)) {
                state = stateSet[originalValue.TextValue];
            }

            if (state == null) {
                var valAsInt = (int) originalValue.NumericValue;
                state = stateSet.States.FirstOrDefault(x => x.Value == valAsInt);
            }

            if (state == null) {
                validatedValue = null;
                return false;
            }

            validatedValue = new TagValue(originalValue.UtcSampleTime, state.Value, state.Name, originalValue.Quality, null);
            return true;
        }

    }
}
