// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Services.Compression
{
    public interface ICompressionService
    {
        Span<byte> Compress(Span<byte> bytes, Span<byte> target);
        byte[] Compress(Span<byte> bytes);
    }
}
