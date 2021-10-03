// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Contracts.Scripting
{
    /// <summary>
    /// Interface for scripting engines
    /// </summary>
    public interface IScriptingEngine : IService, IDisposable
    {
        /// <summary>
        /// Executes the specified script
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        T Execute<T>(object obj);
    }
}
