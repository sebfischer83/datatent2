// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;

namespace Datatent2.Contracts
{
    public interface ICompressionService : IService
    {
        Span<byte> Compress(Span<byte> bytes, Span<byte> target);
        byte[] Compress(Span<byte> bytes);
    }
}