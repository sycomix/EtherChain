using System;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using EtherChain.Models;
using Nethereum.Web3;

namespace EtherChain.Services.Sync
{
    public class EtherSync
    {
        private DataContext _db;
        private BigInteger _lastSyncedBlock;
        public bool StopAutoSync = false;

        public EtherSync(DataContext db)
        {
            _db = db;
            var lastblock = _db.Get("lastblock", "ETH");
            _lastSyncedBlock = string.IsNullOrEmpty(lastblock)? 0: BigInteger.Parse(lastblock);
            if (_lastSyncedBlock == 0)
            {
                if (AppSettings.StartBlock == 0)
                {
                    Web3 web3 = new Web3("https://mainnet.infura.io");
                    var blockCount = web3.Eth.Blocks.GetBlockNumber.SendRequestAsync().Result;
                    _lastSyncedBlock = blockCount.Value - 64;
                }
                else
                    _lastSyncedBlock = AppSettings.StartBlock;
            }

            Console.WriteLine(Directory.GetCurrentDirectory());
            string dir = Directory.GetCurrentDirectory();
            dir = dir.Substring(0, dir.IndexOf("EtherChain") + 10);
            dir += "\\ethereumetl";
            Console.WriteLine("ethereumetl directory = " + dir);
        }

        public void Sync(BigInteger fromBlock, BigInteger toBlock)
        {
            Console.WriteLine($"Getting transactions from block {fromBlock} to {toBlock}");
            // Run etl to extract data
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            string dir = Directory.GetCurrentDirectory();
            dir = dir.Substring(0, dir.IndexOf("EtherChain") + 10);
            dir += "\\ethereumetl";
            startInfo.WorkingDirectory = dir;
            startInfo.Arguments = $"/C ethereumetl.exe export_blocks_and_transactions --start-block {fromBlock} --end-block {toBlock} --provider-uri https://mainnet.infura.io --transactions-output tx.csv";
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            if (!File.Exists(dir + "\\tx.csv"))
            {
                Console.WriteLine("ERROR: tx.csv file not created.");
                return;
            }

            // Open the tx.csv file and extract it.
            var lines = File.ReadAllLines(dir + "\\tx.csv");
            File.Delete(dir + "\\tx.csv");
            int c = 2;
            while (c < lines.Length)
            {
                var data = lines[c].Split(',');
                /*
                0 hash hex_string
                1 nonce bigint
                2 block_hash hex_string
                3 block_number bigint
                4 transaction_index bigint
                5 from_address address
                6 to_address address
                7 value numeric
                8 gas bigint
                9 gas_price bigint
                10 input hex_string
                */
                Transaction tr = new Transaction
                {
                    Amount = BigInteger.Parse(data[7]),
                    BlockHash = data[2],
                    FromAddress = data[5],
                    Gas = BigInteger.Parse(data[8]),
                    GasPrice = BigInteger.Parse(data[9]),
                    Hash = data[0],
                    ToAddress = data[6],
                    Nonce = BigInteger.Parse(data[1])
                };

                _db.AddTransaction(tr, "ETH");

                c += 2;
            }

            // Update last synced block
            _lastSyncedBlock = toBlock;
            _db.Put("lastblock", _lastSyncedBlock.ToString(), "ETH");

            Console.WriteLine($"Done getting transactions from block {fromBlock} to {toBlock}");
        }

        public async Task AutoSync()
        {
            Web3 web3 = new Web3("https://mainnet.infura.io");
            while (!StopAutoSync)
            {
                // Get the latest block count.
                var blockCount = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                if (_lastSyncedBlock == blockCount.Value)
                {
                    System.Threading.Thread.Sleep(10000);
                    continue;
                }

                // apply the block chunks
                BigInteger fromBlock = _lastSyncedBlock + 1;
                BigInteger toBlock = blockCount.Value;
                if (fromBlock >= 2000000 && fromBlock <= 4000000)
                {
                    toBlock = fromBlock; // The blocks from 2000000 to 4000000 is very big so we get blocks one by one.
                }
                else
                {
                    if (toBlock - fromBlock > AppSettings.BlockChunk)
                    {
                        toBlock = fromBlock + AppSettings.BlockChunk;
                    }
                }

                Sync(fromBlock, toBlock);
            }
            Console.WriteLine("AutoSync stopped.");
        }
    }
}
