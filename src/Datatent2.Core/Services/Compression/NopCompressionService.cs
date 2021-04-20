// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Services.Compression
{
    public class NopCompressionService : ICompressionService
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<byte> Compress(Span<byte> bytes, Span<byte> target)
        {
            Unsafe.CopyBlock(ref target[0], ref bytes[0], (uint)bytes.Length);
            target = target.Slice(0, bytes.Length);
            return target.Slice(0, bytes.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public byte[] Compress(Span<byte> bytes)
        {
            var target = ArrayPool<byte>.Shared.Rent(Constants.PAGE_SIZE + 500);
            Compress(bytes, target);

            return target;
        }
    }
}
