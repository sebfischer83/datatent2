using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Page.Header
{
    internal class HeaderContent
    {
        public Guid CompressionAlgo { get; set; }

        public Guid EncryptionAlgo { get; set; }



        public HeaderContent()
        {
            CompressionAlgo = Guid.Empty;
            EncryptionAlgo = Guid.Empty;
        }
    }
}
