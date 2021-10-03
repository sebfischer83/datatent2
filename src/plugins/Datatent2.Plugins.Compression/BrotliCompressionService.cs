// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Prise.Plugin;

namespace Datatent2.Plugins.Compression
{
    /// <summary>
    /// A brotli compression service
    /// </summary>
    [Plugin(PluginType = typeof(ICompressionService))]
    public class BrotliCompressionService : ICompressionService
    {
        /// <inheritdoc />
        public Guid Id => new("EE2259AC-E951-497E-8C21-BF895E98D0D7");

        /// <inheritdoc />
        public string Name => "BrotliCompression";

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<byte> Compress(Span<byte> bytes, Span<byte> target)
        {
            var needMinLength = BrotliEncoder.GetMaxCompressedLength(bytes.Length);
            if (needMinLength > target.Length)
                throw new ArgumentOutOfRangeException(nameof(target), $"The {nameof(target)} must be minimum {needMinLength} bytes");
            var success = BrotliEncoder.TryCompress(bytes, target, out var bytesWritten, 2, 10);
            if (!success)
                throw new InvalidEngineStateException($"The compression was not succesfull");
            
            return target.Slice(0, bytesWritten);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<byte> Compress(Span<byte> bytes)
        {
            var target = new byte[BrotliEncoder.GetMaxCompressedLength(bytes.Length)];
            return Compress(bytes, target);
        }
    }
}
