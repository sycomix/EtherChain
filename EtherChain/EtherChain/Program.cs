using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using EtherChain.Models;
using EtherChain.Services.Sync;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Nethereum.Web3;

namespace EtherChain
{
    class Program
    {
        public static DataContext db;

        static void Main(string[] args)
        {
            db = new DataContext();
            EtherSync syncEth = null,
                syncEtc = null,
                syncErc20 = null;
            Task autoSyncEth = null,
                autoSyncEtc = null,
                autoSyncErc20 = null;

            if (AppSettings.SyncEthereum)
            {
                syncEth = new EtherSync(db, "ETH");
                autoSyncEth = syncEth.AutoSync();
            }
            if (AppSettings.SyncEthereumClassic)
            {
                syncEtc = new EtherSync(db, "ETC");
                autoSyncEtc = syncEtc.AutoSync();
            }
            if (AppSettings.SyncErc20)
            {
                syncErc20 = new EtherSync(db, "ERC20");
                autoSyncErc20 = syncErc20.AutoSync();
            }

            // Run the web server
            CreateWebHostBuilder(args).Build().Run();

            // Stop the syncing
            Console.WriteLine("Shutdown the sync.");
            if (syncEth != null)
                syncEth.StopAutoSync = true;
            autoSyncEth?.Wait();
            if (syncEtc != null)
                syncEtc.StopAutoSync = true;
            autoSyncEtc?.Wait();
            if (syncErc20 != null)
                syncErc20.StopAutoSync = true;
            autoSyncErc20?.Wait();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

    }
}
