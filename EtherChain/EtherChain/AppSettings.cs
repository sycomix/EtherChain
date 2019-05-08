using System.Numerics;

namespace EtherChain
{
    public static class AppSettings
    {
        public static BigInteger StartBlock = 0; // 0 means start the block scan from latest block - 64.
                                                // Other values means start the block scan from this block number
                                               // Useful after publishing the production server.
        public static int BlockChunk = 10000; // How many block to get for processing each time.
        public static int TransactionLimit = 100; // The maximum latest transaction that we will hold for each account
                                                 // 0 = unlimited
        public static bool SyncEthereum = true;
        public static bool SyncErc20 = true;
        public static bool SyncEthereumClassic = true;
    }
}