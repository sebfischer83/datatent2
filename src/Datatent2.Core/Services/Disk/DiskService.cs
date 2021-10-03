// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.IO;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Services.Disk
{
    /// <summary>
    /// Base implementation of a service that offers methods to read and write pages from the underlying io system
    /// </summary>
    internal abstract class DiskService : IDisposable
    {
        /// <summary>
        /// The stream which is used
        /// </summary>
        protected Stream Stream;
        /// <summary>
        /// Waiting line for read requests
        /// </summary>
        protected readonly Channel<ValueTuple<ReadRequest, TaskCompletionSource<ReadResponse>>> ReadChannel;
        /// <summary>
        /// Waiting line for write requests
        /// </summary>
        protected readonly Channel<ValueTuple<WriteRequest, TaskCompletionSource<WriteRespone>>> WriteChannel;
        /// <summary>
        /// The task that do the reads
        /// </summary>
        protected readonly Task ReadTask;
        /// <summary>
        /// The task that do the writes
        /// </summary>
        protected readonly Task WriteTask;
        /// <summary>
        /// The implementation for the read ahead cache
        /// </summary>
        protected IReadAheadPageCache DiskPageCache;
        private readonly int _cacheSize;

        /// <summary>
        /// Holds references to current pending read operations
        /// </summary>
        protected readonly ConcurrentDictionary<uint, TaskCompletionSource<ReadResponse>> ConcurrentDictionaryRead = new();
        /// <summary>
        /// Holds references to current pending write operations
        /// </summary>
        protected readonly ConcurrentDictionary<uint, TaskCompletionSource<WriteRespone>> ConcurrentDictionaryWrite = new();

        /// <summary>
        /// The database settings
        /// </summary>
        protected readonly DatatentSettings Settings;
        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;
        private byte[] _cacheBytes;

        /// <summary>
        /// Creates a new instance of the DiskService for the given settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static DiskService Create(DatatentSettings settings, ILogger logger)
        {
            if (settings.IOSettings.IOSystem == DatatentSettings.IOSystem.InMemory)
            {
                return new InMemoryDiskService(new DatatentSettings());
            }
            if (settings.IOSettings.IOSystem == DatatentSettings.IOSystem.FileStream)
            {
                return (FileDiskService)(new(settings));
            }
            if (settings.IOSettings.IOSystem == DatatentSettings.IOSystem.MemoryMappedFile)
            {
                return (MemoryMappedDiskService)(new(settings, logger));
            }

            throw new ArgumentException(nameof(settings.IOSettings.IOSystem));
        }

        /// <summary>
        /// Gets the current Stream, possible to override for specific changes
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        protected virtual Stream GetStream(uint pageId)
        {
            return Stream;
        }

        protected DiskService(DatatentSettings settings, ILogger logger)
        {
            DiskPageCache = new NullReadAheadPageCache();
            Stream = Stream.Null;
            Settings = settings;
            Logger = logger;
            if (Settings.IOSettings.UseReadAheadCache)
            {
                DiskPageCache = new DiskReadAheadPageCache(settings, logger);
            }
            _cacheSize = Constants.PAGE_SIZE * Constants.MAX_AMOUNT_OF_READ_AHEAD_PAGES;
            _cacheBytes = new byte[Settings.IOSettings.UseReadAheadCache ? _cacheSize : 1];
            ReadChannel = Channel.CreateBounded<ValueTuple<ReadRequest, TaskCompletionSource<ReadResponse>>>(
                new BoundedChannelOptions(100)
                {
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.Wait
                });

            WriteChannel = Channel.CreateBounded<ValueTuple<WriteRequest, TaskCompletionSource<WriteRespone>>>(
                new BoundedChannelOptions(100)
                {
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.Wait
                });
            ReadTask = new Task(Reader, CancellationToken.None, TaskCreationOptions.LongRunning);
            WriteTask = new Task(Writer, CancellationToken.None, TaskCreationOptions.LongRunning);
            ReadTask.Start();
            WriteTask.Start();
        }

        /// <summary>
        /// Take from the waiting line and forward to the IO system
        /// </summary>
        protected async void Reader()
        {
            var reader = ReadChannel.Reader;
            while (await reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                {
                    var segment = Settings.IOSettings.UseReadAheadCache ? ReadPageBufferReadAheadCache(item.Item1.PageId) : ReadPageBuffer(item.Item1.PageId);
                    ConcurrentDictionaryRead.TryRemove(item.Item1.PageId, out _);
                    item.Item2.SetResult(new ReadResponse(item.Item1.Id, segment, item.Item1.PageId));
                }
            }
        }


        protected async void Writer()
        {
            var reader = WriteChannel.Reader;
            while (await reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                {
                    WritePageBuffer(item.Item1.PageId, item.Item1.BufferSegment);
                    ConcurrentDictionaryWrite.TryRemove(item.Item1.PageId, out _);
                    item.Item2.SetResult(new WriteRespone(item.Item1.Id, true));
                }
            }
        }


        public async Task<ReadResponse> GetBuffer(ReadRequest readRequest)
        {
            var source = await LineUpReadRequestOrUseExisting(readRequest).ConfigureAwait(false);
            var response = await source.Task.ConfigureAwait(false);
            return response;
        }

        private async Task<TaskCompletionSource<ReadResponse>> LineUpReadRequestOrUseExisting(ReadRequest readRequest)
        {
            ConcurrentDictionaryRead.TryGetValue(readRequest.PageId, out var completionSource);
            if (completionSource != null)
            {
                return completionSource;
            }
            var writer = ReadChannel.Writer;

            while (await writer.WaitToWriteAsync().ConfigureAwait(false))
            {
                TaskCompletionSource<ReadResponse> source = new TaskCompletionSource<ReadResponse>();
                ValueTuple<ReadRequest, TaskCompletionSource<ReadResponse>> tuple = (readRequest, source);
                ConcurrentDictionaryRead.TryAdd(readRequest.PageId, source);
                await writer.WriteAsync(tuple).ConfigureAwait(false);
                return source;
            }

            throw new DiskIoRequestException("Can't line up request!", readRequest.PageId, DiskIoRequestException.IoDirection.Read);
        }

        public async Task<WriteRespone> WriteBuffer(WriteRequest writeRequest)
        {
            var source = await LineUpWriteRequestOrUseExisting(writeRequest).ConfigureAwait(false);
            var response = await source.Task.ConfigureAwait(false);
            return response;
        }

        private async Task<TaskCompletionSource<WriteRespone>> LineUpWriteRequestOrUseExisting(WriteRequest writeRequest)
        {
#if DEBUG
            Logger.LogDebug($"{nameof(LineUpWriteRequestOrUseExisting)} for page {writeRequest.PageId}");
#endif
            ConcurrentDictionaryWrite.TryGetValue(writeRequest.PageId, out var completionSource);
            if (completionSource != null)
            {
                return completionSource;
            }
            var writer = WriteChannel.Writer;

            while (await writer.WaitToWriteAsync().ConfigureAwait(false))
            {
                TaskCompletionSource<WriteRespone> source = new TaskCompletionSource<WriteRespone>();
                ValueTuple<WriteRequest, TaskCompletionSource<WriteRespone>> tuple = (writeRequest, source);
                ConcurrentDictionaryWrite.TryAdd(writeRequest.PageId, source);
                await writer.WriteAsync(tuple).ConfigureAwait(false);
                return source;
            }

            throw new DiskIoRequestException("Can't line up request!", writeRequest.PageId, DiskIoRequestException.IoDirection.Write);
        }

        protected virtual IBufferSegment ReadPageBuffer(uint pageId)
        {
            var bufferSegment = BufferPoolFactory.Get().Rent(Constants.PAGE_SIZE);
            var stream = GetStream(pageId);

            stream.Seek(BasePage.PageOffset(pageId), SeekOrigin.Begin);
            stream.Read(bufferSegment.Span);
            
            return bufferSegment;
        }

        protected virtual IBufferSegment ReadPageBufferReadAheadCache(uint pageId)
        {
            var cachedBuffer = DiskPageCache.GetIfExists(pageId);
            if (cachedBuffer != null)
            {
                DiskPageCache.Remove(pageId);
                return cachedBuffer;
            }

            var bufferSegment = BufferPoolFactory.Get().Rent(Constants.PAGE_SIZE);
            var tempSpan = (Span<byte>)_cacheBytes;
            var stream = GetStream(pageId);
            stream.Seek(BasePage.PageOffset(pageId), SeekOrigin.Begin);
            stream.Read(_cacheBytes, 0, _cacheSize);
            var span = bufferSegment.Span;
            span.WriteBytes(0, tempSpan.Slice(0, Constants.PAGE_SIZE));

            uint nextPageId = pageId + 1;
            for (int i = 1; i < Constants.MAX_AMOUNT_OF_READ_AHEAD_PAGES; i++)
            {
                if (!DiskPageCache.Contains(nextPageId))
                {
                    var bufferCacheSegment = BufferPoolFactory.Get().Rent(Constants.PAGE_SIZE);
                    var spanCache = bufferCacheSegment.Span;
                    spanCache.WriteBytes(0, tempSpan.Slice(i * Constants.PAGE_SIZE, Constants.PAGE_SIZE));
                    DiskPageCache.Add(nextPageId, bufferCacheSegment);
                }
                nextPageId++;
            }
         
            return bufferSegment;
        }

        protected virtual void WritePageBuffer(uint pageId, IBufferSegment bufferSegment)
        {
#if DEBUG
            Logger.LogDebug($"Write {pageId} to disk");
#endif
            var stream = GetStream(pageId);
            stream.Seek(BasePage.PageOffset(pageId), SeekOrigin.Begin);
            stream.Write(bufferSegment.Span);
        }

        public virtual void Dispose()
        {
            Stream.Dispose();
        }
    }

    internal readonly struct WriteRequest
    {
        public readonly Guid Id;

        public readonly uint PageId;

        public readonly IBufferSegment BufferSegment;

        public WriteRequest(IBufferSegment bufferSegment, uint pageId)
        {
            Id = Guid.NewGuid();
            BufferSegment = bufferSegment;
            PageId = pageId;
        }
    }

    internal readonly struct WriteRespone
    {
        public readonly Guid Id;

        public readonly bool Success;

        public WriteRespone(Guid id, bool success)
        {
            Success = success;
            Id = id;
        }
    }

    internal readonly struct ReadRequest
    {
        public readonly Guid Id;

        public readonly uint PageId;

        public ReadRequest(uint pageId)
        {
            Id = Guid.NewGuid();
            PageId = pageId;
        }
    }

    internal readonly struct ReadResponse
    {
        public readonly Guid Id;

        public readonly uint PageId;

        public readonly IBufferSegment BufferSegment;

        public ReadResponse(Guid id, IBufferSegment bufferSegment, uint pageId)
        {
            BufferSegment = bufferSegment;
            PageId = pageId;
            Id = id;
        }
    }
}
