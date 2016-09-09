using System;
using System.Collections.Generic;
using FinTransConverterLib.FinanceEntities;

namespace FinTransConverterLib.Transactions {
    public interface ITransaction {
        bool IsDuplicate(IEnumerable<ITransaction> transactions);
        void ConvertTransaction(ITransaction t, IFinanceEntity feFrom = null, IFinanceEntity feTo = null);
        string ToString();
    }

    public abstract class Transaction : ITransaction {
        public Transaction() {}

        public virtual void ConvertTransaction(ITransaction t, IFinanceEntity feFrom = null, IFinanceEntity feTo = null) {
            throw new NotSupportedException("The given transaction type is not supported.");
        }

        public abstract bool IsDuplicate(IEnumerable<ITransaction> transactions);

        public abstract override string ToString();
    }
}