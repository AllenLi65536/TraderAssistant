using System;
using System.Collections.Generic;
using System.Data;
using TIBCO.Rendezvous;
using EDLib;
using EDLib.TIBCORV;
using EDLib.SQL;
using System.Data.SqlClient;

namespace tibcoCS
{

    class Program
    {
        // Const parameters
#if DEBUG
        private static readonly RVParameters[] rvParameters = new RVParameters[] { GlobalParameters.WMMLog, GlobalParameters.PM,
                                                                                   GlobalParameters.Liquidity,
                                                                                 // GlobalParameters.TWSE,
                                                                                 // GlobalParameters.Slippage, GlobalParameters.ExecutionReport
                                                                                 };
#else
        private static readonly RVParameters[] rvParameters = new RVParameters[] { GlobalParameters.WMMLog, GlobalParameters.PMnoDaemon,
                                                                                   GlobalParameters.LiquidityNoDaemon,
                                                                                  // GlobalParameters.TWSEnoDaemon,
                                                                                  // GlobalParameters.SlippageNoDaemon, GlobalParameters.ExecutionReportNoDaemon
                                                                                 };
#endif
        private static readonly string lastTDate = TradeDate.LastNTradeDate(1).ToString("yyyyMMdd"); //"20161006";//
        private static readonly string todayStr = DateTime.Today.ToString("yyyyMMdd");
#if !DEBUG
        private static readonly TIBCORVSender sender = new TIBCORVSender("9082", ";239.16.1.6", "10.60.0.128:7500");
#endif
        // Adjustable parameters        
        private const int overLimit = 5000000;
        private const int underLimit = -5000000;

        // Variables   
        static Dictionary<string, Message> marketData = new Dictionary<string, Message>();
        static Dictionary<string, string> WID_UID = new Dictionary<string, string>();
        static Dictionary<string, string> UID_TraderID = new Dictionary<string, string>();
        static Dictionary<string, int> UID_overLimit = new Dictionary<string, int>();
        static Dictionary<string, int> UID_underLimit = new Dictionary<string, int>();
        static DataTable PM_Inventory;
        static DataTable ELN_PGN;
        static DataTable Warrants;


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [MTAThread]
        static void Main() {

            //Initialize Listener components
            ListenerFunc[] callback = new ListenerFunc[rvParameters.Length];
            callback[0] = new ListenerFunc(WMMLog);
            callback[1] = new ListenerFunc(PositionReport);
            callback[2] = new ListenerFunc(MarketLiquidity);
            //callback[3] = new ListenerFunc(MarketDataSnapshot);
            //callback[4] = new ListenerFunc(HedgeInfo);
            //callback[5] = new ListenerFunc(OrdUpdate);
            Console.WriteLine("ListenerFuncs initialized");

            TIBCORVListener listeners = new TIBCORVListener(rvParameters);
            Console.WriteLine("Listeners initialized");

            // Load WID UID lookup table
            // Load UID TraderID lookup table            
            Warrants = MSSQL.ExecSqlQry(@"select distinct TraderId,StkId,WId from Warrants 
                                          where (MarketDate<= CONVERT(varchar(10), GETDATE(), 111) and CONVERT(varchar(10), GETDATE(), 111)<= LastTradeDate) and kgiwrt='自家'",
                GlobalParameters.wmm3SqlConnStr, "Warrants");
            Console.WriteLine("Warrants:" + Warrants.Rows.Count);

            foreach (DataRow Row in Warrants.Rows) {
                if (!UID_TraderID.ContainsKey((string) Row["StkId"]))
                    UID_TraderID.Add((string) Row["StkId"], (string) Row["TraderId"]);
                WID_UID.Add((string) Row["WId"], (string) Row["StkId"]);
            }

            // Load PM_Inventory            
            PM_Inventory = MSSQL.ExecSqlQry(@"SELECT [Symbol],[SecurityDesc],[Underlying],[Position],[Inventory]/1000 Inv,[Trader],[OrigTrader]	       
                    FROM [WMM3].[dbo].[PM_Inventory] as A left join [WMM3].[dbo].[WarrantParam] as B on A.WarrantKey = B.WarrantKey 
                    where A.TradeDate = '" + lastTDate + "'and A.Type = 'WAR' and -Position/(Inventory) > UpLimitReleasingRatio/(100.0-UpLimitReleasingRatio)",
                    GlobalParameters.wmm3SqlConnStr, "PM_Inventory");
            Console.WriteLine("PM_InventoryCount:" + PM_Inventory.Rows.Count);

            sendPM();
            SleepToTarget inv0900 = new SleepToTarget(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 09, 00, 00), sendPM);
            inv0900.Start();

            // Load ELN_PGN data        
            ELN_PGN = MSSQL.ExecSqlQry(@"SELECT [underlying],sum([position]) as sumPos,[user_id]	       	       
                                                FROM [dbo].[eln_pgn_data]
                                                where maturity_date = '" + todayStr + "'"
                                                + "group by underlying, user_id having sum([position]) <> 0",
                                                GlobalParameters.hedgeSqlConnStr, "ELN_PGN");
            Console.WriteLine("ELN_PGN:" + ELN_PGN.Rows.Count);

            sendELN();
            SleepToTarget eln0900 = new SleepToTarget(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 09, 00, 00), sendELN);
            eln0900.Start();
            SleepToTarget eln1320 = new SleepToTarget(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 13, 20, 00), sendELN);
            eln1320.Start();

            //Load parameter table
            using (SqlConnection conn2 = new SqlConnection("Data Source=10.10.1.27;Initial Catalog=WMM3;User ID=hedgeuser;Password=hedgeuser")) { //GlobalParameters.wmm3SqlConnStr 
                conn2.Open();
                using (SqlCommand cmd2 = new SqlCommand("SELECT [UnderlyingID], [OverLimit], [UnderLimit] FROM[dbo].[MessageWindowUnderlyingParam]", conn2))
                using (SqlDataReader parameterReader = cmd2.ExecuteReader()) {
                    while (parameterReader.Read()) {
                        UID_overLimit.Add((string) parameterReader[0], 10000 * (int) parameterReader[1]);
                        UID_underLimit.Add((string) parameterReader[0], 10000 * (int) parameterReader[2]);
                    }
                }
            }

            // Block here
            listeners.Listen(callback);

            // Force optimizer to keep alive listeners up to this point.
            GC.KeepAlive(listeners);

            // Should not go here
            System.Environment.Exit(1);

        }

        static void sendPM() {

            foreach (DataRow Row in PM_Inventory.Rows) {
                Message sendMsg = new Message();
                string WID = Row["Symbol"].ToString();
                string UID = WID_UID[WID];
                sendMsg.AddField("MSGTYPE", "MessageWindow");
                sendMsg.AddField("Time", DateTime.Now);
                sendMsg.AddField("TraderID", UID_TraderID[UID]); // Row["Trader"].ToString()             
                sendMsg.AddField("SymbolNo", UID);
                sendMsg.AddField("TODO", "No");
                sendMsg.AddField("Message", WID + " 庫存僅剩 " + Row["Inv"] + " 張");
                sendMsg.AddField("Type", 1);
                Console.WriteLine(sendMsg.ToString());
#if !DEBUG
                sender.Send(sendMsg, "TW.ED.WMM3.MessageWindow");
#endif
            }
        }
        static void sendELN() {

            foreach (DataRow Row in ELN_PGN.Rows) {
                Message sendMsg = new Message();
                string UID = Row["underlying"].ToString();
                sendMsg.AddField("MSGTYPE", "MessageWindow");
                sendMsg.AddField("Time", DateTime.Now);
                if (UID_TraderID.ContainsKey(UID))
                    sendMsg.AddField("TraderID", UID_TraderID[UID]);
                else
                    sendMsg.AddField("TraderID", "00" + Row["user_id"].ToString());
                sendMsg.AddField("SymbolNo", UID);
                sendMsg.AddField("TODO", "No");
                sendMsg.AddField("Message", "ELN 交割股數提醒: " + double.Parse(Row["sumPos"].ToString()).ToString("N0"));
                sendMsg.AddField("Type", 2);
                Console.WriteLine(sendMsg.ToString());
#if !DEBUG
                sender.Send(sendMsg, "TW.ED.WMM3.MessageWindow");
#endif
            }
        }

        static string StructureTime(string STAMP) {
            return STAMP.Substring(0, 4) + "-" + STAMP.Substring(4, 2) + "-" + STAMP.Substring(6, 2) + " "
                + STAMP.Substring(8, 2) + ":" + STAMP.Substring(10, 2) + ":" + STAMP.Substring(12, 2) + "." + STAMP.Substring(14, 3);
        }

        static void WMMLog(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
            Message message = messageReceivedEventArgs.Message;

            string type = string.Empty;

            try {
                type = message.GetField("MSGTYPE").Value.ToString();
            } catch (Exception e) {
                Console.WriteLine(e);
                return;
            }

            switch (type) {
                case "ServerMsg":
                    //if (message.GetField("Message").Value.ToString()[18] == 'D')
                    return;
                //break;
                case "ServerMsg1":
                    //if (message.GetField("Message").Value.ToString().StartsWith("D"))
                    return;
                //break;

                case "ServerLog":

                    if (message.GetField("Type") == null)
                        return;
                    string Type = message.GetField("Type").Value.ToString();
                    if (!(Type == "100" || Type == "504" || Type == "503"))
                        return;
                    string messageString = message.GetField("Message").Value.ToString();
                    string STAMP = StructureTime(message.GetField("STAMP").Value.ToString());
                    string WID = message.GetField("WID").Value.ToString();
                    string UID = WID_UID[WID];
                    //string UserID = message.GetField("USERID").Value.ToString();
                    Message sendMsg = new Message();

                    sendMsg.AddField("MSGTYPE", "MessageWindow");
                    sendMsg.AddField("Time", STAMP);
                    sendMsg.AddField("TraderID", UID_TraderID[UID]);
                    sendMsg.AddField("SymbolNo", UID);
                    sendMsg.AddField("TODO", "No");

                    int index;
                    if (Type == "100") {
                        if (messageString.Contains("DelayMode3")) {
                            sendMsg.AddField("Message", "進入造市情境三(他家權證被攻擊)");
                            sendMsg.AddField("Type", 3);
                            Console.WriteLine(sendMsg.ToString());
#if !DEBUG
                            sender.Send(sendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                        } else if ((index = messageString.IndexOf("DelayMode4")) != -1) {
                            Console.WriteLine(messageString[index + 11] + " " + messageString[index + 12]);

                            if (messageString[index + 11] == 'L') {
                                sendMsg.AddField("Message", "進入造市情境四(自家權證被多方攻擊)");
                                sendMsg.AddField("Type", 22);
                            } else if (messageString[index + 11] == 'S') {
                                sendMsg.AddField("Message", "進入造市情境四(自家權證被空方攻擊)");
                                sendMsg.AddField("Type", 32);
                            } else {
                                sendMsg.AddField("Message", "進入造市情境四(自家權證被攻擊)");
                                sendMsg.AddField("Type", 4);
                            }

                            if (messageString[index + 12] == 'S') {
                                Message sendMsg2 = new Message();

                                sendMsg2.AddField("MSGTYPE", "MessageWindow");
                                sendMsg2.AddField("Time", STAMP);
                                sendMsg2.AddField("TraderID", UID_TraderID[UID]);
                                sendMsg2.AddField("SymbolNo", UID);
                                sendMsg2.AddField("TODO", "No");
                                sendMsg2.AddField("Message", "進入造市情境四(自家權證被空方攻擊)");
                                sendMsg2.AddField("Type", 32);
#if !DEBUG
                                sender.Send(sendMsg2, "TW.ED.WMM3.MessageWindow");
#endif
                            }

                            Console.WriteLine(sendMsg.ToString());
#if !DEBUG
                            sender.Send(sendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                        }

                    }
                    if (Type == "503") {
                        sendMsg.AddField("Message", "處置股票");
                        sendMsg.AddField("Type", 5);
                        Console.WriteLine(sendMsg.ToString());
#if !DEBUG
                        sender.Send(sendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                    }
                    if (Type == "504") {
                        sendMsg.AddField("Message", "盤中暫緩撮合");
                        sendMsg.AddField("Type", 6);
                        Console.WriteLine(sendMsg.ToString());
#if !DEBUG
                        sender.Send(sendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                    }
                    if (Type == "505") {
                        sendMsg.AddField("Message", "開盤暫緩撮合");
                        sendMsg.AddField("Type", 7);
                        Console.WriteLine(sendMsg.ToString());
#if !DEBUG
                        sender.Send(sendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                    }

                    break;
                default:
                    break;
            }
        }

        static void PositionReport(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
            Message message = messageReceivedEventArgs.Message;
            string type = string.Empty;

            try {
                type = message.GetField("MSGTYPE").Value.ToString();
            } catch (Exception e) {
                Console.WriteLine(e);
                return;
            }

            switch (type) {
                case "PositionReport":
                    if (message.GetField("Depth").Value.ToString() != "3")
                        return;
                    string symbol = message.GetField("Symbol").Value.ToString();
                    //if (!MarketData.ContainsKey(symbol))
                    //   break;

                    // double overOrLack = double.Parse(message.GetField("OverOrLackHedgeNumber").Value.ToString());
                    // double lastPx = double.Parse(MarketData[symbol].GetField("LastPx").Value.ToString());
                    double overOrLackPx = double.Parse(message.GetField("DeltaInCash").Value.ToString()); // OverOrLack * LastPx;
                    int limit;
                    if (overOrLackPx >= 0 && UID_overLimit.TryGetValue(symbol, out limit)) {
                        //if ((overOrLackPx >= 0 && overOrLackPx < UID_overLimit[symbol]) || (overOrLackPx < 0 && overOrLackPx > UID_underLimit[symbol]))
                        if (overOrLackPx < limit)
                            return;
                    } else if (overOrLackPx < 0 && UID_underLimit.TryGetValue(symbol, out limit)) {
                        if (overOrLackPx > limit)
                            return;
                    } else {
                        if ((overOrLackPx >= 0 && overOrLackPx < overLimit) || (overOrLackPx < 0 && overOrLackPx > underLimit))
                            return;
                    }

                    string STAMP = StructureTime(message.GetField("STAMP").Value.ToString());

                    Message sendMsg = new Message();
                    sendMsg.AddField("MSGTYPE", "MessageWindow");
                    sendMsg.AddField("Time", STAMP);
                    if (UID_TraderID.ContainsKey(symbol))
                        sendMsg.AddField("TraderID", UID_TraderID[symbol]);//message.GetField("Trader").Value.ToString()
                    else
                        sendMsg.AddField("TraderID", message.GetField("Trader").Value.ToString());

                    sendMsg.AddField("SymbolNo", symbol);
                    sendMsg.AddField("TODO", "No");
                    if (overOrLackPx > 0) {
                        sendMsg.AddField("Message", "Over:" + Math.Round(overOrLackPx / 10000) + "萬");
                        sendMsg.AddField("Type", 30);
                    } else {
                        sendMsg.AddField("Message", "Lack:" + Math.Round(overOrLackPx / 10000) + "萬");
                        sendMsg.AddField("Type", 20);
                    }

                    Console.WriteLine(sendMsg.ToString());
#if !DEBUG
                    sender.Send(sendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                    break;
                default:

                    break;
            }
        }

        static void MarketLiquidity(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
            Message message = messageReceivedEventArgs.Message;
            string type = string.Empty;

            try {
                type = message.GetField("MSGTYPE").Value.ToString();
            } catch (Exception e) {
                Console.WriteLine(e);
                return;
            }

            switch (type) {
                case "MarketLiquidityInfo":
                    //var watch = System.Diagnostics.Stopwatch.StartNew();                    
                    double[] callBid = Array.ConvertAll(message.GetField("CallBidQty").Value.ToString().Split(','), double.Parse);
                    double[] callAsk = Array.ConvertAll(message.GetField("CallAskQty").Value.ToString().Split(','), double.Parse);
                    double[] putBid = Array.ConvertAll(message.GetField("PutBidQty").Value.ToString().Split(','), double.Parse);
                    double[] putAsk = Array.ConvertAll(message.GetField("PutAskQty").Value.ToString().Split(','), double.Parse);

                    string[] issuers = message.GetField("IssuerList").Value.ToString().Split(',');
                    string UID = message.GetField("UnderlyingID").Value.ToString();
                    double uBidQ = double.Parse(message.GetField("UnderlyBidQty").Value.ToString());
                    double uAskQ = double.Parse(message.GetField("UnderlyAskQty").Value.ToString());
                    //double uBidP = double.Parse(message.GetField("UnderlyBidPx").Value.ToString());
                    //double uAskP = double.Parse(message.GetField("UnderlyAskPx").Value.ToString());
                    int kgi = -1;
                    for (int i = 0; i < issuers.Length; i++)
                        if (issuers[i] == "凱基") {
                            kgi = i;
                            break;
                        }

                    if (kgi == -1)
                        return;

                    double kgiLong = callBid[kgi] + putAsk[kgi];
                    double kgiShort = callAsk[kgi] + putBid[kgi];
                    double otherLong = 0;
                    double otherShort = 0;

                    for (int i = 0; i < issuers.Length; i++) {
                        if (i == kgi)
                            continue;
                        otherLong += callBid[i] + putAsk[i];
                        otherShort += callAsk[i] + putBid[i];
                    }


                    if (kgiLong > otherLong || kgiShort > otherShort) {
                        string STAMP = StructureTime(message.GetField("STAMP").Value.ToString());
                        Message sendMsg = new Message();
                        sendMsg.AddField("MSGTYPE", "MessageWindow");
                        sendMsg.AddField("Time", STAMP);
                        //if (UID_TraderID.ContainsKey(UID))
                        sendMsg.AddField("TraderID", UID_TraderID[UID]);//message.GetField("Trader").Value.ToString()
                        sendMsg.AddField("SymbolNo", UID);
                        sendMsg.AddField("TODO", "No");
                        if (kgiLong > otherLong) {
                            sendMsg.AddField("Message", "自家權證多:" + kgiLong + " 他家多:" + otherLong);
                            sendMsg.AddField("Type", 31);
                        } else if (kgiShort > otherShort) {
                            sendMsg.AddField("Message", "自家權證空:" + kgiShort + " 他家空:" + otherShort);
                            sendMsg.AddField("Type", 21);
                        } else
                            sendMsg.AddField("Message", " ");

#if !DEBUG
                        sender.Send(sendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                        Console.WriteLine(sendMsg.ToString());
                        Console.WriteLine("uBid:" + uBidQ + " uAsk:" + uAskQ + " kgiLong:" + kgiLong + " kgiShort:" + kgiShort + " otherLong:" + otherLong + " otherShort:" + otherShort);
                    }

                    /*
                    if (kgiLong + otherLong > 40 * uBidQ || kgiShort + otherShort > 40 * uAskQ) {
                        string STAMP = StructureTime(message.GetField("STAMP").Value.ToString());
                        Message SendMsg = new Message();
                        SendMsg.AddField("MSGTYPE", "MessageWindow");
                        SendMsg.AddField("Time", STAMP);
                        //if (UID_TraderID.ContainsKey(UID))
                        SendMsg.AddField("TraderID", UID_TraderID[UID]);//message.GetField("Trader").Value.ToString()
                        SendMsg.AddField("SymbolNo", UID);
                        SendMsg.AddField("TODO", "No");
                        if (kgiLong + otherLong > 20 * uBidQ)
                            SendMsg.AddField("Message", "市場權證多:" + kgiLong + " 個股多:" + uBidQ);
                        else if (kgiShort + otherShort > 20 * uAskQ)
                            SendMsg.AddField("Message", "市場權證空:" + kgiShort + " 個股空:" + uAskQ);
                        else
                            SendMsg.AddField("Message", " ");
                        SendMsg.AddField("Type", 9);
#if !DEBUG
                        Sender.Send(SendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                        Console.WriteLine(SendMsg.ToString());
                        Console.WriteLine("uBid:" + uBidQ + " uAsk:" + uAskQ + " kgiLong:" + kgiLong + " kgiShort:" + kgiShort + " otherLong:" + otherLong + " otherShort:" + otherShort);
                    }
                    */
                    //watch.Stop();
                    //Console.WriteLine( watch.ElapsedTicks + " " + watch.ElapsedMilliseconds);

                    break;
                default:

                    break;
            }

        }


        static void MarketDataSnapshot(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
            Message message = messageReceivedEventArgs.Message;

            string type = string.Empty;
            string symbol = string.Empty;

            try {
                symbol = message.GetField("Symbol").Value.ToString();
                type = message.GetField("MSGTYPE").Value.ToString();
            } catch (Exception e) {
                Console.WriteLine(e);
                return;
            }

            switch (type) {
                case "MarketDataSnapshotFullRefresh":
                    if (marketData.ContainsKey(symbol))
                        marketData[symbol] = message;
                    else
                        marketData.Add(symbol, message);

                    //Console.WriteLine(message.ToString());
                    //Console.WriteLine(MarketData[Symbol].GetField("Symbol").Value + " " + MarketData[Symbol].GetField("LastPx").Value);
                    break;
                default:
                    Console.WriteLine(message);
                    break;
            }
        }

        static void HedgeInfo(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
            Message message = messageReceivedEventArgs.Message;
            string type = string.Empty;

            try {
                type = message.GetField("MSGTYPE").Value.ToString();
            } catch (Exception e) {
                Console.WriteLine(e);
                return;
            }

            //string Buffer;

            switch (type) {
                case "HedgeInfo":
                    /*Buffer = message.GetField("STAMP").Value.ToString() + "," + message.GetField("WarrantOrderID").Value.ToString();
                    Buffer += "," + message.GetField("UnderlySymbol").Value.ToString();
                    Buffer += "," + message.GetField("HedgeLots").Value.ToString();
                    Buffer += "," + message.GetField("HedgeOrderPx").Value.ToString();
                    Buffer += "," + message.GetField("HedgeOrderQty").Value.ToString();
                    Buffer += "," + message.GetField("UnderlyingBidPx").Value.ToString();
                    Buffer += "," + message.GetField("UnderlyingBidQty").Value.ToString();
                    Buffer += "," + message.GetField("UnderlyingAskPx").Value.ToString();
                    Buffer += "," + message.GetField("UnderlyingAskQty").Value.ToString();
                    Buffer += "," + message.GetField("HedgeOrderBS").Value.ToString();
                    Buffer += "," + message.GetField("WarrantFilledQty").Value.ToString();*/
                    //Console.WriteLine(Buffer);
                    break;
                default:
                    break;
            }

        }

        static void OrdUpdate(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
            Message message = messageReceivedEventArgs.Message;
            string type = string.Empty;

            try {
                type = message.GetField("MSGTYPE").Value.ToString();
            } catch (Exception e) {
                Console.WriteLine(e);
                return;
            }
            //string Buffer;
            switch (type) {
                case "STKordupdate":
                    /*Buffer = message.GetField("STAMP").Value.ToString() + "," + message.GetField("SECID").Value.ToString();
                    Buffer += "," + message.GetField("EXCH").Value.ToString();
                    Buffer += "," + message.GetField("SIDE").Value.ToString();
                    Buffer += "," + message.GetField("FQTY").Value.ToString();
                    Buffer += "," + message.GetField("FPRICE").Value.ToString();
                    Buffer += "," + message.GetField("STAT").Value.ToString();
                    Buffer += "," + message.GetField("EXECID").Value.ToString();
                    Buffer += "," + message.GetField("OPERATOR").Value.ToString();*/
                    //Console.WriteLine(Buffer);
                    break;
                case "FUTordupdate":
                    /*//sw = new StreamWriter("D:\\DailyData\\" + todayStr + "Future.txt", true);
                    Buffer = message.GetField("STAMP").Value.ToString() + "," + message.GetField("COMMODITY").Value.ToString();
                    Buffer += "," + message.GetField("SETMTH").Value.ToString();
                    Buffer += "," + message.GetField("EXCH").Value.ToString();
                    Buffer += "," + message.GetField("BSCODE").Value.ToString();
                    Buffer += "," + message.GetField("TRADETYPE").Value.ToString();
                    Buffer += "," + message.GetField("ORDERID").Value.ToString();
                    Buffer += "," + message.GetField("FQTY").Value.ToString();
                    Buffer += "," + message.GetField("FPRICE").Value.ToString();
                    Buffer += "," + message.GetField("STAT").Value.ToString();
                    Buffer += "," + message.GetField("EXECID").Value.ToString();
                    Buffer += "," + message.GetField("OPERATOR").Value.ToString();
                    sw.WriteLine(Buffer);
                    Console.WriteLine(Buffer);
                    sw.Close();*/
                    break;
                default:
                    /* sw = new StreamWriter("D:\\Dailydata\\" + todayStr + "Other.txt" , true);
                     sw.WriteLine(type);
                     sw.Close();*/

                    break;
            }

        }
    }
}
