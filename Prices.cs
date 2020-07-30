using IBApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace blockshift_ib
{
    public class Prices
    {
        private readonly Parameters @params;
        private readonly SimpleLogger log;

        public Prices()
        {
        }
        public Prices(SimpleLogger log, Parameters @params)
        {
            this.log = log;
            this.@params = @params;
        }

        public double BidPrice { get; set; }
        public double AskPrice { get; set; }
        public static string FieldOne { get; set; }
        public static string FieldTwo { get; set; }
        public static bool IBBidPriceFlag { get; set; }
        public static bool IBAskPriceFlag { get; set; }
        public static bool IBDepthBidPriceFlag { get; set; }
        public static bool IBDepthAskPriceFlag { get; set; }
        public static string SideOne { get; set; }
        public static string SideTwo { get; set; }
        public Prices StartListener(EClientSocket clientSocket, Contract contract, Strategy strategy, Prices prices, SimpleLogger log)
        {
            string ticker;
            string strBid;
            string strAsk;

            double bid;
            double ask;

            int maxLoopCount = 30;

            Prices.IBDepthBidPriceFlag = false;
            Prices.IBDepthAskPriceFlag = false;

            if (strategy.Symbol != "MGC")
            {
                int cnt = 0;

                clientSocket.reqMarketDepth(strategy.StrategyId, contract, 1, null);

                while (true)
                {
                    ticker = SignalFile.GetWantedText(Prices.SideOne, "TickerId:");
                    strBid = SignalFile.GetWantedText(Prices.SideOne, "Price:");
                    strAsk = SignalFile.GetWantedText(Prices.SideTwo, "Price:");

                    if (Prices.IBDepthBidPriceFlag && Prices.IBDepthAskPriceFlag)
                    {
                        bid = Convert.ToDouble(strBid);
                        ask = Convert.ToDouble(strAsk);

                        if (bid > 0 && ask > 0)
                        {
                            prices.BidPrice = bid;
                            prices.AskPrice = ask;

                            clientSocket.cancelMktDepth(strategy.StrategyId);
                            log.Info($"Received Market Depth for TickerId:{ticker}, Symbol:{strategy.Symbol}, Bid:{strBid}, Ask:{strAsk}");
                            break;
                        }
                        else
                        {
                            log.Info($"Unable to get Market Depth for TickerId:{ticker}, Market Depth Bid:{strBid}, Ask:{strAsk}");
                            clientSocket.cancelMktDepth(strategy.StrategyId);
                            break;
                        }
                    }
                    if (cnt > maxLoopCount)
                    {
                        clientSocket.cancelMktDepth(strategy.StrategyId);

                        log.Warning($"Unable to get Market Depth for TickerId:{ticker}, Symbol:{strategy.Symbol}, Market Depth Bid:{strBid}, Ask:{strAsk}");
                        break;
                    }

                        log.Info($"Getting Market Depth... {cnt}");
                    cnt ++;
                }
            }

            if (prices.BidPrice <= 0 || prices.AskPrice <= 0)
            {
                int cnt = 0;

                clientSocket.reqMktData(strategy.StrategyId, contract, "", false, null);

                Prices.IBBidPriceFlag = false;
                Prices.IBAskPriceFlag = false;

                while (true)
                {
                    ticker = SignalFile.GetWantedText(Prices.SideOne, "TickerId:");
                    strBid = SignalFile.GetWantedText(Prices.FieldOne, "Price:");
                    strAsk = SignalFile.GetWantedText(Prices.FieldTwo, "Price:");

                    bid = Convert.ToDouble(strBid);
                    ask = Convert.ToDouble(strAsk);

                    if (Prices.IBBidPriceFlag && Prices.IBAskPriceFlag)
                    {
                        if (bid > 0 && ask > 0)
                        {
                            prices.BidPrice = bid;
                            prices.AskPrice = ask;

                            clientSocket.cancelMktData(strategy.StrategyId);
                            log.Info($"Received Snapshot Data for TickerId:{ticker}, Symbol:{strategy.Symbol}, Bid:{strBid}, Ask:{strAsk}");
                            break;
                        }
                        else
                        {
                            log.Info($"Unable to get Snapshot Data for TickerId:{ticker}, Symbol:{strategy.Symbol}, Bid:{strBid}, Ask:{strAsk}");
                            clientSocket.cancelMktDepth(strategy.StrategyId);
                            break;
                        }
                    }
                    if (cnt >= maxLoopCount)
                    {
                        clientSocket.cancelMktData(strategy.StrategyId);

                        log.Warning($"Unable to get Snapshot Data for TickerId:{ticker}, Symbol:{strategy.Symbol}, Bid:{strBid}, Ask:{strAsk}");
                        break;
                    }

                    log.Info($"Getting Price Snapshot Data... {cnt}");
                    cnt ++;
                }
            }
            return prices;
        }
    }
}