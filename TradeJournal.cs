#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using System.IO;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class TradeJournal : Indicator
	{
		private NinjaTrader.NinjaScript.Indicators.LizardIndicators.amaCurrentDayVWAP amaCurrentDayVWAP1;
		
		private double Open_D = 0.0;
		private double Close_D = 0.0;
		private double TradeJournal_D = 0.0;
		private string message = @"no message";
		private long startTime = 0;
		private	long endTime = 0;
		private int startBar = 0;
		private int rthBarCount = 0;
		private double Y_High = 0.0;
		private double Y_Low = 0.0;
		private bool inRange = false;
		
		private Account account;
		private double xAccountSize;
		private bool Realized		= true;
		private double intradayLoss = 0.0;
		private double gain = 0.0;
		private double PriorGain = 0.0;
		private string name = "NA";
		
		/// buttons
		private System.Windows.Controls.Button myLocationButton;
		private System.Windows.Controls.Button myQualButton;
		private System.Windows.Controls.Button myEntryButton;
		private System.Windows.Controls.Button myManageButton;
		private System.Windows.Controls.Grid   myGrid; 
		private bool QualButtonIsOn = true;
		private bool EntryButtonIsOn = true;
		private bool ManagementButtonIsOn = true; 
		/// Trade reporting filled by buttons
		private string Area = "VAH";
		private string Entry = "Good";
		private string Management = "Good";
		private string Qualifier = "HH";
		
		/// CSV
		private bool 	headerIn = false;
		private bool	ThisFIleExists = false; 
		
		#region Set Up 
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Trade Journal";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				RTHopen						= DateTime.Parse("08:30", System.Globalization.CultureInfo.InvariantCulture);
				RTHclose					= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				UsePoints									= true;
				Contracts									= 5;
				JournalFileName								= @"TradeJournal.csv";
				///path										= NinjaTrader.Core.Globals.UserDataDir + JournalFileName;
				path = "no_path";
				//C:\Users\trade\OneDrive\Documents\NinjaTrader 8\bin\Custom
				VwapDebug									= false;
			}
			else if (State == State.Configure)
			{
				startTime = long.Parse(RTHopen.ToString("HHmmss"));
			 	endTime = long.Parse(RTHclose.ToString("HHmmss"));
				path = @"C:\Users\trade\OneDrive\Documents\NinjaTrader 8\" + JournalFileName;
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				ClearOutputWindow();
			}
			else if (State == State.DataLoaded)
			{				
				amaCurrentDayVWAP1				= amaCurrentDayVWAP(Close, amaSessionTypeVWAPD.Full_Session, 
				amaBandTypeVWAPD.Standard_Deviation, amaTimeZonesVWAPD.Exchange_Time, @"08:30", @"15:15", 0.5, 1, 1.5,  0.5, 1, 1.5);
			}
			// Once the NinjaScript object has reached State.Historical, our custom control can now be added to the chart
			else if (State == State.Historical)
			{
				  Print("State.Historical"); 
			    ChartControl.Dispatcher.InvokeAsync((() =>
			    {
			        // Grid already exists
			        if (UserControlCollection.Contains(myGrid))
			          return;
			 
			        // Add a control grid which will host our custom buttons
			        myGrid = new System.Windows.Controls.Grid
			        {
			          Name = "MyCustomGrid",
			          // Align the control to the top right corner of the chart
			          HorizontalAlignment = HorizontalAlignment.Left,
			          VerticalAlignment = VerticalAlignment.Bottom,
			        };
			 
			        // Define the two columns in the grid, one for each button
			        System.Windows.Controls.ColumnDefinition column1 = new System.Windows.Controls.ColumnDefinition();
			        System.Windows.Controls.ColumnDefinition column2 = new System.Windows.Controls.ColumnDefinition();
			 		System.Windows.Controls.ColumnDefinition column3 = new System.Windows.Controls.ColumnDefinition();
					System.Windows.Controls.ColumnDefinition column4 = new System.Windows.Controls.ColumnDefinition();
					
			        // Add the columns to the Grid
			        myGrid.ColumnDefinitions.Add(column1);
			        myGrid.ColumnDefinitions.Add(column2);
			 		myGrid.ColumnDefinitions.Add(column3);
					myGrid.ColumnDefinitions.Add(column4);
					
					myLocationButton = new System.Windows.Controls.Button
			        {
			          Name = "myLoactionButton",
			          Content = "trend",
			          Foreground = Brushes.White,
			          Background = Brushes.DimGray
			        };
					
					
			        // Define the custom Buy Button control object -- qualifier - entry - management
			        myQualButton = new System.Windows.Controls.Button
			        {
			          Name = "myQualButton",
			          Content = "Qualifier",
			          Foreground = Brushes.White,
			          Background = Brushes.LimeGreen
			        };
			 
			        // Define the custom Sell Button control object
			        myEntryButton = new System.Windows.Controls.Button
			        {
			          Name = "myEntryButton",
			          Content = "Entry",
			          Foreground = Brushes.White,
			          Background = Brushes.Red
			        };
			 
					myManageButton = new System.Windows.Controls.Button
			        {
			          Name = "myManageButton",
			          Content = "Manage",
			          Foreground = Brushes.White,
			          Background = Brushes.Red
			        };
					
			        // Subscribe to each buttons click event to execute the logic we defined in OnMyButtonClick()
			        myQualButton.Click += OnMyButtonClick;
			        myEntryButton.Click += OnMyButtonClick;
					myManageButton.Click += OnMyButtonClick;
			        // Define where the buttons should appear in the grid
					System.Windows.Controls.Grid.SetColumn(myLocationButton, 0);
			        System.Windows.Controls.Grid.SetColumn(myQualButton, 1);
			        System.Windows.Controls.Grid.SetColumn(myEntryButton, 2);
			 		System.Windows.Controls.Grid.SetColumn(myManageButton, 3);
					
			        // Add the buttons as children to the custom grid
					 myGrid.Children.Add(myLocationButton);
			        myGrid.Children.Add(myQualButton);
			        myGrid.Children.Add(myEntryButton);
			 		myGrid.Children.Add(myManageButton);
			        // Finally, add the completed grid to the custom NinjaTrader UserControlCollection
			        UserControlCollection.Add(myGrid);
			 
			    }));
			  }
		}
		
		#endregion
		
		protected override void OnBarUpdate()
		{
			if ( CurrentBar < 5 ) { return; }

			//myQualButton.SetValue(myGrid.col, "val");
			
			if ("Sunday"  == Time[0].DayOfWeek.ToString()) { return; }
			
			lock (Account.All)  {         
			    account = Account.All.FirstOrDefault(a => a.Name == AccountNames);
				gain =  account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar);
			    name = account.Name;
				 
			}
			SetVwapTrend(ShowVwap: VwapDebug, ShowTrend: false);
			
			
			/// loss not calculating correctly tried to reset intraday loss @ open
			if (  gain < intradayLoss ) 
			{ 
				intradayLoss = gain; 
			}
			
			/// reset profit at the open
			if (BarsInProgress == 1 && ToTime(Time[0]) == startTime ) { 
				intradayLoss = 0.0;
				gain = 0.0;
				Open_D = Open[0];
				TradeJournal_D = Open_D - Close_D;
				message =  @Time[0].ToShortDateString() + " "  + Time[0].ToShortTimeString() + "   OpenContracts " + Contracts.ToString(); 
				//Print(message); 
				
			}
			
			/// pre marketreset profit
			if (ToTime(Time[0]) < startTime ) { 
				TradeJournal_D = Close[0] - Close_D;
				//message =  Time[0].ToShortDateString() + " \t Contracts: " + Contracts.ToString();
				//Print(message);
				message += @"  Gain: $" + gain.ToString("N0");
				// set gain when we first load to fix bank row on load
				gain =  account.Get(AccountItem.GrossRealizedProfitLoss, Currency.UsDollar);
				//Print(Time[0] + " setting gain" + gain.ToString());
			}

			
			/// RTH ready peformance results
			if ( ToTime(Time[0]) > startTime ) { 
				message =  Time[0].ToShortDateString() + "  " + name  +  "\n .";
				if ( UsePoints ) {
					
					/// update gain
					intradayLoss =  intradayLoss / ( 5 * Contracts );
					gain =  account.Get(AccountItem.GrossRealizedProfitLoss, Currency.UsDollar);
					double PointVal = Instrument.MasterInstrument.PointValue;
					gain = gain / ( PointVal * Contracts );
					
					string path2 = @"\\mypc\shared\project";
					string CurrentTime = @Time[0].ToShortDateString();
					string Ct = Contracts.ToString();
					string TheGain = gain.ToString("N2");
					message = CurrentTime  + "  " + @name + @"   Contracts: " + @Ct + "  Gain: " + @TheGain + @" pts " +  "\n .";
					/// get trade infro from Executions
					string Direction = "NA";
					int Quantity = 0;
					
					if (account != null)
			        { 
			              lock (account.Executions)
			                  foreach (Execution execution in account.Executions) { 
								  if ( execution.IsEntry )  {
								  		Direction = execution.MarketPosition.ToString();
									    Quantity = execution.Quantity; 								  	
								  } 
							  }  
			        } 
					
					/// check for new gain
					if (PriorGain != gain && Count - 2 == CurrentBar ) {
						// record this trade profit / loss
						double ThisTradePoints = gain - PriorGain;
						double ThisTradeProfit = (ThisTradePoints * Contracts) * PointVal;  
						string NewTrade = Time[0].ToShortDateString() + ", " + Time[0].ToShortTimeString() + ", " + name + ", " +
							Bars.Instrument.MasterInstrument.Name + ", " +
							Contracts + ", " +
							Direction + 
							", " + ThisTradePoints +  				
							", " + ThisTradeProfit.ToString("C") + 
							", " + Area +
							", " + Qualifier +
							", " + Entry +
							", " + Management;
						Print("New Trade " + NewTrade); 
						string header = "Date, Time, Account, Symbol, Contracts, Direction, Point Gain, Profit, Area, Qualifier, Entry, Management, Notes"; 
						AddHeader(header: header);
						WriteFile(path: path, newLine: NewTrade, header: false);
					}
					
					PriorGain = gain;
				} else {
					message += "  Gain: $" + gain.ToString("N0") + "  DD: $" + intradayLoss.ToString("N0") + "/n /n 123456 ... ";
				}
				Draw.TextFixed(this, "MyTextFixed", message, TextPosition.BottomLeft);
			}
			
		}
		
		#region Save CSV 
		
		private void CheckFIleExists() {
			if (File.Exists(path)) {
  				Print("The file exists." + path);
				ThisFIleExists = true;
			}
		}
		
		private void SetFileName() {
			if (Count - 2 == CurrentBar) {
				string inst = Instrument.FullName;
				string instOnly = inst.Remove(inst.Length-6);
				DateTime myDate = DateTime.Today;  // DateTime type
				string prettyDate = myDate.ToString("M_d_yyyy");
				string instDate = instOnly + "_" + prettyDate + ".csv";
				//path += instDate;
				//Print(instDate);
				Print(path);
			}
		}
		
		private void AddHeader(string header) {
			CheckFIleExists(); 
			if ( !headerIn && !ThisFIleExists ) {
				SetFileName();  
				Print(header);
				WriteFile(path: path, newLine: header, header: true);
				headerIn = true;
			} 
		}
		
		private void WriteFile(string path, string newLine, bool header)
        {
			if ( header ) {
				ClearFile(path: path);
				using (var tw = new StreamWriter(path, true))
	            {
	                tw.WriteLine(newLine); 
	                tw.Close();
	            }
				return;
			}
			
            using (var tw = new StreamWriter(path, true))
            {
                tw.WriteLine(newLine);
                tw.Close();
            }
        }
		
		private void ClearFile(string path)
        {
            try    
			{    
				// Check if file exists with its full path    
				if (File.Exists(path))    
				{    
					// If file found, delete it    
					File.Delete(path);    
					Print("File deleted.");    
				} 
				else  Print("File not found");    
			}    
			catch (IOException ioExp)    
			{    
				Print(ioExp.Message);    
			} 
			
        }

		#endregion
		
		#region Button Logic 
		
		private void OnMyButtonClick(object sender, RoutedEventArgs rea)
		{
		  System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
		  if (button != null) {
			  
			  //MARK : _ TODO _ Find a way to auto update this var, right now relies on a click
			  myLocationButton.Content = Area;
			   
			  if ( button.Name == "myQualButton") {
				  if ( QualButtonIsOn ) {
				 	  Qualifier = "HH";
				  } else {
					  Qualifier = "LL";
				  }
				  AllButtonToggle( b: button, content: Qualifier, toggle: QualButtonIsOn );   
				  QualButtonIsOn = !QualButtonIsOn;
			  } 
			  
			  if ( button.Name == "myEntryButton") {
				  if ( EntryButtonIsOn ) {
				 	  Entry = "Good";
				  } else {
					  Entry = "Bad";
				  }
				  AllButtonToggle( b: button, content: Entry, toggle: EntryButtonIsOn);   
				  EntryButtonIsOn = !EntryButtonIsOn;
			  }

			  if ( button.Name == "myManageButton") {
				  if ( ManagementButtonIsOn ) {
				 	  Management = "Good";
				  } else {
					  Management = "Bad";
				  }
				  AllButtonToggle( b: button, content: Management, toggle: ManagementButtonIsOn);   
				  ManagementButtonIsOn = !ManagementButtonIsOn;
			  }
		  	}
		}
		
		private void AllButtonToggle(System.Windows.Controls.Button b, string content, bool toggle) {
			if ( toggle ) {
				  b.Background = Brushes.LimeGreen;
				  b.Foreground = Brushes.Black;
			  } else {
				  b.Background = Brushes.Red;
				  b.Foreground = Brushes.White;
			  } 
			 b.Content = content; 
		}
		
		#endregion
		
		#region Vwap Trend
		
		///  Vwap to show VAL, VAH, Up Trend , Down Trend 
		void SetVwapTrend(bool ShowVwap, bool ShowTrend ) { 
			
			if (BarsInProgress == 1 ) {  return; }
			double MyVwapU05 = amaCurrentDayVWAP1.UpperBand1[0];
			double MyVwapU1 = amaCurrentDayVWAP1.UpperBand2[0];
			double MyVwapU15 = amaCurrentDayVWAP1.UpperBand3[0];
			
			if ( ShowVwap ) {
				Draw.Dot(this, "MyVwapU1", false, 0, MyVwapU05, Brushes.Red);
				Draw.Dot(this, "MyVwapU2", false, 0, MyVwapU1, Brushes.White);
				Draw.Dot(this, "MyVwapU3", false, 0, MyVwapU15, Brushes.Blue);
			}
			
			double MyVwapL05 =  amaCurrentDayVWAP1.LowerBand1[0];
			double MyVwapL1 = amaCurrentDayVWAP1.LowerBand2[0];
			double MyVwapL15 = amaCurrentDayVWAP1.LowerBand3[0];
			
			if ( ShowVwap ) {
				Draw.Dot(this, "MyVwapL05", false, 0, MyVwapL05, Brushes.Red);
				Draw.Dot(this, "MyVwapL1", false, 0, MyVwapL1, Brushes.White);
				Draw.Dot(this, "MyVwapL15", false, 0, MyVwapL15, Brushes.Blue);
			}
			
			if (Close[0] > MyVwapU15) {
				Area = "Up Trend";
				if ( ShowTrend ) { Draw.Dot(this, "Up" + CurrentBar, false, 0, High[0], Brushes.Blue); }
			}
			if (Close[0] >= MyVwapU05 && Close[0] <= MyVwapU15) {
				Area = "VAH";
				if ( ShowTrend ) { Draw.Dot(this, "VAH" + CurrentBar, false, 0, High[0], Brushes.Cyan); }
			}
			
			if (Close[0] < MyVwapU05 && Close[0] >= MyVwapL05) {
				Area = "VWAP";
				if ( ShowTrend ) { Draw.Dot(this, "VWAP" + CurrentBar, false, 0, High[0], Brushes.White); }
			}

			if (Close[0] <= MyVwapL05 && Close[0] >= MyVwapL15) {
				Area = "VAL";
				if ( ShowTrend ) { Draw.Dot(this, "VAL" + CurrentBar, false, 0, Low[0], Brushes.Magenta); }
			}
			
			if (Close[0] < MyVwapL15) {
				Area = "Down Trend";
				if ( ShowTrend ) { Draw.Dot(this, "Down" + CurrentBar, false, 0, Low[0], Brushes.Red); }
			}
			
		}
		
		#endregion
		
		#region Properties
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHopen", Order=1, GroupName="Parameters")]
		public DateTime RTHopen
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHclose", Order=2, GroupName="Parameters")]
		public DateTime RTHclose
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Points", Order=3, GroupName="Parameters")]
		public bool UsePoints
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Contracts", Order=3, GroupName="Parameters")]
		public int Contracts
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Journal File Name", Order=4, GroupName="Parameters")]
		public string JournalFileName
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="File Path", Order=5, GroupName="Parameters")]
		public string path
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Vwap Debug", Order=6, GroupName="Parameters")]
		public bool VwapDebug
		{ get; set; }
		
		
		
		
		[NinjaScriptProperty]
		[TypeConverter(typeof(AccountConverter))]
		public string AccountNames
		{get;set;}
	
		public class AccountConverter : TypeConverter
	    {
		
		   public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		   {
			   if (context == null)
			   {
				   return null;
			   }
	
			   List <string> list = new List <string> ();
			
		
			   foreach (Account sampleAccount in Account.All)
			   {
				   list.Add(sampleAccount.Name);
			   }
				
			
			   return new TypeConverter.StandardValuesCollection(list);
		   }
		
	

		   public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		   {
			   return true;
		   }
	    }
	}
	
	#endregion

}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TradeJournal[] cacheTradeJournal;
		public TradeJournal TradeJournal(DateTime rTHopen, DateTime rTHclose, bool usePoints, int contracts, string journalFileName, string path, bool vwapDebug, string accountNames)
		{
			return TradeJournal(Input, rTHopen, rTHclose, usePoints, contracts, journalFileName, path, vwapDebug, accountNames);
		}

		public TradeJournal TradeJournal(ISeries<double> input, DateTime rTHopen, DateTime rTHclose, bool usePoints, int contracts, string journalFileName, string path, bool vwapDebug, string accountNames)
		{
			if (cacheTradeJournal != null)
				for (int idx = 0; idx < cacheTradeJournal.Length; idx++)
					if (cacheTradeJournal[idx] != null && cacheTradeJournal[idx].RTHopen == rTHopen && cacheTradeJournal[idx].RTHclose == rTHclose && cacheTradeJournal[idx].UsePoints == usePoints && cacheTradeJournal[idx].Contracts == contracts && cacheTradeJournal[idx].JournalFileName == journalFileName && cacheTradeJournal[idx].path == path && cacheTradeJournal[idx].VwapDebug == vwapDebug && cacheTradeJournal[idx].AccountNames == accountNames && cacheTradeJournal[idx].EqualsInput(input))
						return cacheTradeJournal[idx];
			return CacheIndicator<TradeJournal>(new TradeJournal(){ RTHopen = rTHopen, RTHclose = rTHclose, UsePoints = usePoints, Contracts = contracts, JournalFileName = journalFileName, path = path, VwapDebug = vwapDebug, AccountNames = accountNames }, input, ref cacheTradeJournal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TradeJournal TradeJournal(DateTime rTHopen, DateTime rTHclose, bool usePoints, int contracts, string journalFileName, string path, bool vwapDebug, string accountNames)
		{
			return indicator.TradeJournal(Input, rTHopen, rTHclose, usePoints, contracts, journalFileName, path, vwapDebug, accountNames);
		}

		public Indicators.TradeJournal TradeJournal(ISeries<double> input , DateTime rTHopen, DateTime rTHclose, bool usePoints, int contracts, string journalFileName, string path, bool vwapDebug, string accountNames)
		{
			return indicator.TradeJournal(input, rTHopen, rTHclose, usePoints, contracts, journalFileName, path, vwapDebug, accountNames);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TradeJournal TradeJournal(DateTime rTHopen, DateTime rTHclose, bool usePoints, int contracts, string journalFileName, string path, bool vwapDebug, string accountNames)
		{
			return indicator.TradeJournal(Input, rTHopen, rTHclose, usePoints, contracts, journalFileName, path, vwapDebug, accountNames);
		}

		public Indicators.TradeJournal TradeJournal(ISeries<double> input , DateTime rTHopen, DateTime rTHclose, bool usePoints, int contracts, string journalFileName, string path, bool vwapDebug, string accountNames)
		{
			return indicator.TradeJournal(input, rTHopen, rTHclose, usePoints, contracts, journalFileName, path, vwapDebug, accountNames);
		}
	}
}

#endregion
