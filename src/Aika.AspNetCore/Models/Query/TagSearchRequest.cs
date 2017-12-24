using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Aika;

namespace Aika.AspNetCore.Models.Query {
    public class TagSearchRequest {

        [Required]
        [Range(1, 100)]
        public int PageSize { get; set; }

        [Required]
        [Range(1, Int32.MaxValue)]
        public int Page { get; set; }

        [Required]
        public TagDefinitionFilterJoinType Type { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string Description { get; set; }

        [MaxLength(100)]
        public string Units { get; set; }


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
