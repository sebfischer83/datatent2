using System;
using System.Runtime.InteropServices;
using Dawn;
using static Datatent2.Core.Constants;

namespace Datatent2.Core.Page.AllocationInformation
{
    [StructLayout(LayoutKind.Explicit, Size = PAGE_SPECIFIC_HEADER_SIZE)]
    internal readonly struct AllocationInformationPageHeader
    {
      
        public static AllocationInformationPageHeader FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<AllocationInformationPageHeader>(span.Slice(PAGE_COMMON_HEADER_SIZE));
        }

        public static AllocationInformationPageHeader FromBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).Min(0);
            return FromBuffer(span.Slice(offset + PAGE_COMMON_HEADER_SIZE));
        }

        public void ToBuffer(Span<byte> span)
        {
            Guard.Argument(span.Length).Min(PAGE_SPECIFIC_HEADER_SIZE);
            AllocationInformationPageHeader a = this;
            MemoryMarshal.Write(span.Slice(PAGE_COMMON_HEADER_SIZE), ref a);
        }

        public void ToBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).Min(0);
            ToBuffer(span.Slice(offset + PAGE_COMMON_HEADER_SIZE));
        }
    }
}