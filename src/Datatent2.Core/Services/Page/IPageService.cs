// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System.Threading.Tasks;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Data;
using Datatent2.Core.Page.Table;

namespace Datatent2.Core.Services.Page
{
    internal interface IPageService
    {
        ValueTask<T?> GetPage<T>(uint id) where T : BasePage;

        ValueTask<T?> GetPage<T>(PageAddress address) where T : BasePage
        {
            return GetPage<T>(address.PageId);
        }

        /// <summary>
        /// Save all dirty pages to the underlying disk and clears all cached pages
        /// </summary>
        /// <returns></returns>
        Task CheckPoint();

        /// <summary>
        /// Write a page to the disk
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        Task WritePage(BasePage page);

        Task UpdatePageStatistics(BasePage page);
        Task<TablePage> GetTablePageForTable(string name);

        /// <summary>
        /// Search an existing data page with free space or creates a new one
        /// </summary>
        /// <returns></returns>
        ValueTask<DataPage> GetDataPageWithFreeSpace();

        Task<T> CreateNewPage<T>(string? strParam = null) where T : BasePage;
    }
}