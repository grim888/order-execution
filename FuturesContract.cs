using IBApi;

namespace blockshift_ib
{
    public class FuturesContract
    {
        private readonly Parameters @params;
        public FuturesContract()
        {
        }
        public FuturesContract(Parameters @params)
        {
            this.@params = @params;
        }
        public  string Symbol { get; set; }
        //public  string MapSymbol { get; set; }
        public  string Exchange { get; set; }
        //public  string OrderType { get; set; }
        //public  int MaxPosition { get; set; }
        //public  int LotSize { get; set; }
        public FuturesContract GetContractDetails(Contract contract, FuturesContract futuresContract, SignalFile signalFile)
        {         
            if (signalFile.Symbol.Contains("ES"))
            {
                signalFile.Symbol = "ES";

                //switch (signalFile.StrategyName)
                //{
                //    case "MES TMA_Slope Kase Bar 3.75":                  
                contract.LocalSymbol = string.Empty;
                contract.Symbol = "MES"; //MES  ***USE MICRO CONTRACT FOR GO LIVE
                contract.SecType = @params.sp500SecType;
                contract.LastTradeDateOrContractMonth = @params.sp500Expiry;
                contract.Exchange = @params.sp500Exchange;
                contract.Currency = @params.sp500Currency;

                futuresContract.Symbol = contract.Symbol;
                futuresContract.Exchange = @params.sp500Exchange;

                //break;
                //default:
                //    contract.LocalSymbol = string.Empty;
                //    contract.Symbol = signalFile.Symbol; //MES  ***USE MICRO CONTRACT FOR GO LIVE
                //    contract.SecType = @params.sp500SecType;
                //    contract.LastTradeDateOrContractMonth = @params.sp500Expiry;
                //    contract.Exchange = @params.sp500Exchange;
                //    contract.Currency = @params.sp500Currency;

                //    Symbol = contract.Symbol;
                //    Exchange = @params.sp500Exchange;
                //    break;
                //}           
            }
            else if (signalFile.Symbol.Contains("QM"))
            {
                signalFile.Symbol = "QM";
                contract.LocalSymbol = string.Empty;
                contract.Symbol = signalFile.Symbol;
                contract.SecType = @params.crudeSecType;
                contract.LastTradeDateOrContractMonth = @params.crudeExpiry;
                contract.Exchange = @params.crudeExchange;
                contract.Currency = @params.crudeCurrency;

                futuresContract.Symbol = contract.Symbol;
                futuresContract.Exchange = @params.crudeExchange;
            }
            else if (signalFile.Symbol.Contains("CL"))
            {
                signalFile.Symbol = "CL";
                contract.LocalSymbol = string.Empty;
                contract.Symbol = signalFile.Symbol;
                contract.SecType = @params.crudeSecType;
                contract.LastTradeDateOrContractMonth = @params.crudeExpiry;
                contract.Exchange = @params.crudeExchange;
                contract.Currency = @params.crudeCurrency;

                futuresContract.Symbol = contract.Symbol;
                futuresContract.Exchange = @params.crudeExchange;
            }
            else if (signalFile.Symbol.Contains("GC"))
            {
                signalFile.Symbol = "GC";
                contract.LocalSymbol = string.Empty;
                contract.Symbol = "MGC";    //signalFile.Symbol;
                contract.SecType = @params.goldSecType;
                contract.LastTradeDateOrContractMonth = @params.goldExpiry;
                contract.Exchange = @params.goldExchange;
                contract.Currency = @params.goldCurrency;

                futuresContract.Symbol = contract.Symbol;
                futuresContract.Exchange = @params.goldExchange;
            }
            else if (signalFile.Symbol.Contains("TY"))
            {
                signalFile.Symbol = "ZN";
                contract.LocalSymbol = string.Empty;
                contract.Symbol = signalFile.Symbol;
                contract.SecType = "FUT";
                contract.LastTradeDateOrContractMonth = @params.crudeExpiry;
                contract.Exchange = "ECBOT";
                contract.Currency = "USD";

                futuresContract.Symbol = signalFile.Symbol;
                futuresContract.Exchange = "ECBOT";
            }

            return futuresContract;
        }
        public static double GetTickSize(FuturesContract futuresContract)
        {
            switch (futuresContract.Symbol)
            {
                case "ES": return 0.25;
                case "MES": return 0.25;
                case "CL": return 0.01;
                case "GC": return 0.10;
                case "MGC": return 0.10;
                case "QM": return 0.025;
                case "RTY": return 0.10;
                case "YM": return 1.00;
                default: return 0.00;
            }
        }
    }
}