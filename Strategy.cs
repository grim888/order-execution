using System.Collections.Generic;
using System.Linq;

namespace blockshift_ib
{
    public class Strategy
    {
        private readonly SimpleLogger log;
        public Strategy()
        {

        }
        public Strategy(SimpleLogger log)
        {
            this.log = log;
        }
        public int StrategyId { get; set; }
        public string StrategyName { get; set; }
        public string Symbol { get; set; }
        public string OrderType { get; set; }
        public int MaxPositionForSymbol { get; set; }
        public double StrategyMaxPosition { get; set; }
        public int LotSize { get; set; }
        public bool Status { get; set; }

        int Id { get; set; }
        string Name { get; set; }
        string Ticker { get; set; }
        string OType { get; set; }
        int MaxPosition { get; set; }
        int Contracts { get; set; }
        bool Enabled { get; set; }

        public static IList<Strategy> StrategyList = new List<Strategy>()
        {
            new Strategy() { Id = 101, Name = "QM MACD 5 Min", Ticker = "QM", OType = "LMT", Contracts = 0, MaxPosition = 1, Enabled = false } ,
            new Strategy() { Id = 102, Name = "QM TMA Slope Kase Bar 0.20", Ticker = "QM", OType = "LMT", Contracts = 1, MaxPosition = 1, Enabled = true } ,
            //new Strategy() { Id = 103, Name = "ES TMA_Slope Kase Bar 3.00", symbol = "ES", OType = "LMT", Contracts = 2, MaxPosition = 2, Enabled = true  } ,
            new Strategy() { Id = 104, Name = "MES TMA_Slope Kase Bar 3.75", Ticker = "MES", OType = "LMT", Contracts = 1, MaxPosition = 2, Enabled = false  },
            new Strategy() { Id = 103, Name = "MES Supply Demand 60min", Ticker = "MES", OType = "LMT", Contracts = 1, MaxPosition = 2, Enabled = true },
            new Strategy() { Id = 105, Name = "MGC TMA_Slope Kase Bar 2.00", Ticker = "MGC", OType = "LMT", Contracts = 2, MaxPosition = 2, Enabled = false }
        };
        public static void GetStrategyId(FuturesContract futuresContract, Strategy strategy, SignalFile signalFile, SimpleLogger log)
        {
            var strategyInfo = from s in StrategyList
                               where s.Name == signalFile.StrategyName
                               select new { s.Id, s.Name, s.Ticker, s.OType, s.Contracts, s.MaxPosition, s.Enabled };
            
            var symbolMaxPosition = from s in StrategyList
                                    where s.Ticker == futuresContract.Symbol
                                    group s by s.Ticker into sg
                                    orderby sg.Key
                                    select new { sg.Key, Total = sg.Sum(x => x.MaxPosition) };

            foreach (var g in strategyInfo)
            {
                strategy.StrategyId = g.Id;
                strategy.Symbol = g.Ticker;
                strategy.StrategyName = g.Name;
                strategy.OrderType = g.OType;
                strategy.LotSize = g.Contracts;
                strategy.StrategyMaxPosition = g.MaxPosition;
                strategy.Status = g.Enabled;

                foreach (var c in symbolMaxPosition)
                {
                    strategy.MaxPositionForSymbol = c.Total;
                }
            }

            log.Info($"[STRATEGY INFO] StrategyId:{strategy.StrategyId} StrategyName:{strategy.StrategyName}");
        }
    }
}
