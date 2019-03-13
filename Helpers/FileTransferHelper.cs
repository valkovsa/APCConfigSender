using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using APCConfigSender;

namespace APCConfigSender
{
	/// <summary>
	/// Вспомогательный класс на скачивание и выгрузку конфигурационных файлов
	/// </summary>
	public class FileTransferHelper : IDisposable
	{
		public string Login { get; set; }
		public string Password { get; set; }
		public WriteLogMes Wlm { get; set; }
		
		private FtpWebRequest _request;
		private Dictionary<string, string> _errors;
		
		public FileTransferHelper(string login, string password, WriteLogMes wlm)
		{	
			Login = login;
			Password = password;
			Wlm = wlm;
		}
		
		/// <summary>
		/// Запускает загрузку файла config.ini с устройства
		/// </summary>
		/// <param name="progress">Вывод промежуточной информации через Progress<string></param>
		/// <returns>
		/// Возвращает config.ini в формате string 
		/// </returns>
		public string DownloadConfigIni(string sIpAddr)
		{
			IPAddress IpAddr = ParseFromString.GetOneIp(sIpAddr);
			if (IpAddr == null)
			{
				Wlm.PrLog.Report( ReportTemplate(new string('-',14),"IP address is incorrect") );
				Wlm.PrPBar.Report(0);
				Wlm.PrSBar.Report("Error");
				
				return null;
			}
			
			double pbarNum = 20;
			
			Wlm.PrSBar.Report("Downloading config.ini");
			Wlm.PrPBar.Report(pbarNum);
			Wlm.PrLog.Report(ReportTemplate(IpAddr.ToString(), "Start download config.ini..."));
			
			if(isPingable(IpAddr))
			{
				Wlm.PrPBar.Report(pbarNum += 20);
				string addressFile = string.Format("ftp://{0}/config.ini", IpAddr.ToString());
				
				try {
					_request = (FtpWebRequest)WebRequest.Create(addressFile);
		            _request.Method = WebRequestMethods.Ftp.DownloadFile;  
		            _request.UsePassive = true;
		            _request.KeepAlive = false;
		            _request.Credentials = new NetworkCredential(Login, Password);
		            _request.Timeout = 5000;
		            
		            using (FtpWebResponse response = (FtpWebResponse)_request.GetResponse()) 
		            {
		            	Wlm.PrLog.Report(ReportTemplate(IpAddr.ToString(), response.StatusCode.ToString(), response.StatusDescription.ToString()));
		            	Wlm.PrPBar.Report(pbarNum += 20);
	
		            	using (Stream responseStream = response.GetResponseStream())
		            	{
		            		using (StreamReader reader = new StreamReader(responseStream)) 
		            		{
		            			Wlm.PrPBar.Report(pbarNum += 20);
		            			
		            			string buff = reader.ReadToEnd();
		            			
		            			Wlm.PrLog.Report(ReportTemplate(IpAddr.ToString(), response.StatusCode.ToString(), response.StatusDescription.ToString()));
		            			Wlm.PrPBar.Report(pbarNum += 20);
		            			Wlm.PrSBar.Report("Config.ini successful downloaded");
				            	
		            			return buff;
		            		}
		            	}
		            }
		            
				} catch (Exception ex)	{
					
					Wlm.PrLog.Report(ReportTemplate(IpAddr.ToString(), "Error", ex.Message));
					Wlm.PrPBar.Report(0);
					Wlm.PrSBar.Report("File not available");
					
					return string.Empty;
				}
			
			} else {
				Wlm.PrLog.Report(ReportTemplate(IpAddr.ToString(), "Device not available"));
				Wlm.PrPBar.Report(0);
				Wlm.PrSBar.Report(ReportTemplate(IpAddr.ToString(), "Device not available"));
				
				return string.Empty;
			}
		}
		
		/// <summary>
		/// Передает конфигурацию плате управления
		/// </summary>
		/// <param name="sIpAddr">IP адрес платы управления в формате string</param>
		/// <param name="config">Конфигурация для передачи в формате string</param>
		/// <param name="isBatch">Параметр влияющий на вывод событий в главное окно, true - конфигурация нескольких устройств, false - конфигурация одиночного устройства</param>
		public void UploadConfigIni(string sIpAddr, string config, bool isBatch = false)
		{
			if (!isBatch) 
			{
				Wlm.PrSBar.Report("Uploading config.ini");
				Wlm.PrPBar.Report(0);
			}
			
			IPAddress IpAddr = ParseFromString.GetOneIp(sIpAddr);
			if (IpAddr == null)
			{
				Wlm.PrLog.Report( ReportTemplate(new string('-',14),"IP address is incorrect") );
				if (!isBatch) 
				{
					Wlm.PrPBar.Report(0);
					Wlm.PrSBar.Report("Error, IP incorrect");	
				}
				else
				{
					_errors.Add(sIpAddr, "IP incorrect");
				}
			}
			else
			{
				if(isPingable(IpAddr))
				{
					if (!isBatch) 
					{
						Wlm.PrSBar.Report("Uploading config.ini");
						Wlm.PrPBar.Report(50);
					}
					
					string _addressFile = string.Format("ftp://{0}/config1.ini", IpAddr.ToString());
		            _request = (FtpWebRequest)WebRequest.Create(_addressFile);
		            _request.Method = WebRequestMethods.Ftp.UploadFile;
		            _request.UsePassive = true;
		            _request.KeepAlive = false;
		            _request.Credentials = new NetworkCredential(Login, Password);
					
		            byte[] fileContents = Encoding.UTF8.GetBytes(config);
					
		            try {
		            	
		            	using (Stream requestStream = _request.GetRequestStream()) 
		            	{
		            		requestStream.Write(fileContents, 0, fileContents.Length);	
		            	}
		            
			            using (FtpWebResponse response = (FtpWebResponse)_request.GetResponse())
			            {
			            	Wlm.PrLog.Report(ReportTemplate(IpAddr.ToString(), "Upload File Complete!"));
			            	if (!isBatch) 
							{
								Wlm.PrSBar.Report("Upload success");
								Wlm.PrPBar.Report(100);
							}
			            }
		            } catch (Exception ex) {
			            
		            	Wlm.PrLog.Report(ReportTemplate(IpAddr.ToString(), "Upload error: ", ex.Message));
			            if (!isBatch) 
						{
							Wlm.PrSBar.Report("Upload error");
							Wlm.PrPBar.Report(0);
						}
			            else
			            {
			            	_errors.Add(sIpAddr, ex.Message + "\n");
			            }
		            	
			        }
		            
		         } else {
					
					Wlm.PrLog.Report(ReportTemplate(IpAddr.ToString(), "Device not available"));
					if (!isBatch) 
					{
						Wlm.PrSBar.Report("Upload error");
						Wlm.PrPBar.Report(0);
					}
					else
					{
						_errors.Add(sIpAddr, "Device not available");
					}
				}   
			}
		}
		
		/// <summary>
		/// Передает конфигурацию платам управления
		/// </summary>
		/// <param name="addressesForBatch">Адреса плат управления в формате List<c>IPAddress</c></param>
		/// <param name="config">Конфигурация для передачи в формате string</param>
		/// <param name="ct">Токен для отмены операции формат - CancellationToken</param>
		public void BatchUploadConfigIni(List<IPAddress> addressesForBatch, string config, CancellationToken ct)
		{
			Wlm.PrPBar.Report(0);
			Wlm.PrLog.Report( string.Format("\n{0}: Uploading config.ini for multiple devices ...\n", DateTime.Now.ToLongTimeString()));
			Wlm.PrSBar.Report("Batch upload config ini");
			
			_errors = new Dictionary<string, string>();
			double i = 1;
			foreach (var ip in addressesForBatch) 
			{
				UploadConfigIni(ip.ToString(), config, true);
				
				Wlm.PrPBar.Report( i/addressesForBatch.Count *100 );
				Wlm.PrSBar.Report( string.Format("Устройство {0} из {1}", i, addressesForBatch.Count));
				i++;
				
				if (ct.IsCancellationRequested) 
				{
					Wlm.PrLog.Report( ReportTemplate("batch IP", "batch config braked") );
					PrintErrors();
					break;
				}
			}
			
			if (!ct.IsCancellationRequested) 
			{
				Wlm.PrLog.Report( ReportTemplate("batch IP", "batch config complete") );
				PrintErrors();
			}
			
		}
		
		#region Accessory methods
		
		/// <summary>
		///Проверяет доступность устройства по ping
		/// </summary>
		/// <param name="progress">Вывод промежуточной информации через Progress<string></param>
		/// <param name="address">Адрес в формате string</param>
		/// <param name="numEfforts">Количество попыток int, по умолчанию = 1</param>
		/// <returns>Возвращает bool, true - доступно по ping, false - не доступно по ping</returns>
		private bool isPingable(IPAddress address, int numEfforts = 3)
		{
			Ping ping = new Ping();
			
			try {
				
				for (int i = 0; i < numEfforts; i++)
				{
					PingReply pingReply = ping.Send(address);
					
					if (pingReply.Status == IPStatus.Success)
					{
						Wlm.PrLog.Report(ReportTemplate(address.ToString(),"Ping success from effort", (i+1).ToString()));
						return true;
					}
					
					Wlm.PrLog.Report(ReportTemplate(address.ToString(), "Ping failed from effort", (i+1).ToString()));
				}
				return false;
				
			} catch (Exception ex) {
				
				Wlm.PrLog.Report(ReportTemplate(address.ToString(), "Ping error: ", ex.Message));
				return false;
			}
		}		
		
		/// <summary>
		/// Формирует строку для вывода сообщений по определенному шаблону
		/// </summary>
		/// <param name="source">Адрес источника</param>
		/// <param name="act">Аннотация</param>
		/// <param name="mess">Сообщение, по умолчанию = null</param>
		/// <returns>string - время IP: анотация - сообщение</returns>
		public string ReportTemplate(string source, string act, string mess = null)
		{
			if(mess == null) 
			{
				return string.Format("{0} IP: {1} - {2}\r\n",
			                     DateTime.Now.ToLongTimeString(),
			                     source, act); 
			} 
			else 
			{
				return string.Format("{0} IP: {1} - {2} - {3}\r\n",
			                     DateTime.Now.ToLongTimeString(),
			                     source, act, mess.TrimEnd('\n', '\r', '\t'));
			}
		}
		
		/// <summary>
		/// Создает коллекцию IP адресов
		/// </summary>
		/// <param name="readFile"></param>
		/// <returns></returns>
		public List<IPAddress> LoadBatchDevices(string readFile)
		{
			List<IPAddress> addressesForBatch = ParseFromString.GetMultipleIp(readFile);
			
			if (addressesForBatch == null) 
			{
				Wlm.PrLog.Report(ReportTemplate("batch","IP not found in file"));
			}
			else
			{
				Wlm.PrLog.Report(ReportTemplate("batch","Config for devices:"));
				
				string buf = string.Empty;
				foreach (var ipAddr in addressesForBatch) 
				{
					buf += string.Format("\t{0}\r\n", ipAddr.ToString());
				}
				
				Wlm.PrLog.Report(buf);
			}
			
			return addressesForBatch;
			
		}
		
		private void PrintErrors()
		{
			if (_errors.Count > 0) 
			{
				Wlm.PrLog.Report("\n * * * * * * Errors occurred: * * * * * *\n");
				foreach (var el in _errors) 
				{
					Wlm.PrLog.Report(string.Format("{0}: {1}\n",el.Key, el.Value));
				}
			}
			else
			{
				Wlm.PrLog.Report("Ошибок не обнаружено.");
			}
			
			_errors.Clear();
		}
		
		#endregion
		
		public void Dispose()
		{
			Login = null;
			Password = null;
			_request = null;
		}
	}
}
