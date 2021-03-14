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

namespace Datatent2.Core.Services.Data
{
    internal class DataService
    {
        private readonly ICompressionService _compressionService;
        private readonly PageService _pageService;

        public DataService(ICompressionService compressionService, PageService pageService)
        {
            _compressionService = compressionService;
            _pageService = pageService;
        }

        private byte[] PrepareObject(object obj)
        {
            // step 1 serialize object to byte[]
            var serializedBytes = Utf8Json.JsonSerializer.Serialize(obj);

            //// step 2 compress data
            //var compressedBytes = _compressionService.Compress(serializedBytes);

            return serializedBytes;
        }

        private T RetrieveObject<T>(byte[] bytes)
        {
            return Utf8Json.JsonSerializer.Deserialize<T>(bytes);
        }

        public async Task<T> Get<T>(PageAddress pageAddress)
        {
            var page = await _pageService.GetPage<DataPage>(pageAddress.PageId);
            if (page == null)
            {
                throw new Exception();
            }

            DataBlock block = new DataBlock(page, pageAddress.SlotId);
            List<byte> bytes = new List<byte>();
            bytes.AddRange(block.GetData());

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

            return RetrieveObject<T>(bytes.ToArray());
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

            Memory<byte> tempSpan = bytes;
            int remainingBytes = bytes.Length;
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
                var block = dataPage.InsertBlock((ushort)((ushort)bytesToWrite + Constants.BLOCK_HEADER_SIZE), blockNr > 0);
                block.FillData(tempSpan.Span.Slice(bytes.Length - remainingBytes, bytesToWrite));

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
