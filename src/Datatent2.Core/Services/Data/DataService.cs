// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Collections.Pooled;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Block;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Data;
using Datatent2.Core.Services.Page;
using Datatent2.Core.Services.Transactions;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Services.Data
{
    internal class DataService
    {
        private readonly ICompressionService _compressionService;
        private readonly PageService _pageService;
        private readonly TransactionManager _transactionManager;
        private readonly ILogger _logger;

        public DataService(ICompressionService compressionService, PageService pageService, TransactionManager transactionManager, ILogger logger)
        {
            _compressionService = compressionService;
            _pageService = pageService;
            _transactionManager = transactionManager;
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
            using (PooledList<byte> bytes = new PooledList<byte>(Constants.PAGE_SIZE, ClearMode.Never))
            {
                DataPage? page;
                DataBlock block;
                PageAddress address = pageAddress;
                uint checksum = 0;

                do
                {
                    page = await _pageService.GetPage<DataPage>(address.PageId).ConfigureAwait(false);
                    if (page == null)
                    {
                        throw new InvalidPageException("GET", address.PageId);
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
        }

        public async Task<List<(object, PageAddress)>> BulkInsert(IList<object> objects)
        {
            List<(object, PageAddress)> list = new();
            var transaction = _transactionManager.CreateTransaction();
            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                var address = await InsertInternal(obj, transaction).ConfigureAwait(false);
                list.Add((obj, address));
            }

            var ids = list.Select(tuple => tuple.Item2.PageId).Distinct();
            foreach (var id in ids)
            {
                var page = await _pageService.GetPage<BasePage>(id).ConfigureAwait(false);
            }

            transaction.Commit();
            return list;
        }

        public async Task<PageAddress> Insert(object obj)
        {
            var transaction = _transactionManager.CreateTransaction();
            var address = await InsertInternal(obj, transaction).ConfigureAwait(false);
            transaction.Commit();

            return address;
        }

        private async Task<PageAddress> InsertInternal(object obj, Transaction transaction)
        {
            var bytes = PrepareObject(obj);
#if DEBUG
            _logger.LogDebug($"Insert object {obj.GetType().Name} with size {bytes.Data.Length} checksum {bytes.Checksum}");
#endif
            Memory<byte> tempSpan = bytes.Data;

            // add checksum size to data size
            int remainingBytes = bytes.Data.Length + sizeof(uint);
            int blockNr = 0;
            DataBlock? lastBlock = null;
            PageAddress pageAddress = PageAddress.Empty;

            // get free page and write data until done
            while (remainingBytes > 0)
            {
                var dataPage = await _pageService.GetDataPageWithFreeSpace().ConfigureAwait(false);
                transaction.Assign(dataPage);
                var bytesThatCanBeWritten = dataPage.MaxFreeUsableBytes - Constants.BLOCK_HEADER_SIZE;
                if (bytesThatCanBeWritten <= 0)
                {
                    continue;
                }

                var bytesToWrite = Math.Min(bytesThatCanBeWritten, remainingBytes);
                var block = dataPage.InsertBlock((ushort)bytesToWrite, blockNr > 0);

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

                await _pageService.UpdatePageStatistics(dataPage).ConfigureAwait(false);
                lastBlock = block;
                remainingBytes -= bytesToWrite;
                blockNr++;
            }

            return pageAddress;
        }
    }
}
