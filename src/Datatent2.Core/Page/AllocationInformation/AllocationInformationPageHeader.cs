// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Runtime.InteropServices;
using Datatent2.Contracts;
using Dawn;

namespace Datatent2.Core.Page.AllocationInformation
{
    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_SPECIFIC_HEADER_SIZE)]
    internal readonly struct AllocationInformationPageHeader
    {
      
        public static AllocationInformationPageHeader FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<AllocationInformationPageHeader>(span.Slice(Constants.PAGE_COMMON_HEADER_SIZE));
        }

        public static AllocationInformationPageHeader FromBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).Min(0);
            return FromBuffer(span.Slice(offset + Constants.PAGE_COMMON_HEADER_SIZE));
        }

        public void ToBuffer(Span<byte> span)
        {
            Guard.Argument(span.Length).Min(Constants.PAGE_SPECIFIC_HEADER_SIZE);
            AllocationInformationPageHeader a = this;
            MemoryMarshal.Write(span.Slice(Constants.PAGE_COMMON_HEADER_SIZE), ref a);
        }

        public void ToBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).Min(0);
            ToBuffer(span.Slice(offset + Constants.PAGE_COMMON_HEADER_SIZE));
        }
    }
}