using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
#pragma warning disable CS0419 // Ambiguous reference in cref attribute
    /// <summary>
    /// Describes a data query function that can be using in calls to <see cref="AikaHistorian.ReadProcessedData"/>.
    /// </summary>
    public class DataQueryFunction : IEquatable<DataQueryFunction> {
#pragma warning restore CS0419 // Ambiguous reference in cref attribute

        /// <summary>
        /// The hash code for the function.
        /// </summary>
        private readonly int _hashCode;

        /// <summary>
        /// Gets the function name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the functon description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets a flag that specifies if the data function is natively supported by the 
        /// <see cref="IHistorian"/> being used by Aika.
        /// </summary>
        public bool IsNativeFuction { get; }


        /// <summary>
        /// Creates a new <see cref="DataQueryFunction"/> object.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="description">The function description.</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or white space.</exception>
        public DataQueryFunction(string name, string description) : this(name, description, true) { }


        /// <summary>
        /// Creates a new <see cref="DataQueryFunction"/> object.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="description">The function description.</param>
        /// <param name="isNativeFunction">Flags if the data function is natively supported or not.</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or white space.</exception>
        private DataQueryFunction(string name, string description, bool isNativeFunction) {
            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException(Resources.Error_DataFunctionNameIsRequired, nameof(name));
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


        /// <summary>
        /// Gets the hash code for the object.
        /// </summary>
        /// <returns>
        /// The hash code.
        /// </returns>
        public override int GetHashCode() {
            return _hashCode;
        }


        /// <summary>
        /// Tests if this object is equivalent to another object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>
        /// <see langword="true"/> if the objects are equivalent, otherwise <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj) {
            return Equals(obj as DataQueryFunction);
        }


        /// <summary>
        /// Tests if this object is equivalent to another object.
        /// </summary>
        /// <param name="other">The object to compare with.</param>
        /// <returns>
        /// <see langword="true"/> if the objects are equivalent, otherwise <see langword="false"/>.
        /// </returns>
        public bool Equals(DataQueryFunction other) {
            if (other == null) {
                return false;
            }
            return String.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
