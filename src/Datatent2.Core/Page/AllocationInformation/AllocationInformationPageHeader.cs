// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Runtime.InteropServices;
using Datatent2.Contracts;
using Dawn;

namespace Datatent2.Core.Page.AllocationInformation
{
    /// <summary>
    /// The header of an AIM page
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_SPECIFIC_HEADER_SIZE)]
    internal readonly struct AllocationInformationPageHeader
    {
      
        /// <summary>
        /// Retrieve from buffer
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static AllocationInformationPageHeader FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<AllocationInformationPageHeader>(span.Slice(Constants.PAGE_COMMON_HEADER_SIZE));
        }

        /// <summary>
        /// Retrieve from buffer
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static AllocationInformationPageHeader FromBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).Min(0);
            return FromBuffer(span.Slice(offset + Constants.PAGE_COMMON_HEADER_SIZE));
        }

        /// <summary>
        /// To buffer
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public void ToBuffer(Span<byte> span)
        {
            Guard.Argument(span.Length).Min(Constants.PAGE_SPECIFIC_HEADER_SIZE);
            AllocationInformationPageHeader a = this;
            MemoryMarshal.Write(span.Slice(Constants.PAGE_COMMON_HEADER_SIZE), ref a);
        }

        /// <summary>
        /// To buffer
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public void ToBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).Min(0);
            ToBuffer(span.Slice(offset + Constants.PAGE_COMMON_HEADER_SIZE));
        }
    }
}