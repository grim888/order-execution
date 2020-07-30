using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace blockshift_ib
{
    public class SignalFile
    {
        public SignalFile()
        {
        }
        public string LastFileLine { get; set; }
        public string StrategyName { get; set; }
        public string Symbol { get; set; }
        public string TradeDate { get; set; }
        public string TradeDateTime { get; set; }
        public int Signal { get; set; }
        public string EntryPrice { get; set; }
        public string Exit { get; set; }
        public double LimitPrice { get; set; }
        public string Output { get; set; }
        public double Adx { get; set; }
        public double EMAAngle { get; set; }
        public double Trend { get; set; }
        public int Macd { get; set; }

        private readonly static Dictionary<int, string> FileColumns = new Dictionary<int, string>()
        {
            {1, "strategy:"},
            {2, "symbol:"},
            {3, "entry_date:"},
            {4, "entry_time:"},
            {5, "signal:"},
            {6, "entry_price:"},
            {7, "exit:"},
            {8, "adx:"},
            {9, "emaAngle:"},
            {10, "trend:"},
            {11, "macd:"}
         };
        public static SignalFile ParseSignalFile(SignalFile signalFile, SimpleLogger log)
        {
            string strColumn;

            foreach (KeyValuePair<int, string> column in FileColumns)
            {
                strColumn = GetWantedText(signalFile.LastFileLine, column.Value);

                if (string.IsNullOrEmpty(strColumn))
                {
                    log.Warning($"SignalFile field {column.Value} missing data.");
                    break;
                }

                switch (column.Value)
                {
                    case "strategy:":
                        signalFile.StrategyName = strColumn;
                        break;
                    case "symbol:":
                        signalFile.Symbol = strColumn;
                        break;
                    case "entry_date:":
                        signalFile.TradeDate = strColumn;
                        break;
                    case "entry_time:":
                        signalFile.TradeDateTime = strColumn;
                        break;
                    case "signal:":
                        signalFile.Signal = int.Parse(strColumn, CultureInfo.InvariantCulture);
                        break;
                    case "entry_price:":
                        signalFile.EntryPrice = strColumn;
                        break;
                    case "exit:":
                        signalFile.Exit = strColumn.Substring(0, 1);
                        break;
                    case "adx:":
                        signalFile.Adx = int.Parse(strColumn, CultureInfo.InvariantCulture);
                        break;
                    case "emaAngle:":
                        signalFile.EMAAngle = double.Parse(strColumn, CultureInfo.InvariantCulture);
                        break;
                    case "trend:":
                        signalFile.Trend = double.Parse(strColumn, CultureInfo.InvariantCulture);
                        break;
                    case "macd:":
                        signalFile.Macd = int.Parse(strColumn, CultureInfo.InvariantCulture);
                        break;
                }
            }

            signalFile.LimitPrice = double.Parse(signalFile.EntryPrice, CultureInfo.InvariantCulture);

            signalFile.Output = $"strategy:{signalFile.StrategyName}, symbol:{signalFile.Symbol}, entryDate:{signalFile.TradeDate}, entryDateTime: {signalFile.TradeDateTime} signal:{signalFile.Signal}, price:{signalFile.EntryPrice}, exit:{signalFile.Exit}, adx:{signalFile.Adx}, emaAngle:{signalFile.EMAAngle}, trend:{signalFile.Trend}, macd:{signalFile.Macd}";
            log.Signal("[PARSED DATA] " + signalFile.Output);

            return signalFile;
        }
        public static string GetWantedText(string s, string p)
        {
            string WantedText = null;
            if ((!string.IsNullOrEmpty(s)))
            {
                //  s = Regex.Replace(s.Trim(), @" ");
                Match match = Regex.Match(s, (p + "[^,]*"), RegexOptions.IgnoreCase);

                if ((!string.IsNullOrEmpty(match.Value)))
                {
                    WantedText = match.Value.Substring((match.Value.Length - (match.Value.Length - p.Length)));
                }
            }
            return WantedText;
        }

    }


}
