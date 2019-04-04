using System;
using System.IO;
using System.Numerics;
using EtherChain.Models;
using EtherChain.Services.Sync;
using ZeroFormatter.Formatters;

namespace EtherChain
{
    class Program
    {
        static void Main(string[] args)
        {
            // Add big integer formatter
            ZeroFormatter.Formatters.Formatter<DefaultResolver, BigInteger>.Register(new BigIntegerFormatter<DefaultResolver>());

            DataContext db = new DataContext();
            EtherSync sync = new EtherSync(db);
            sync.Sync(5000000, 5000100);

            Console.ReadLine();
        }
    }
}
