using System;
using System.IO;
using System.Numerics;
using EtherChain.Models;
using EtherChain.Services.Sync;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace EtherChain
{
    class Program
    {
        public static DataContext db;

        static void Main(string[] args)
        {
            db = new DataContext();
            EtherSync sync = new EtherSync(db);
            var autoSync = sync.AutoSync();

            // Run the web server
            CreateWebHostBuilder(args).Build().Run();

            // Stop the syncing
            Console.WriteLine("Shutdown the sync.");
            sync.StopAutoSync = true;
            autoSync.Wait();            
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

    }
}
