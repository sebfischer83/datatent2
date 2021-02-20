using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Tests
{
    public static class TestHelper
    {
        public static byte[] GenerateByteArray(int length, byte value)
        {
            byte[] array = new byte[length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }

            return array;
        }
    }
}
