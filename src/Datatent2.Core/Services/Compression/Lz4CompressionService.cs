// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Dawn;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Encoders;

namespace Datatent2.Core.Services.Compression
{
    public class Lz4CompressionService : ICompressionService
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<byte> Compress(Span<byte> bytes, Span<byte> target)
        {
            var result = LZ4Pickler.Pickle(bytes);
            Guard.Argument(result.Length).LessThan(target.Length);
            Unsafe.CopyBlock(ref target[0], ref result[0], (uint) result.Length);
            return target.Slice(0, result.Length);
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
