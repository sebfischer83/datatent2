using Datatent2.Core.Page;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Datatent2.Core.Services.Transactions
{
    internal sealed class TransactionManager
    {
        private readonly Dictionary<Guid, Transaction> _transactions = new();
        private readonly ILogger _logger;

        public TransactionManager(ILogger logger)
        {
            _logger = logger;
        }

        public Transaction CreateTransaction()
        {
            var transaction = new Transaction(this);
            transaction.IsRunning = true;
            _transactions.Add(transaction.Id, transaction);

            return transaction;
        }

        public void Remove(Transaction transaction)
        {
            _transactions.Remove(transaction.Id);
        }

        public async Task WaitForAllTransactionsToCompletitionAsync(CancellationToken cancellationToken)
        {
            if (_transactions.Count == 0)
                return;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (_transactions.Count == 0)
                    return;
                await Task.Delay(5000).ConfigureAwait(false);
            }
        }
    }
}
