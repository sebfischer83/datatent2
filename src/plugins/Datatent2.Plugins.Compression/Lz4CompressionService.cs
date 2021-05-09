// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Datatent2.Contracts;
using K4os.Compression.LZ4;
using Microsoft.VisualBasic;
using Prise.Plugin;
using Constants = Datatent2.Contracts.Constants;

namespace Datatent2.Plugins.Compression
{
    [Plugin(PluginType = typeof(ICompressionService))]
    public class Lz4CompressionService : ICompressionService
    {
        public Guid Id => new("E37967E9-BE8D-4E35-A343-35980DF75C7D");

        public string Name => "Lz4Compression";

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<byte> Compress(Span<byte> bytes, Span<byte> target)
        {
            var result = LZ4Pickler.Pickle(bytes);
            Unsafe.CopyBlock(ref target[0], ref result[0], (uint)result.Length);
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
