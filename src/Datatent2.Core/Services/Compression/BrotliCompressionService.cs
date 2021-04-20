// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Dawn;

namespace Datatent2.Core.Services.Compression
{
    internal class BrotliCompressionService : ICompressionService
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<byte> Compress(Span<byte> bytes, Span<byte> target)
        {
            var needMinLength = BrotliEncoder.GetMaxCompressedLength(bytes.Length);
            Guard.Argument(target.Length).GreaterThan(needMinLength);

            var success = BrotliEncoder.TryCompress(bytes, target, out var bytesWritten, 2, 10);
            Guard.Argument(success).Require(true);

            return target.Slice(0, bytesWritten);
        }

        /// <summary>
        /// Return an array from the ArrayPool
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public byte[] Compress(Span<byte> bytes)
        {
            var target = ArrayPool<byte>.Shared.Rent(BrotliEncoder.GetMaxCompressedLength(bytes.Length));
            Compress(bytes, target);

            return target;
        }
    }
}
