// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

namespace Datatent2.Contracts
{
    /// <summary>
    /// The type of implementation that should be used for the page buffer pool
    /// </summary>
    public enum BufferPoolImplementation
    {
        /// <summary>
        /// Pool from unmanaged memory
        /// </summary>
        Unmanaged,
        /// <summary>
        /// Pool from managed memory
        /// </summary>
        Managed
    }
}