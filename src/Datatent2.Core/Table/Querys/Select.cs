using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collections.Pooled;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Data;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Datatent2.Core.Table
{
    public sealed partial class Table<TValue, TKey> where TValue : class
    {
        /// <summary>
        /// Return the object for that key, if there are many the first result is returned. (can happen when there is only a HeapIndexService that is not unique)
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<TValue?> Get(TKey key)
        {
            var index = await Services.Index.IndexService.LoadIndex(this._mainIndexPageAddress, _pageService, _logger).ConfigureAwait(false);
            var address = await index.Find(key).ConfigureAwait(false);

            if (!address.HasValue)
            {
                _logger.LogDebug($"No object found in index {index}");
                return null;
            }

            var obj = await _dataService.Get<TValue>(address.Value).ConfigureAwait(false);

            return obj;
        }
    }
}
