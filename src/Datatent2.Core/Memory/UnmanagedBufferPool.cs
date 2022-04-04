// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Datatent2.Core.Memory
{
    /// <summary>
    /// The unmanaged memory implementation of a buffer pool
    /// </summary>
    internal unsafe class UnmanagedBufferPool : BufferPoolBase
    {
        /// <summary>
        /// The buffer pool
        /// </summary>
        private static readonly Lazy<UnmanagedBufferPool>
            LAZY =
                new Lazy<UnmanagedBufferPool>
                    (() =>
                    {
                        if (InitFunction == null)
                        {
                            throw new InvalidEngineStateException($"{nameof(InitFunction)} can't be null!");
                        }

                        var vals = InitFunction();
                        return new UnmanagedBufferPool(vals.Item1, vals.Item2);
                    });

        /// <summary>
        /// The shared instance
        /// </summary>
        public new static UnmanagedBufferPool Shared => LAZY.Value;

        /// <summary>
        /// The init function to setup the pool
        /// </summary>
        public static Func<(DatatentSettings?, ILogger)> InitFunction { get; set; } = () => new(null, NullLogger.Instance);

        private ILogger _logger = NullLogger.Instance;
        private readonly IntPtr _memoryPtr;
        private readonly Memory<byte> _memory;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly UnmanagedMemoryManager<byte> _memoryManager;
        /// <summary>
        /// The list of free slots available for renting
        /// </summary>
        private readonly Queue<int> _freeSlots = new Queue<int>();
        private readonly DatatentSettings? _datatentSettings;

        /// <summary>
        /// The logger instance
        /// </summary>
        public ILogger Logger
        {
            set => _logger = value;
        }

        /// <summary>
        /// Gets the pointer to a slot in the unmanaged memory.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>An IntPtr.</returns>
        public IntPtr GetPointerToSlot(int key)
        {
            return IntPtr.Add(_memoryPtr, Constants.PAGE_SIZE * (key - 1));
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="datatentSettings"></param>
        /// <param name="logger"></param>
        public UnmanagedBufferPool(DatatentSettings? datatentSettings, ILogger logger)
        {
            Logger = logger;
            _datatentSettings = datatentSettings;
            // alloc the memory
            // TODO: in .net 6 replace with NativeMemory.Alloc
            _memoryPtr = Marshal.AllocHGlobal(MaxBufferSize);
            _memoryManager = new UnmanagedMemoryManager<byte>((byte*)_memoryPtr, MaxBufferSize);
            _memory = _memoryManager.Memory;
            // save all available page buffers for renting
            foreach (var i in Enumerable.Range(1, MaxBufferSize / Constants.PAGE_SIZE))
            {
                _freeSlots.Enqueue(i);
            }
        }

        /// <summary>
        /// Free slots available
        /// </summary>
        /// <returns></returns>
        public bool HasFreeSlots()
        {
            return _freeSlots.Count > 0;
        }

        /// <summary>
        /// Return a buffer to the pool
        /// </summary>
        /// <param name="segment"></param>
        public override void Return(IBufferSegment segment)
        {
            segment.Clear();
            _freeSlots.Enqueue(((UnmanagedBufferSegment)segment).Key);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            _freeSlots.Clear();
            // free the unmanaged memory
            Marshal.FreeHGlobal(_memoryPtr);
        }

        /// <inheritdoc />
        public override IBufferSegment Rent(int minBufferSize = -1)
        {
            var freeKey = _freeSlots.Dequeue();
            return new UnmanagedBufferSegment(_memory.Slice(Constants.PAGE_SIZE * (freeKey - 1), Constants.PAGE_SIZE),
                freeKey, this);
        }

        /// <summary>
        /// Rent
        /// </summary>
        /// <returns></returns>
        private IBufferSegment RentCore() => this.Rent();

        /// <summary>
        /// The maximum size of the buffer pool for renting
        /// </summary>
        public sealed override int MaxBufferSize =>
            (Constants.PAGE_SIZE * _datatentSettings!.Engine.MaxPageCacheSize) + (Constants.PAGE_SIZE * _datatentSettings.IO.MaxPageReadAheadCacheSize) + (Constants.PAGE_SIZE * 100);
    }
}
