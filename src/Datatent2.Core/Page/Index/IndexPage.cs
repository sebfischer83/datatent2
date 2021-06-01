using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Index;
using Datatent2.Core.Memory;
using Dawn;

namespace Datatent2.Core.Page.Index
{
    internal class IndexPage : BasePage
    {
        public bool IsStartPage => Header.PrevPageId == uint.MaxValue;

        public IndexPageHeader IndexPageHeader { get; private set; }

        public IndexPage(IBufferSegment buffer) : base(buffer)
        {
            Guard.Argument(Header.Type == PageType.Index).True();
            IndexPageHeader = IndexPageHeader.FromBuffer(Buffer.Span[Constants.PAGE_COMMON_HEADER_SIZE..]);
        }

        public IndexPage(IBufferSegment buffer, uint id) : base(buffer, id, PageType.Index)
        {

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

    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_SPECIFIC_HEADER_SIZE)]
    internal readonly struct IndexPageHeader
    {
        [FieldOffset(TYPE)]
        public readonly IndexType Type;

        private const int TYPE = 0; // byte index type

        public IndexPageHeader(IndexType type)
        {
            Type = type;
        }

        public static IndexPageHeader FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<IndexPageHeader>(span);
        }

        public static IndexPageHeader FromBuffer(Span<byte> span, int offset)
        {
            return FromBuffer(span[offset..]);
        }

        public void ToBuffer(Span<byte> span)
        {
            IndexPageHeader a = this;
            MemoryMarshal.Write(span, ref a);
        }

        public void ToBuffer(Span<byte> span, int offset)
        {
            ToBuffer(span[offset..]);
        }
    }
}
