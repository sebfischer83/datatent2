using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Block;
using Datatent2.Core.Index;
using Datatent2.Core.Index.Heap;
using Datatent2.Core.Memory;
using Datatent2.Core.Services.Index;
using Datatent2.Core.Services.Index.Heap;
using Dawn;

namespace Datatent2.Core.Page.Index
{
    /// <summary>
    /// Holds data from an index
    /// </summary>
    internal class IndexPage : BasePage
    {
        /// <summary>
        /// Is this the first page of the index?
        /// </summary>
        public bool IsStartPage => Header.PrevPageId == uint.MaxValue;

        public override ushort FreeContinuousBytes
        {
            get
            {
                if (IndexPageHeader.Type == IndexType.SkipList)
                {
                    return base.FreeContinuousBytes;
                }
                return (ushort)((Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE) - Header.UsedBytes);
            }
        }

        public IndexPageHeader IndexPageHeader { get; private set; }

        public IndexPage(IBufferSegment buffer) : base(buffer)
        {
            Guard.Argument(Header.Type == PageType.Index).True();
            IndexPageHeader = IndexPageHeader.FromBuffer(Buffer.Span[Constants.PAGE_COMMON_HEADER_SIZE..]);
        }

        public IndexPage(IBufferSegment buffer, uint id) : base(buffer, id, PageType.Index)
        {

        }

        /// <summary>
        /// Adds a value to a HeapIndexService
        /// </summary>
        /// <param name="heapKey"></param>
        public bool AddHeapIndexKey(in HeapKey heapKey)
        {
            if (IndexPageHeader.Type != IndexType.Heap)
                throw new InvalidPageException($"Access index of type {Enum.GetName(typeof(IndexType), IndexPageHeader.Type)} with heap index methods is forbidden!", Header.PageId);

            var usedBytes = (ushort)(Header.UsedBytes + heapKey.Length);
            var nextFreePosition = (ushort)(Header.NextFreePosition + heapKey.Length);

            if (usedBytes > FreeContinuousBytes)
                return false;

            heapKey.Write(Buffer.Span, Header.NextFreePosition);

            Header = new PageHeader(Header.PageId, Header.Type, Header.PrevPageId, Header.NextPageId,
                usedBytes, Header.ItemCount,
                nextFreePosition, Header.UnalignedFreeBytes, Header.HighestSlotId);
            IndexPageHeader = new IndexPageHeader(IndexPageHeader.Type, (ushort)(IndexPageHeader.NodesCount + 1));
            IsDirty = true;

            return true;
        }

        /// <summary>
        /// Search if a key exists in this index page, when true returns the <see cref="PageAddress"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key to search</param>
        /// <param name="singleResult">Return the first result that is found.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private PageAddress[] SearchHeapIndexKeyInternal<T>(T key, bool singleResult)
        {
            List<PageAddress>? pageAddresses = null;

            // don't waste memory for creating a list that will never be used
            if (!singleResult)
                pageAddresses = new();
            int offset = 0;
            var span = Buffer.Span[Constants.PAGE_HEADER_SIZE..];
            for (int i = 0; i < IndexPageHeader.NodesCount; i++)
            {
                var foundKey = HeapKey.Read(span, offset);
                if (foundKey.Type == HeapKeyType.Empty)
                    break;

                switch (key)
                {
                    case string s:
                        if (s == foundKey.StringValue)
                        {
                            if (singleResult)
                                return new[] { foundKey.PageAddress };
                            pageAddresses!.Add(foundKey.PageAddress);
                        }
                        break;
                    case sbyte sb:
                        if (sb == foundKey.NumericalValue)
                        {
                            if (singleResult)
                                return new[] { foundKey.PageAddress };
                            pageAddresses!.Add(foundKey.PageAddress);
                        }
                        break;
                    case byte b:
                        if (b == foundKey.UnsignedNumericalValue)
                        {
                            if (singleResult)
                                return new[] { foundKey.PageAddress };
                            pageAddresses!.Add(foundKey.PageAddress);
                        }
                        break;
                    case short sh:
                        if (sh == foundKey.NumericalValue)
                        {
                            if (singleResult)
                                return new[] { foundKey.PageAddress };
                            pageAddresses!.Add(foundKey.PageAddress);
                        }
                        break;
                    case int it:
                        if (it == foundKey.NumericalValue)
                        {
                            if (singleResult)
                                return new[] { foundKey.PageAddress };
                            pageAddresses!.Add(foundKey.PageAddress);
                        }
                        break;
                    case long l:
                        if (l == foundKey.NumericalValue)
                        {
                            if (singleResult)
                                return new[] { foundKey.PageAddress };
                            pageAddresses!.Add(foundKey.PageAddress);
                        }
                        break;
                    case ushort us:
                        if (us == foundKey.UnsignedNumericalValue)
                        {
                            if (singleResult)
                                return new[] { foundKey.PageAddress };
                            pageAddresses!.Add(foundKey.PageAddress);
                        }
                        break;
                    case uint ui:
                        if (ui == foundKey.UnsignedNumericalValue)
                        {
                            if (singleResult)
                                return new[] { foundKey.PageAddress };
                            pageAddresses!.Add(foundKey.PageAddress);
                        }
                        break;
                    case ulong:
                        if ((ulong)System.Convert.ChangeType(key, TypeCode.UInt64) == foundKey.UnsignedNumericalValue)
                        {
                            if (singleResult)
                                return new[] { foundKey.PageAddress };
                            pageAddresses!.Add(foundKey.PageAddress);
                        }
                        break;
                    case Guid g:
                        if (g == foundKey.GuidValue)
                        {
                            if (singleResult)
                                return new[] { foundKey.PageAddress };
                            pageAddresses!.Add(foundKey.PageAddress);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(key));
                }

                offset += foundKey.Length;
            }

            if (pageAddresses == null)
                return Array.Empty<PageAddress>();

            return pageAddresses!.ToArray();
        }

        public PageAddress[] SearchHeapIndexKeyMany<T>(T key)
        {
            if (IndexPageHeader.Type != IndexType.Heap)
                throw new InvalidEngineStateException($"Only {nameof(IndexType.Heap)} index is supported but method is called on {nameof(IndexPageHeader.Type)} at page {PageHeader.PageId}.");

            return SearchHeapIndexKeyInternal(key, false);
        }

        public PageAddress? SearchHeapIndexKey<T>(T key)
        {
            if (IndexPageHeader.Type != IndexType.Heap && IndexPageHeader.Type != IndexType.HeapUnique)
                throw new InvalidPageException($"Access index of type {Enum.GetName(typeof(IndexType), IndexPageHeader.Type)} with heap index methods is forbidden!", Header.PageId);

            var result = SearchHeapIndexKeyInternal(key, true);
            return result.Length > 0 ? result[0] : null;
        }

        public void InitHeader(IndexType indexType)
        {
            IndexPageHeader = new IndexPageHeader(indexType);
        }

        internal override void SaveHeader()
        {
            base.SaveHeader();
            IndexPageHeader.ToBuffer(Buffer.Span[Constants.PAGE_COMMON_HEADER_SIZE..]);
        }

        public IndexBlock InsertBlock(ushort length)
        {
            if (IndexPageHeader.Type == IndexType.Undefined)
                throw new InvalidEngineStateException($"An index needs to be initialized before use!");

            var span = Insert((ushort)(length + Constants.BLOCK_HEADER_SIZE), out var index);

            return new IndexBlock(this, index, PageAddress.Empty, false);
        }

        public void UpdateBlock(Span<byte> bytes, PageAddress pageAddress)
        {
            if (IndexPageHeader.Type == IndexType.Undefined)
                throw new InvalidEngineStateException($"An index needs to be initialized before use!");

            try
            {
                var block = new IndexBlock(this, pageAddress.SlotId);
                block.FillData(bytes);
                IsDirty = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
