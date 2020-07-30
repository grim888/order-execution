using System;
using System.Collections.Generic;
//using Mono.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using SQLite;
using System.Data.SQLite;


namespace blockshift_ib
{
    public class Parameters
    {
        // Stores all the parameters defined
        // in the configuration file.
        public List<ExchangeInfo> exchangeInfoList;

        public bool verbose;
        public bool demoMode;
        public string currDirectory;
        public string configFile;

        //public CURL curl;
        public string apiHost;
        // public string apiPort;

        public string filePath;
        public int connectionRetryLimit;
        public int secondsToConnect;
        public int maxLoopCount;
        public bool sendSlackMessage;
        public bool profitOrder;

        public bool crudeEnabled;
        public string crudeLocalSymbol;
        public string crudeSymbol;
        public string crudeSecType;
        public string crudeExpiry;
        public string crudeExchange;
        public string crudeCurrency;
        public string crudeOrderType;
        public int crudeMaxPosition;
        public int crudeLotSize;
        public int crudeSpread;

        public bool sp500Enabled;
        public string sp500LocalSymbol;
        public string sp500Symbol;
        public string sp500SecType;
        public string sp500Expiry;
        public string sp500Exchange;
        public string sp500Currency;
        public string sp500OrderType;
        public int sp500MaxPosition;
        public int sp500LotSize;
        public int sp500ProfitOne;
        public int sp500ProfitTwo;
        public int sp500Spread;

        public bool goldEnabled;
        public string goldLocalSymbol;
        public string goldSymbol;
        public string goldSecType;
        public string goldExpiry;
        public string goldExchange;
        public string goldCurrency;
        public string goldOrderType;
        public int goldMaxPosition;
        public int goldLotSize;

        public string dbFile;

        public static readonly object SyncObject = new object();

        public Parameters(string fileName)
        {
            configFile = fileName;
            if (!File.Exists(configFile))
            {
                Console.Write("ERROR: ");
                Console.Write(configFile);
                Console.Write(" cannot be open.\n");
                Environment.Exit(1);
            }
            verbose = getBool(GetParameter("Verbose", configFile));
            demoMode = getBool(GetParameter("DemoMode", configFile));
            apiHost = (GetParameter("ApiHost", configFile));
            filePath = (GetParameter("FilePath", configFile));

            maxLoopCount = getInt(GetParameter("MaxLoopCount", configFile));
            connectionRetryLimit = getInt(GetParameter("ConnectionRetryLimit", configFile));
            secondsToConnect = getInt(GetParameter("SecondsToConnect", configFile));
            sendSlackMessage = getBool(GetParameter("SendSlackMessage", configFile));
            profitOrder = getBool(GetParameter("ProfitOrder", configFile));
            
            crudeEnabled = getBool(GetParameter("CrudeEnabled", configFile));
            crudeLocalSymbol = (GetParameter("CrudeLocalSymbol", configFile));
            crudeSymbol = (GetParameter("CrudeSymbol", configFile));
            crudeSecType = (GetParameter("CrudeSecType", configFile));
            crudeExpiry = (GetParameter("CrudeExpiry", configFile));
            crudeExchange = (GetParameter("CrudeExchange", configFile));
            crudeCurrency = (GetParameter("CrudeCurrency", configFile));
            crudeOrderType = (GetParameter("CrudeOrderType", configFile));
            crudeMaxPosition = getInt(GetParameter("CrudeMaxPosition", configFile));
            crudeLotSize = getInt(GetParameter("CrudeLotSize", configFile));
            crudeSpread = getInt(GetParameter("CrudeSpread", configFile));

            sp500Enabled = getBool(GetParameter("SP500Enabled", configFile));
            sp500LocalSymbol = (GetParameter("SP500LocalSymbol", configFile));
            sp500Symbol = (GetParameter("SP500Symbol", configFile));
            sp500SecType = (GetParameter("SP500SecType", configFile));
            sp500Expiry = (GetParameter("SP500Expiry", configFile));
            sp500Exchange = (GetParameter("SP500Exchange", configFile));
            sp500Currency = (GetParameter("SP500Currency", configFile));
            sp500OrderType = (GetParameter("SP500OrderType", configFile));
            sp500MaxPosition = getInt(GetParameter("SP500MaxPosition", configFile));
            sp500LotSize = getInt(GetParameter("SP500LotSize", configFile));
            sp500ProfitOne = getInt(GetParameter("SP500ProfitOne", configFile));
            sp500ProfitTwo = getInt(GetParameter("SP500ProfitTwo", configFile));
            sp500Spread = getInt(GetParameter("SP500Spread", configFile));

            goldEnabled = getBool(GetParameter("GoldEnabled", configFile));
            goldLocalSymbol = (GetParameter("GoldLocalSymbol", configFile));
            goldSymbol = (GetParameter("GoldSymbol", configFile));
            goldSecType = (GetParameter("GoldSecType", configFile));
            goldExpiry = (GetParameter("GoldExpiry", configFile));
            goldExchange = (GetParameter("GoldExchange", configFile));
            goldCurrency = (GetParameter("GoldCurrency", configFile));
            goldOrderType = (GetParameter("GoldOrderType", configFile));
            goldMaxPosition = getInt(GetParameter("GoldMaxPosition", configFile));
            goldLotSize = getInt(GetParameter("GoldLotSize", configFile));

            dbFile = GetParameter("DBFile", configFile);
            if (dbFile != string.Empty)
            {
                DBFunctions.CreatePositionDBTable();
                DBFunctions.CreateStrategyPositionDBTable();
            }
            exchangeInfoList = new List<ExchangeInfo>();
        }

        public void AddExchange(string name, double fee, bool canShort, bool isImplemented)
        {
            exchangeInfoList.Add(new ExchangeInfo
            {
                exchName = name,
                fees = fee,
                canShort = canShort,
                isImplemented = isImplemented
            });
        }

        public int nbExch()
        {
            return exchangeInfoList.Count;
        }

        public static string GetParameter(string parameter, string configFile)
        {
            foreach (string line in File.ReadLines(configFile))
            {
                if (line.Length > 0 && line[0] != '#')
                {
                    string key = line.Substring(0, line.IndexOf('=')).Replace(" ", "");
                    string value = line.Substring(line.IndexOf('=') + 1, line.Length - line.IndexOf('=') - 1).Replace(" ", "");
                    if (key == parameter)
                    {
                        return value;
                    }
                }
            }
            Console.Write("ERROR: parameter '");
            Console.Write(parameter);
            Console.Write("' not found. Your configuration file might be too old.\n");
            Console.Write("Config file location: " + configFile);
            Environment.Exit(1);
            return string.Empty;

        }

        public static bool getBool(string value)
        {
            return value.ToLower() == "true";
        }

        public static double getDouble(string value)
        {
            return Convert.ToDouble(value);
        }

        public static int getInt(string value)
        {
            return Convert.ToInt32(value);
        }

        public static uint getUnsigned(string value)
        {
            return Convert.ToUInt32(value.Contains(".") ? value.Substring(0, value.IndexOf('.')) : value);
        }

    }

    public struct ExchangeInfo
    {
        public string exchName;
        public double fees;
        public bool canShort;
        public bool isImplemented;

    }

}
