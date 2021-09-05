using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts.Exceptions;
using Xunit;

namespace Datatent2.Core.Tests.Index.SkipList
{
    public class SkipListIndexServiceTest
    {
        [Fact]
        public void Test()
        {
            switch (typeof(int))
            {
                case { } intType when intType == typeof(int):
                    throw new InvalidEngineStateException("");
                    break;

            }

        }
    }
}
