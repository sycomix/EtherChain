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
            WaitForSync(ref autoSyncEth);
            if (syncEtc != null)
                syncEtc.StopAutoSync = true;
            WaitForSync(ref autoSyncEtc);
            if (syncErc20 != null)
                syncErc20.StopAutoSync = true;
            WaitForSync(ref autoSyncErc20);

            // Dispose the database
            db.Dispose();
            Console.WriteLine("Shutdown was successful.");
        }

        private static void WaitForSync(ref Task sync)
        {
            if (sync == null)
                return;

            try
            {
                sync.Wait();
            }
            catch (AggregateException e)
            {
                Console.WriteLine("\nAggregateException thrown with the following inner exceptions:");
                // Display information about each exception. 
                foreach (var v in e.InnerExceptions)
                {
                    if (v is TaskCanceledException)
                        Console.WriteLine("   TaskCanceledException: Task {0}",
                            sync.Id);
                    else
                        Console.WriteLine("   Exception: {0}", v.GetType().Name);
                }
                Console.WriteLine();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

    }
}
