using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Contracts.Exceptions
{
    [Serializable]
    public class PageNotFoundException : Exception
    {
        public PageNotFoundException()
        {
        }

        public PageNotFoundException(string message) : base(message)
        {
        }

        public PageNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PageNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
