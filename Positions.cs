using System;
using System.Collections.Generic;
using System.Linq;

namespace blockshift_ib
{
    public class Positions
    {
        private readonly Parameters @params;
        private readonly SimpleLogger log;
        private readonly Slack slack = new Slack();

        public Positions()
        {
        }
        public Positions(SimpleLogger log, Parameters @params, Slack slack)
        {
            this.log = log;
            this.@params = @params;
            this.slack = slack;
        }
        public string IBSymbol { get; set; }
        public int IBCurrentPosition { get; set; }
        public static string IBExchange { get; set; }
        public static string IBCcy { get; set; }
        public static bool PositionEndFlag { get; set; }
        public static List<string> PositionList { get; set; } = new List<string>();
        public Tuple<bool, int> ComparePositionAtIBandDB(Strategy strategy, SignalFile signalFile, Positions positions, SimpleLogger log, Parameters @params)
        {
            bool dbPosConflict = false;

            int dbPosition = DBFunctions.GetCurrentStrategyPositionFromDB(log, strategy);

            if (dbPosition != positions.IBCurrentPosition && signalFile.StrategyName == strategy.StrategyName)
            {
                string slackMsg = $"*[POSITION CONFLICT]* *{strategy.StrategyName}* curr_Strategy_Position table:*{dbPosition}* and IB:*{IBCurrentPosition}*";

                dbPosConflict = true;

                log.Warning(slackMsg);

                slack.SendMessageToSlack(slackMsg, @params);
            }
            return new Tuple<bool, int>(dbPosConflict, dbPosition);
        }
        private double CalculateMaxPosition(double position, Strategy strategy, SignalFile signalFile, Transactions transactions, SimpleLogger log)
        {
            double calculatedMaxPosition = 0;

            if (signalFile.Signal == transactions.LastTradeSide)
            {
                calculatedMaxPosition = (position + strategy.StrategyMaxPosition);
            }
            else if (signalFile.Signal != transactions.LastTradeSide)
            {
                calculatedMaxPosition = (strategy.StrategyMaxPosition - position);

            }
            else if (transactions.LastTradeSide == 0)
            {
                calculatedMaxPosition = (strategy.StrategyMaxPosition - position);
            }

            log.Warning($"signalFile.Signal:{signalFile.Signal} transactions.LastTradeSide:{transactions.LastTradeSide} position:{position} strategy.StrategyMaxPosition:{strategy.StrategyMaxPosition} calculatedMaxPosition:{calculatedMaxPosition}");

            return calculatedMaxPosition;
        }
        public Tuple<bool, bool, int> GetMaxPosition(Strategy strategy, SignalFile signalFile, Transactions transactions, Positions positions, SimpleLogger log, Parameters @params)
        {
            bool MaxTradeFlag = true;
            bool maxPosFlag;
            double _position;

            Tuple<bool, int> db = ComparePositionAtIBandDB(strategy, signalFile, positions, log, @params);

            bool dbPosConflict = db.Item1;
            int strategyCurrentPosition = db.Item2;
            double calculatedMaxPosition;

            //Converting position to positive number to validate if the number exceeds the max number for the strategy maximum position(s)         
            if (!dbPosConflict)
            {
                _position = (strategyCurrentPosition < 0) ? -1 * strategyCurrentPosition : strategyCurrentPosition;
                calculatedMaxPosition = CalculateMaxPosition(_position, strategy, signalFile, transactions, log);

            }
            else
            {
                _position = (positions.IBCurrentPosition < 0) ? -1 * positions.IBCurrentPosition : positions.IBCurrentPosition;

                if (dbPosConflict)//if dbconflict then the position in the database must be incorrect and last trade was never filled
                {
                    if (positions.IBCurrentPosition > 0)
                    {
                        transactions.LastTradeSide = 1;
                    }
                    else if (positions.IBCurrentPosition < 0)
                    {
                        transactions.LastTradeSide = -1;
                    }

                    strategyCurrentPosition = positions.IBCurrentPosition;

                    log.Warning($"Positions conflict. Using IB Position for last trade side: {transactions.LastTradeSide}");
                }

                calculatedMaxPosition = CalculateMaxPosition(_position, strategy, signalFile, transactions, log);
            }

            if (calculatedMaxPosition > strategy.StrategyMaxPosition && calculatedMaxPosition >= strategy.MaxPositionForSymbol)
            {
                maxPosFlag = true;

                log.Warning($"{signalFile.StrategyName} is trying to Exceed Strategy Max Position of {strategy.StrategyMaxPosition}.");
            }       
            else if (calculatedMaxPosition <= strategy.StrategyMaxPosition)
            {
                maxPosFlag = false;
            }
            else
            {
                maxPosFlag = false;
            }
            log.Warning($"{signalFile.StrategyName} dbPosConflict:{dbPosConflict} MaxTradeFlag:{MaxTradeFlag} calculatedMaxPosition:{calculatedMaxPosition} strategy.StrategyMaxPosition:{strategy.StrategyMaxPosition} strategy.MaxPositionForSymbol:{strategy.MaxPositionForSymbol}");

            if (signalFile.StrategyName == strategy.StrategyName && !maxPosFlag && strategyCurrentPosition != 0)
            {
                switch (signalFile.Signal)
                {
                    case 1:
                        if (strategyCurrentPosition < 0)
                        {
                            MaxTradeFlag = false;
                        }
                        else if (strategyCurrentPosition > 0 && _position <= strategy.MaxPositionForSymbol)
                        {
                            MaxTradeFlag = false;
                        }
                        break;

                    case -1:
                        if (strategyCurrentPosition > 0)
                        {
                            MaxTradeFlag = false;
                        }
                        else if (strategyCurrentPosition < 0 && _position <= strategy.MaxPositionForSymbol)
                        {
                            MaxTradeFlag = false;
                        }
                        break;
                }
            }
            else if (strategyCurrentPosition == 0)
            {
                MaxTradeFlag = false;
            }

            return new Tuple<bool, bool, int>(MaxTradeFlag, dbPosConflict, strategyCurrentPosition);
        }
        public static void LoadPositions(Strategy strategy, Positions positions, SimpleLogger log)
        {
            foreach (string line in Positions.PositionList)
            {
                string symbol = SignalFile.GetWantedText(line, "Symbol:");

                if (strategy.Symbol == symbol)
                {
                    positions.IBSymbol = strategy.Symbol;
                    string pos = SignalFile.GetWantedText(line, "Position:");
                    positions.IBCurrentPosition = Convert.ToInt32(pos);
                    break;
                }
            }

            if (string.IsNullOrEmpty(positions.IBSymbol))
            {
                int strategyPosition = 0;//DBFunctions.GetCurrentStrategyPositionFromDB(log, strategy);

                Positions.SetIBPosition(strategy, strategyPosition, positions);

                log.Broker($"IB Position does not exist. Setting position for symbol: {strategy.Symbol}, position: {strategyPosition}");
                //log.Broker($"Using curr_Strategy_Position table for Positions Update. symbol: {strategy.Symbol}, position: {strategyPosition}");
            }
        }
        private static string MicroSymbolMapper(string symbol)
        {
            string mapSymbol;

            switch (symbol)
            {
                case "MGC":
                    mapSymbol = "GC";
                    break;
                //case "MES":
                //    mapSymbol = "ES";
                //    break;
                default:
                    mapSymbol = symbol;
                    break;
            }
            return mapSymbol;
        }
        public static Positions SetIBPosition(Strategy strategy, int strategyPosition, Positions positions)
        {
            positions.IBSymbol = strategy.Symbol;
            positions.IBCurrentPosition = strategyPosition;

            return positions;
        }
    }
}