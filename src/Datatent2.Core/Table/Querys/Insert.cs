using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Datatent2.Core.Table
{
    public sealed partial class Table<T> where T : class
    {
        //public async Task Insert<TObj, TKey>(TObj obj, TKey key)
        //{
        //    if (obj == null)
        //        return;
        //    await Insert(obj, key);
        //}

        //public async Task Insert<TObj>(TObj obj)
        //{
        //    if (obj == null)
        //        return;
        //    //await Insert(obj, key);
        //}

        public async Task Insert<TObj, TKey>(TObj obj, TKey key)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var address = await _dataService!.Insert(obj);
            var index = await Services.Index.IndexService.LoadIndex(this._mainIndexPageAddress, _pageService, _logger);
            await index.Insert(key, address);

            _logger.LogInformation($"[{_name}] object of type {obj.GetType().Name} written to table {this.Name} at {address} with key {key}");
        }
    }
}
