// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;

namespace Datatent2.Contracts
{
    public interface IService
    {
        public Guid Id { get; }

        public string Name { get; }
    }
}