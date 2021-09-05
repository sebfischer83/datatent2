using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Services.Page;

namespace Datatent2.Core.Services.Index.SkipList
{
    internal class SkipListIndexService<TKey>
    {
        private readonly IPageService _pageService;
        private IndexPage? _initialPage;
        private const float PROBABILITY = 0.5f;
        private const int MAX_LEVEL = 16;

        public float Probability { get; private set; }

        public int MaxLevel { get; private set; }

        private SkipListNode<TKey> _head;
        private SkipListNode<TKey> _end;

        public SkipListIndexService(IPageService pageService) : this(PROBABILITY, MAX_LEVEL, pageService)
        {
            
        }

        private SkipListIndexService(float probability, int maxLevel, IPageService pageService)
        {
            _pageService = pageService;
            Probability = probability;
            MaxLevel = maxLevel;
        }

        public async Task Initialize(IndexPage? initialPage)
        {
            if (initialPage == null)
            {
                _initialPage = await _pageService.CreateNewPage<IndexPage>();
                _initialPage.InitHeader(IndexType.SkipList);
            }

            _initialPage = initialPage;
            if (_initialPage!.IndexPageHeader.Type != IndexType.SkipList)
                throw new InvalidEngineStateException(
                    $"Invalid index type {nameof(_initialPage.IndexPageHeader.Type)} in page {_initialPage.PageHeader.PageId}");

            if (_initialPage.PageHeader.ItemCount == 0)
            {
                // new index
                // create first and last entry
                
            }
        }

        private (TKey Min, TKey Max) GetBounds()
        {
            switch (typeof(TKey))
            {
                case { } intType when intType == typeof(int):
                    return ((TKey) (object)Int32.MinValue, (TKey)(object)Int32.MaxValue);
                    
            }

            throw new InvalidEngineStateException($"{nameof(TKey)} is not supported by this function {nameof(GetBounds)}.");
        }

        public void Insert(TKey key, PageAddress pageAddress)
        {

        }

        public void Delete(TKey key)
        {

        }

        public PageAddress Find(TKey key)
        {
            return PageAddress.Empty;
        }
    }
}
