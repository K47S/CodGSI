using System;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cod2GSI
{
    class Program
    {
        static void Main(string[] args)
        {
            var suh = new ScoreUpdateHandler();
            suh.Start();

            Console.Write("Press any key to exit... ");
            Console.ReadKey();

        }

    }
}
