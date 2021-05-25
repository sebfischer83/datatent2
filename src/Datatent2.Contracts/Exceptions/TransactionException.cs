using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Contracts.Exceptions
{
    [Serializable]
    public class TransactionException : Exception
    {
        public uint PageId { get; }

        public TransactionException()
        {
        }

        public TransactionException(string message, uint pageId) : base(message)
        {
            PageId = pageId;
        }



        protected TransactionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

    }
}
