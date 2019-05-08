using System;
using System.Collections.Generic;
using System.Text;
using RocksDbSharp;
using MessagePack;
using System.Numerics;

namespace EtherChain.Models
{
    public class DataContext: IDisposable
    {
        private readonly RocksDb _db;
        private readonly Dictionary<string, long> _lastTxIdDictionary;
        private readonly Dictionary<string, ColumnFamilyHandle> _columnFamiliesDictionary;

        public DataContext(string path = "ether.db")
        {           
            _lastTxIdDictionary = new Dictionary<string, long>();

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
                {
                    foreach (var col in cols)
                        _columnFamiliesDictionary.Add(col, _db.GetColumnFamily(col));
                    foreach (var col in cols)
                    {
                        // Load latest transaction Ids
                        if (!col.Contains(':') && col != "default")
                        {
                            var lastTxIdstr = _db.Get("lastTxId", GetColFamily(col + ":lastid"));
                            _lastTxIdDictionary.Add(col,
                                string.IsNullOrEmpty(lastTxIdstr) ? 0 : long.Parse(lastTxIdstr));
                        }
                    }
                }
            }
        }


        public void Dispose()
        {
            // Save the last ids to database
            foreach (var id in _lastTxIdDictionary)
            {
                _db.Put("lastTxId", id.Value.ToString(), GetColFamily(id.Key + ":lastid"));
            }

            _db?.Dispose();
        }

        private ColumnFamilyHandle GetColFamily(string name)
        {
            if (_columnFamiliesDictionary.ContainsKey(name))
                return _columnFamiliesDictionary[name];

            var cf = _db.CreateColumnFamily(new ColumnFamilyOptions(), name);
            _columnFamiliesDictionary.Add(name, cf);          

            return cf;
        }

        private long GetLastTxId(string name)
        {
            if (!_lastTxIdDictionary.ContainsKey(name))
                _lastTxIdDictionary.Add(name, 0);

            return ++_lastTxIdDictionary[name];
        }

        public void CreateCheckPoint()
        {
            _db.Checkpoint();
        }

        public Address GetAddress(string address, string coinName)
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
            var fromDataBytes = _db.Get(Encoding.ASCII.GetBytes(address), GetColFamily(coinName));
            if (fromDataBytes != null)
            {
                add = LZ4MessagePackSerializer.Deserialize<Address>(fromDataBytes);
            }

            return add;
        }

        private void PutAddress(Address address, string name, string coinName)
        {
            if (AppSettings.TransactionLimit > 0 && address.TrKeys.Count > AppSettings.TransactionLimit)
            {
                // Remove the earliest transaction from database.
                var txId = address.TrKeys[0];
                _db.Remove(LZ4MessagePackSerializer.Serialize(txId), GetColFamily(coinName + ":tx"));
                address.TrKeys.RemoveAt(0);
            }

            _db.Put(Encoding.ASCII.GetBytes(name), LZ4MessagePackSerializer.Serialize(address), 
                GetColFamily(coinName));
        }

        public long AddTransaction(Transaction transaction, string coinName, 
            ref Block block)
        {
            var lastTxId = GetLastTxId(coinName);

            // Update the addresses
            Address from = GetAddress(transaction.FromAddress, coinName);
            from.Balance -= transaction.Amount + transaction.Gas * transaction.GasPrice;
            from.TrKeys.Add(lastTxId);
            from.Nonce = transaction.Nonce;
            PutAddress(from, transaction.FromAddress, coinName);

            if (!string.IsNullOrEmpty(transaction.ToAddress))
            {
                Address to = GetAddress(transaction.ToAddress, coinName);
                to.Balance += transaction.Amount;
                to.TrKeys.Add(lastTxId);
                PutAddress(to, transaction.ToAddress, coinName);
            }

            // Add the transaction
            _db.Put(LZ4MessagePackSerializer.Serialize(lastTxId), 
                LZ4MessagePackSerializer.Serialize(transaction), GetColFamily(coinName + ":tx"));

            // Add transaction to the block
            if (!block.TransactionIds.ContainsKey(coinName))
                block.TransactionIds.Add(coinName, new List<long>());
            block.TransactionIds[coinName].Add(lastTxId);

            return lastTxId;
        }

        public long AddTransaction(Erc20Transaction transaction, string coinName,
            ref Block block)
        {
            var lastTxId = GetLastTxId(coinName);

            // Update the addresses
            Address from = GetAddress(transaction.FromAddress, coinName);
            from.Balance -= transaction.Amount;
            from.TrKeys.Add(lastTxId);
            PutAddress(from, transaction.FromAddress, coinName);

            if (!string.IsNullOrEmpty(transaction.ToAddress))
            {
                Address to = GetAddress(transaction.ToAddress, coinName);
                to.Balance += transaction.Amount;
                to.TrKeys.Add(lastTxId);
                PutAddress(to, transaction.ToAddress, coinName);
            }

            // Add the transaction
            _db.Put(LZ4MessagePackSerializer.Serialize(lastTxId),
                LZ4MessagePackSerializer.Serialize(transaction), GetColFamily(coinName + ":tx"));

            // Add transaction to the block
            if (!block.TransactionIds.ContainsKey(coinName))
                block.TransactionIds.Add(coinName, new List<long>());
            block.TransactionIds[coinName].Add(lastTxId);

            return lastTxId;
        }

        public void AddBlock(BigInteger num, Block block, string BlockChain)
        {
            _db.Put(LZ4MessagePackSerializer.Serialize(num), LZ4MessagePackSerializer.Serialize(block),
                GetColFamily(BlockChain));
            _db.Remove(LZ4MessagePackSerializer.Serialize(num - 11), GetColFamily(BlockChain));
        }

        public Block GetBlock(BigInteger number, string BlockChain)
        {
            var blockBytes = _db.Get(LZ4MessagePackSerializer.Serialize(number), GetColFamily(BlockChain));
            return blockBytes == null ? null : LZ4MessagePackSerializer.Deserialize<Block>(blockBytes);
        }

        public Transaction GetTransaction(long id, string coinName)
        {
            var trBytes = _db.Get(LZ4MessagePackSerializer.Serialize(id), GetColFamily(coinName + ":tx"));
            return trBytes == null ? null : LZ4MessagePackSerializer.Deserialize<Transaction>(trBytes);
        }

        private void RollBlockTransaction(long txId, string coinName)
        {
            var transaction = GetTransaction(txId, coinName);

            // Update the addresses
            Address from = GetAddress(transaction.FromAddress, coinName);
            from.Balance += transaction.Amount;
            from.TrKeys.Remove(txId);
            PutAddress(from, transaction.FromAddress, coinName);

            if (!string.IsNullOrEmpty(transaction.ToAddress))
            {
                Address to = GetAddress(transaction.ToAddress, coinName);
                to.Balance -= transaction.Amount;
                to.TrKeys.Remove(txId);
                PutAddress(to, transaction.ToAddress, coinName);
            }

            // Remove the transaction
            _db.Remove(LZ4MessagePackSerializer.Serialize(txId), GetColFamily(coinName + ":tx"));

        }

        public void RollBackBlock(BigInteger number, string blockChain)
        {
            var block = GetBlock(number, blockChain);
            foreach (var trs in block.TransactionIds)
            {
                foreach (var trId in trs.Value)
                {
                    RollBlockTransaction(trId, trs.Key);
                }
            }

            // Delete the block
            _db.Remove(LZ4MessagePackSerializer.Serialize(number));
        }

        public void Put(string key, string value, string coinName)
        {
            _db.Put(key, value, GetColFamily(coinName));
        }

        public string Get(string key, string coinName)
        {
            return _db.Get(key, GetColFamily(coinName));
        }
    }
}
