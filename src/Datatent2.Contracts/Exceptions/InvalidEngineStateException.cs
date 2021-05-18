using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Contracts.Exceptions
{
    [Serializable]
    public class InvalidEngineStateException : Exception
    {
        public InvalidEngineStateException()
        {
        }

        public InvalidEngineStateException(string message) : base(message)
        {
        }

        public InvalidEngineStateException(string message, Exception inner) : base(message, inner)
        {
        }


        protected InvalidEngineStateException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
