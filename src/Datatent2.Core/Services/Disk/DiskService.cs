// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;

namespace Datatent2.Core.Services.Disk
{
    internal abstract class DiskService : IDisposable
    {
        protected readonly Stream Stream;
        protected readonly Channel<ValueTuple<ReadRequest, TaskCompletionSource<ReadResponse>>> ReadChannel;
        protected readonly Channel<ValueTuple<WriteRequest, TaskCompletionSource<WriteRespone>>> WriteChannel;
        protected readonly Task ReadTask;
        protected readonly Task WriteTask;

        // holds references to current pending read operations
        protected readonly ConcurrentDictionary<uint, TaskCompletionSource<ReadResponse>> ConcurrentDictionaryRead = new();
        // holds references to current pending write operations
        protected readonly ConcurrentDictionary<uint, TaskCompletionSource<WriteRespone>> ConcurrentDictionaryWrite = new();

        public static DiskService Create(DatatentSettings settings)
        {
            if (settings.InMemory)
            {
                return new InMemoryDiskService();
            }

            FileStream fileStream = new FileStream(settings.DatabasePath!, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.Read, Constants.PAGE_SIZE,
                FileOptions.RandomAccess);

            return (FileDiskService)(new(fileStream, settings));
        }

        protected DiskService(Stream stream)
        {
            Stream = stream;
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

        protected async void Reader()
        {
            var reader = ReadChannel.Reader;
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var item))
                {
                    var segment = ReadPageBuffer(item.Item1.PageId);
                    ConcurrentDictionaryRead.TryRemove(item.Item1.PageId, out _);
                    item.Item2.SetResult(new ReadResponse(item.Item1.Id, segment, item.Item1.PageId));
                }
            }
        }

        protected async void Writer()
        {
            var reader = WriteChannel.Reader;
            while (await reader.WaitToReadAsync())
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
            var source = await LineUpReadRequestOrUseExisting(readRequest);
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

            while (await writer.WaitToWriteAsync())
            {
                TaskCompletionSource<ReadResponse> source = new TaskCompletionSource<ReadResponse>();
                ValueTuple<ReadRequest, TaskCompletionSource<ReadResponse>> tuple = (readRequest, source);
                ConcurrentDictionaryRead.TryAdd(readRequest.PageId, source);
                await writer.WriteAsync(tuple);
                return source;
            }

            throw new DiskIoRequestException("Can't line up request!", readRequest.PageId, DiskIoRequestException.IoDirection.Read);
        }

        public async Task<WriteRespone> WriteBuffer(WriteRequest writeRequest)
        {
            var source = await LineUpWriteRequestOrUseExisting(writeRequest);
            var response = await source.Task.ConfigureAwait(false);
            return response;
        }

        private async Task<TaskCompletionSource<WriteRespone>> LineUpWriteRequestOrUseExisting(WriteRequest writeRequest)
        {
            ConcurrentDictionaryWrite.TryGetValue(writeRequest.PageId, out var completionSource);
            if (completionSource != null)
            {
                return completionSource;
            }
            var writer = WriteChannel.Writer;

            while (await writer.WaitToWriteAsync())
            {
                TaskCompletionSource<WriteRespone> source = new TaskCompletionSource<WriteRespone>();
                ValueTuple<WriteRequest, TaskCompletionSource<WriteRespone>> tuple = (writeRequest, source);
                ConcurrentDictionaryWrite.TryAdd(writeRequest.PageId, source);
                await writer.WriteAsync(tuple);
                return source;
            }

            throw new DiskIoRequestException("Can't line up request!", writeRequest.PageId, DiskIoRequestException.IoDirection.Write);
        }

        protected IBufferSegment ReadPageBuffer(uint pageId)
        {
            var bufferSegment = BufferPoolFactory.Get().Rent(Constants.PAGE_SIZE);
            Stream.Seek(BasePage.PageOffset(pageId), SeekOrigin.Begin);
            Stream.Read(bufferSegment.Span.Slice(0, Constants.PAGE_SIZE));

            return bufferSegment;
        }

        protected void WritePageBuffer(uint pageId, IBufferSegment bufferSegment)
        {
            Stream.Seek(BasePage.PageOffset(pageId), SeekOrigin.Begin);
            Stream.Write(bufferSegment.Span);
        }

        public void Dispose()
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
