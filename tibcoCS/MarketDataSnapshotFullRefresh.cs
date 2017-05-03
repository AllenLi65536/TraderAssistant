using System;
using System.ComponentModel;
using System.Reflection;
using TIBCO.Rendezvous;

namespace tibcoCS
{
    public class MarketDataSnapshotFullRefresh
    {
        /// <summary>
        /// 盤別列舉
        /// </summary>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2015/9/4 下午 05:09
        ///   </history>
        public enum SessionIDEnum { Closed, PreOpen1, Session1, PreClose1, PreOpen2, Session2, PreClose2 }

        private string _TransportSerialNo;

        /// <summary>
        /// Gets or sets the transport serial no.
        /// </summary>
        /// <value>
        /// The transport serial no.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2016/2/17 下午 04:09
        /// </history>

        public string TransportSerialNo {
            get { return _TransportSerialNo; }
            set { _TransportSerialNo = value; }
        }

        private string _Symbol;

        /// <summary>
        /// Gets or sets the symbol.
        /// </summary>
        /// <value>
        /// The symbol.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/7,下午 06:42
        /// </history>

        public string Symbol {
            get { return _Symbol; }
            set { _Symbol = value; }
        }

        private string _SymbolName = "";

        /// <summary>
        /// Gets or sets the name of the symbol.
        /// </summary>
        /// <value>
        /// The name of the symbol.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/12,下午 03:16
        /// </history>

        public string SymbolName {
            get { return _SymbolName; }
            set { _SymbolName = value; }
        }

        private double _ReferencePrice;

        /// <summary>
        /// Gets or sets the reference price.
        /// </summary>
        /// <value>
        /// The reference price.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/12,下午 03:28
        /// </history>

        public double ReferencePrice {
            get { return _ReferencePrice; }
            set { _ReferencePrice = value; }
        }

        private double _UpLimitPrice;

        /// <summary>
        /// Gets or sets up limit price.
        /// </summary>
        /// <value>
        /// Up limit price.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/12,下午 03:29
        /// </history>

        public double UpLimitPrice {
            get { return _UpLimitPrice; }
            set { _UpLimitPrice = value; }
        }

        private double _DownLimitPrice;

        /// <summary>
        /// Gets or sets down limit price.
        /// </summary>
        /// <value>
        /// Down limit price.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/12,下午 03:29
        /// </history>

        public double DownLimitPrice {
            get { return _DownLimitPrice; }
            set { _DownLimitPrice = value; }
        }

        private double _OpenPx;

        /// <summary>
        /// Gets or sets the 開盤價.
        /// </summary>
        /// <value>
        /// The open px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/14,下午 01:00
        /// </history>

        public double OpenPx {
            get { return _OpenPx; }
            set { _OpenPx = value; }
        }

        private double _HighPx;

        /// <summary>
        /// Gets or sets the 最高成交價格.
        /// </summary>
        /// <value>
        /// The high px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/14,下午 01:01
        /// </history>

        public double HighPx {
            get { return _HighPx; }
            set { _HighPx = value; }
        }

        private double _LowPx;

        /// <summary>
        /// Gets or sets the 最低成交價格.
        /// </summary>
        /// <value>
        /// The low px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/14,下午 01:01
        /// </history>

        public double LowPx {
            get { return _LowPx; }
            set { _LowPx = value; }
        }

        private double _LastPx;

        /// <summary>
        /// Gets or sets the 成交價.
        /// </summary>
        /// <value>
        /// The last px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:00
        /// </history>

        public double LastPx {
            get { return _LastPx; }
            set { _LastPx = value; }
        }

        private double _PreClosePx;

        /// <summary>
        /// Gets or sets the 前日收盤價.
        /// </summary>
        /// <value>
        /// The pre close px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/19,下午 07:12
        /// </history>

        public double PreClosePx {
            get { return _PreClosePx; }
            set { _PreClosePx = value; }
        }

        private int _LastShares;

        /// <summary>
        /// Gets or sets the 成交量.
        /// </summary>
        /// <value>
        /// The last shares.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:01
        /// </history>

        public int LastShares {
            get { return _LastShares; }
            set { _LastShares = value; }
        }

        private int _CumQty;

        /// <summary>
        /// 取得 或 設定 累計成交數量.
        /// </summary>
        /// <value>
        /// The cum qty.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/1/22,上午 10:25
        /// </history>

        public int CumQty {
            get { return _CumQty; }
            set { _CumQty = value; }
        }

        #region Bid

        private double _Bid1Px;

        /// <summary>
        /// Gets or sets the Bid1價.
        /// </summary>
        /// <value>
        /// The bid1 px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:01
        /// </history>

        public double Bid1Px {
            get { return _Bid1Px; }
            set { _Bid1Px = value; }
        }

        private int _Bid1Shares;

        /// <summary>
        /// Gets or sets the Bid1量.
        /// </summary>
        /// <value>
        /// The bid1 shares.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:01
        /// </history>

        public int Bid1Shares {
            get { return _Bid1Shares; }
            set { _Bid1Shares = value; }
        }

        private double _Bid2Px;

        /// <summary>
        /// Gets or sets the Bid2價.
        /// </summary>
        /// <value>
        /// The bid2 px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:01
        /// </history>

        public double Bid2Px {
            get { return _Bid2Px; }
            set { _Bid2Px = value; }
        }

        private int _Bid2Shares;

        /// <summary>
        /// Gets or sets the Bid2量.
        /// </summary>
        /// <value>
        /// The bid2 shares.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:02
        /// </history>

        public int Bid2Shares {
            get { return _Bid2Shares; }
            set { _Bid2Shares = value; }
        }

        private double _Bid3Px;

        /// <summary>
        /// Gets or sets the Bid3價.
        /// </summary>
        /// <value>
        /// The bid3 px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:03
        /// </history>

        public double Bid3Px {
            get { return _Bid3Px; }
            set { _Bid3Px = value; }
        }

        private int _Bid3Shares;

        /// <summary>
        /// Gets or sets the Bid3量.
        /// </summary>
        /// <value>
        /// The bid3 shares.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:03
        /// </history>

        public int Bid3Shares {
            get { return _Bid3Shares; }
            set { _Bid3Shares = value; }
        }

        private double _Bid4Px;

        /// <summary>
        /// Gets or sets the Bid4價.
        /// </summary>
        /// <value>
        /// The bid4 px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:04
        /// </history>

        public double Bid4Px {
            get { return _Bid4Px; }
            set { _Bid4Px = value; }
        }

        private int _Bid4Shares;

        /// <summary>
        /// Gets or sets the Bid4量.
        /// </summary>
        /// <value>
        /// The bid4 shares.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:04
        /// </history>

        public int Bid4Shares {
            get { return _Bid4Shares; }
            set { _Bid4Shares = value; }
        }

        private double _Bid5Px;

        /// <summary>
        /// Gets or sets the Bid5價.
        /// </summary>
        /// <value>
        /// The bid5 px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:05
        /// </history>

        public double Bid5Px {
            get { return _Bid5Px; }
            set { _Bid5Px = value; }
        }

        private int _Bid5Shares;

        /// <summary>
        /// Gets or sets the Bid5量.
        /// </summary>
        /// <value>
        /// The bid5 shares.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:05
        /// </history>

        public int Bid5Shares {
            get { return _Bid5Shares; }
            set { _Bid5Shares = value; }
        }

        #endregion


        #region Ask

        private double _Ask1Px;

        /// <summary>
        /// Gets or sets the Ask1價.
        /// </summary>
        /// <value>
        /// The Ask1 px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:01
        /// </history>

        public double Ask1Px {
            get { return _Ask1Px; }
            set { _Ask1Px = value; }
        }

        private int _Ask1Shares;

        /// <summary>
        /// Gets or sets the Ask1量.
        /// </summary>
        /// <value>
        /// The Ask1 shares.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:01
        /// </history>

        public int Ask1Shares {
            get { return _Ask1Shares; }
            set { _Ask1Shares = value; }
        }

        private double _Ask2Px;

        /// <summary>
        /// Gets or sets the Ask2價.
        /// </summary>
        /// <value>
        /// The Ask2 px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:01
        /// </history>

        public double Ask2Px {
            get { return _Ask2Px; }
            set { _Ask2Px = value; }
        }

        private int _Ask2Shares;

        /// <summary>
        /// Gets or sets the Ask2量.
        /// </summary>
        /// <value>
        /// The Ask2 shares.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:02
        /// </history>

        public int Ask2Shares {
            get { return _Ask2Shares; }
            set { _Ask2Shares = value; }
        }

        private double _Ask3Px;

        /// <summary>
        /// Gets or sets the Ask3價.
        /// </summary>
        /// <value>
        /// The Ask3 px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:03
        /// </history>

        public double Ask3Px {
            get { return _Ask3Px; }
            set { _Ask3Px = value; }
        }

        private int _Ask3Shares;

        /// <summary>
        /// Gets or sets the Ask3量.
        /// </summary>
        /// <value>
        /// The Ask3 shares.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:03
        /// </history>

        public int Ask3Shares {
            get { return _Ask3Shares; }
            set { _Ask3Shares = value; }
        }

        private double _Ask4Px;

        /// <summary>
        /// Gets or sets the Ask4價.
        /// </summary>
        /// <value>
        /// The Ask4 px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:04
        /// </history>

        public double Ask4Px {
            get { return _Ask4Px; }
            set { _Ask4Px = value; }
        }

        private int _Ask4Shares;

        /// <summary>
        /// Gets or sets the Ask4量.
        /// </summary>
        /// <value>
        /// The Ask4 shares.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:04
        /// </history>

        public int Ask4Shares {
            get { return _Ask4Shares; }
            set { _Ask4Shares = value; }
        }

        private double _Ask5Px;

        /// <summary>
        /// Gets or sets the Ask5價.
        /// </summary>
        /// <value>
        /// The Ask5 px.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:05
        /// </history>

        public double Ask5Px {
            get { return _Ask5Px; }
            set { _Ask5Px = value; }
        }

        private int _Ask5Shares;

        /// <summary>
        /// Gets or sets the Ask5量.
        /// </summary>
        /// <value>
        /// The Ask5 shares.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 12:05
        /// </history>

        public int Ask5Shares {
            get { return _Ask5Shares; }
            set { _Ask5Shares = value; }
        }

        #endregion

        private double _NetChg;

        /// <summary>
        /// Gets or sets the 漲跌價差.
        /// </summary>
        /// <value>
        /// The net CHG.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 01:18
        /// </history>

        public double NetChg {
            get { return _NetChg; }
            set { _NetChg = value; }
        }

        private double _NetChgPct;

        /// <summary>
        /// Gets or sets the 漲跌幅.
        /// </summary>
        /// <value>
        /// The net CHG PCT.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 01:19
        /// </history>

        public double NetChgPct {
            get { return _NetChgPct; }
            set { _NetChgPct = value; }
        }

        private bool _Matched;

        /// <summary>
        /// Gets or sets the 是否有成交.
        /// </summary>
        /// <value>
        /// The display flag.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,上午 11:59
        /// </history>

        public bool Matched {
            get { return _Matched; }
            set { _Matched = value; }
        }

        private int _MatchStatus = -1;

        /// <summary>
        /// Gets or sets the 成交狀態.
        /// 0 = 非內外盤成交，1 = 內盤成交，2 = 外盤成交
        /// </summary>
        /// <value>
        /// The match status.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,下午 01:24
        /// </history>

        public int MatchStatus {
            get { return _MatchStatus; }
            set { _MatchStatus = value; }
        }

        private string _MatchTime = "";

        /// <summary>
        /// Gets or sets the 撮合時間.
        /// </summary>
        /// <value>
        /// The match time.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/13,上午 11:59
        /// </history>

        public string MatchTime {
            get { return _MatchTime; }
            set { _MatchTime = value; }
        }

        private int _SessionID = 0;

        /// <summary>
        /// Gets or sets the 盤別資訊.
        /// 選項參考本類別SessionIDEnum列舉內容
        /// </summary>
        /// <value>
        /// The session ID.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2015/9/4 下午 04:59
        /// </history>

        public int SessionID {
            get { return _SessionID; }
            set { _SessionID = value; }
        }

        private int _Status = 0;

        /// <summary>
        /// Gets or sets the 狀態註記.
        /// 現貨選項參考證交所電文格式六漲跌停註記 Bit 1-0 列舉內容
        /// 期貨選項參考期交所電文格式I020 status-code 列舉內容
        /// </summary>
        /// <value>
        /// The session ID.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2015/9/4 下午 04:59
        /// </history>

        public int Status {
            get { return _Status; }
            set { _Status = value; }
        }

        private string _Currency = "TWD";

        /// <summary>
        /// Gets or sets the 交易幣別.
        /// </summary>
        /// <value>
        /// The session ID.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2015/9/4 下午 04:59
        /// </history>

        public string Currency {
            get { return _Currency; }
            set { _Currency = value; }
        }

        private string _Exch = "XTAI";

        /// <summary>
        /// Gets or sets the 交易所代碼.
        /// 採用MIC code
        /// </summary>
        /// <value>
        /// The exch.
        /// </value>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2015/9/24 下午 12:58
        /// </history>

        public string Exch {
            get { return _Exch; }
            set { _Exch = value; }
        }

        private DateTime _RecvTime;

        public DateTime RecvTime {
            get { return _RecvTime; }
            set { _RecvTime = value; }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="MarketDataSnapshotFullRefresh" /> class.
        /// </summary>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/7,下午 06:34
        ///   </history>
        public MarketDataSnapshotFullRefresh()
            : base() {
            //this.MSGTYPE = KernelMsgTypeEnum.MarketDataSnapshotFullRefresh;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketDataSnapshotFullRefresh"/> class.
        /// </summary>
        /// <param name="setDefaultValue">if set to <c>true</c> [set default value].</param>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/27,下午 06:28
        /// </history>
        /*public MarketDataSnapshotFullRefresh(bool setDefaultValue)
            : base(setDefaultValue) {
            //this.MSGTYPE = KernelMsgTypeEnum.MarketDataSnapshotFullRefresh;
        }*/

        /// <summary>
        /// 根據傳入的 報價格式資料，設定目前報價新值.
        /// </summary>
        /// <param name="msgObject">The MSG object.</param>
        /// <history>
        /// Author: Gary Chiu , DateTime: 2014/3/12,下午 03:15
        /// </history>
        public void setMarketDataContent(Message msgObject) {
            object value = null;
            TypeConverter converter = null;
                        
            PropertyInfo[] pi_SourceFields = msgObject.GetType().GetProperties();

            foreach (PropertyInfo field in pi_SourceFields) {
                Console.WriteLine(field.GetValue(msgObject,null));
                PropertyInfo pi_ThisField = this.GetType().GetProperty(field.Name);
                value = field.GetValue(msgObject , null);
                //來源Property有值，且目前物件有此名稱的Property
                if ((value != null) && (pi_ThisField != null)) {
                    converter = TypeDescriptor.GetConverter(field.PropertyType);
                    pi_ThisField.SetValue(this , converter.ConvertFromString(value.ToString()) , null);
                }
            }
        }

    }
}
