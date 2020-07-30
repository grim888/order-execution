using IBApi;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace blockshift_ib
{
    public class Broker
    {
        private readonly EClientSocket clientSocket;
        private readonly Parameters @params;
        private readonly SimpleLogger log;
        public static string[] Ordinal = { "st", "nd", "rd", "th" };
        private readonly Slack slack = new Slack();

        public Broker()
        {

        }
        public Broker(EClientSocket clientSocket, SimpleLogger log, Parameters @params, Slack slack)
        {
            this.log = log;
            this.@params = @params;
            this.clientSocket = clientSocket;
            this.slack = slack;
        }
        public void RestartProgram()
        {
            int cnt = 1;

            if (!clientSocket.IsConnected())
            {
                while (!clientSocket.IsConnected())
                {
                    if (clientSocket.IsConnected())
                    {
                        break;
                    }
                    if (cnt > 100)
                    {
                        log.Warning("[IB WARNING] Could not connect to IB Api. Proceeding with Restart.");
                        break;
                    }
                    cnt++;
                }
            }

            // Get file path of current process 
            var filePath = Assembly.GetExecutingAssembly().Location;

            string slackMsg = $"*BlockShift App is Restarting...*";

            slack.SendMessageToSlack(slackMsg, @params);

            log.Warning("BlockShift App is Restarting...");

            Thread.Sleep(TimeSpan.FromSeconds(15));

            var process = Process.Start(filePath);
            log.Info($"New Process Id {process.Id} has been started.");

            var currentProcess = Process.GetCurrentProcess();
            log.Info($"Current Process Id {currentProcess.Id} has been stopped.");
            currentProcess.Kill();

            //   Debugger.Break();
            //  Debugger.Launch(process.Id);            
        }
        public void ConnectionToBroker(Strategy strategy, Positions positions)
        {
            if (!clientSocket.IsConnected())
            {
                ValidateConnectionToBroker(strategy, positions);
            }
            //if (clientSocket.IsConnected())
            //{
            //    UpdateIBPositionDictionary();
            //}
        }
        public void UpdateIBpositions(Strategy strategy, Positions positions, SimpleLogger log)
        {
            if (!clientSocket.IsConnected())
            {
                ValidateConnectionToBroker(strategy, positions);
            }
            if (clientSocket.IsConnected())
            {
                UpdateIBPositionList(strategy, positions);

                Positions.LoadPositions(strategy, positions, log);
            }
        }
        public void ValidateConnectionToBroker(Strategy strategy, Positions positions)
        {
            int count = 0;
            int n;

            if (clientSocket.IsConnected())
            {
                UpdateIBPositionList(strategy, positions);
            }
            else
            {
                EWrapperImpl ibClient = new EWrapperImpl();

                while (!clientSocket.IsConnected())
                {
                    try { clientSocket.eConnect("127.0.0.1", 7496, 100); } catch (Exception ex) { this.log.Trace(ex.Message.ToString()); }

                    if (clientSocket.IsConnected())
                    {
                        var reader = new EReader(clientSocket, ibClient.Signal);

                        reader.Start();

                        new Thread(() =>
                        {
                            while (clientSocket.IsConnected())
                            {
                                ibClient.Signal.waitForSignal();
                                reader.processMsgs();
                            }
                        })
                        { IsBackground = true }.Start();

                        GetNextValideOrderId(ibClient);
                    }

                    if (!clientSocket.IsConnected())
                    {
                        if (count < Ordinal.Length - 1)
                        {
                            n = count;
                        }
                        else
                        {
                            n = Ordinal.Length - 1;
                        }
                        log.Warning($"{count + 1}{Ordinal[n].ToString()} attempt could not connect to IBApi. Retry in {this.@params.secondsToConnect} seconds.");
                        // this.log.Warning($"{count + 1} attempt could not connect to IBApi. Retry in {this.@params.secondsToConnect} seconds.");

                        Thread.Sleep(TimeSpan.FromSeconds(this.@params.secondsToConnect));
                        count++;
                    }
                    if (count > this.@params.connectionRetryLimit)
                    {
                        this.log.Warning("Closing. Could not establish connection.");

                        slack.SendMessageToSlack("*Closing App... Could not Establish Connection to Broker.*", @params);

                        Environment.Exit(1);
                    }
                }
            }
        }
        private void UpdateIBPositionList(Strategy strategy, Positions positions)
        {
            bool connectionOK = true;
            int cnt = 0;
            int n;

            if (!string.IsNullOrEmpty(strategy.Symbol))
            {
                Positions.PositionList.Clear();

                Positions.PositionEndFlag = false;

                clientSocket.reqPositions();

                while (!Positions.PositionEndFlag)
                {
                    if (Positions.PositionEndFlag)
                    {
                        break;
                    }

                    if (!clientSocket.IsConnected())
                    {
                        ValidateConnectionToBroker(strategy, positions);
                    }

                    if (cnt >= @params.maxLoopCount)
                    {
                        connectionOK = false;
                        this.log.Warning("[IB WARNING] Could not update IB Positions.");
                        break;
                    }

                    if (cnt < Ordinal.Length - 1)
                    {
                        n = cnt;
                    }
                    else
                    {
                        n = Ordinal.Length - 1;
                    }
                    this.log.Warning($"Waiting for IB Positions to Update. {cnt + 1}{Ordinal[n].ToString()} try.");
                    cnt++;
                }

                if (connectionOK == false)
                {
                    TimeSpan time = DateTime.Now.TimeOfDay;

                    if (time > new TimeSpan(00, 00, 00) && time < new TimeSpan(08, 00, 00))
                    {
                        slack.SendMessageToSlack("*Connection to Broker must have been lost.*", @params);
                        Thread.Sleep((TimeSpan.FromSeconds(60)));
                        RestartProgram();
                    }
                    else
                    {
                        slack.SendMessageToSlack("*Connection to Broker must have been lost.*", @params);
                        RestartProgram();
                    }
                }
            }
        }
        private void GetNextValideOrderId(EWrapperImpl ibClient)
        {
            ibClient.nextValidId(OrderDetails.NextOrderId);

            int num = 1;

            while (OrderDetails.NextOrderId == 0)
            {
                log.Info("Next Valid OrderId " + OrderDetails.NextOrderId);
                Thread.Sleep(1000);
                if (num > 25)
                {
                    ibClient.nextValidId(OrderDetails.NextOrderId);
                }
                if (num > 50)
                {
                    ibClient.nextValidId(OrderDetails.NextOrderId);
                }
                if (num > 75)
                {
                    break;
                }
                num++;
            }
            log.Info("Next Valid OrderId " + OrderDetails.NextOrderId);
        }
    }
}