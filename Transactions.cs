
using System;

namespace blockshift_ib
{
    public class Transactions
    {
        private readonly SimpleLogger log;
        public Transactions()
        {

        }
        public Transactions(SimpleLogger log)
        {
            this.log = log;
        }
        public string Strategy { get; set; }
        public string Symbol { get; set; }
        public string TradeDate { get; set; }
        public string LastTradeSideText { get; set; }
        public string EntryPrice { get; set; }
        public int LastTradeSide { get; set; }

        public static Transactions CheckLastPositionDataFromDB(Transactions transactions, Strategy strategy, SignalFile signalFile, SimpleLogger log)
        {
            string[] dbResults;

            if (signalFile.StrategyName != strategy.StrategyName)
            {
                log.Warning($"Strategy {signalFile.StrategyName} does exist in Position_Data table.");
            }
            else
            {
                dbResults = DBFunctions.GetLastTradeSideFromPositionDataTable(log, strategy);

                transactions.LastTradeSideText = dbResults[1];
                transactions.Strategy = dbResults[2];

                if (transactions.LastTradeSideText == "BUY" && signalFile.StrategyName == transactions.Strategy)
                {
                    transactions.LastTradeSide = 1;
                }
                else if (transactions.LastTradeSideText == "SELL" && signalFile.StrategyName == transactions.Strategy)
                {
                    transactions.LastTradeSide = -1;
                }
                else if (string.IsNullOrEmpty(transactions.LastTradeSideText))
                {
                    transactions.LastTradeSide = 0;
                }
            }
            return transactions;
        }
        public static void CheckLastTrade(SignalFile signalFile, Transactions transactions, SimpleLogger log, Positions positions, Tuple<bool, bool, int> db)
        {
            if (db.Item1)
            {
                log.Warning($"Trying to Exceed Max Position. No order sent for {signalFile.Output}");
            }

            if (signalFile.Signal == 1 && transactions.LastTradeSide == 1 && positions.IBCurrentPosition > 0 && signalFile.StrategyName == transactions.Strategy)
            {
                log.Warning($"Last order sent in db was a BUY for {signalFile.Output}");
            }

            if (signalFile.Signal == -1 && transactions.LastTradeSide == -1 && positions.IBCurrentPosition < 0 && signalFile.StrategyName == transactions.Strategy)
            {
                log.Warning($"Last order sent in db was a SELL for {signalFile.Output}");
            }
        }

    }
}
