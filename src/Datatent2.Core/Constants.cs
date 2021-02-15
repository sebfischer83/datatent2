using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Datatent2.Core.Tests")]
[assembly: InternalsVisibleTo("Datatent2.CoreBench")]
namespace Datatent2.Core
{
    internal static class Constants
    {
        /// <summary>
        /// Maximum size of each page. Maybe configurable in the future.
        /// </summary>
        public const int PAGE_SIZE = 8192;

        /// <summary>
        /// How many pages the IO system will load to prefetch data access.
        /// </summary>
        public const int MAX_AMOUNT_OF_READ_AHEAD_PAGES = 8;

        /// <summary>
        /// The header of a page take this amount of bytes
        /// </summary>
        public const int PAGE_HEADER_SIZE = 64;

        public const int PAGE_ADDRESS_SIZE = 8;

        public const int BLOCK_HEADER_SIZE = 12;

        public const int BLOCK_FOOTER_SIZE = 8;

        /// <summary>
        /// The amount of bytes that can be used without the header and minimum of one block header and footer
        /// </summary>
        public const int MAX_USABLE_BYTES_IN_PAGE = PAGE_SIZE - PAGE_HEADER_SIZE - BLOCK_HEADER_SIZE - BLOCK_FOOTER_SIZE;

        public const int VERSION = 1;

        /// <summary>
        /// Document can be maximum take 2000 pages
        /// </summary>
        public const int MAX_DOCUMENT_SIZE = 2000 * MAX_USABLE_BYTES_IN_PAGE;
    }
}
