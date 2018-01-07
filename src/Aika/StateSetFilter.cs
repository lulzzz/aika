using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    /// <summary>
    /// Describes a search filter for state sets.
    /// </summary>
    public class StateSetFilter {

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
        /// Gets or sets the state set name filter.
        /// </summary>
        public string Filter { get; set; }


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

    }
}
