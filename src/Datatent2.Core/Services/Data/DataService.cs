// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Block;
using Datatent2.Core.Page;
using Datatent2.Core.Services.Compression;
using Datatent2.Core.Services.Page;
using Dawn;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Services.Data
{
    internal class DataService
    {
        private readonly ICompressionService _compressionService;
        private readonly PageService _pageService;
        private readonly ILogger<DataService> _logger;

        public DataService(ICompressionService compressionService, PageService pageService, ILogger<DataService> logger)
        {
            _compressionService = compressionService;
            _pageService = pageService;
            _logger = logger;
        }

        private (byte[] Data, uint Checksum) PrepareObject(object obj)
        {
            // step 1 serialize object to byte[]
            var serializedBytes = Utf8Json.JsonSerializer.Serialize(obj);
            //// step 2 compress data
            //var compressedBytes = _compressionService.Compress(serializedBytes);

            var checksum = Force.Crc32.Crc32Algorithm.Compute(serializedBytes);

            return (serializedBytes, checksum);
        }

        private T RetrieveObject<T>(byte[] array, uint orgChecksum)
        {
            var checksum = Force.Crc32.Crc32Algorithm.Compute(array);
            var orgCheckSumNumber = orgChecksum;
            if (checksum != orgCheckSumNumber)
            {
                _logger.LogCritical($"checksum doesn't match {checksum} vs {orgChecksum} type {typeof(T)}");
                throw new Exception("Checksum don't match.");
            }

            return Utf8Json.JsonSerializer.Deserialize<T>(array);
        }

        public async Task<T> Get<T>(PageAddress pageAddress)
        {
#if DEBUG
            _logger.LogInformation($"Get object {typeof(T)} at {pageAddress}");
#endif

            var page = await _pageService.GetPage<DataPage>(pageAddress.PageId);
            if (page == null)
            {
                throw new Exception();
            }

            DataBlock block = new DataBlock(page, pageAddress.SlotId);
            List<byte> bytes = new List<byte>();
            bytes.AddRange(block.GetData());
            var checksum = block.Header.Checksum;

            while (!block.Header.NextBlockAddress.IsEmpty())
            {
                var nextPage = await _pageService.GetPage<DataPage>(block.Header.NextBlockAddress.PageId);
                if (nextPage == null)
                {
                    throw new Exception();
                }
                block = new DataBlock(nextPage, block.Header.NextBlockAddress.SlotId);
                bytes.AddRange(block.GetData());
            }

            var array = bytes.ToArray();
            return RetrieveObject<T>(array, checksum);
        }

        public async Task<List<(object, PageAddress)>> BulkInsert(IList<object> objects)
        {
            List<(object, PageAddress)> list = new();
            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                var address = await Insert(obj, true);
                list.Add((obj, address));
            }

            var ids = list.Select(tuple => tuple.Item2.PageId).Distinct();
            foreach (var id in ids)
            {
                var page = await _pageService.GetPage<BasePage>(id);
                if (page != null)
                    await _pageService.WritePage(page);
            }

            return list;
        }

        public async Task<PageAddress> Insert(object obj, bool delayWrite = false)
        {
            var bytes = PrepareObject(obj);

            Memory<byte> tempSpan = bytes.Data;
            int remainingBytes = bytes.Data.Length;
            int blockNr = 0;
            DataBlock? lastBlock = null;
            PageAddress pageAddress = PageAddress.Empty;
            HashSet<BasePage> pages = new();

            while (remainingBytes > 0)
            {
                var dataPage = await _pageService.GetDataPageWithFreeSpace();
                if (!pages.Contains(dataPage))
                    pages.Add(dataPage);
                var bytesThatCanBeWritten = dataPage.MaxFreeUsableBytes - Constants.BLOCK_HEADER_SIZE;
                if (bytesThatCanBeWritten <= 0)
                {
                    continue;
                }

                var bytesToWrite = Math.Min(bytesThatCanBeWritten, remainingBytes);
                var block = dataPage.InsertBlock((ushort)((ushort)bytesToWrite + Constants.BLOCK_HEADER_SIZE), blockNr > 0, blockNr == 0 ? bytes.Checksum : DataBlock.EMPTY_CHECKSUM);
                block.FillData(tempSpan.Span.Slice(bytes.Data.Length - remainingBytes, bytesToWrite));

                lastBlock?.SetFollowingBlock(block.Position);

                if (pageAddress.IsEmpty())
                    pageAddress = block.Position;

                lastBlock = block;
                remainingBytes -= bytesToWrite;
                blockNr++;
            }

            if (!delayWrite)
                foreach (var page in pages)
                {
                    await _pageService.WritePage(page);
                }

            //ArrayPool<byte>.Shared.Return(bytes);
            return pageAddress;
        }
    }
}
