// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using Datatent2.Core.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Datatent2.Core.IO
{
    internal sealed class DiskPageCache
    {
        private readonly DatatentSettings _datatentSettings;
        private readonly ILogger _logger;
        private readonly Dictionary<uint, IBufferSegment> _cache;
        private SpinLock _spinLock;

        public DiskPageCache(DatatentSettings datatentSettings, ILogger logger)
        {
            _spinLock = new SpinLock();
            _datatentSettings = datatentSettings;
            _logger = logger;
            _cache = new Dictionary<uint, IBufferSegment>(_datatentSettings.IOSettings.MaxPageReadAheadCacheSize, new Comparer());
        }

        public IBufferSegment? GetIfExists(uint pageId)
        {
            IBufferSegment? segment = null;
            _cache.TryGetValue(pageId, out segment);

            return segment;
        }

        public void Add(uint pageId, IBufferSegment segment)
        {
            bool taken = false;
            _spinLock.Enter(ref taken);
            try
            {
                if (!_cache.ContainsKey(pageId))
                {
                    if (_cache.Count == _datatentSettings.IOSettings.MaxPageReadAheadCacheSize)
                    {
                        Vacuum();
                    }
                    _cache.Add(pageId, segment);
                }
            }
            finally
            {
                _spinLock.Exit();
            }
        }

        [SuppressMessage("Security", "SCS0005:Weak random number generator.", Justification = "doesnt care here")]
        private void Vacuum()
        {
            // free 1/8 of all pages
            var numberToFree = _datatentSettings.EngineSettings.MaxPageCacheSize / 8;
            _logger.LogInformation($"Vacuum needed in {nameof(DiskPageCache)} remove {numberToFree} items from cache.");

            var random = new Random();
            
            var toRemove = _cache.Keys.OrderBy(a => random.NextDouble()).Take(numberToFree).ToList();
            for (int i = 0; i < toRemove.Count; i++)
            {
                var buffer = _cache[toRemove[i]];
                _cache.Remove(toRemove[i]);
                buffer.Dispose();
            }
            _logger.LogInformation($"Vacuum done. {_cache.Count} items remains in cache.");
        }

        public void Remove(uint pageId, bool free = false)
        {
            bool taken = false;
            _spinLock.Enter(ref taken);
            try
            {
                if (_cache.ContainsKey(pageId))
                {
                    var buffer = _cache[pageId];
                    _cache.Remove(pageId);
                    if (free)
                        buffer.Dispose();
                }
            }
            finally
            {
                _spinLock.Exit();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool Contains(uint pageId)
        {
            return _cache.ContainsKey(pageId);
        }

        private class Comparer : IEqualityComparer<uint>
        {
            public bool Equals(uint x, uint y)
            {
                return x == y;
            }

            public int GetHashCode([DisallowNull] uint obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
