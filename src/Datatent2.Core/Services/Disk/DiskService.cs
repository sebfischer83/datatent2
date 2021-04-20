// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Datatent2.Core.Services.Disk
{ 
    internal abstract class DiskService : IDisposable
    {
        protected readonly Stream Stream;
        protected readonly Channel<ValueTuple<ReadRequest, TaskCompletionSource<ReadResponse>>> ReadChannel;
        protected readonly Channel<ValueTuple<WriteRequest, TaskCompletionSource<WriteRespone>>> WriteChannel;
        protected readonly Task ReadTask;
        protected readonly Task WriteTask;

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
                    item.Item2.SetResult(new WriteRespone(item.Item1.Id, true));
                }
            }
        }


        public async Task<ReadResponse> GetBuffer(ReadRequest readRequest)
        {
            var writer = ReadChannel.Writer;

            while (await writer.WaitToWriteAsync())
            {
                TaskCompletionSource<ReadResponse> source = new TaskCompletionSource<ReadResponse>();
                ValueTuple<ReadRequest, TaskCompletionSource<ReadResponse>> tuple = (readRequest, source);
                await writer.WriteAsync(tuple);
                var response = await source.Task.ConfigureAwait(false);
                return response;
            }

            return default;
        }

        public async Task<WriteRespone> WriteBuffer(WriteRequest writeRequest)
        {
            var writer = WriteChannel.Writer;

            while (await writer.WaitToWriteAsync())
            {
                TaskCompletionSource<WriteRespone> source = new TaskCompletionSource<WriteRespone>();
                ValueTuple<WriteRequest, TaskCompletionSource<WriteRespone>> tuple = (writeRequest, source);
                await writer.WriteAsync(tuple);
                var response = await source.Task.ConfigureAwait(false);
                return response;
            }

            return default;
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
