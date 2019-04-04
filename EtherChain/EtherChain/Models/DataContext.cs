using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using RocksDbSharp;
using ZeroFormatter;

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
            // Update the balances
            Address add = new Address
            {
                Balance = 0,
                TrKeys = new List<long>()
            };
            var fromDataBytes = _db.Get(Encoding.ASCII.GetBytes(address));
            if (fromDataBytes != null)
            {
                add = ZeroFormatterSerializer.Deserialize<Address>(fromDataBytes);
            }

            return add;
        }

        private void PutAddress(Address address, string name)
        {
            _db.Put(Encoding.ASCII.GetBytes(name), ZeroFormatterSerializer.Serialize(address));
        }

        public long AddTransaction(Transaction transaction)
        {
            _lastTxId++;

            // Update the addresses
            Address from = GetAddress(transaction.FromAddress);
            Console.WriteLine(transaction.FromAddress + ": " + from.Balance);
            from.Balance -= transaction.Amount + transaction.Gas * transaction.GasPrice;
            from.TrKeys.Add(_lastTxId);
            PutAddress(from, transaction.FromAddress);

            if (!string.IsNullOrEmpty(transaction.ToAddress))
            {
                Address to = GetAddress(transaction.ToAddress);
                Console.WriteLine(transaction.ToAddress + ": " + to.Balance);
                to.Balance += transaction.Amount;
                to.TrKeys.Add(_lastTxId);
                PutAddress(to, transaction.ToAddress);
            }

            // Add the transaction
            _db.Put(BitConverter.GetBytes(_lastTxId), ZeroFormatterSerializer.Serialize(transaction));

            return _lastTxId;
        }

        public void Put(string key, string value)
        {
            _db.Put(key, value);
        }

        public string Get(string key)
        {
            return _db.Get(key);
        }
    }
}