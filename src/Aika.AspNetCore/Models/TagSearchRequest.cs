using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Aika;

namespace Aika.AspNetCore.Models {

    /// <summary>
    /// Describes a tag search.
    /// </summary>
    public class TagSearchRequest {

        /// <summary>
        /// Gets or sets the page size for the results.
        /// </summary>
        [Required]
        [Range(1, 100)]
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the results page to return.
        /// </summary>
        [Required]
        [Range(1, Int32.MaxValue)]
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the join type for the search filters (i.e. should filters be AND'd or OR'd together).
        /// </summary>
        [Required]
        public TagDefinitionFilterJoinType Type { get; set; }

        /// <summary>
        /// Gets or sets the tag name filter.
        /// </summary>
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the tag description filter.
        /// </summary>
        [MaxLength(100)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the tag units filter.
        /// </summary>
        [MaxLength(100)]
        public string Units { get; set; }


        /// <summary>
        /// Converts the <see cref="TagSearchRequest"/> into a <see cref="TagDefinitionFilter"/> used internally by Aika.
        /// </summary>
        /// <returns>
        /// The equivalent <see cref="TagDefinitionFilter"/>.
        /// </returns>
        internal TagDefinitionFilter ToTagDefinitionFilter() {
            var clauses = new List<TagDefinitionFilterClause>();

            if (!String.IsNullOrWhiteSpace(Name)) {
                clauses.Add(new TagDefinitionFilterClause() { Field = TagDefinitionFilterField.Name, Value = Name });
            }
            if (!String.IsNullOrWhiteSpace(Description)) {
                clauses.Add(new TagDefinitionFilterClause() { Field = TagDefinitionFilterField.Name, Value = Name });
            }
            if (!String.IsNullOrWhiteSpace(Units)) {
                clauses.Add(new TagDefinitionFilterClause() { Field = TagDefinitionFilterField.Name, Value = Name });
            }

            return new TagDefinitionFilter() {
                PageSize = PageSize,
                Page = Page,
                FilterType = Type,
                FilterClauses = clauses
            };
        }

    }
}
