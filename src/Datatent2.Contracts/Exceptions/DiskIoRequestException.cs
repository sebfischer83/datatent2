using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Contracts.Exceptions
{
    [Serializable]
    public class DiskIoRequestException : Exception
    {
        public uint PageId { get; }
        public IoDirection Direction { get; }

        public DiskIoRequestException()
        {
        }

        public DiskIoRequestException(string message, uint pageId, IoDirection direction) : base(message)
        {
            PageId = pageId;
            Direction = direction;
        }

        public DiskIoRequestException(string message, uint pageId, IoDirection direction, Exception inner) : base(message, inner)
        {
            PageId = pageId;
            Direction = direction;
        }


        protected DiskIoRequestException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public enum IoDirection
        {
            Read,
            Write
        }
    }
}
