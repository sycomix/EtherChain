using System;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using EtherChain.Models;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;

namespace EtherChain.Services.Sync
{
    public class EtherSync
    {
        private DataContext _db;
        private BigInteger _lastSyncedBlock;
        private string _blockChain;
        public bool StopAutoSync = false;

        private Web3 getWeb3()
        {
            if (_blockChain == "ETH" || _blockChain == "ERC20")
                return new Web3("https://mainnet.infura.io");
            
            return new Web3("https://ethereumclassic.network");
        }

        public EtherSync(DataContext db, string BlockChain)
        {
            _db = db;
            _blockChain = BlockChain;
            var lastblock = _db.Get("lastblock", _blockChain);
            _lastSyncedBlock = string.IsNullOrEmpty(lastblock)? 0: BigInteger.Parse(lastblock);
            if (_lastSyncedBlock == 0)
            {
                if (AppSettings.StartBlock == 0)
                {
                    Web3 web3 = getWeb3();
                    var blockCount = web3.Eth.Blocks.GetBlockNumber.SendRequestAsync().Result;
                    _lastSyncedBlock = blockCount.Value - 64;
                }
                else
                    _lastSyncedBlock = AppSettings.StartBlock;
            }

            Console.WriteLine(Directory.GetCurrentDirectory());
            string dir = Directory.GetCurrentDirectory();
            dir = dir.Substring(0, dir.IndexOf("EtherChain") + 10);
            dir += "\\deps\\ethereum-etl";
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
            dir += "\\deps\\ethereum-etl";
            startInfo.WorkingDirectory = dir;
            if (_blockChain == "ETH")
                startInfo.Arguments =
                    $"/C python ethereumetl.py export_blocks_and_transactions --start-block {fromBlock} --end-block {toBlock} --provider-uri https://mainnet.infura.io --transactions-output tx.csv";
            else if (_blockChain == "ERC20")
                startInfo.Arguments =
                    $"/C python ethereumetl.py export_token_transfers --start-block {fromBlock} --end-block {toBlock} --provider-uri wss://mainnet.infura.io/ws --output erc20.csv -w 1";
            if (_blockChain == "ETC")
                startInfo.Arguments =
                    $"/C python ethereumetl.py export_blocks_and_transactions --start-block {fromBlock} --end-block {toBlock} --provider-uri https://ethereumclassic.network --transactions-output etc.csv";

            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            if (_blockChain == "ETH" && !File.Exists(dir + "\\tx.csv"))
            {
                Console.WriteLine("ERROR: tx.csv file not created.");
                Thread.Sleep(1000);
                return;
            }

            if (_blockChain == "ETC" && !File.Exists(dir + "\\etc.csv"))
            {
                Console.WriteLine("ERROR: etc.csv file not created.");
                Thread.Sleep(1000);
                return;
            }

            if (_blockChain == "ERC20" && !File.Exists(dir + "\\erc20.csv"))
            {
                Console.WriteLine("ERROR: erc20.csv file not created.");
                Thread.Sleep(1000);
                return;
            }
            
            // Open the tx.csv file and extract it.
            string filename = "tx.csv";
            if (_blockChain == "ETC")
                filename = "etc.csv";
            else if (_blockChain == "ERC20")
                filename = "erc20.csv";
            string[] lines = File.ReadAllLines(dir + "\\" + filename);
            File.Delete(dir + "\\" + filename);
            int c = 2;
            Block block = new Block();
            BigInteger blockNo = 0;
            while (c < lines.Length)
            {
                var data = lines[c].Split(',');
                if (_blockChain == "ETH" || _blockChain == "ETC")
                {
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
                        Block = BigInteger.Parse(data[3]),
                        FromAddress = data[5],
                        Gas = BigInteger.Parse(data[8]),
                        GasPrice = BigInteger.Parse(data[9]),
                        Hash = data[0],
                        ToAddress = data[6],
                        Nonce = BigInteger.Parse(data[1])
                    };

                    // Check if we need to create a new block
                    if (string.IsNullOrEmpty(block.Hash))
                    {
                        block.Hash = data[2];
                        blockNo = tr.Block;
                    }
                    else if (blockNo != tr.Block)
                    {
                        _db.AddBlock(blockNo, block, _blockChain);
                        block = new Block();
                        blockNo = tr.Block;
                    }

                    _db.AddTransaction(tr, _blockChain, ref block);
                }
                else if (_blockChain == "ERC20")
                {
                    /*
                       0 token_address	    address
                       1 from_address	    address
                       2 to_address	        address
                       3 value	            numeric
                       4 transaction_hash	hex_string
                       5 log_index	        bigint
                       6 block_number	    bigint
                     */
                    var tr = new Erc20Transaction
                    {
                        Amount = BigInteger.Parse(data[3]),
                        Block = BigInteger.Parse(data[6]),
                        FromAddress = data[1],
                        ToAddress = data[2],
                        Hash = data[4],
                        LogIndex = BigInteger.Parse(data[5])
                    };
                    
                    // Check if we need to create a new block
                    if (string.IsNullOrEmpty(block.Hash))
                    {
                        Web3 web3 = getWeb3();
                        block.Hash = web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(
                            new HexBigInteger(tr.Block)).Result.BlockHash;
                        blockNo = tr.Block;
                    }
                    else if (blockNo != tr.Block)
                    {
                        _db.AddBlock(blockNo, block, _blockChain);
                        block = new Block();
                        blockNo = tr.Block;
                    }

                    _db.AddTransaction(tr, data[0], ref block);
                }

                c += 2;
            }
            _db.AddBlock(blockNo, block, _blockChain);

            // Update last synced block
            _lastSyncedBlock = toBlock;
            _db.Put("lastblock", _lastSyncedBlock.ToString(), _blockChain);

            Console.WriteLine($"{_blockChain}: Done getting transactions from block {fromBlock} to {toBlock}");
        }

        private async Task CheckBlocks()
        {
            BigInteger startBlock = _lastSyncedBlock - 10;
            if (startBlock < 1)
                startBlock = 1;
            BigInteger tempLastBlock = _lastSyncedBlock;
            for (BigInteger i = startBlock; i <= _lastSyncedBlock; i++)
            {
                // Check for hash
                var block = _db.GetBlock(i, _blockChain);
                if (block == null)
                    continue;

                string hash;
                try
                {
                    Web3 web3 = getWeb3();
                    hash = (await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(
                        new HexBigInteger(i))).BlockHash;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return;
                }
                if (block.Hash == hash.ToString())
                    continue;

                // Oh shit block chain fork we have to rollback database.
                Console.WriteLine($"Block roll back from {i}");
                try
                {
                    _db.RollBackBlock(i, _blockChain);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                if (tempLastBlock == _lastSyncedBlock)
                    tempLastBlock = i - 1;
            }
            _lastSyncedBlock = tempLastBlock;
        }

        public async Task AutoSync()
        {
            Web3 web3 = getWeb3();
            StopAutoSync = false;
            while (!StopAutoSync)
            {
                // Get the latest block count.
                var blockCount = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                if (_lastSyncedBlock == blockCount.Value)
                {
                    System.Threading.Thread.Sleep(10000);
                    continue;
                }

                await CheckBlocks();

                // apply the block chunks
                BigInteger fromBlock = _lastSyncedBlock + 1;
                BigInteger toBlock = blockCount.Value;
                if ((fromBlock >= 2000000 && fromBlock <= 4000000 && _blockChain == "ETH") || _blockChain == "ERC20")
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
