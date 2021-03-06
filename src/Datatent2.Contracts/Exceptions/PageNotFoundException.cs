﻿using System;
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
        public uint PageId { get; }

        public PageNotFoundException()
        {
            
        }

        public PageNotFoundException(string message, uint pageId) : base(message)
        {
            PageId = pageId;
        }

        public PageNotFoundException(string message, uint pageId, Exception inner) : base(message, inner)
        {
            PageId = pageId;
        }

        protected PageNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
