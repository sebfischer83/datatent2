// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Datatent2.Core
{
    public class Datatent
    {
        public Datatent()
        {
        }

        public static Datatent Create(DatatentSettings datatentSettings)
        {


            return null;
        }

        public static Datatent Load(string path, string pwd)
        {

            return null;
        }
    }

    public sealed class DatatentSettings
    {
        public string? Path { get; set; }

        public bool InMemory { get; set; }
    }
}
