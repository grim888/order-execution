using IBApi;
using System;
using System.IO;
using System.Threading;

namespace blockshift_ib
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            EWrapperImpl ibClient = new EWrapperImpl();
            EClientSocket clientSocket = ibClient.ClientSocket;


            Console.ForegroundColor = ConsoleColor.DarkGreen;
            string logo = @"
 /$$$$$$$  /$$                     /$$        /$$$$$$                      /$$   /$$               /$$       /$$$$$$                        
| $$__  $$| $$                    | $$       /$$__  $$                    |__/  | $$              | $$      |_  $$_/                        
| $$  \ $$| $$  /$$$$$$   /$$$$$$$| $$   /$$| $$  \__/  /$$$$$$   /$$$$$$  /$$ /$$$$$$    /$$$$$$ | $$        | $$   /$$$$$$$   /$$$$$$$    
| $$$$$$$ | $$ /$$__  $$ /$$_____/| $$  /$$/| $$       |____  $$ /$$__  $$| $$|_  $$_/   |____  $$| $$        | $$  | $$__  $$ /$$_____/    
| $$__  $$| $$| $$  \ $$| $$      | $$$$$$/ | $$        /$$$$$$$| $$  \ $$| $$  | $$      /$$$$$$$| $$        | $$  | $$  \ $$| $$          
| $$  \ $$| $$| $$  | $$| $$      | $$_  $$ | $$    $$ /$$__  $$| $$  | $$| $$  | $$ /$$ /$$__  $$| $$        | $$  | $$  | $$| $$          
| $$$$$$$/| $$|  $$$$$$/|  $$$$$$$| $$ \  $$|  $$$$$$/|  $$$$$$$| $$$$$$$/| $$  |  $$$$/|  $$$$$$$| $$       /$$$$$$| $$  | $$|  $$$$$$$ /$$
|_______/ |__/ \______/  \_______/|__/  \__/ \______/  \_______/| $$____/ |__/   \___/   \_______/|__/      |______/|__/  |__/ \_______/|__/
                                                                | $$                                                                        
                                                                | $$                                                                        
                                                                |__/                                                                        

                            ";
            Console.WriteLine(logo);
            Console.ResetColor();

            Console.Write("BlockShift Trading App");
            Console.Write("\n");
            Console.Write("DISCLAIMER: USING THIS SOFTWARE AT MY OWN RISK\n");
            Console.Write("\n");

            try
            {
                ConsoleWindow.QuickEditMode(false);

                Parameters @params = new Parameters(Path.Combine(Environment.CurrentDirectory, "config.conf"));

                SimpleLogger log = new SimpleLogger();

                Slack slack = new Slack(@params);

                Prices prices = new Prices(log, @params);

                FuturesContract futures = new FuturesContract(@params);

                Strategy strategy = new Strategy(log);

                Broker broker = new Broker(clientSocket, log, @params, slack);

                Positions positions = new Positions(log, @params, slack);

                Transactions transactions = new Transactions(log);

                Controller ctl = new Controller(clientSocket, log, @params, strategy, broker, positions, futures, prices, transactions, slack);

               broker.ConnectionToBroker(strategy, positions);

                ctl.Run();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message.ToString());
                Console.Write(ex.StackTrace.ToString());
            }


            Console.ReadLine();
        }


    }
}
