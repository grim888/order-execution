using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.Text;
using System.Data.SQLite;

namespace blockshift_ib
{
    public static class DBFunctions
    {
        public static SQLiteConnection CreateConnection()
        {
            SQLiteConnection sqlite_conn;

            // Create a new database connection:
            sqlite_conn = new SQLiteConnection("Data Source=blackbox.db; New = True; Compress = True; ");

            // Open the connection:
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message.ToString());
                Console.Write(ex.StackTrace.ToString());
            }
            return sqlite_conn;
        }
        public static void CreatePositionDBTable()
        {
            var sqlite_conn = CreateConnection();

            SQLiteCommand sqlite_cmd;

            string sql = @"CREATE TABLE IF NOT EXISTS Position_Data 
                        (Date DATETIME not null, DateTime DATETIME not null, OrderId Text not null, Strategy Text not null, Exchange Text not null, Side Text not null, OrderStatus Text not null, Amount DECIMAL(8, 2))";
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = sql;
            sqlite_cmd.ExecuteNonQuery();

            sqlite_conn.Close();
        }
        public static void CreateStrategyPositionDBTable()
        {
            int result1;
            int result2;
            int result3;
            try
            {
                var sqlite_conn = CreateConnection();

                string sql1 = @"CREATE TABLE IF NOT EXISTS curr_Strategy_Position 
                        (StrategyId INTEGER not null, Symbol Text not null, Strategy Text not null, Side Text not null, Position INTEGER not null, OrderId Text not null)";

                string sql2 = @"CREATE UNIQUE INDEX IF NOT EXISTS idx_strategy_id ON curr_Strategy_Position (StrategyId)";

                string sql3 = @" CREATE TABLE IF NOT EXISTS hist_Strategy_Position 
                (StrategyId INTEGER not null, Symbol Text not null, Strategy Text not null, Side Text not null, Position INTEGER not null, OrderId Text not null)";

                using (SQLiteCommand sqlite_cmd1 = new SQLiteCommand(sql1, sqlite_conn))
                {
                    sqlite_cmd1.CommandText = sql1;
                    result1 = sqlite_cmd1.ExecuteNonQuery();
                }
                using (SQLiteCommand sqlite_cmd2 = new SQLiteCommand(sql2, sqlite_conn))
                {
                    sqlite_cmd2.CommandText = sql2;
                    result2 = sqlite_cmd2.ExecuteNonQuery();
                }
                using (SQLiteCommand sqlite_cmd3 = new SQLiteCommand(sql3, sqlite_conn))
                {
                    sqlite_cmd3.CommandText = sql3;
                    result3 = sqlite_cmd3.ExecuteNonQuery();
                }
                if (result1 > 0) { Console.WriteLine("Successfully created curr_Strategy_Position table."); }
                if (result2 > 0) { Console.WriteLine("Successfully created unique index for curr_Strategy_Position table."); }
                if (result3 > 0) { Console.WriteLine("Successfully created hist_Strategy_Position table."); }

                sqlite_conn.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
        public static void InsertData(SQLiteConnection conn)
        {
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "INSERT INTO SampleTable(Col1, Col2) VALUES('Test Text ', 1); ";
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = "INSERT INTO SampleTable(Col1, Col2) VALUES('Test1 Text1 ', 2); ";
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = "INSERT INTO SampleTable(Col1, Col2) VALUES('Test2 Text2 ', 3); ";
            sqlite_cmd.ExecuteNonQuery();


            sqlite_cmd.CommandText = "INSERT INTO SampleTable1(Col1, Col2) VALUES('Test3 Text3 ', 3); ";
            sqlite_cmd.ExecuteNonQuery();

        }
        public static string[] GetLastTradeSideFromPositionDataTable(SimpleLogger log, Strategy strategy)
        {
            var sqlite_conn = CreateConnection();
            string[] dbResults = new string[4];

            SQLiteCommand sqlite_cmd;

            var sqlCmd = $"SELECT Date, Side, Strategy, OrderId FROM Position_Data WHERE Strategy = '{strategy.StrategyName}' ORDER BY rowid DESC LIMIT 1;";
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = sqlCmd;

            using (SQLiteDataReader sqlite_datareader = sqlite_cmd.ExecuteReader())
            {
                while (sqlite_datareader.Read())
                {
                    for (int i = 0; i < 4; i++)
                    {
                        dbResults[i] = sqlite_datareader.GetString(i);
                    }
                }
            }

            sqlite_conn.Close();

            log.Info($"Position_Data table Last trade on {dbResults[0]} is a {dbResults[1]} order for {dbResults[2]} OrderId:{dbResults[3]}");
            return dbResults;
        }
        public static int GetCurrentStrategyPositionFromDB(SimpleLogger log, Strategy strategy)
        {
            var sqlite_conn = CreateConnection();
            int dbCurrentPosition = 0;

            SQLiteCommand sqlite_cmd;

            var sqlCmd = $"SELECT Position FROM curr_Strategy_Position WHERE StrategyId = {strategy.StrategyId};";
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = sqlCmd;

            using (SQLiteDataReader sqlite_datareader = sqlite_cmd.ExecuteReader())
            {
                if (sqlite_datareader.Read())
                {
                    dbCurrentPosition = sqlite_datareader.GetInt32(0);
                }
            }

            sqlite_conn.Close();
            log.Info($"Curr_Strategy_Position table for {strategy.StrategyName} has a position of {dbCurrentPosition}");
            return dbCurrentPosition;
        }
        public static void UpdateStrategyPosition(SimpleLogger log, int currentPositionForDB, Strategy strategy, OrderDetails orderDetails)
        {
            try
            {
                var sqlite_conn = CreateConnection();
                int result = 0;
                int result2 = 0;

                var sql1 = $"REPLACE INTO curr_Strategy_Position VALUES ({strategy.StrategyId}, '{strategy.Symbol}', '{strategy.StrategyName}', '{orderDetails.BuyOrSell}', {currentPositionForDB}, '{orderDetails.OrderId}'); ";
                var sql2 = $"REPLACE INTO hist_Strategy_Position VALUES ({strategy.StrategyId}, '{strategy.Symbol}', '{strategy.StrategyName}', '{orderDetails.BuyOrSell}', {currentPositionForDB}, '{orderDetails.OrderId}'); ";

                using (SQLiteCommand sqlite_cmd1 = new SQLiteCommand(sql1, sqlite_conn))
                {
                    sqlite_cmd1.CommandText = sql1;
                    result = sqlite_cmd1.ExecuteNonQuery();
                }

                using (SQLiteCommand sqlite_cmd2 = new SQLiteCommand(sql2, sqlite_conn))
                {
                    sqlite_cmd2.CommandText = sql2;
                    result2 = sqlite_cmd2.ExecuteNonQuery();
                }

                if (result > 0)
                {
                    log.Db("Successfully updated curr_Strategy_Position table.");
                }
                else
                {
                    log.Db("Failed to update curr_Strategy_Position table.");
                }

                if (result2 > 0)
                {
                    log.Db("Successfully updated his_Strategy_Position table.");
                }
                else
                {
                    log.Db("Failed to update hist_Strategy_Position table.");
                }

                sqlite_conn.Close();

            }
            catch (SQLiteException ex)
            {
                log.Trace("Error " + ex.Message.ToString());
                log.Trace(ex.StackTrace.ToString());
            }
        }
        public static void UpdatePositionDataToDb(SimpleLogger log, FuturesContract futuresContract, SignalFile signalFile, OrderDetails orderDetails, double limitPrice)
        {
            try
            {
                var sqlite_conn = CreateConnection();

                SQLiteCommand sqlite_cmd;
                sqlite_cmd = sqlite_conn.CreateCommand();
                sqlite_cmd.CommandText = "INSERT INTO Position_Data" +
                        " VALUES (  '" + signalFile.TradeDate +
                                    "','" + signalFile.TradeDateTime +
                                    "','" + orderDetails.OrderId +
                                    "','" + signalFile.StrategyName +
                                    "','" + futuresContract.Exchange +
                                    "','" + orderDetails.BuyOrSell +
                                    "','" + orderDetails.OrderStatus +
                                    "'," + limitPrice.ToString("0." + new string('#', 20)) +
                                    ");";
                int result = sqlite_cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    log.Db("Successfully updated Position_Data table.");
                }
                else
                {
                    log.Db("Failed to update Position_Data table.");
                }

                sqlite_conn.Close();
            }
            catch (SQLiteException ex)
            {
                log.Trace("Error " + ex.Message.ToString());
                log.Trace(ex.StackTrace.ToString());
            }
        }
        public static void CalculateProfitandLoss(SimpleLogger log, Strategy strategy, SignalFile signalFile)
        {
            var sqlite_conn = CreateConnection();

            List<string> results = new List<string>();
            string amt;
            string side;

            SQLiteCommand sqlite_cmd;

            //var sqlCmd = $"SELECT Side, Amount FROM (SELECT * FROM Position_Data WHERE Strategy = {signalFile.StrategyName} ORDER BY OrderId DESC LIMIT 2)ORDER BY OrderId ASC;";
            var sqlCmd = $"SELECT Side, Amount FROM Position_Data where Strategy = '{signalFile.StrategyName}' LIMIT 2 OFFSET(SELECT COUNT(*) FROM Position_Data WHERE Strategy = '{signalFile.StrategyName}') - 2;";
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = sqlCmd;

            using (SQLiteDataReader sqlite_datareader = sqlite_cmd.ExecuteReader())
            {
                while (sqlite_datareader.Read())
                {
                    side = (string)sqlite_datareader["Side"];
                    decimal t = (decimal)sqlite_datareader["Amount"];
                    amt = Convert.ToString(t);
                    results.Add(side);
                    results.Add(amt);
                }
            }
            sqlite_conn.Close();

            if (results.Count > 2)
            {
                string side1 = results[0];
                //string side2 = results[2];

                decimal amt1 = Convert.ToDecimal(results[1]);
                decimal amt2 = Convert.ToDecimal(results[3]);
                int multiplier = GetMultiplier(strategy);

                decimal proftLoss = (side1 == "BUY") ? ((amt2 - amt1) * multiplier) : ((amt1 - amt2) * multiplier);

                proftLoss *= strategy.LotSize;
                string pnl = string.Format("{0:C2}", proftLoss);

                string type = (side1 == "BUY") ? " Long " : " Short ";

                string msg = string.Format(signalFile.StrategyName + type + "trade Pnl: {0:C2} ", proftLoss);

                log.Info(msg);
                UpdateProfitLoss(log, signalFile, pnl);
            }
        }
        public static void UpdateProfitLoss(SimpleLogger log, SignalFile signalFile, string pnl)
        {
            try
            {
                var sqlite_conn = CreateConnection();
                int result = 0;

                var sql1 = $"REPLACE INTO profit_Loss VALUES ('{ signalFile.TradeDate}', '{signalFile.TradeDateTime}', '{signalFile.StrategyName}', '{pnl}'); ";

                using (SQLiteCommand sqlite_cmd1 = new SQLiteCommand(sql1, sqlite_conn))
                {
                    sqlite_cmd1.CommandText = sql1;
                    result = sqlite_cmd1.ExecuteNonQuery();
                }

                if (result > 0)
                {
                    log.Db("Successfully updated profit_Loss table.");
                }
                else
                {
                    log.Db("Failed to update profit_Loss table.");
                }

                sqlite_conn.Close();

            }
            catch (SQLiteException ex)
            {
                log.Trace("Error " + ex.Message.ToString());
            }
        }
        private static int GetMultiplier(Strategy strategy)
        {
            switch (strategy.Symbol)
            {
                case "MES": return 5;
                case "MGC": return 10;
                case "QM": return 500;
                case "ES": return 50;
                case "CL": return 1000;
                case "GC": return 100;
                case "RTY": return 10;
                case "YM": return 1;
                default: return 1;
            }
        }
    }
}