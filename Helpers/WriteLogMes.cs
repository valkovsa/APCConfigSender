using System;
using System.IO;
using System.Text;

namespace APCConfigSender
{
	/// <summary>
	/// Description of WriteLog.
	/// </summary>
	public class WriteLogMes : IDisposable
	{
		/// <summary>
		///Передает сообщение string в окно логов
		/// </summary>
		public IProgress<string> PrLog { get; set; }
		
		/// <summary>
		///Передает значение double в ProgressBar
		/// </summary>
		public IProgress<double> PrPBar { get; set; }
		
		/// <summary>
		///Передает сообщение string в StatusBar
		/// </summary>
		public IProgress<string> PrSBar { get; set; }
		
		
		/// <summary>
		///Создает объекты IProgress для передачи сообщений между потоками 
		/// </summary>
		/// <param name="actionLog">Метод который должен выполняться для записи в блок Log</param>
		/// <param name="actionProgressBar">Метод который изменяет ProgressBar</param>
		/// <param name="actionStatusBar">Метод который изменяет StatusBar</param>
		/// <param name="actionStatusBar">Используется внутренний метод void Write для записи в файл</param>
		public WriteLogMes(Action<string> actionLog, Action<double> actionProgressBar, Action<string> actionStatusBar)
		{
			if (!Directory.Exists("logs"))
			{
				Directory.CreateDirectory("logs");
			}
			
			PrLog = new Progress<string>(actionLog);
			PrPBar = new Progress<double>(actionProgressBar);
			PrSBar = new Progress<string>(actionStatusBar);
		}
		
		public void Write(string mes)
		{
			string fileName = string.Format("logs/{0}-{1}-{2}.txt", DateTime.Now.Date.Year.ToString(), DateTime.Now.Date.Month.ToString(), DateTime.Now.Day.ToString());
			StreamWriter sw = new StreamWriter(fileName, true, Encoding.UTF8);
			sw.AutoFlush = true;
			
			if (sw.BaseStream.CanWrite) 
			{
				sw.Write(mes);
				sw.Close();
			}
		}
		
		
		public void Dispose()
		{
		
		}
	}
}
