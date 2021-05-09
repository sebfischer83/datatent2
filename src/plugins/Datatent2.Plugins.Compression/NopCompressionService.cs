// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Datatent2.Contracts;
using Prise.Plugin;

namespace Datatent2.Plugins.Compression
{
    [Plugin(PluginType = typeof(ICompressionService))]
    public class NopCompressionService : ICompressionService
    {
        public Guid Id => new("B7647CEF-6338-477B-B514-9A48B1E2205A");

        public string Name => "NopCompression";

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