using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using RocksDbSharp;

namespace EtherChain.Models
{
    public class DataContext
    {
        private readonly RocksDb _db;
        private long _lastTxId;

        public DataContext(string path = "ether.db")
        {
            var options = new DbOptions()
                .SetCreateIfMissing(true);
            _db = RocksDb.Open(options, path);
            var lastTxIdstr = _db.Get("lastTxId");
            _lastTxId = string.IsNullOrEmpty(lastTxIdstr) ? 0 : long.Parse(lastTxIdstr);
        }

        public void CreateCheckPoint()
        {
            _db.Checkpoint();
        }

        public Address GetAddress(string address)
        {
            var formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();

            // Update the balances
            Address add = new Address();
            var fromDataBytes = _db.Get(Encoding.ASCII.GetBytes(address));
            if (fromDataBytes != null)
            {
                stream.Write(fromDataBytes);
                add = (Address)formatter.Deserialize(stream);
            }

            return add;
        }

        private void PutAddress(Address address, string name)
        {
            var formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, address);
            _db.Put(Encoding.ASCII.GetBytes(name), stream.GetBuffer());
        }

        public long AddTransaction(Transaction transaction)
        {
            _lastTxId++;

            // Update the addresses
            Address from = GetAddress(transaction.FromAddress);
            from.Balance -= transaction.Amount + transaction.Gas * transaction.GasPrice;
            from.TrKeys.Add(_lastTxId);
            PutAddress(from, transaction.FromAddress);

            Address to = GetAddress(transaction.ToAddress);
            to.Balance = transaction.Amount;
            to.TrKeys.Add(_lastTxId);
            PutAddress(to, transaction.ToAddress);

            // Add the transaction
            var formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, transaction);
            _db.Put(BitConverter.GetBytes(_lastTxId), stream.GetBuffer());

            return _lastTxId;
        }
    }
}