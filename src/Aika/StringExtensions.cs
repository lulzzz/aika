using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Aika {
    /// <summary>
    /// Extension methods for strings.
    /// </summary>
    public static class StringExtensions {

        /// <summary>
        /// Escaped versions of characters that have special meanings in regular expressions unless escaped.
        /// </summary>
        private static readonly string[] RegexSpecialCharacterEscapes = { @"\\", @"\.", @"\$", @"\^", @"\{", @"\[", @"\(", @"\|", @"\)", @"\*", @"\+", @"\?" };

        /// <summary>
        /// Converts the string into a regular expression.
        /// </summary>
        /// <param name="s">The string containing the pattern to match.</param>
        /// <param name="options">The options to use when creating the regular expression.</param>
        /// <returns>
        /// A <see cref="Regex"/> that will match the string.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// When generating the regular expression, characters in <paramref name="s"/> that have special meaning in 
        /// regular expressions (e.g. '$') will be replaced with escaped versions of themselves (e.g. '\$').
        /// </remarks>
        public static Regex ToRegex(this string s, RegexOptions options) {
            if (s == null) {
                throw new ArgumentNullException(nameof(s));
            }

            var pattern = RegexSpecialCharacterEscapes.Aggregate(s, (current, specialCharacter) => Regex.Replace(current, specialCharacter, specialCharacter, options));
            return new Regex(pattern, options);
        }


        /// <summary>
        /// Determines if the string matches the specified regular expression.
        /// </summary>
        /// <param name="s">The string to test.</param>
        /// <param name="expression">The regular expression to match.</param>
        /// <returns>
        /// <see langword="true"/> if the string matches the expression, <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If <paramref name="s"/> is <see langword="null"/>, the method will always return <see langword="false"/>.
        /// </remarks>
        public static bool Like(this string s, Regex expression) {
            if (s == null) {
                return false;
            }

            if (expression == null) {
                throw new ArgumentNullException(nameof(expression));
            }

            return expression.Match(s).Success;
        }


        /// <summary>
        /// Determines if the string matches the specified wildcard pattern.
        /// </summary>
        /// <param name="s">The string to test.</param>
        /// <param name="pattern">The wildcard pattern to match.</param>
        /// <returns><see langword="true"/> if the string matches the wildcard pattern, <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// 
        /// <para>
        /// If <paramref name="s"/> is <see langword="null"/>, the method will always return <see langword="false"/>.
        /// </para>
        /// 
        /// <para>
        /// When specifying the wildcard pattern, <c>*</c> is interpreted as a multi-character 
        /// wildcard; <c>?</c> is interpreted as a single-character wildcard.
        /// </para>
        /// 
        /// <para>
        /// The pattern matching is case-insensitive.
        /// </para>
        /// 
        /// </remarks>
        /// <example>
        /// The following example demonstrates how to use the method:
        /// 
        /// <code>
        /// string myString = "Hello, world!";
        /// bool like1 = myString.Like("He*o, wor?d!"); // returns true
        /// bool like2 = myString.Like("He?o, wor?d!"); // returns false
        /// </code>
        /// 
        /// </example>
        public static bool Like(this string s, string pattern) {
            if (s == null) {
                return false;
            }

            if (pattern == null) {
                throw new ArgumentNullException(nameof(pattern));
            }

            // Construct a regex that can be used to search the string using the specified pattern as its base.
            //
            // The following characters must be escaped so that their presence doesn't modify the generated regex
            // \ . $ ^ { [ ( | ) +
            //
            // * and ? are special cases that will modify the regex behaviour:
            //
            // * = 0+ characters (i.e. ".*?" in regex-speak)
            // ? = 1 character (i.e. "." in regex-speak)

            // Put '\' first so that it doesn't affect the subsequent special cases when we process it.
            var specialCases = new[] { @"\", ".", "$", "^", "{", "[", "(", "|", ")", "+" };

            pattern = specialCases.Aggregate(pattern, (current, t) => current.Replace(t, @"\" + t));

            pattern = pattern.Replace('?', '.');
            pattern = pattern.Replace("*", ".*?");

            return s.Like(new Regex(pattern, RegexOptions.IgnoreCase));
        }

    }
}
