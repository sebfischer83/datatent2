using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Datatent2.Core.Table
{
    public sealed partial class Table<T> where T : class
    {
        public async Task<T> Get<TKey>(TKey key)
        {
            var index = await Index.Index.LoadIndex(this._mainIndexPageAddress, _pageService, _logger);
            var address = await index.Find(key);

            throw new Exception();
        }
    }
}
