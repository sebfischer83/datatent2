// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;

namespace Datatent2.Contracts
{
    /// <summary>
    /// The interface for the compression services
    /// </summary>
    public interface ICompressionService : IService
    {
        /// <summary>
        /// Compress the data int a target span
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        Span<byte> Compress(Span<byte> bytes, Span<byte> target);

        /// <summary>
        /// Compress the data into a new array
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        Span<byte> Compress(Span<byte> bytes);
    }
}