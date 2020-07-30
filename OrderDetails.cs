using System;
using System.Collections.Generic;
using System.Linq;

namespace blockshift_ib
{
    public class OrderDetails
    {
        public OrderDetails()
        {
        }
        public string OrderStatus { get; set; }
        public string OrderId { get; set; }
        public int LotSize { get; set; }
        public string SideText { get; set; }
        public string BuyOrSell { get; set; }

        public static int NextOrderId { get; set; }
        public static bool OrderStatusFlag { get; set; }

        public static OrderDetails GetLotSize(Tuple<bool, bool, int> db, OrderDetails orderDetails, Strategy strategy, SignalFile signalFile, Positions positions)
        {
            int signalDirection = signalFile.Signal;
            //int tradeDirection;

            int strategyCurrentPosition = db.Item3;

            //Trade direction         
            //if (strategyCurrentPosition > 0 && signalDirection < 0)
            //{
            //    tradeDirection = -1;
            //}
            //else if (strategyCurrentPosition < 0 && signalDirection > 0)
            //{
            //    tradeDirection = 1;
            //}
            //else if (strategyCurrentPosition < 0 && signalDirection < 0)
            //{
            //    tradeDirection = -1;
            //}
            //else if (strategyCurrentPosition > 0 && signalDirection > 0)
            //{
            //    tradeDirection = 1;
            //}
            //else
            //{
            //    tradeDirection = signalFile.Signal;
            //}

            if (positions.IBSymbol == strategy.Symbol && signalFile.StrategyName == strategy.StrategyName)
            {
                switch (signalFile.Exit)
                {
                    case "0":
                        if (strategyCurrentPosition > 0 && strategyCurrentPosition != 0 && signalDirection < 0)
                        {
                            orderDetails.LotSize = (strategy.LotSize * 2);
                        }
                        else if (strategyCurrentPosition < 0 && strategyCurrentPosition != 0 && signalDirection > 0)
                        {
                            orderDetails.LotSize = (strategy.LotSize * 2);
                        }
                        else if (strategyCurrentPosition != 0)
                        {
                            orderDetails.LotSize = strategy.LotSize;
                        }
                        else
                        {
                            orderDetails.LotSize = strategy.LotSize;
                        }
                        break;

                    case "1":
                        //if (strategyCurrentPosition > 0 && signalDirection != tradeDirection)
                        //{
                        //    orderDetails.LotSize = strategy.LotSize;
                        //}
                        //else if (strategyCurrentPosition < 0 && signalDirection != tradeDirection)
                        //{
                        //    orderDetails.LotSize = strategy.LotSize;
                        //}
                        //else
                        //{
                            orderDetails.LotSize = strategy.LotSize;
                        //}
                        break;
                }
            }
            else if (string.IsNullOrEmpty(positions.IBSymbol) && strategyCurrentPosition == 0)
            {
                orderDetails.LotSize = strategy.LotSize;
            }

            GetOrderSide(signalFile, orderDetails);

            return orderDetails;
        }
        private static OrderDetails GetOrderSide(SignalFile signalFile, OrderDetails orderDetails)
        {
            if (signalFile.Signal == -1)
            {
                orderDetails.SideText = "SHORT";
                orderDetails.BuyOrSell = "SELL";
            }
            else
            {
                orderDetails.SideText = "LONG";
                orderDetails.BuyOrSell = "BUY";
            }
            return orderDetails;
        }
        public class OrderStatusList
        {
            public static List<string> DataList { get; set; } = new List<string>();

            public void AddData()
            {
                DataList.Add("");
            }
            public void ClearData()
            {
                DataList.Clear();
            }
            public void Count()
            {
                DataList.Count();
            }
        }
        public class OrderIdList
        {
            public static List<string> DataList { get; set; } = new List<string>();

        }
    }

    public class OpenOrder
    {
        public OpenOrder()
        {
        }
        public static string OrderState { get; set; }
        public static string OrderId { get; set; }
        public static string Symbol { get; set; }
        public static string Action { get; set; }
        public static bool OpenOrderFlag { get; set; }
        public class OpenOrderList
        {
            public static List<string> DataList { get; set; } = new List<string>();

            public void AddData()
            {
                DataList.Add("");
            }
            public void ClearData()
            {
                DataList.Clear();
            }
            public void Count()
            {
                DataList.Count();
            }
        }
        public class MessageOrderList
        {
            public static List<int> DataList { get; set; } = new List<int>();

            public void AddData()
            {
                DataList.Add(0);
            }
            public void ClearData()
            {
                DataList.Clear();
            }
            public void Count()
            {
                DataList.Count();
            }
        }
    }

}
