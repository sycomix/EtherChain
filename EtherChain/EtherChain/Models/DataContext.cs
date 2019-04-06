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
        private readonly Dictionary<string, ColumnFamilyHandle> _columnFamiliesDictionary;

        public DataContext(string path = "ether.db")
        {           
            var options = new DbOptions()
                .SetCreateIfMissing(true)
                .SetCreateMissingColumnFamilies(true);

            // Create column families
            IEnumerable<string> cols = null;
            ColumnFamilies columnFamilies = new ColumnFamilies();
            try
            {
                cols = RocksDb.ListColumnFamilies(options, path);
                foreach (var col in cols)
                {
                    columnFamilies.Add(col, new ColumnFamilyOptions());
                }
            }
            catch (Exception e)
            {
                // Database not exist nothing todo
            }
            finally
            {
                _db = RocksDb.Open(options, path, columnFamilies);

                // Load column families to the dictionary
                _columnFamiliesDictionary = new Dictionary<string, ColumnFamilyHandle>();
                if (cols != null)
                    foreach (var col in cols)
                    {
                        _columnFamiliesDictionary.Add(col, _db.GetColumnFamily(col));
                    }

                var lastTxIdstr = _db.Get("lastTxId", GetColFamily("ETH:tx"));
                _lastTxId = string.IsNullOrEmpty(lastTxIdstr) ? 0 : long.Parse(lastTxIdstr);
            }
        }

        private ColumnFamilyHandle GetColFamily(string name)
        {
            if (_columnFamiliesDictionary.ContainsKey(name))
                return _columnFamiliesDictionary[name];

            var cf = _db.CreateColumnFamily(new ColumnFamilyOptions(), name);
            _columnFamiliesDictionary.Add(name, cf);          

            return cf;
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
            if (string.IsNullOrEmpty(address))
            {
                Console.WriteLine("Empty from address");
                return add;
            }
            var fromDataBytes = _db.Get(Encoding.ASCII.GetBytes(address), GetColFamily("ETH"));
            if (fromDataBytes != null)
            {
                add = ZeroFormatterSerializer.Deserialize<Address>(fromDataBytes);
            }

            return add;
        }

        private void PutAddress(Address address, string name)
        {
            if (AppSettings.TransactionLimit > 0 && address.TrKeys.Count > AppSettings.TransactionLimit)
            {
                // Remove the earliest transaction from database.
                var txId = address.TrKeys[0];
                _db.Remove(ZeroFormatterSerializer.Serialize(txId), GetColFamily("ETH:tx"));
                address.TrKeys.RemoveAt(0);
            }

            _db.Put(Encoding.ASCII.GetBytes(name), ZeroFormatterSerializer.Serialize(address), 
                GetColFamily("ETH"));
        }

        public long AddTransaction(Transaction transaction)
        {
            _lastTxId++;

            // Update the addresses
            Address from = GetAddress(transaction.FromAddress);
            from.Balance -= transaction.Amount + transaction.Gas * transaction.GasPrice;
            from.TrKeys.Add(_lastTxId);
            from.Nonce = transaction.Nonce;
            PutAddress(from, transaction.FromAddress);

            if (!string.IsNullOrEmpty(transaction.ToAddress))
            {
                Address to = GetAddress(transaction.ToAddress);
                to.Balance += transaction.Amount;
                to.TrKeys.Add(_lastTxId);
                PutAddress(to, transaction.ToAddress);
            }

            // Add the transaction
            _db.Put(ZeroFormatterSerializer.Serialize(_lastTxId), 
                ZeroFormatterSerializer.Serialize(transaction), GetColFamily("ETH:tx"));
            _db.Put("lastTxId", _lastTxId.ToString(), GetColFamily("ETH:tx"));

            return _lastTxId;
        }

        public Transaction GetTransaction(long id)
        {
            var trBytes = _db.Get(ZeroFormatterSerializer.Serialize(id), GetColFamily("ETH:tx"));
            return trBytes == null ? null : ZeroFormatterSerializer.Deserialize<Transaction>(trBytes);
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