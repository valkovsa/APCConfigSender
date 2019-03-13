using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Microsoft.Win32;

namespace APCConfigSender
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		// Для групповой работы с файлами
		private List<IPAddress> _addressesForBatch;
		
		// Для вывода сообщений		
		private WriteLogMes _wlm;
		
		// Для прерывания задач
		private CancellationTokenSource _cts;
		
		// Для выбора секций конфигурационного файла в Combo Box
		private ObservableCollection<KeyValuePair<string, string>> _ocConfig;
		
		public Window1() 
		{
			InitializeComponent();

			txtLog.TextChanged += (s, e) => txtLog.ScrollToLine(txtLog.LineCount-1);
			
			BTNUploadBatchConfigStart.IsEnabled = false;
			BTNUploadBatchConfigStop.IsEnabled = false;
			RBSingle.IsChecked = true;
			
			_wlm = new WriteLogMes(
				(s) => { txtLog.AppendText(s); _wlm.Write(s); },
				(n) => { PBar.Value = n; },
				(s) => { SBarText.Text = s; }
			);
			
			_ocConfig = new ObservableCollection<KeyValuePair<string, string>>();
			CBConfig.ItemsSource = _ocConfig;
		}
		
		async void BTNDownloadFile_Click(object sender, RoutedEventArgs e)	
		{
			txtConf.Text="";
			using (FileTransferHelper fth = new FileTransferHelper(TBLogin.Text, TBPass.Password, _wlm))
			{
				_ocConfig.Clear();
				string sIpAddr = TBAddress.Text;
				string config = await Task<string>.Factory.StartNew( ()=> fth.DownloadConfigIni(sIpAddr), TaskCreationOptions.LongRunning );
				
				CreateSectionsComboBox(config);
				CBConfig.SelectedIndex = 0;
				
			}
		}
		
		async void BTNUpload_Click(object sender, RoutedEventArgs e)
		{
			using (FileTransferHelper fth = new FileTransferHelper(TBLogin.Text, TBPass.Password, _wlm) )
			{
				string sIpAddr = TBAddress.Text;
				string conf = txtConf.Text;

				await Task.Factory.StartNew( ()=> fth.UploadConfigIni(sIpAddr, conf), TaskCreationOptions.LongRunning );
			}
		}

		async void BTNLoadBatchDevices_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "txt files|*.txt|All Files (*.*)|*.*" };
			if(openFileDialog.ShowDialog() == true)
			{
				using (FileTransferHelper fth = new FileTransferHelper(TBLogin.Text, TBPass.Password, _wlm) )
				{
					_addressesForBatch = fth.LoadBatchDevices(
													await Task<string>.Factory.StartNew( 
						                        	   ()=> File.ReadAllText(openFileDialog.FileName, Encoding.UTF8) 
						                 ));
				}
				
			}
			
			if (_addressesForBatch != null) 
			{
				BTNUploadBatchConfigStart.IsEnabled = true;
			}
			else
			{
				BTNUploadBatchConfigStart.IsEnabled = false;
			}
			
		}
		
		async void BTNUploadBatchConfigStart_Click(object sender, RoutedEventArgs e)
		{
			txtConf.IsReadOnly = true;
			txtConf.Background = Brushes.LightGray;
			BTNUploadBatchConfigStart.IsEnabled = false;
			BTNUploadBatchConfigStop.IsEnabled = true;
			
			using (FileTransferHelper fth = new FileTransferHelper(TBLogin.Text, TBPass.Password, _wlm))
			{
				string conf = txtConf.Text;
				
				using (_cts = new CancellationTokenSource()) 
				{
					await Task.Factory.StartNew( ()=> fth.BatchUploadConfigIni(_addressesForBatch, conf, _cts.Token), _cts.Token);
				}
 			}
			
			txtConf.IsReadOnly = false;
			txtConf.Background = Brushes.White;
			BTNUploadBatchConfigStart.IsEnabled = true;
			BTNUploadBatchConfigStop.IsEnabled = false;
		}
		
		void BTNUploadBatchConfigStop_Click(object sender, RoutedEventArgs e)
		{
			_cts.Cancel();
			BTNUploadBatchConfigStart.IsEnabled = true;
			BTNUploadBatchConfigStop.IsEnabled = false;
		}
		
		async void BTNOpenFile_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "ini files|*.ini|All Files (*.*)|*.*" };
			string config = string.Empty;
			if(openFileDialog.ShowDialog() == true)
			{
				config = await Task<string>.Factory.StartNew( ()=> File.ReadAllText(openFileDialog.FileName, Encoding.UTF8));
			}
			
			CreateSectionsComboBox(config);
			CBConfig.SelectedIndex = 0;	
			
		}
		
		void BTNClearConf_Click(object sender, RoutedEventArgs e)
		{
			txtConf.Text = "";
		}
		
		async void BTNSaveFile_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog{ AddExtension = true, DefaultExt = ".ini", Filter = "ini files|*.ini|All Files (*.*)|*.*" };
			if (saveFileDialog.ShowDialog() == true)
			{
				string buf = txtConf.Text;
				await Task.Factory.StartNew( ()=> File.WriteAllText(saveFileDialog.FileName, buf, Encoding.UTF8) );
			}
		}

		void RBSingle_Checked(object sender, RoutedEventArgs e)
		{
			BlockButtonsSingleDevice.Visibility = Visibility.Visible;
			BlockButtonsMultiplyDevices.Visibility = Visibility.Collapsed;
		}
		
		void RBMultiply_Checked(object sender, RoutedEventArgs e)
		{
			BlockButtonsSingleDevice.Visibility = Visibility.Collapsed;
			BlockButtonsMultiplyDevices.Visibility = Visibility.Visible;
		}
		
		void CBConfig_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (CBConfig.SelectedIndex >= 0) 
			{
				txtConf.Text = _ocConfig.ElementAt(CBConfig.SelectedIndex).Value;
			}
			
		}
		
		void CreateSectionsComboBox(string config)
		{
			if (_ocConfig.Count > 0)
			{
				_ocConfig.Clear();
			}
			
			var listConf = ParseFromString.ConfigToList(config);
			if (listConf != null) 
			{
				foreach (var el in listConf) 
				{
					_ocConfig.Add(new KeyValuePair<string, string>(el.Key, el.Value));
				}
			}
			
			else
			{
				txtLog.Text += string.Format( "{0}: Config file is wrong.\n", DateTime.Now.ToLongTimeString() );
			}
		}
		
	}
}



