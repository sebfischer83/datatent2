using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Data;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Page.Table;
using Datatent2.Core.Services.Page;

#nullable enable

namespace Datatent2.Core.Tests.Mocks
{
    internal class FakePageService : IPageService
    {
        private Dictionary<uint, BasePage> _pages = new();

        public Task<T?> GetPage<T>(uint id) where T : BasePage
        {
            if (_pages.ContainsKey(id))
            {
                return Task.FromResult((T?) _pages[id]);
            }

            return Task.FromResult((T?)null);
        }

        public Task CheckPoint()
        {
            return Task.CompletedTask;
        }

        public Task WritePage(BasePage page)
        {
            return Task.CompletedTask;
        }

        public Task UpdatePageStatistics(BasePage page)
        {
            return Task.CompletedTask;
        }

        public Task<TablePage> GetTablePageForTable(string name)
        {
            throw new NotImplementedException();
        }

        public ValueTask<DataPage> GetDataPageWithFreeSpace()
        {
            throw new NotImplementedException();
        }

        public Task<T> CreateNewPage<T>(string? strParam = null) where T : BasePage
        {
            var id = (uint)_pages.Count + 1;
            if (typeof(T) == typeof(DataPage))
            {
                var page = (T)(object)new DataPage(new BufferSegment(Constants.PAGE_SIZE),  id);
                _pages.Add(id, page);
                
                return Task.FromResult((T) page);
            }
            if (typeof(T) == typeof(TablePage))
            {
                var page = (T)(object)new TablePage(new BufferSegment(Constants.PAGE_SIZE), id, strParam!);
                _pages.Add(id, page);

                return Task.FromResult((T)page);
            }
            if (typeof(T) == typeof(IndexPage))
            {
                var page = (T)(object)new IndexPage(new BufferSegment(Constants.PAGE_SIZE), id);
                _pages.Add(id, page);

                return Task.FromResult((T)page);
            }

            throw new Exception();
        }
    }
}
