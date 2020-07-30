using IBApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace blockshift_ib
{
    public class Controller
    {
        private readonly Parameters @params;
        public SimpleLogger log;
        private readonly EClientSocket clientSocket;
        private readonly Contract contract = new Contract();
        private readonly Broker broker;
        private readonly Strategy strategy;
        private readonly FuturesContract futures;
        private readonly Prices prices;
        private readonly Transactions transactions;
        private readonly Slack slack;
        public List<Order> orders = new List<Order>();

        public Controller()
        {
        }
        public Controller(EClientSocket clientSocket, SimpleLogger log, Parameters @params, Strategy strategy, Broker broker, Positions positions, FuturesContract futures, Prices prices, Transactions transactions, Slack slack)
        {
            this.@params = @params;
            this.log = log;
            this.clientSocket = clientSocket;
            this.strategy = strategy;
            this.broker = broker;
            this.futures = futures;
            this.prices = prices;
            this.transactions = transactions;
            this.slack = slack;
        }
        public void Run()
        {
            // InitializeValues();

            RunMainLoop();

        }
        private bool InitializeValues(Strategy strategy, SignalFile signalFile)
        {
            if (strategy.Symbol == "QM" && strategy.Status) //@params.crudeEnabled)
            {
                return true;
            }
            if (strategy.Symbol == "MES" && strategy.Status) //@params.sp500Enabled)
            {
                return true;
            }
            if (strategy.Symbol == "MGC" && strategy.Status) //@params.goldEnabled)
            {
                return true;
            }

            log.Warning($"{strategy.StrategyName} is disabled.");

            string side = (signalFile.Signal == 1) ? "BUY" : "SELL";
            slack.SendMessageToSlack($"[*{strategy.StrategyName} {side}. Disabled]*\n*{signalFile.LastFileLine}*", @params);
            return false;
        }
        private void RunMainLoop()
        {
            if (!File.Exists(@params.filePath))
            {
                log.Error("ERROR: ");
                log.Error(@params.filePath);
                log.Error(" cannot be open.\n");
                Thread.Sleep(TimeSpan.FromSeconds(60));
            }
            else
            {
                MonitorSignalFile();
            }
        }
        private void MonitorSignalFile()
        {
            long initialFileSize = new FileInfo(@params.filePath).Length;
            long lastReadLength = initialFileSize - 1024;

            if (lastReadLength < 0)
            {
                lastReadLength = 0;
            }

            while (true)
            {
                try
                {
                    long fileSize = new FileInfo(@params.filePath).Length;

                    TimeSpan time = DateTime.Now.TimeOfDay;

                    if (time > new TimeSpan(00, 50, 00) && time < new TimeSpan(00, 50, 15))   //Hours, Minutes, Seconds
                    {
                        break;
                    }

                    if (fileSize > lastReadLength)
                    {
                        using (FileStream fs = new FileStream(@params.filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fs.Seek(lastReadLength, SeekOrigin.Begin);
                            byte[] buffer = new byte[1024];

                            while (true)
                            {
                                DateToday.Today = DateTime.Today.ToString("MM-dd-yyyy");

                                int bytesRead = fs.Read(buffer, 0, buffer.Length);
                                lastReadLength += bytesRead;

                                if (bytesRead == 0)
                                {
                                    break;
                                }

                                string[] lines;
                                List<string> list = new List<string>();
                                FileStream fileStream = new FileStream(@params.filePath, FileMode.Open, FileAccess.Read);

                                using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8))
                                {
                                    string line;
                                    while ((line = streamReader.ReadLine()) != null)
                                    {
                                        list.Add(line);
                                    }
                                }

                                SignalFile signalFile = new SignalFile();

                                lines = list.ToArray();
                                signalFile.LastFileLine = lines.Last().ToString();

                                log.Signal($"[RAW DATA] {signalFile.LastFileLine}");

                                //Parse signal file data for trade validation
                                signalFile = SignalFile.ParseSignalFile(signalFile, log);

                                //Validate signal and process order if valid
                                ValidateParsedDataForTrade(signalFile);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Trace(ex.Message.ToString());
                    log.Trace(ex.StackTrace.ToString());
                }

                Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            //Restart App after IB Trader Workstation does its daily restart
            broker.RestartProgram();
        }
        private void ValidateParsedDataForTrade(SignalFile signalFile)
        {
            const int _max = 1000000;
            Stopwatch s1 = Stopwatch.StartNew();

            DateTime tradeDateTime = Convert.ToDateTime(string.Format("{0:MM-dd-yyyy}", signalFile.TradeDateTime.Trim()), CultureInfo.CurrentCulture);
            string entryDateTime = tradeDateTime.ToString("MM-dd-yyyy");

            if (signalFile.TradeDate == DateToday.Today && entryDateTime == DateToday.Today)
            {
                //Map TS symbol to IB symbol and set contract details
                FuturesContract futuresContract = new FuturesContract();
                futuresContract = futures.GetContractDetails(contract, futuresContract, signalFile);

                //Map TS strategies to strategies defined in application data list
                Strategy strategy = new Strategy(log);
                Strategy.GetStrategyId(futuresContract, strategy, signalFile, log);

                if (InitializeValues(strategy, signalFile))
                {
                    //Check if app is connected to IB Api and get current IB positions   
                    Positions positions = new Positions();
                    broker.UpdateIBpositions(strategy, positions, log);

                    //Check last position for strategy in Position_Data table
                    Transactions transactions = new Transactions();
                    Transactions.CheckLastPositionDataFromDB(transactions, strategy, signalFile, log);

                    //Check if strategy has exceeded maximum position
                    //Tuple MaxTradeFlag, dbPosConflict, strategyCurrentPosition
                    Tuple<bool, bool, int> db = positions.GetMaxPosition(strategy, signalFile, transactions, positions, log, @params);

                    //Display transaction conflict message
                    Transactions.CheckLastTrade(signalFile, transactions, log, positions, db);

                    //Set lot size based on current IB position
                    OrderDetails orderDetails = new OrderDetails();
                    orderDetails = OrderDetails.GetLotSize(db, orderDetails, strategy, signalFile, positions);

                    //Check if a limit order was never filled at the Broker and cancel any open order(s)
                    //Tuple MaxTradeFlag, dbPosConflict, strategyCurrentPosition
                    if (db.Item2) //db.Item1 ||
                    {
                        bool allOpenOrder = false;
                        CancelOpenOrders(strategy, db.Item2, orderDetails, allOpenOrder);
                    }

                    //If trade is for today and less than max position excute order
                    ProcessSignalForTrade(futuresContract, signalFile, orderDetails, strategy, db, log);
                }
            }
            s1.Stop();
            string diag = string.Format("Processed Order in: " + (s1.Elapsed.TotalMilliseconds * 1000000 / _max).ToString("0.00 ns"));
            log.Info(diag);
        }
        private void ProcessSignalForTrade(FuturesContract futuresContract, SignalFile signalFile, OrderDetails orderDetails, Strategy strategy, Tuple<bool, bool, int> db, SimpleLogger log)
        {
            if (!db.Item1 && !@params.demoMode)
            {
                slack.SendMessageToSlack($"[*{strategy.Symbol} {orderDetails.BuyOrSell}]*\n*{signalFile.LastFileLine}*", @params);

                //Cancel any open orders
                bool dbconflict = false;
                bool allOpenOrder = true;
                CancelOpenOrders(strategy, dbconflict, orderDetails, allOpenOrder);

                //Send order to Broker
                double limitPrice = ExecuteOrder(futuresContract, signalFile, orderDetails, strategy, log, db);

                //Used to check order status @ Broker
                //CheckOrderStatus();

                //Update db with executed strategy order
                UpdateDBTables(futuresContract, signalFile, orderDetails, strategy, limitPrice);
            }
        }
        private string OverNight(SignalFile signalFile, Tuple<bool, bool, int> db)
        {
            string orderType = null;

            TimeSpan time = DateTime.Now.TimeOfDay;
            if (db.Item3 != 0)
            {
                if (time > new TimeSpan(00, 00, 00) && time < new TimeSpan(08, 00, 00))   //Hours, Minutes, Seconds
                {
                    orderType = "MKT";
                }
                else if (time > new TimeSpan(22, 00, 00) && time < new TimeSpan(23, 59, 59))
                {
                    orderType = "MKT";
                }
                //else if (time > new TimeSpan(18, 00, 00) && time < new TimeSpan(23, 59, 59))
                //{
                //    orderType = "MKT";
                //}
            }
            return orderType;
        }
        public double GetProfitTicks(Strategy strategy, SignalFile signalFile)
        {
            double profitTicks = 0;

            if (strategy.Symbol == "QM")
            {
                if (signalFile.Adx < 15)
                {
                    profitTicks = .35;
                    return profitTicks;
                }
                else
                {
                    profitTicks = 1.00;
                    return profitTicks;
                }
            }
            else if (strategy.Symbol == "MES")
            {
                if (signalFile.Adx < 15)
                {
                    profitTicks = @params.@sp500ProfitOne;
                    return profitTicks;
                }
                else
                {
                    profitTicks = @params.@sp500ProfitTwo;
                    return profitTicks;
                }
            }
            else if (strategy.Symbol == "MGC")
            {
                if (signalFile.Adx < 15)
                {
                    profitTicks = 5.00;
                    return profitTicks;
                }
                else
                {
                    profitTicks = 15.00;
                    return profitTicks;
                }
            }
            return profitTicks;
        }
        private double ExecuteOrder(FuturesContract futuresContract, SignalFile signalFile, OrderDetails orderDetails, Strategy strategy, SimpleLogger log, Tuple<bool, bool, int> db)
        {
            Prices prices = new Prices();
            prices = prices.StartListener(clientSocket, contract, strategy, prices, log);

            double limitPrice = GetPrices(futuresContract, signalFile, strategy, orderDetails, prices, db);

            if (limitPrice <= 0)
            {
                limitPrice = signalFile.LimitPrice;
                log.Warning($"LimitPrice for {strategy.StrategyName} is null using signalFile price {signalFile.LimitPrice}");
            }

            //Use market orders overnight so we don't miss getting filled
            //string orderType = OverNight(signalFile, db);

            //if (string.IsNullOrEmpty(orderType))
            //{
            //    orderType = strategy.OrderType;
            //}

            Order parent = new Order();
            Order openOrder = new Order();
            List<Order> orders = new List<Order>();

            if (db.Item3 != 0) //If there is a position 
            {
                //Use market orders overnight so we don't miss getting filled
                string orderType = OverNight(signalFile, db);

                //if (string.IsNullOrEmpty(orderType))
                //{
                //    orderType = strategy.OrderType;
                //}

                //Close position with market order
                parent = new Order()
                {
                    OrderId = OrderDetails.NextOrderId++,

                    Action = orderDetails.BuyOrSell,
                    OrderType = (string.IsNullOrEmpty(orderType) ? strategy.OrderType : orderType), //strategy.OrderType, //"MKT",
                    TotalQuantity = strategy.LotSize,
                    LmtPrice = (orderDetails.BuyOrSell ==  "SELL") ? prices.BidPrice : prices.AskPrice,//limitPrice,
                    AuxPrice = (orderDetails.BuyOrSell == "SELL") ? prices.BidPrice : prices.AskPrice,//limitPrice,
                    Tif = "GTC",

                    Transmit = true
                };

                openOrder = new Order()
                {
                    OrderId = OrderDetails.NextOrderId++,
                    Action = orderDetails.BuyOrSell,
                    OrderType = strategy.OrderType, //(strategy.OrderType == "MKT" ? strategy.OrderType : orderType),
                    TotalQuantity = strategy.LotSize,
                    LmtPrice = limitPrice,
                    AuxPrice = limitPrice,
                    Tif = "GTC",
                    Transmit = true
                };

                orders = new List<Order>()
                {
                        parent,
                        openOrder
                };
            }
            else
            {
                openOrder = new Order()
                {
                    OrderId = OrderDetails.NextOrderId++,

                    Action = orderDetails.BuyOrSell,
                    OrderType = strategy.OrderType, //(strategy.OrderType == "MKT" ? strategy.OrderType : orderType),
                    TotalQuantity = orderDetails.LotSize,
                    LmtPrice = limitPrice,
                    AuxPrice = limitPrice,
                    Tif = "GTC",

                    Transmit = true
                };
                orders = new List<Order>()
                {
                        openOrder
                };
            }

            foreach (Order o in orders)
            {
                clientSocket.placeOrder(o.OrderId, contract, o);
            }

            orderDetails.OrderId = parent.OrderId.ToString();
            orderDetails.OrderStatus = "Submitted";

            if (db.Item3 != 0)
            {
                log.Signal($"Sent Closing {parent.Action} {parent.OrderType} Order for {strategy.Symbol} LimitPrice:{parent.LmtPrice} LotSize:{parent.TotalQuantity} OrderId:{parent.OrderId}");
                log.Signal($"Sent Opening {openOrder.Action} {openOrder.OrderType} Order for {strategy.Symbol} LimitPrice:{openOrder.LmtPrice} LotSize:{openOrder.TotalQuantity} OrderId:{openOrder.OrderId}");
            }
            else
            {
                log.Signal($"Sent Opening {openOrder.Action} {openOrder.OrderType} Order for {strategy.Symbol} LimitPrice:{openOrder.LmtPrice} LotSize:{openOrder.TotalQuantity} OrderId:{openOrder.OrderId}");
            }

            OrderDetails.OrderIdList.DataList.Add(orderDetails.OrderId);

            return limitPrice;
        }
        private double GetPrices(FuturesContract futuresContract, SignalFile signalFile, Strategy strategy, OrderDetails orderDetails, Prices prices, Tuple<bool, bool, int> db)
        {
            double limitPrice = 0;

            if (prices.BidPrice > 0 && prices.AskPrice > 0)
            {
                limitPrice = GetBrokerLimitPrice(futuresContract, orderDetails, prices, strategy, db, signalFile);
            }
            else if (limitPrice <= 0)
            {
                limitPrice = GetSignalFileLimitPrice(futuresContract, orderDetails, signalFile, strategy, db);
            }

            return limitPrice;
        }
        private double GetBrokerLimitPrice(FuturesContract futuresContract, OrderDetails orderDetails, Prices prices, Strategy strategy, Tuple<bool, bool, int> db, SignalFile signalFile)
        {
            double tick = FuturesContract.GetTickSize(futuresContract);
            double limitPrice = 0;

            switch (strategy.Symbol)
            {
                case "QM":
                    //tick *= (db.Item3 == 0 || db.Item3 != 0) ? @params.crudeSpread : 1;                    
                    //if (signalFile.Macd == 1)
                    //{
                    //    tick *= (signalFile.Trend < 40 && orderDetails.BuyOrSell == "SELL") ? @params.crudeSpread : 4;
                    //}
                    //else if (signalFile.Macd == 2)
                    //{
                    //    tick *= (signalFile.Trend < 40 && orderDetails.BuyOrSell == "SELL") ? @params.crudeSpread : 3;
                    //}
                    //if (signalFile.Macd == -1)
                    //{
                    //    tick *= (signalFile.Trend < 40 && orderDetails.BuyOrSell == "BUY") ? @params.crudeSpread : 4;
                    //}
                    //else if (signalFile.Macd == -2)
                    //{
                    //    tick *= (signalFile.Trend < 40 && orderDetails.BuyOrSell == "BUY") ? @params.crudeSpread : 3;
                    //}

                    if (orderDetails.BuyOrSell == "BUY")
                    {
                        //If Up trend than our bid is spread = 2 and offer is  @params.crudeSpread = 5                      
                        //tick *= (signalFile.EMAAngle < 40 && signalFile.EMAAngle > -40) ? @params.crudeSpread : 8;
                        tick *= (signalFile.Trend < 40) ? @params.crudeSpread : 4;

                        limitPrice = Math.Min(prices.BidPrice, signalFile.LimitPrice); //prices.BidPrice;
                        limitPrice -= tick;
                    }
                    else if (orderDetails.BuyOrSell == "SELL")
                    {
                        //If Down trend than our offer is spread = 2 and out bid is @params.crudeSpread = 5
                        //tick *= (signalFile.EMAAngle < 40 && signalFile.EMAAngle > -40) ? @params.crudeSpread : 8;
                        tick *= (signalFile.Trend < 40) ? @params.crudeSpread : 4;

                        limitPrice = Math.Max(prices.AskPrice, signalFile.LimitPrice); //prices.AskPrice;
                        limitPrice += tick;
                    }
                    return limitPrice;
                case "MES":
                    //tick *= (db.Item3 == 0 || db.Item3 != 0) ? @params.sp500Spread : 1;
                    //tick *= (signalFile.EMAAngle < 40 && signalFile.EMAAngle > -40) ? @params.sp500Spread : @params.sp500Spread ;
                    tick *= (signalFile.Trend < 40 ) ? @params.sp500Spread : 5;

                    if (orderDetails.BuyOrSell == "BUY")
                    {
                        limitPrice = Math.Min(prices.BidPrice, signalFile.LimitPrice);
                        limitPrice -= tick;
                    }
                    else if (orderDetails.BuyOrSell == "SELL")
                    {
                        limitPrice = Math.Max(prices.AskPrice, signalFile.LimitPrice);
                        limitPrice += tick;
                    }
                    return limitPrice;
            }
            return limitPrice;
        }
        private double GetSignalFileLimitPrice(FuturesContract futuresContract, OrderDetails orderDetails, SignalFile signalFile, Strategy strategy, Tuple<bool, bool, int> db)
        {
            double tick;
            double limitPrice = signalFile.LimitPrice;

            switch (strategy.Symbol)
            {
                case "QM":
                    tick = FuturesContract.GetTickSize(futuresContract);
                    decimal nearOf = 0.025M;

                    //tick *= (signalFile.EMAAngle < 40 && signalFile.EMAAngle > -40) ? @params.crudeSpread : 4;
                    tick *= (signalFile.Trend < 40) ? @params.crudeSpread : 4;

                    if (orderDetails.BuyOrSell == "BUY")
                    {
                        limitPrice -= tick;
                    }
                    else if (orderDetails.BuyOrSell == "SELL")
                    {
                        limitPrice += tick;
                    }

                    decimal _limitPrice = Convert.ToDecimal(limitPrice);
                    _limitPrice = Precision.UltimateRoundingFunction(_limitPrice, nearOf, 0.49M);
                    limitPrice = Convert.ToDouble(_limitPrice);

                    return limitPrice;

                case "MES":
                    tick = FuturesContract.GetTickSize(futuresContract);
                    tick *= (signalFile.EMAAngle < 40 && signalFile.EMAAngle > -40) ? @params.sp500Spread : @params.sp500Spread / 2;

                    if (signalFile.Adx < 15)
                    {
                        if (orderDetails.BuyOrSell == "BUY")
                        {
                            limitPrice -= tick;
                        }
                        else if (orderDetails.BuyOrSell == "SELL")
                        {
                            limitPrice += tick;
                        }
                    }

                    return limitPrice;

                case "MGC":
                    tick = FuturesContract.GetTickSize(futuresContract);

                    if (signalFile.Adx < 15)
                    {
                        tick *= 1;

                        if (orderDetails.BuyOrSell == "BUY")
                        {
                            limitPrice -= tick;
                            return limitPrice;
                        }
                        else if (orderDetails.BuyOrSell == "SELL")
                        {
                            limitPrice += tick;
                            return limitPrice;
                        }
                    }
                    else
                    {
                        tick *= 2;

                        if (orderDetails.BuyOrSell == "BUY")
                        {
                            limitPrice += tick;
                            return limitPrice;
                        }
                        else if (orderDetails.BuyOrSell == "SELL")
                        {
                            limitPrice -= tick;
                            return limitPrice;
                        }
                    }

                    break;
            }
            return limitPrice;
        }
        private bool CancelOpenOrders(Strategy strategy, bool dbconflict, OrderDetails orderDetails, bool allOpenOrder)
        {
            bool openOrderstatus = false;
            OpenOrder.OpenOrderFlag = false;
            int cnt = 0;

            if (strategy.OrderType == "LMT")
            {
                OpenOrder.OpenOrderList.DataList.Clear();

                clientSocket.reqAllOpenOrders();

                while (!OpenOrder.OpenOrderFlag)
                {
                    if (OpenOrder.OpenOrderFlag)
                    {
                        break;
                    }

                    if (cnt > @params.maxLoopCount)
                    {
                        log.Warning("[IB WARNING] Could not update IB Open Order.");
                        break;
                    }

                    log.Warning($"Waiting for IB Open Order to Update. Try # {cnt}");
                    cnt++;
                }

                List<int> cancelList = new List<int>();

                foreach (string openOrder in OpenOrder.OpenOrderList.DataList)
                {
                    OpenOrder.Symbol = SignalFile.GetWantedText(openOrder, "Symbol:");
                    OpenOrder.OrderId = SignalFile.GetWantedText(openOrder, "Id:");
                    OpenOrder.OrderState = SignalFile.GetWantedText(openOrder, "OrderState:");
                    OpenOrder.Action = SignalFile.GetWantedText(openOrder, "Action:");

                    if (allOpenOrder && OpenOrder.Symbol == strategy.Symbol && OpenOrder.OrderState.Contains("Submit"))
                    {
                        int id = Convert.ToInt32(OpenOrder.OrderId);
                        cancelList.Add(id);
                    }
                    else if (dbconflict && OpenOrder.Action == orderDetails.BuyOrSell && OpenOrder.Symbol == strategy.Symbol && OpenOrder.OrderState.Contains("Submit"))
                    {
                        int id = Convert.ToInt32(OpenOrder.OrderId);
                        cancelList.Add(id);
                    }
                    else if (!dbconflict && OpenOrder.Symbol == strategy.Symbol && OpenOrder.OrderState.Contains("Submit"))
                    {
                        int id = Convert.ToInt32(OpenOrder.OrderId);
                        cancelList.Add(id);
                    }
                }

                if (cancelList.Count() > 0)
                {
                    foreach (int id in cancelList)
                    {
                        clientSocket.cancelOrder(id);
                        log.Warning($"Cancelling open order for {strategy.StrategyName} OrderId: {id}.");
                        openOrderstatus = true;
                    }
                }
            }

            return openOrderstatus;
        }
        private int GetCurrentPositionForDB(OrderDetails orderDetails, Strategy strategy)
        {
            int currentPositionForDB = 0;

            if (orderDetails.BuyOrSell == "BUY")
            {
                currentPositionForDB = strategy.LotSize;
            }
            else if (orderDetails.BuyOrSell == "SELL")
            {
                currentPositionForDB = (-1 * strategy.LotSize);
            }

            return currentPositionForDB;
        }
        private void UpdateDBTables(FuturesContract futuresContract, SignalFile signalFile, OrderDetails orderDetails, Strategy strategy, double limitPrice)
        {
            int currentPositionForDB = GetCurrentPositionForDB(orderDetails, strategy);

            DBFunctions.UpdatePositionDataToDb(log, futuresContract, signalFile, orderDetails, limitPrice);

            DBFunctions.UpdateStrategyPosition(log, currentPositionForDB, strategy, orderDetails);

            DBFunctions.CalculateProfitandLoss(log, strategy, signalFile);
        }

        //private bool CheckOrderStatus(FuturesContract futuresContract, SignalFile signalFile, OrderDetails orderDetails)
        //{
        //    string[] lines;
        //    bool orderstatus = false;

        //    bool updated = UpdateOrderStatus();

        //    if (!updated)
        //    {
        //        return orderstatus;
        //    }

        //    Parallel.ForEach(OrderDetails.OrderIdList.DataList, (id) =>
        //    {
        //        for (int i = 0; i < OrderDetails.OrderStatusList.DataList.Count(); i++)
        //        {
        //            orderDetails.OrderStatus = null;

        //            lines = OrderDetails.OrderStatusList.DataList.ToArray();

        //            string orderId = SignalFile.GetWantedText(lines[i].ToString(), "Id:");
        //            //orderDetails.OrderStatus = signalFile.GetWantedText(lines[i].ToString(), "Status:");
        //            string symbol = SignalFile.GetWantedText(lines[i].ToString(), " Symbol:");
        //            orderDetails.OrderStatus = SignalFile.GetWantedText(lines[i].ToString(), "OrderState:");

        //            if (orderId == id && strategy.Symbol == symbol)
        //            {
        //                switch (orderDetails.OrderStatus)
        //                {
        //                    case "Submitted":
        //                    case "PreSubmitted":
        //                    case "Filled":
        //                    case "Cancelled":
        //                    case "PendingCancel":
        //                    case "PendingSubmit":
        //                    case "ApiCancelled":
        //                        orderstatus = true;
        //                        break;
        //                }
        //            }

        //            if (!orderstatus && i >= OrderDetails.OrderStatusList.DataList.Count())
        //            {
        //                UpdateOrderStatus();
        //            }

        //            if (orderstatus)
        //            {
        //                orderDetails.OrderId = id;

        //                OrderDetails.OrderStatusList.DataList.RemoveAll(s => s.Contains(orderId));
        //                OrderDetails.OrderIdList.DataList.Remove(id);

        //                log.Warning($"{signalFile.StrategyName} {orderDetails.BuyOrSell} order OrderId:{orderDetails.OrderId} status:{orderDetails.OrderStatus} ");

        //                UpdateDBTables(futuresContract, signalFile, orderDetails, strategy);
        //            }
        //        }
        //    });
        //    //  }
        //    return orderstatus;
        //}    

        //private bool UpdateOrderStatus()
        //{
        //    OrderDetails.OrderStatusFlag = false;
        //    bool orderstatus = false;
        //    int cnt = 0;

        //    clientSocket.reqOpenOrders();

        //    while (!OrderDetails.OrderStatusFlag)
        //    {
        //        if (OrderDetails.OrderStatusFlag)
        //        {
        //            orderstatus = true;
        //            return orderstatus;
        //        }

        //        if (cnt > @params.maxLoopCount)
        //        {
        //            log.Warning("[IB WARNING] Could not update IB Order Status.");

        //            orderstatus = false;
        //            return orderstatus;
        //        }

        //        log.Warning($"Waiting for IB Order Status to Update. Try # {cnt}");
        //        cnt++;
        //    }



        //    return orderstatus;
        //}
    }
}
