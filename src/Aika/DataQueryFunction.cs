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


        public DataQueryFunction(string name, string description) {
            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("You must specify a data function name.", nameof(name));
            }

            Name = name.Trim();
            _hashCode = Name.GetHashCode();
            Description = description;
        }

        /// <summary>
        /// Raw, unaggregated data.
        /// </summary>
        public static readonly DataQueryFunction Raw = new DataQueryFunction("RAW", "Returns raw, unprocessed data");

        /// <summary>
        /// Interpolated data.
        /// </summary>
        public static readonly DataQueryFunction Interpolated = new DataQueryFunction("INTERP", "Calculates interpolated values at a given sample interval");

        /// <summary>
        /// Visualization-friendly data.
        /// </summary>
        public static readonly DataQueryFunction Plot = new DataQueryFunction("PLOT", "Visualization-friendly data");


        public static readonly DataQueryFunction Average = new DataQueryFunction("AVG", "Calculates the average value of a tag at a given sample interval");

        public static readonly DataQueryFunction Minimum = new DataQueryFunction("MIN", "Calculates the minimum value of a tag at a given sample interval");

        public static readonly DataQueryFunction Maximum = new DataQueryFunction("MAX", "Calculates the maximum value of a tag at a given sample interval");

        public static readonly IEnumerable<DataQueryFunction> DefaultFunctions = new[] {
            Raw,
            Interpolated,
            Plot,
            Average,
            Minimum,
            Maximum
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
