// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;

namespace Datatent2.Contracts
{
    /// <summary>
    /// Base interface for all database services
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// The id for the database configuration
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The visible name for the user
        /// </summary>
        public string Name { get; }
    }
}