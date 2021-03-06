﻿// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using Datatent2.Core.Page;
using Datatent2.Core.Page.Data;
using Datatent2.Core.Services.Transactions;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Datatent2.Core.Services.Cache
{
    internal class CacheService : IEnumerable<BasePage>
    {
        private readonly Dictionary<uint, BasePage> _allPages = new();
        private readonly Dictionary<uint, BasePage> _freeDataPages = new();

        public CacheService()
        {

        }

        public void Add(BasePage page)
        {
            if (page.Type == PageType.Data && !page.IsFull)
            {
                _freeDataPages.Add(page.Id, page);
            }
            _allPages.Add(page.Id, page);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool HasPage(uint id)
        {
            return _allPages.ContainsKey(id);
        }

        public T? Get<T>(uint id) where T : BasePage
        {
            if (!HasPage(id))
                return null;
            return (T)_allPages[id];
        }

        public DataPage? GetDataPage(bool withFreeSpace = true)
        {
            if (_freeDataPages.Count == 0)
                return null;

            DataPage? dataPage = null;
            List<uint> list = new();
            for (int i = 0; i < _freeDataPages.Keys.Count; i++)
            {
                var pageKey = _freeDataPages.Keys.ElementAt(i);
                var page = _freeDataPages[pageKey];
                if (!withFreeSpace && page.Transaction == null)
                {
                    dataPage = (DataPage)page;
                    break;
                }

                if (page.FillFactor < PageFillFactor.NinetyFiveToNinetyNine && page.Transaction == null)
                {
                    dataPage = (DataPage)page;
                    break;
                }
                if (page.FillFactor > PageFillFactor.NinetyFiveToNinetyNine)
                    list.Add(pageKey);
            }

            for (int i = 0; i < list.Count; i++)
            {
                _freeDataPages.Remove(list[i]);
            }

            return dataPage;
        }

        public void Clear()
        {
            foreach (var page in _allPages.Values)
            {
                page.Dispose();
            }
            _allPages.Clear();
            _freeDataPages.Clear();
        }

        public IEnumerable<T> GetAllPagesOfType<T>(PageType pageType)
        {
            foreach (var page in _allPages)
            {
                if (page.Value.Type == pageType)
                {
                    yield return (T)(object)page;
                }
            }
        }

        public IEnumerator<BasePage> GetEnumerator()
        {
            return _allPages.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
