// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Datatent2.Contracts;
using Prise.Plugin;

namespace Datatent2.Plugins.Compression
{
    [Plugin(PluginType = typeof(ICompressionService))]
    public class BrotliCompressionService : ICompressionService
    {
        public Guid Id => new("EE2259AC-E951-497E-8C21-BF895E98D0D7");

        public string Name => "BrotliCompression";

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<byte> Compress(Span<byte> bytes, Span<byte> target)
        {
            var needMinLength = BrotliEncoder.GetMaxCompressedLength(bytes.Length);
            var success = BrotliEncoder.TryCompress(bytes, target, out var bytesWritten, 2, 10);
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
