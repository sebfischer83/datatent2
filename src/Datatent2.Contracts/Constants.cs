// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Numerics;

namespace Datatent2.Contracts
{
    public static class Constants
    {
        /// <summary>
        /// Maximum size of each page. Maybe configurable in the future.
        /// </summary>
        public const int PAGE_SIZE = 8192;

        /// <summary>
        /// Maximum of 8000 pages in the cache, currently approx. 64mb
        /// </summary>
        public const int MAX_PAGE_CACHE_SIZE = 8000;

        /// <summary>
        /// How many pages the IO system will load to prefetch data access.
        /// </summary>
        public const int MAX_AMOUNT_OF_READ_AHEAD_PAGES = 8;

        /// <summary>
        /// The header of a page take this amount of bytes
        /// </summary>
        public const int PAGE_HEADER_SIZE = 64;

        public const int PAGE_COMMON_HEADER_SIZE = 32;

        public const int PAGE_SPECIFIC_HEADER_SIZE = 32;

        public const int PAGE_ADDRESS_SIZE = 8;

        public const int BLOCK_HEADER_SIZE = 10;

        public const int PAGE_DIRECTORY_ENTRY_SIZE = 4;

        public const int ALLOCATION_INFORMATION_ENTRY_SIZE = 8;

        // ReSharper disable once InconsistentNaming
        public static readonly BigInteger MAX_DATABASE_SIZE = new BigInteger(PAGE_SIZE) * uint.MaxValue;

        /// <summary>
        /// The amount of bytes that can be used without the header and minimum of one block header and footer
        /// </summary>
        public const int MAX_USABLE_BYTES_IN_PAGE = PAGE_SIZE - PAGE_HEADER_SIZE - PAGE_DIRECTORY_ENTRY_SIZE;

        public const int VERSION = 1;

        /// <summary>
        /// Document can be maximum take 2000 pages
        /// </summary>
        public const int MAX_DOCUMENT_SIZE = 2000 * MAX_USABLE_BYTES_IN_PAGE;

        public const BufferPoolImplementation BUFFER_POOL_IMPLEMENTATION = BufferPoolImplementation.Unmanaged;

        public static Guid NopCompressionPluginId => new("B7647CEF-6338-477B-B514-9A48B1E2205A");
    }
}