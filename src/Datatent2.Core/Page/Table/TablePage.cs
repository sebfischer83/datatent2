using Datatent2.Core.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Page.Table
{
    /// <summary>
    /// Holds the information
    /// </summary>
    internal class TablePage : BasePage
    {
        public TablePage(IBufferSegment buffer) : base(buffer)
        {
        }

        public TablePage(IBufferSegment buffer, uint id) : base(buffer, id, PageType.Table)
        {
        }

        public bool ContainsTable(string name)
        {
            return false;
        }
    }
}
