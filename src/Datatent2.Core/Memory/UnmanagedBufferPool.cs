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
    internal unsafe class UnmanagedBufferPool : BufferPoolBase
    {
        private static readonly Lazy<UnmanagedBufferPool>
            Lazy =
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

        public new static UnmanagedBufferPool Shared => Lazy.Value;

        public static Func<(DatatentSettings?, ILogger)> InitFunction { get; set; } = () => new(null, NullLogger.Instance);

        private ILogger _logger = NullLogger.Instance;
        private readonly IntPtr _memoryPtr;
        private readonly Memory<byte> _memory;
        private readonly UnmanagedMemoryManager<byte> _memoryManager;
        private readonly Queue<int> _freeSlots = new Queue<int>();
        private readonly DatatentSettings? _datatentSettings;

        public ILogger Logger
        {
            set => _logger = value;
        }

        public IntPtr GetPointerToSlot(int key)
        {
            return IntPtr.Add(_memoryPtr, Constants.PAGE_SIZE * (key - 1));
        }

        public UnmanagedBufferPool(DatatentSettings? datatentSettings, ILogger logger)
        {
            Logger = logger;
            _datatentSettings = datatentSettings;
            _memoryPtr = Marshal.AllocHGlobal(MaxBufferSize);
            _memoryManager = new UnmanagedMemoryManager<byte>((byte*)_memoryPtr, MaxBufferSize);
            _memory = _memoryManager.Memory;
            foreach (var i in Enumerable.Range(1, MaxBufferSize / Constants.PAGE_SIZE))
            {
                _freeSlots.Enqueue(i);
            }
        }

        public bool HasFreeSlots()
        {
            return _freeSlots.Count > 0;
        }

        public override void Return(IBufferSegment segment)
        {
            segment.Clear();
            _freeSlots.Enqueue(((UnmanagedBufferSegment)segment).Key);
        }

        protected override void Dispose(bool disposing)
        {
            _freeSlots.Clear();
            Marshal.FreeHGlobal(_memoryPtr);
        }

        public override IBufferSegment Rent(int minBufferSize = -1)
        {
            var freeKey = _freeSlots.Dequeue();
            return new UnmanagedBufferSegment(_memory.Slice(Constants.PAGE_SIZE * (freeKey - 1), Constants.PAGE_SIZE),
                freeKey, this);
        }

        private IBufferSegment RentCore() => this.Rent();

        public sealed override int MaxBufferSize =>
            (Constants.PAGE_SIZE * _datatentSettings!.EngineSettings.MaxPageCacheSize) + (Constants.PAGE_SIZE * _datatentSettings.IOSettings.MaxPageReadAheadCacheSize) + (Constants.PAGE_SIZE * 100);
    }
}
