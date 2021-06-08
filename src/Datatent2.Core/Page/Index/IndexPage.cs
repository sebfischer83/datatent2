using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Index;
using Datatent2.Core.Index.Heap;
using Datatent2.Core.Memory;
using Dawn;

namespace Datatent2.Core.Page.Index
{
    internal class IndexPage : BasePage
    {
        public bool IsStartPage => Header.PrevPageId == uint.MaxValue;

        public override ushort FreeContinuousBytes =>
            (ushort) ((Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE) - Header.UsedBytes);
        public IndexPageHeader IndexPageHeader { get; private set; }

        public IndexPage(IBufferSegment buffer) : base(buffer)
        {
            Guard.Argument(Header.Type == PageType.Index).True();
            IndexPageHeader = IndexPageHeader.FromBuffer(Buffer.Span[Constants.PAGE_COMMON_HEADER_SIZE..]);
        }

        public IndexPage(IBufferSegment buffer, uint id) : base(buffer, id, PageType.Index)
        {

        }

        public void AddHeapIndexKey(HeapKey heapKey)
        {
            if (IndexPageHeader.Type != IndexType.Heap)
                throw new InvalidPageException($"Access index of type {Enum.GetName(typeof(IndexType), IndexPageHeader.Type)} with heap index methods is forbidden!", Header.PageId);

            var usedBytes = (ushort) (Header.UsedBytes + heapKey.Length);
            var nextFreePosition = (ushort) (Header.NextFreePosition + heapKey.Length);

            heapKey.Write(Buffer.Span, Header.NextFreePosition);
            
            Header = new PageHeader(Header.PageId, Header.Type, Header.PrevPageId, Header.NextPageId,
                usedBytes, Header.ItemCount,
                nextFreePosition, Header.UnalignedFreeBytes, Header.HighestSlotId);
            IndexPageHeader = new IndexPageHeader(IndexPageHeader.Type, (ushort) (IndexPageHeader.NodesCount + 1));
            IsDirty = true;
        }

        public PageAddress? SearchHeapIndexKey<T>(T key)
        {
            if (IndexPageHeader.Type != IndexType.Heap)
                throw new InvalidPageException($"Access index of type {Enum.GetName(typeof(IndexType), IndexPageHeader.Type)} with heap index methods is forbidden!", Header.PageId);

            int offset = 0;
            var span = Buffer.Span[Constants.PAGE_HEADER_SIZE..];
            for (int i = 0; i < IndexPageHeader.NodesCount; i++)
            {
                var foundKey = HeapKey.Read(span, offset);
                if (foundKey == null)
                    break;

                switch (key)
                {
                    case string s:
                        if (s == foundKey.StringValue)
                            return foundKey.PageAddress;
                        break;
                    case sbyte:
                    case byte:
                    case short:
                    case int:
                    case long:
                        if (((long) (object) key) == foundKey.NumericalValue)
                            return foundKey.PageAddress;
                        break;
                    case ushort:
                    case uint:
                    case ulong:
                        if (((ulong)(object)key) == foundKey.UnsignedNumericalValue)
                            return foundKey.PageAddress;
                        break;
                    case Guid g:
                        if (g == foundKey.GuidValue)
                            return foundKey.PageAddress;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(key));
                }

                offset += foundKey.Length;
            }

            return null;
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
    }
}
