using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    /// <summary>
    /// Provides constants for common data query functions.
    /// </summary>
    public class DataQueryFunction : IEquatable<DataQueryFunction> {

        private readonly int _hashCode;

        public string Name { get; }

        public string Description { get; }

        public bool IsNativeFuction { get; }


        public DataQueryFunction(string name, string description) : this(name, description, true) { }


        private DataQueryFunction(string name, string description, bool isNativeFunction) {
            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("You must specify a data function name.", nameof(name));
            }

            Name = name.Trim();
            _hashCode = Name.GetHashCode();
            Description = description;
            IsNativeFuction = isNativeFunction;
        }


        /// <summary>
        /// Interpolated data.
        /// </summary>
        public const string Interpolated = "INTERP";

        /// <summary>
        /// Visualization-friendly data.
        /// </summary>
        public const string Plot = "PLOT";

        /// <summary>
        /// Average value, calculated over a given sample interval.
        /// </summary>
        public const string Average = "AVG";

        /// <summary>
        /// Minimum value, calculated over a given sample interval.
        /// </summary>
        public const string Minimum = "MIN";

        /// <summary>
        /// Maximum value, calculated over a given sample interval.
        /// </summary>
        public const string Maximum = "MAX";

        /// <summary>
        /// Default data function definitions.
        /// </summary>
        internal static readonly IEnumerable<DataQueryFunction> DefaultFunctions = new[] {
            new DataQueryFunction(Interpolated, "Calculates interpolated values at a given sample interval", false),
            new DataQueryFunction(Plot, "Visualization-friendly data", false),
            new DataQueryFunction(Average, "Calculates the average value of a tag at a given sample interval", false),
            new DataQueryFunction(Minimum, "Calculates the minimum value of a tag at a given sample interval", false),
            new DataQueryFunction(Maximum, "Calculates the maximum value of a tag at a given sample interval", false)
        };

        public override int GetHashCode() {
            return _hashCode;
        }

        public override bool Equals(object obj) {
            return Equals(obj as DataQueryFunction);
        }

        public bool Equals(DataQueryFunction other) {
            if (other == null) {
                return false;
            }
            return String.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
