// #define dosend
using System;
using System.Collections.Generic;
using System.Data;
//using System.Data.SqlClient;
using TIBCO.Rendezvous;
using EDLib;
using EDLib.TIBCORV;

namespace tibcoCS
{

    class Program
    {        
        // Const parameters       
        private static readonly string[] service = {
                                                     "9082",
                                                     "9013",
                                                     null
                                                     //null,
                                                     //null,                                                  
                                                     //null
                                                   };
        private static readonly string[] network = {
                                                     ";239.16.1.6",
                                                     "172.31.2;239.16.1.72",
                                                     "172.31.2;239.16.1.72"
                                                     //"172.31.2;239.16.1.72",
                                                     //"172.31.2;239.16.1.72",
                                                     //"172.31.2;239.16.1.72"
                                                   };
        private static readonly string[] daemon = {
                                                    "10.60.0.128:7500",
                                                    "10.60.0.101:7500",
                                                    "10.60.0.128:7500" //"172.31.2.1:7500",
                                                    //"10.60.0.128:7500", 
                                                    //"10.60.0.101:7500",
                                                    //"10.60.0.129:7500"
                                                  };
        private static readonly string[] topic = {
                                                   "TW.ED.WMM3.CLIENT.LOG" ,
                                                   "TW.WMM3.PM.PositionReport.>" ,
                                                   "MarketLiquidityInfo.*"
                                                   //"TWSE.MarketDataSnapshotFullRefresh",
                                                   //"TW.WMM3.SlippageCost.HedgeInfo.PROD" ,
                                                   //"TW.WMM3.FilledReportRelayService.ExecutionReport.PROD"
                                                 };
        private static readonly string LastTDate = TradeDate.LastNTradeDate(1); // LastTradeDate();//"20161006";//
        private static readonly string todayStr = DateTime.Today.ToString("yyyyMMdd");
#if dosend
        private static readonly TIBCORVSender Sender = new TIBCORVSender("9082", ";239.16.1.6", "10.60.0.128:7500");
#endif
        // Adjustable parameters        
        private const int OverLimit = 5000000;
        private const int UnderLimit = -5000000;

        // Variables   
        static Dictionary<string, Message> MarketData = new Dictionary<string, Message>();
        static Dictionary<string, string> WID_UID = new Dictionary<string, string>();
        static Dictionary<string, string> UID_TraderID = new Dictionary<string, string>();       
        static DataTable PM_Inventory;
        static DataTable ELN_PGN;       
        static DataTable Warrants;

        //static StreamWriter temp = new StreamWriter("./temptile.txt");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [MTAThread]
        static void Main() {
            //DateTime close_time = new DateTime(DateTime.Now.Year , DateTime.Now.Month , DateTime.Now.Day , 13 , 35 , 00);
            //SleepToTarget Temp = new SleepToTarget(close_time , flush_sw);
            //Temp.Start();

            //Initialize Listener components
            ListenerFunc[] callback = new ListenerFunc[service.Length];
            callback[0] = new ListenerFunc(OnMessageReceived2);
            callback[1] = new ListenerFunc(OnMessageReceived3);
            callback[2] = new ListenerFunc(OnMessageReceived5);
            //callback[3] = new ListenerFunc(OnMessageReceived4);            
            //callback[4] = new ListenerFunc(OnMessageReceived);
            //callback[5] = new ListenerFunc(OnMessageReceived1);
            Console.WriteLine("ListeneFunc initialized");
            TIBCORVListener Listeners = new TIBCORVListener(service, network, daemon);
            Console.WriteLine("Listeners initialized");


            //Load WID UID lookup table
            // Load UID TraderID lookup table            
            Warrants = Utility.ExecSqlQry("select distinct TraderId,StkId,WId from Warrants where (MarketDate<= CONVERT(varchar(10), GETDATE(), 111) and CONVERT(varchar(10), GETDATE(), 111)<= LastTradeDate) and kgiwrt='自家'", 
                "Data Source=10.101.10.5;Initial Catalog=WMM3;User ID=hedgeuser;Password=hedgeuser", "Warrants");
            Console.WriteLine("Warrants:" + Warrants.Rows.Count);

            foreach (DataRow Row in Warrants.Rows) {
                if (!UID_TraderID.ContainsKey((string) Row["StkId"]))
                    UID_TraderID.Add((string) Row["StkId"], (string) Row["TraderId"]);
                WID_UID.Add((string) Row["WId"], (string) Row["StkId"]);
            }

            // Load PM_Inventory            
            PM_Inventory = Utility.ExecSqlQry(@"SELECT [Symbol],[SecurityDesc],[Underlying],[Position],[Inventory]/1000 Inv,[Trader],[OrigTrader]	       
                    FROM [WMM3].[dbo].[PM_Inventory] as A left join [WMM3].[dbo].[WarrantParam] as B on A.WarrantKey = B.WarrantKey 
                    where A.TradeDate = '" + LastTDate + "'and A.Type = 'WAR' and -Position/(Inventory) > UpLimitReleasingRatio/(100.0-UpLimitReleasingRatio)",
                    "Data Source=10.101.10.5;Initial Catalog=WMM3;User ID=hedgeuser;Password=hedgeuser", "PM_Inventory");
            Console.WriteLine("PM_InventoryCount:" + PM_Inventory.Rows.Count);

            sendPM();
            SleepToTarget inv0900 = new SleepToTarget(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 09, 00, 00), sendPM);
            inv0900.Start();

            // Load ELN_PGN data        
            ELN_PGN = Utility.ExecSqlQry(@"SELECT [underlying],sum([position]) as sumPos,[user_id]	       	       
                                                FROM [dbo].[eln_pgn_data]
                                                where maturity_date = '" + todayStr + "'"
                                                + "group by underlying, user_id having sum([position]) <> 0",
                                                "Data Source=10.101.10.5;Initial Catalog=HEDGE;User ID=hedgeuser;Password=hedgeuser",
                                                "ELN_PGN");
            Console.WriteLine("ELN_PGN:" + ELN_PGN.Rows.Count);

            sendELN();
            SleepToTarget eln0900 = new SleepToTarget(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 09, 00, 00), sendELN);
            eln0900.Start();
            SleepToTarget eln1320 = new SleepToTarget(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 13, 20, 00), sendELN);
            eln1320.Start();


            // Block here
            Listeners.Listen(topic, callback);

            // Force optimizer to keep alive listeners up to this point.
            GC.KeepAlive(Listeners);

            // Should not go here
            System.Environment.Exit(1);

        }

        static void sendPM() {

            foreach (DataRow Row in PM_Inventory.Rows) {
                Message SendMsg = new Message();
                string WID = Row["Symbol"].ToString();
                string UID = WID_UID[WID];
                SendMsg.AddField("MSGTYPE", "MessageWindow");
                SendMsg.AddField("Time", DateTime.Now);
                SendMsg.AddField("TraderID", UID_TraderID[UID]); // Row["Trader"].ToString()             
                SendMsg.AddField("SymbolNo", UID);
                SendMsg.AddField("TODO", "No");
                SendMsg.AddField("Message", WID + " 庫存僅剩 " + Row["Inv"] + " 張");
                SendMsg.AddField("Type", 1);
                Console.WriteLine(SendMsg.ToString());
#if dosend
                Sender.Send(SendMsg, "TW.ED.WMM3.MessageWindow");
#endif
            }
        }
        static void sendELN() {
            // Load ELN_PGN data
            foreach (DataRow Row in ELN_PGN.Rows) {
                Message SendMsg = new Message();
                string UID = Row["underlying"].ToString();
                SendMsg.AddField("MSGTYPE", "MessageWindow");
                SendMsg.AddField("Time", DateTime.Now);
                if (UID_TraderID.ContainsKey(UID))
                    SendMsg.AddField("TraderID", UID_TraderID[UID]);
                else
                    SendMsg.AddField("TraderID", "00" + Row["user_id"].ToString());
                SendMsg.AddField("SymbolNo", UID);
                SendMsg.AddField("TODO", "No");
                SendMsg.AddField("Message", "ELN 交割股數提醒: " + double.Parse(Row["sumPos"].ToString()).ToString("N0"));
                SendMsg.AddField("Type", 2);
                Console.WriteLine(SendMsg.ToString());
#if dosend
                Sender.Send(SendMsg, "TW.ED.WMM3.MessageWindow");
#endif
            }
        }

        static string StructureTime(string STAMP) {
            return STAMP.Substring(0, 4) + "-" + STAMP.Substring(4, 2) + "-" + STAMP.Substring(6, 2) + " " + STAMP.Substring(8, 2) + ":" + STAMP.Substring(10, 2) + ":" + STAMP.Substring(12, 2) + "." + STAMP.Substring(14, 3);
        }

        static void OnMessageReceived2(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
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
                    if (message.GetField("Message").Value.ToString()[18] == 'D')
                        break;

                    //Buffer = message.GetField("STAMP").Value.ToString() + "," + message.GetField("Message").Value.ToString();
                    //Buffer += "," + message.GetField("USERID").Value.ToString().Substring(3 , 4);
                    //Console.WriteLine(Buffer);

                    break;
                case "ServerMsg1":
                    if (message.GetField("Message").Value.ToString().StartsWith("D"))
                        break;

                    //Buffer = message.GetField("STAMP").Value.ToString() + "," + message.GetField("Message").Value.ToString();
                    //Buffer += "," + message.GetField("USERID").Value.ToString().Substring(3 , 4);
                    //Console.WriteLine(Buffer);

                    break;

                case "ServerLog":

                    if (message.GetField("Type") == null)
                        return;
                    string Type = message.GetField("Type").Value.ToString();
                    if (!(Type == "100" || Type == "504" || Type == "503"))
                        break;
                    string MessageString = message.GetField("Message").Value.ToString();
                    string STAMP = StructureTime(message.GetField("STAMP").Value.ToString());
                    string WID = message.GetField("WID").Value.ToString();
                    string UID = WID_UID[WID];
                    //string UserID = message.GetField("USERID").Value.ToString();
                    Message SendMsg = new Message();

                    SendMsg.AddField("MSGTYPE", "MessageWindow");
                    SendMsg.AddField("Time", STAMP);
                    SendMsg.AddField("TraderID", UID_TraderID[UID]);
                    SendMsg.AddField("SymbolNo", UID); // WID + "/" +
                    SendMsg.AddField("TODO", "No");

                    if (Type == "100") {
                        if (MessageString.Contains("DelayMode3")) {
                            SendMsg.AddField("Message", "進入造市情境三(他家權證被攻擊)");
                            SendMsg.AddField("Type", 3);
                            Console.WriteLine(SendMsg.ToString());
#if dosend
                            Sender.Send(SendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                        } else if (MessageString.Contains("DelayMode4")) {
                            SendMsg.AddField("Message", "進入造市情境四(自家權證被攻擊)");
                            SendMsg.AddField("Type", 4);
                            Console.WriteLine(SendMsg.ToString());
#if dosend
                            Sender.Send(SendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                        }

                    }
                    if (Type == "503") {
                        SendMsg.AddField("Message", "處置股票");
                        SendMsg.AddField("Type", 5);
                        Console.WriteLine(SendMsg.ToString());
#if dosend
                        Sender.Send(SendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                    }
                    if (Type == "504") {
                        SendMsg.AddField("Message", "盤中暫緩撮合");
                        SendMsg.AddField("Type", 6);
                        Console.WriteLine(SendMsg.ToString());
#if dosend
                        Sender.Send(SendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                    }
                    if (Type == "505") {
                        SendMsg.AddField("Message", "開盤暫緩撮合");
                        SendMsg.AddField("Type", 7);
                        Console.WriteLine(SendMsg.ToString());
#if dosend
                        Sender.Send(SendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                    }

                    break;
                default:

                    break;
            }

        }


        static void OnMessageReceived3(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
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
                        break;
                    string Symbol = message.GetField("Symbol").Value.ToString();
                    //if (!MarketData.ContainsKey(Symbol))
                    //   break;

                    // double OverOrLack = double.Parse(message.GetField("OverOrLackHedgeNumber").Value.ToString());
                    // double LastPx = double.Parse(MarketData[Symbol].GetField("LastPx").Value.ToString());
                    double OverOrLackPx = double.Parse(message.GetField("DeltaInCash").Value.ToString()); // OverOrLack * LastPx;

                    if ((OverOrLackPx >= 0 && OverOrLackPx < OverLimit) || (OverOrLackPx < 0 && OverOrLackPx > UnderLimit))
                        break;

                    string STAMP = StructureTime(message.GetField("STAMP").Value.ToString());

                    Message SendMsg = new Message();
                    SendMsg.AddField("MSGTYPE", "MessageWindow");
                    SendMsg.AddField("Time", STAMP);
                    if (UID_TraderID.ContainsKey(Symbol))
                        SendMsg.AddField("TraderID", UID_TraderID[Symbol]);//message.GetField("Trader").Value.ToString()
                    else
                        SendMsg.AddField("TraderID", message.GetField("Trader").Value.ToString());

                    SendMsg.AddField("SymbolNo", Symbol);
                    SendMsg.AddField("TODO", "No");
                    if (OverOrLackPx > 0)
                        SendMsg.AddField("Message", "Over:" + Math.Round(OverOrLackPx / 10000) + "萬");
                    else
                        SendMsg.AddField("Message", "Lack:" + Math.Round(OverOrLackPx / 10000) + "萬");
                    SendMsg.AddField("Type", 8);

                    Console.WriteLine(SendMsg.ToString());
#if dosend
                    Sender.Send(SendMsg, "TW.ED.WMM3.MessageWindow");
#endif
                    // Buffer = message.GetField("STAMP").Value.ToString() + "," + Symbol;
                    //Buffer += "," + message.GetField("TriggerType").Value.ToString();
                    //Buffer += "," + OverOrLackPx;
                    //Buffer += "," + message.GetField("OverOrLackHedgeNumberT1").Value.ToString();
                    //sw.WriteLine(Buffer);

                    break;
                default:

                    break;
            }
        }

        static void OnMessageReceived5(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
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
                    //Console.WriteLine(message.ToString());
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
                    for (int i = 0; i < issuers.Length; i++) {
                        if (issuers[i] == "凱基") {                           
                            kgi = i;
                            break;
                        }
                    }
                    if (kgi == -1)
                        break;

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



                    /*if (kgiLong > 3*uBidQ || kgiShort > 3*uAskQ) {
                        string STAMP = StructureTime(message.GetField("STAMP").Value.ToString());
                        Message SendMsg = new Message();
                        SendMsg.AddField("MSGTYPE", "MessageWindow");
                        SendMsg.AddField("Time", STAMP);
                        //if (UID_TraderID.ContainsKey(UID))
                        SendMsg.AddField("TraderID", UID_TraderID[UID]);//message.GetField("Trader").Value.ToString()
                        SendMsg.AddField("SymbolNo", UID);
                        SendMsg.AddField("TODO", "No");
                        if (kgiLong > uBidQ)
                            SendMsg.AddField("Message", "自家權證多:" + kgiLong + " 個股多:" + uBidQ);
                        else if (kgiShort > uAskQ)
                            SendMsg.AddField("Message", "自家權證空:" + kgiShort + " 個股空:" + uAskQ);
                        else
                            SendMsg.AddField("Message", " ");
                        SendMsg.AddField("Type", 9);
                        Console.WriteLine(SendMsg.ToString());
                        Console.WriteLine("uBid:" + uBidQ + " uAsk:" + uAskQ + " kgiLong:" + kgiLong + " kgiShort:" + kgiShort + " otherLong:" + otherLong + " otherShort:" + otherShort);
                    }*/
                    if (kgiLong > otherLong || kgiShort > otherShort) {
                        string STAMP = StructureTime(message.GetField("STAMP").Value.ToString());
                        Message SendMsg = new Message();
                        SendMsg.AddField("MSGTYPE", "MessageWindow");
                        SendMsg.AddField("Time", STAMP);
                        //if (UID_TraderID.ContainsKey(UID))
                        SendMsg.AddField("TraderID", UID_TraderID[UID]);//message.GetField("Trader").Value.ToString()
                        SendMsg.AddField("SymbolNo", UID);
                        SendMsg.AddField("TODO", "No");
                        if (kgiLong > otherLong)
                            SendMsg.AddField("Message", "自家權證多:" + kgiLong + " 他家多:" + otherLong);
                        else if (kgiShort > otherShort)
                            SendMsg.AddField("Message", "自家權證空:" + kgiShort + " 他家空:" + otherShort);
                        else
                            SendMsg.AddField("Message", " ");
                        SendMsg.AddField("Type", 9);
                        Console.WriteLine(SendMsg.ToString());
                        Console.WriteLine("uBid:" + uBidQ + " uAsk:" + uAskQ + " kgiLong:" + kgiLong + " kgiShort:" + kgiShort + " otherLong:" + otherLong + " otherShort:" + otherShort);
                    }

                    //watch.Stop();
                    //Console.WriteLine( watch.ElapsedTicks + " " + watch.ElapsedMilliseconds);

                    break;
                default:

                    break;
            }

        }


        static void OnMessageReceived4(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
            Message message = messageReceivedEventArgs.Message;
            //temp.WriteLine(message.ToString());

            string type = string.Empty;
            string Symbol = string.Empty;

            try {
                Symbol = message.GetField("Symbol").Value.ToString();
                type = message.GetField("MSGTYPE").Value.ToString();
            } catch (Exception e) {
                Console.WriteLine(e);
                return;
            }

            switch (type) {
                case "MarketDataSnapshotFullRefresh":
                    if (MarketData.ContainsKey(Symbol))
                        MarketData[Symbol] = message;
                    else
                        MarketData.Add(Symbol, message);


                    //Console.WriteLine(message.ToString());
                    //Console.WriteLine(MarketData[Symbol].GetField("Symbol").Value + " " + MarketData[Symbol].GetField("LastPx").Value);
                    break;
                default:
                    Console.WriteLine(message);
                    break;
            }


        }


        static void OnMessageReceived(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
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

        static void OnMessageReceived1(object listener, MessageReceivedEventArgs messageReceivedEventArgs) {
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
