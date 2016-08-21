namespace FinTransConverterLib.Transactions {
    public interface ITransaction {
    }

    public abstract class Transaction : ITransaction {
        public Transaction() {}

        public abstract override string ToString();
    }
}