using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Page;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Services.Transactions
{
    internal sealed class Transaction
    {
        public Guid Id { get; private set; }
        public bool IsRunning { get; set; }

        private Dictionary<uint, BasePage> _pages;
        private readonly TransactionManager _transactionManager;

        public Transaction(TransactionManager transactionManager)
        {
            _pages = new();
            Id = Guid.NewGuid();
            IsRunning = true;
            _transactionManager = transactionManager;
        }

        public void Assign(BasePage page)
        {
            if (page.Transaction != null && !page.Transaction.Equals(this))
            {
                throw new TransactionException("The page is already in an existing transaction!", page.Id);
            }

            if (!_pages.ContainsKey(page.Id))
                _pages.Add(page.Id, page);
            page.Transaction = this;
        }

        public void Commit()
        {
            foreach (var page in _pages.Values)
            {
                page.Transaction = null;
            }
            _pages.Clear();
            IsRunning = false;
            _transactionManager.Remove(this);
        }

        public override bool Equals(object? obj)
        {
            return Id.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
