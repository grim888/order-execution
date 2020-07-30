using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using IBApi;
using System.Text.RegularExpressions;
using System.Threading;

namespace blockshift_ib
{
    public class EWrapperImpl : EWrapper
    {
        EClientSocket clientSocket;
        public readonly EReaderSignal Signal;

        public int nextOrderId;
        public int positionEndFlag;
        readonly SimpleLogger log = new SimpleLogger();
        private readonly Slack slack = new Slack();

        public EWrapperImpl()
        {
            Signal = new EReaderMonitorSignal();
            clientSocket = new EClientSocket(this, Signal);            
        }

        public EClientSocket ClientSocket
        {
            get { return clientSocket; }
            set { clientSocket = value; }
        }

        public int NextOrderId { get; internal set; }
        public int positionFlag { get; internal set; }

        public void accountDownloadEnd(string account)
        {
            // throw new NotImplementedException();
        }

        public void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            // throw new NotImplementedException();
        }

        public void accountSummaryEnd(int reqId)
        {
            // throw new NotImplementedException();
        }

        public void bondContractDetails(int reqId, ContractDetails contract)
        {
            // throw new NotImplementedException();
        }

        public void commissionReport(CommissionReport commissionReport)
        {
            // throw new NotImplementedException();
        }

        public void connectionClosed()
        {
            //// throw new NotImplementedException();
        }

        public void contractDetails(int reqId, ContractDetails ContractDetails)
        {
            // throw new NotImplementedException();
        }

        public void contractDetailsEnd(int reqId)
        {
            // throw new NotImplementedException();
        }

        public void currentTime(long time)
        {
            // throw new NotImplementedException();
        }

        public void deltaNeutralValidation(int reqId, UnderComp underComp)
        {
            // throw new NotImplementedException();
        }

        public void displayGroupList(int reqId, string groups)
        {
            // throw new NotImplementedException();
        }

        public void displayGroupUpdated(int reqId, string contractInfo)
        {
            // throw new NotImplementedException();
        }

        public void error(string str)
        {
            // throw new NotImplementedException();
        }

        public void error(Exception e)
        {
            //Delegation.Error(e);
            //Delegation.Message("ERROR:" + e.Message);      

            log.Broker("Error:" + e);
        }

        public void error(int id, int errorCode, string errorMsg)
        {
            string str = "Error Id:" + id + ", Code:" + errorCode + ", Msg:" + errorMsg;

            if (errorCode != 2137)
            {
                log.Broker(str);
            }
        }

        public void execDetails(int reqId, Contract contract, Execution execution)
        {
            //  throw new NotImplementedException();
        }

        public void execDetailsEnd(int reqId)
        {
            //throw new NotImplementedException();
        }

        public void fundamentalData(int reqId, string data)
        {
            // throw new NotImplementedException();
        }

        public void historicalData(int reqId, string date, double open, double high, double low, double close, int volume, int count, double WAP, bool hasGaps)
        {
            string str = ("HistoricalData. " + reqId + " - Date:" + date + ", Open:" + open + ", High:" +
                    high + ", Low:" + low + ", Close:" + close + ", Volume:" + volume + ", Count:" +
                    count + ", WAP:" + WAP + ", HasGaps:" + hasGaps + "\n");

            string time = date.Substring(date.Length - 9);
        }

        public void historicalDataEnd(int reqId, string start, string end)
        {
            // Console.WriteLine("Historical data end - " + reqId + " from " + start + " to " + end);
            //  Console.WriteLine("historicalDataEnd//");
        }

        public void managedAccounts(string accountsList)
        {

        }

        public void marketDataType(int reqId, int marketDataType)
        {
            // throw new NotImplementedException();
        }

        public void nextValidId(int orderId)
        {
            if (orderId > 0)
            {
                log.Broker("NextValidId - OrderId [" + orderId + "]");

                OrderDetails.NextOrderId = orderId;
            }
        }

        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            String str = ("OpenOrder. Id:" + orderId + ", Symbol:" + contract.Symbol + ", SecType:" + contract.SecType + " Exchange:" + contract.Exchange + " Action:" + order.Action + ", OrderType:" + order.OrderType + ", Price:" + order.LmtPrice + " Quantity:" + order.TotalQuantity + ", OrderState:" + orderState.Status );

            log.Broker(str);

            if (orderState.Status.Contains("Submitted") || orderState.Status.Contains("Cancel"))
            {
                OrderDetails.OrderStatusList.DataList.Add(str);
            }

            OpenOrder.OpenOrderList.DataList.Add(str);

            if (orderState.Status.Contains("Filled") && !OpenOrder.MessageOrderList.DataList.Contains(orderId))
            {
                Slack slack = new Slack();
                slack.SendMessageToSlack($"[*{str}*");
                OpenOrder.MessageOrderList.DataList.Add(orderId);
            }
        }
     
        public void openOrderEnd()
        {
            //Console.WriteLine("Order Updated End \n");
            OrderDetails.OrderStatusFlag = true;
            OpenOrder.OpenOrderFlag = true;
        }

        /*public void orderStatus(int orderId, string status, int filled, int remaining, double avgFillPrice, 
            int permId, int parentId, double lastFillPrice, int clientId, string whyHeld)
        {
       
            
        }*/

        public void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice,
            int permId, int parentId, double lastFillPrice, int clientId, string whyHeld)
        {
            string str = ("OrderStatus. Id:" + orderId + ", Status:" + status + ", Filled" + filled + ", Remaining:" + remaining
           + ", AvgFillPrice:" + avgFillPrice + ", PermId:" + permId + ", ParentId:" + parentId + ", LastFillPrice:" + lastFillPrice + ", ClientId:" + clientId + ", WhyHeld:" + whyHeld);

             //log.Broker(str);
            //if (status.Contains("Submitted") || status.Contains("Cancel"))// || status == "Filled")
            //{
            //    orderDetails.OrderStatusList.DataList.Add(str);
            //}

        }

        public virtual void position(string account, Contract contract, double pos, double avgCost)
        {
            Positions.PositionEndFlag = false;

            string str = ("Account " + account + " - Symbol:" + contract.Symbol + ", SecType:" + contract.SecType
                + ", Currency:" + contract.Currency + ", Position:" + pos + ", Avg cost:" + avgCost);
            
            if (contract.SecType == "FUT")
            {
                log.Broker(str);
                Positions.PositionList.Add(str);
            }
        }

        public void positionEnd()
        {
            //Console.WriteLine("PositionEnd \n");
            Positions.PositionEndFlag = true;
        }

        public void realtimeBar(int reqId, long time, double open, double high, double low, double close, long volume, double WAP, int count)
        {
            // throw new NotImplementedException();
        }

        public void receiveFA(int faDataType, string faXmlData)
        {
            // throw new NotImplementedException();
        }

        public void scannerData(int reqId, int rank, ContractDetails ContractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            // throw new NotImplementedException();
        }

        public void scannerDataEnd(int reqId)
        {
            // throw new NotImplementedException();
        }

        public void scannerParameters(string xml)
        {
            // throw new NotImplementedException();
        }

        public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureExpiry, double dividendImpact, double dividendsToExpiry)
        {
            // throw new NotImplementedException();
        }

        public void tickGeneric(int tickerId, int field, double value)
        {
            // string str = ("TickerId " + tickerId + " Field:" + field + ", Price:" + value);

            // log.Broker(str);
        }

        public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            // throw new NotImplementedException();
        }

        public void tickPrice(int tickerId, int field, double price, int canAutoExecute)
        {
            string str = ("TickerId " + tickerId + " Field:" + field + ", Price:" + price + ", CanAutoExecute:" + canAutoExecute);
           // log.Broker(str);

            if (field == 1 && !Prices.IBBidPriceFlag)
            {
                Prices.FieldOne = $"TickerId: {tickerId.ToString()}, Field:{field}, price:{price}, CanAutoExecute:{canAutoExecute}";
                log.Broker(Prices.FieldOne);
                Prices.IBBidPriceFlag = true;
            }

            if (field == 2 && !Prices.IBAskPriceFlag)
            {
                Prices.FieldTwo = $"TickerId:{tickerId.ToString()}, Field:{field}, price:{price}, CanAutoExecute:{canAutoExecute}";
                log.Broker(Prices.FieldTwo);
                Prices.IBAskPriceFlag = true;
            }
        }

        public void tickSize(int tickerId, int field, int size)
        {
          //  string str = ("TickerId " + tickerId + " Field:" + field + ", Size:" + size);
           // Console.WriteLine(str);
            // throw new NotImplementedException();
        }

        public void tickSnapshotEnd(int tickerId)
        {
            // throw new NotImplementedException();
        }

        public void tickString(int tickerId, int field, string value)
        {
            // throw new NotImplementedException();
        }

        public void updateAccountTime(string timestamp)
        {
            // throw new NotImplementedException();
        }

        public void updateAccountValue(string key, string value, string currency, string accountName)
        {
            // throw new NotImplementedException();
        }

        public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            string str = string.Format("UpdateMarketDepth. TickerId:" + tickerId + " - Position:" + position + ", Operation:" + operation + ", Side:" + side + ", Price:" + price + ", Size:" + size);

          //  log.Broker(str);


            if (side == 1 && !Prices.IBDepthBidPriceFlag)
            {
                Prices.SideOne = $"TickerId:{tickerId.ToString()}, Side:{side}, Price:{price}, Size:{size}";
                log.Broker(Prices.SideOne);
                Prices.IBDepthBidPriceFlag = true;
            }

            if (side == 0 && !Prices.IBDepthAskPriceFlag)
            {
                Prices.SideTwo = $"TickerId:{tickerId.ToString()}, Side:{side}, Price:{price}, Size:{size}";
                log.Broker(Prices.SideTwo);
                Prices.IBDepthAskPriceFlag = true;
            }
        }

        public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size)
        {
            // throw new NotImplementedException();
        }

        public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            // throw new NotImplementedException();
        }

        public void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue,
              double averageCost, double unrealisedPNL, double realisedPNL, string accountName)
        {

        }

        public void verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            // throw new NotImplementedException();
        }

        public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
            // throw new NotImplementedException();
        }

        public void verifyCompleted(bool isSuccessful, string errorText)
        {
            // throw new NotImplementedException();
        }

        public void verifyMessageAPI(string apiData)
        {
            // throw new NotImplementedException();
        }

        public void connectAck() { }

        /**
         * @brief provides the portfolio's open positions.
         * @param requestId the id of request
         * @param account the account holding the position.
         * @param modelCode the model code holding the position.
         * @param contract the position's Contract
         * @param pos the number of positions held.
         * @Param avgCost the average cost of the position.
         * @sa positionMultiEnd, EClientSocket::reqPositionsMulti
         */
        public void positionMulti(int requestId, string account, string modelCode, Contract contract, double pos, double avgCost) { }

        /**
         * @brief Indicates all the positions have been transmitted.
         * @sa positionMulti, EClient::reqPositionsMulti
         */
        public void positionMultiEnd(int requestId) { }


        public void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency) { }

        /**
         * @brief Indicates all the account updates have been transmitted
         * @sa EWrapper::accountUpdateMulti, EClientSocket::reqAccountUpdatesMulti
         */
        public void accountUpdateMultiEnd(int requestId) { }

        public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes) { }

        /**
		* @brief called when all callbacks to securityDefinitionOptionParameter are complete
		* @param reqId the ID used in the call to securityDefinitionOptionParameter
		* @sa securityDefinitionOptionParameter, EClient::reqSecDefOptParams
		*/
        public void securityDefinitionOptionParameterEnd(int reqId) { }

        public void softDollarTiers(int reqId, SoftDollarTier[] tiers) { }
    }
}
