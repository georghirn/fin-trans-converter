using System;
using FinTransConverterLib.FinanceEntities;

namespace FinTransConverterLib.Transactions {
    public interface ITransaction {
        bool IsDuplicate(ITransaction t);
        void ConvertTransaction(ITransaction t, FinanceEntity feFrom = null, FinanceEntity feTo = null);
        string ToString();
    }

    public abstract class Transaction : ITransaction {
        public Transaction() {}

        public virtual void ConvertTransaction(ITransaction t, FinanceEntity feFrom = null, FinanceEntity feTo = null) {
            throw new NotSupportedException("The given transaction type is not supported.");
        }

        public abstract bool IsDuplicate(ITransaction t);

        public abstract override string ToString();
    }
}