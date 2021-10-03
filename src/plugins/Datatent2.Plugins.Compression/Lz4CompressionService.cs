// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using Datatent2.Contracts;
using K4os.Compression.LZ4;
using Microsoft.VisualBasic;
using Prise.Plugin;
using Constants = Datatent2.Contracts.Constants;

namespace Datatent2.Plugins.Compression
{
    /// <summary>
    /// A lz4 based compression service
    /// </summary>
    [Plugin(PluginType = typeof(ICompressionService))]
    public class Lz4CompressionService : ICompressionService
    {
        private SpinLock _spinLock = new SpinLock();

        private readonly byte[] _tempBytes = new byte[Constants.PAGE_SIZE + 500];

        /// <inheritdoc />
        public Guid Id => new("E37967E9-BE8D-4E35-A343-35980DF75C7D");

        /// <inheritdoc />
        public string Name => "Lz4Compression";

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<byte> Compress(Span<byte> bytes, Span<byte> target)
        {
            var result = LZ4Pickler.Pickle(bytes);
            Unsafe.CopyBlock(ref target[0], ref result[0], (uint)result.Length);
            return target.Slice(0, result.Length);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<byte> Compress(Span<byte> bytes)
        {
            bool taken = false;
            _spinLock.Enter(ref taken);
            try
            {
                return Compress(bytes, _tempBytes);
            }
            finally
            {
                _spinLock.Exit();
            }

        }
    }
}
