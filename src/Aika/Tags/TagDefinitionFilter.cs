using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aika.Tags {
    /// <summary>
    /// Describes a tag search filter.
    /// </summary>
    public class TagDefinitionFilter {

        /// <summary>
        /// The default page size for search results.
        /// </summary>
        public const int DefaultPageSize = 10;

        /// <summary>
        /// The page size.
        /// </summary>
        private int _pageSize = DefaultPageSize;

        /// <summary>
        /// Gets or sets the page size to use for the search results.
        /// </summary>
        public int PageSize {
            get { return _pageSize; }
            set { _pageSize = value < 1 ? 1 : value; }
        }

        /// <summary>
        /// The results page.
        /// </summary>
        private int _page = 1;

        /// <summary>
        /// Gets or sets the search results page to return.
        /// </summary>
        public int Page {
            get { return _page; }
            set { _page = value < 1 ? 1 : value; }
        }

        /// <summary>
        /// Gets or sets the filter type.
        /// </summary>
        public TagDefinitionFilterJoinType FilterType { get; set; }

        /// <summary>
        /// The filter clauses.
        /// </summary>
        private IEnumerable<TagDefinitionFilterClause> _filterClauses = new TagDefinitionFilterClause[0];

        /// <summary>
        /// Gets or sets the filter clauses.
        /// </summary>
        public IEnumerable<TagDefinitionFilterClause> FilterClauses {
            get { return _filterClauses; }
            set { _filterClauses = value ?? new TagDefinitionFilterClause[0]; }
        }


        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the <see cref="PageSize"/> for the 
        /// filter is greater than the specified limit.
        /// </summary>
        /// <param name="limit">The page size limit.</param>
        public void ThrowIfPageSizeGreaterThanLimit(int limit) {
            if (PageSize > limit) {
                throw new InvalidOperationException();
            }
        }


        /// <summary>
        /// Applies the filter to the specified list of tags and returns a page of matching results.
        /// </summary>
        /// <param name="tags">The full set of tags to match against.</param>
        /// <param name="predicate">An optional predicate that can be used to apply additional search constraints (e.g. based on authorization).</param>
        /// <returns>
        /// A page of matching tags.
        /// </returns>
        public IEnumerable<TagDefinition> GetMatchingTags(IEnumerable<TagDefinition> tags, Func<TagDefinition, bool> predicate) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }

            IEnumerable<TagDefinition> result = FilterType == TagDefinitionFilterJoinType.And 
                ? tags 
                : new TagDefinition[0];

            foreach (var clause in FilterClauses ?? new TagDefinitionFilterClause[0]) {
                if (String.IsNullOrWhiteSpace(clause.Value)) {
                    continue;
                }

                Func<TagDefinition, bool> pred = null;

                switch (clause.Field) {
                    case TagDefinitionFilterField.Name:
                        pred = tag => tag.Name.Like(clause.Value);
                        break;
                    case TagDefinitionFilterField.Description:
                        pred = tag => tag.Description.Like(clause.Value);
                        break;
                    case TagDefinitionFilterField.Units:
                        pred = tag => tag.Units.Like(clause.Value);
                        break;
                }

                if (pred == null) {
                    continue;
                }

                if (FilterType == TagDefinitionFilterJoinType.And) {
                    result = result.Where(pred);
                }
                else {
                    result = result.Concat(tags.Where(pred));
                }
            }

            if (FilterType == TagDefinitionFilterJoinType.Or) {
                result = result.Distinct();
            }

            if (predicate != null) {
                result = result.Where(predicate);
            }

            result = result.OrderBy(x => x.Name).Skip((Page - 1) * PageSize).Take(PageSize);

            return result.ToArray();
        }

    }


    /// <summary>
    /// Describes how tag definition filter clauses should be joined.
    /// </summary>
    public enum TagDefinitionFilterJoinType {
        /// <summary>
        /// Clauses should use logical AND.
        /// </summary>
        And,
        /// <summary>
        /// Clauses should use logical OR.
        /// </summary>
        Or
    }
}
