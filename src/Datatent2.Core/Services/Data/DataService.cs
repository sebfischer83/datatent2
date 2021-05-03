// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Datatent2.Core.Block;
using Datatent2.Core.Page;
using Datatent2.Core.Services.Compression;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Services.Data
{
    internal class DataService
    {
        private readonly ICompressionService _compressionService;
        private readonly PageService _pageService;
        private readonly ILogger _logger;

        public DataService(ICompressionService compressionService, PageService pageService, ILogger logger)
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
            Span<byte> span = array;
            var checksum = Force.Crc32.Crc32Algorithm.Compute(array, 0, array.Length);
            if (checksum != orgChecksum)
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
            List<byte> bytes = new List<byte>();
            DataPage? page;
            DataBlock block;
            PageAddress address = pageAddress;
            uint checksum = 0;

            do
            {
                page = await _pageService.GetPage<DataPage>(address.PageId);
                if (page == null)
                {
                    throw new Exception();
                }
                block = new DataBlock(page, address.SlotId);
                bytes.AddRange(block.GetData());
                address = block.Header.NextBlockAddress;
            } while (!block.Header.NextBlockAddress.IsEmpty());
            
            var array = bytes.Take(bytes.Count - sizeof(uint)).ToArray();
            var check = bytes.Skip(bytes.Count - sizeof(uint)).ToArray();
            checksum = BitConverter.ToUInt32(check);
#if DEBUG
            _logger.LogInformation($"Object at {pageAddress} has length of {array.Length} bytes");
#endif

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
#if DEBUG
            _logger.LogInformation($"Insert object {obj.GetType().Name} with size {bytes.Data.Length} checksum {bytes.Checksum}");
#endif
            Memory<byte> tempSpan = bytes.Data;

            // add checksum size to data size
            int remainingBytes = bytes.Data.Length + sizeof(uint);
            int blockNr = 0;
            DataBlock? lastBlock = null;
            PageAddress pageAddress = PageAddress.Empty;
            HashSet<BasePage> pages = new();

            // get free page and write data until done
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
                var block = dataPage.InsertBlock((ushort)((ushort)bytesToWrite + Constants.BLOCK_HEADER_SIZE), blockNr > 0);

#if DEBUG
                _logger.LogInformation($"Write {bytesToWrite} bytes from {remainingBytes} bytes to {block}");
#endif

                if (bytesToWrite - remainingBytes == 0)
                {
                    // adjust indexes to checksum
                    block.FillData(tempSpan.Span.Slice(bytes.Data.Length - remainingBytes + sizeof(uint),
                        bytesToWrite - sizeof(uint)), bytes.Checksum);
                }
                else
                    block.FillData(tempSpan.Span.Slice(bytes.Data.Length + sizeof(uint) - remainingBytes, bytesToWrite));

                lastBlock?.SetFollowingBlock(block.Position);

                if (pageAddress.IsEmpty())
                    pageAddress = block.Position;

                await _pageService.UpdatePageStatisticsAsync(dataPage);
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
