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
    /// <summary>
    /// A nop compression service
    /// </summary>
    [Plugin(PluginType = typeof(ICompressionService))]
    public class NopCompressionService : ICompressionService
    {
        /// <inheritdoc />
        public Guid Id => new("B7647CEF-6338-477B-B514-9A48B1E2205A");

        /// <inheritdoc />
        public string Name => "NopCompression";

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<byte> Compress(Span<byte> bytes, Span<byte> target)
        {
            return bytes;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<byte> Compress(Span<byte> bytes)
        {
            return Compress(bytes, Array.Empty<byte>());
        }
    }
}