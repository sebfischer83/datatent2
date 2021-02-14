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
        /// Maximum size of each page in a table
        /// </summary>
        public const int PAGE_SIZE = 8192;

        /// <summary>
        /// The header of a page take this amount of bytes
        /// </summary>
        public const int PAGE_HEADER_SIZE = 64;

        public const int PAGE_ADDRESS_SIZE = 8;

        public const int BLOCK_HEADER_SIZE = 12;

        /// <summary>
        /// The amount of bytes that can be used without the header
        /// </summary>
        public const int MAX_USABLE_BYTES_IN_PAGE = PAGE_SIZE - PAGE_HEADER_SIZE;

        public const int VERSION = 1;

        /// <summary>
        /// Document can be maximum take 2000 pages
        /// </summary>
        public const int MAX_DOCUMENT_SIZE = 2000 * MAX_USABLE_BYTES_IN_PAGE;
    }
}
