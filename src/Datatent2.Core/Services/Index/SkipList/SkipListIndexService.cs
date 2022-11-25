using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Block;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Services.Page;
using DotNext.Collections.Generic;
using GiGraph.Dot.Entities.Edges.Endpoints;
using GiGraph.Dot.Entities.Graphs;
using GiGraph.Dot.Extensions;
using GiGraph.Dot.Types.Colors;
using GiGraph.Dot.Types.Edges;
using GiGraph.Dot.Types.Layout;
using GiGraph.Dot.Types.Nodes;
using GiGraph.Dot.Types.Records;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;

namespace Datatent2.Core.Services.Index.SkipList
{
    internal class SkipListIndexService : Services.Index.IndexService
    {
        private IndexPage? _initialPage;
        private const float PROBABILITY = 0.5f;
        private const int MAX_LEVEL = 16;
        // level starts with 0 because it makes indexing much easier, so level = 15 are 16 slots
        private int _level = 0;
        private readonly Random _random = new Random();

        public float Probability { get; private set; }

        public int MaxLevel { get; private set; }

        private SkipListNode _head;
        //private SkipListNode _end;
        private PageAddress _headPageAddress;
        private IndexPage? _currentIndexPage;

        public SkipListIndexService(uint firstPageIndex, IPageService pageService, ILogger logger) : this(PROBABILITY, MAX_LEVEL, firstPageIndex, pageService, logger)
        {

        }

        private SkipListIndexService(float probability, int maxLevel, uint firstPageIndex, IPageService pageService, ILogger logger) : base(firstPageIndex, pageService, logger)
        {
            Probability = probability;
            MaxLevel = maxLevel;
        }

        public override async Task Initialize()
        {
#if DEBUG
            Logger.LogInformation($"Initialize {nameof(SkipListIndexService)} at page {FirstPageIndex}.");
#endif

            if (FirstPageIndex == 0)
            {
                _initialPage = await PageService.CreateNewPage<IndexPage>().ConfigureAwait(false);
                _initialPage.InitHeader(IndexType.SkipList);
            }
            else
            {
                _initialPage = await PageService.GetPage<IndexPage>(FirstPageIndex).ConfigureAwait(false);
            }

            if (_initialPage!.IndexPageHeader.Type != IndexType.SkipList)
                throw new InvalidEngineStateException(
                    $"Invalid index type {nameof(_initialPage.IndexPageHeader.Type)} in page {_initialPage.PageHeader.PageId}");

            _currentIndexPage = _initialPage;
            if (_initialPage.PageHeader.ItemCount == 0)
            {
#if DEBUG
                Logger.LogInformation($"Create a new {nameof(SkipListIndexService)} at page {FirstPageIndex}.");
#endif
                // new index
                // create first and last entry
                _head = new SkipListNode(MaxLevel);
                for (int i = 0; i < _level; i++)
                {
                    _head.Forward[i] = PageAddress.Empty;
                }

                var pos = await InsertNode(_head).ConfigureAwait(false);
                _headPageAddress = pos;
            }
            else
            {
                // 1 = first entry; 2 = last entry

            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "SCS0005:Weak random number generator.", Justification = "No better random number needed here.")]
        private int CoinFlip()
        {
            float r = (float)_random.Next() / int.MaxValue;
            int lvl = 0;

            while (r < Probability && lvl < MaxLevel)
            {
                lvl++;
                r = (float)_random.Next() / int.MaxValue;
            }

            if (lvl == MaxLevel)
                lvl--;

            return lvl;

        }

        private async Task UpdateNode(SkipListNode node, PageAddress pageAddress)
        {
            var bytes = node.ToBytes();
            var page = await PageService.GetPage<IndexPage>(pageAddress).ConfigureAwait(false);

            page!.UpdateBlock(bytes, pageAddress);
        }

        private async Task<PageAddress> InsertNode(SkipListNode node)
        {
            if (_currentIndexPage == null)
                throw new InvalidEngineStateException($"{nameof(_currentIndexPage)} is not allowed to be null!");
            var bytes = node.ToBytes();


            var indexPage = _currentIndexPage;
            var bytesThatCanBeWritten = _currentIndexPage.MaxFreeUsableBytes - Constants.BLOCK_HEADER_SIZE;
            if (bytesThatCanBeWritten < bytes.Length || !indexPage.IsInsertPossible((ushort)bytes.Length))
            {
                indexPage = await PageService.CreateNewPage<IndexPage>().ConfigureAwait(false);
                indexPage.InitHeader(IndexType.SkipList);
                _currentIndexPage.SetNextPage(indexPage.Id);
                indexPage.SetPreviousPage(_currentIndexPage.Id);
                _currentIndexPage = indexPage;
            }

            var block = indexPage.InsertBlock((ushort)bytes.Length);
            block.FillData(bytes,  0);

            return block.Position;
        }

        public override IndexType Type => IndexType.SkipList;
        public override async Task<PageAddress?> Find<T>(T key)
        {
            SkipListNode? current = _head;
            for (int i = _level; i >= 0; i--)
            {
                var node = await GetNodeAtAddress(current.Value.Forward[i]).ConfigureAwait(false);
                while (node.HasValue && current.Value.Forward[i] != PageAddress.Empty &&
                       Comparer<T>.Default.Compare(key, (T)node!.Value.Key) > 0)
                {
                    current = node;
                    node = await GetNodeAtAddress(current.Value.Forward[i]).ConfigureAwait(false);
                }
            }

            var address = current.Value.Forward[0];
            if (address == PageAddress.Empty)
                return address;
            current = await GetNodeAtAddress(current.Value.Forward[0]).ConfigureAwait(false);
            if (Comparer<T>.Default.Compare(key, (T)current!.Value.Key) == 0)
            {
                return current.Value.PageAddress;
            }
            return PageAddress.Empty;
        }
        
        public override Task<PageAddress[]> FindMany<T>(T key)
        {
            throw new NotImplementedException();
        }

        public override async Task Insert<T>(T key, PageAddress pageAddress)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            await SemaphoreSlim.WaitAsync().ConfigureAwait(false);
            KeyValuePair<SkipListNode, PageAddress?>[] update = new KeyValuePair<SkipListNode, PageAddress?>[MaxLevel + 1];
            
            try
            {
                SkipListNode? current = _head;

                PageAddress currentPageAddress = _headPageAddress;

                for (int i = _level; i >= 0; i--)
                {
                    var node = await GetNodeAtAddress(current.Value.Forward[i]).ConfigureAwait(false);
                    while (node.HasValue && current.Value.Forward[i] != PageAddress.Empty &&
                           Comparer<T>.Default.Compare(key, (T)node!.Value.Key) > 0)
                    {
                        currentPageAddress = current.Value.Forward[i];
                        current = node;
                        node = await GetNodeAtAddress(current.Value.Forward[i]).ConfigureAwait(false);
                    }

                    update[i] = new KeyValuePair<SkipListNode, PageAddress?>(current.Value, currentPageAddress);
                }

                if (current.Value.Forward[0] != PageAddress.Empty)
                {
                    current = await GetNodeAtAddress(current.Value.Forward[0]).ConfigureAwait(false);
                }
                else
                {
                    current = null;
                }

                if (current == null || !EqualityComparer<T>.Default.Equals((T)current.Value.Key, key))
                {
                    var rLevel = CoinFlip();
                    if (rLevel > _level)
                    {
                        for (int i = _level + 1; i < rLevel + 1; i++)
                        {
                            update[i] = new KeyValuePair<SkipListNode, PageAddress?>(_head, _headPageAddress);
                        }

                        _level = rLevel;
                    }

                    SkipListNode n = new SkipListNode(key!, pageAddress, rLevel + 1);
                    var pos = await InsertNode(n).ConfigureAwait(false);
                    for (int i = 0; i <= rLevel; i++)
                    {
                        var tempPair = update[i];
                        n.Forward[i] = tempPair.Key.Forward[i];
                        update[i].Key.Forward[i] = pos;
                        if (tempPair.Value!.Value != PageAddress.Empty)
                            await UpdateNode(tempPair.Key, tempPair.Value!.Value).ConfigureAwait(false);
                    }

                    await UpdateNode(n, pos).ConfigureAwait(false);
                }
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        private async ValueTask<SkipListNode?> GetNodeAtAddress(PageAddress pageAddress)
        {
            if (pageAddress == PageAddress.Empty)
                return null;
#if DEBUG
            Logger.LogTrace($"Search index node at {pageAddress}");
#endif
            var page = await PageService.GetPage<IndexPage>(pageAddress.PageId).ConfigureAwait(false);
            if (page == null)
                throw new PageNotFoundException("Index page not found", pageAddress.PageId);
            var indexBlock = new IndexBlock(page, pageAddress.SlotId);
            var node = SkipListNode.FromBytes(indexBlock.GetData());

            return node;
        }

        public override Task Delete<T>(T key)
        {
            throw new NotImplementedException();
        }

        public override Task DeleteIndex()
        {
            throw new NotImplementedException();
        }

        public override async IAsyncEnumerable<(T Key, PageAddress Address)> GetAll<T>()
        {
            int a = 0;

            PageAddress pageAddress = _head.Forward[0];
            do
            {
                var current = await GetNodeAtAddress(pageAddress).ConfigureAwait(false);

                yield return ((T) current!.Value.Key, current.Value.PageAddress);

                pageAddress = current!.Value.Forward[0];
                a++;
            } while (pageAddress != PageAddress.Empty);
        }

        public override async Task<string> Print(PrintStyle printStyle)
        {
            Dictionary<PageAddress, int> dictionary = new();

            var graph = new DotGraph(nameof(SkipListIndexService), directed: true);
            //graph.Attributes. = DotEdgeShape.Line;
            //graph.Attributes.Layout.Direction = DotLayoutDirection.LeftToRight;
            //graph.Attributes.Layout.NodeSeparation = 0;

            // header

            var builder = new DotRecordBuilder();

            int i = _level;
            do
            {
                builder.AppendField(printStyle.AttachIndexAddresses ? $"• {_head.Forward[i]}" : $"•", $"h{i}");
                i--;
            } while (i >= 0);

            builder.AppendField(printStyle.AttachIndexAddresses ? $"Head {_headPageAddress}" : $"Head");
            graph.Nodes.Add("Head", attributes =>
            {
                attributes.Style.FillStyle = DotNodeFillStyle.Normal;
                attributes.FillColor = new DotColor(Color.LemonChiffon);
            }).ToRecordNode(builder.Build());

            int a = 0;
            // follow the links

            PageAddress pageAddress = _head.Forward[0];
            do
            {
                var current = await GetNodeAtAddress(pageAddress).ConfigureAwait(false);

                builder = new DotRecordBuilder();

                i = current!.Value.Forward.Length - 1;
                do
                {
                    builder.AppendField(printStyle.AttachIndexAddresses ? $"• {current.Value.Forward[i]}" : $"•",
                        $"{a}n{i}");

                    i--;
                } while (i >= 0);

                builder.AppendField(printStyle.AttachIndexAddresses ? $"{current.Value.Key} {pageAddress}" : $"{current.Value.Key}");
                graph.Nodes.Add($"{a}n", attributes =>
                {

                }).ToRecordNode(builder.Build());
                dictionary.Add(pageAddress, a);

                pageAddress = current.Value.Forward[0];
                a++;
            } while (pageAddress != PageAddress.Empty);

            // Tail

            builder = new DotRecordBuilder();
            i = _level;
            do
            {
                builder.AppendField($"inf", $"t{i}");
                i--;
            } while (i >= 0);

            builder.AppendField($"Tail");
            graph.Nodes.Add("Tail", attributes =>
            {
                attributes.Style.FillStyle = DotNodeFillStyle.Normal;
                attributes.FillColor = new DotColor(Color.LemonChiffon);
            }).ToRecordNode(builder.Build());

            // edges line 1
            var order = dictionary.ToArray().OrderBy(pair => pair.Value).ToList();
            graph.Edges.Add(new DotEndpoint("Head", "h0"), new DotEndpoint("0n", "0n0"));

            for (int j = 0; j < order.Count - 1; j++)
            {
                graph.Edges.Add(new DotEndpoint($"{order[j].Value}n", $"{order[j].Value}n0"),
                    new DotEndpoint($"{order[j + 1].Value}n", $"{order[j + 1].Value}n0"));
            }

            graph.Edges.Add(new DotEndpoint($"{a - 1}n", $"{a - 1}n0"), new DotEndpoint("Tail", "t0"));

            // edges
            // start at the head 
            // from the most upper level to the second lowest

            i = _level;
            do
            {
                pageAddress = _head.Forward[i];
                SkipListNode? last = _head;
                PageAddress lastPageAddress = PageAddress.Empty;

                while (pageAddress != PageAddress.Empty)
                {
                    SkipListNode? node = await GetNodeAtAddress(pageAddress).ConfigureAwait(false);
                    string firstPart = "";
                    string firstNode = "";
                    string endPart = "";
                    string endNode = "";

                    if (last!.Value.TypeCode == SkipListNodeTypeCode.Start)
                    {
                        firstPart = $"h{i}";
                        firstNode = "Head";
                    }
                    else
                    {
                        var pos = dictionary.First(pair => pair.Key == lastPageAddress).Value;
                        firstPart = $"{pos}n{i}";
                        firstNode = $"{pos}n";
                    }

                    var cpos = dictionary.First(pair => pair.Key == pageAddress).Value;
                    endPart = $"{cpos}n{i}";
                    endNode = $"{cpos}n";

                    graph.Edges.Add(new DotEndpoint(firstNode, firstPart),
                       new DotEndpoint(endNode, endPart));

                    last = node;
                    lastPageAddress = pageAddress;
                    pageAddress = node!.Value.Forward[i];
                    if (pageAddress == PageAddress.Empty)
                    {
                        graph.Edges.Add(new DotEndpoint(endNode, endPart),
                            new DotEndpoint("Tail", $"t{i }"));
                    }

                }
                i--;
            } while (i > 0);
            
            return graph.Build();
        }
    }
}




//private (TKey Min, TKey Max) GetBounds()
//{
//    switch (typeof(TKey))
//    {
//        case { } t when t == typeof(byte):
//            return ((TKey)(object)byte.MinValue, (TKey)(object)byte.MaxValue);
//        case { } t when t == typeof(sbyte):
//            return ((TKey)(object)sbyte.MinValue, (TKey)(object)sbyte.MaxValue);
//        case { } t when t == typeof(int):
//            return ((TKey) (object)Int32.MinValue, (TKey)(object)Int32.MaxValue);
//        case { } t when t == typeof(uint):
//            return ((TKey)(object)uint.MinValue, (TKey)(object)uint.MaxValue);
//        case { } t when t == typeof(long):
//            return ((TKey)(object)long.MinValue, (TKey)(object)long.MaxValue);
//        case { } t when t == typeof(ulong):
//            return ((TKey)(object)ulong.MinValue, (TKey)(object)ulong.MaxValue);
//        case { } t when t == typeof(short):
//            return ((TKey)(object)short.MinValue, (TKey)(object)short.MaxValue);
//        case { } t when t == typeof(ushort):
//            return ((TKey)(object)ushort.MinValue, (TKey)(object)ushort.MaxValue);
//    }

//    throw new InvalidEngineStateException($"{nameof(TKey)} is not supported by this function {nameof(GetBounds)}.");
//}