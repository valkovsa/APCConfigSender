using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace APCConfigSender
{
	/// <summary>
	/// Класс выделяет из входящей строки IP адреса
	/// </summary>
	/// <param name="addressesForBatch">Не разобранная строка с IP адресами</param>
	/// <returns>IP адрес/са в формате IPAddress / List<IPAddress></returns>
	public static class ParseFromString
	{
		/// <summary>
		/// Выделяет все IP адреса из входящей строки
		/// </summary>
		/// <param name="str">Входящая строка</param>
		/// <returns>Коллекция IP адресов в формате - List<IPAddress>, если адреса не найдены - null</returns>
		public static List<IPAddress> GetMultipleIp(string str)
		{
			List<IPAddress> result = new List<IPAddress>();
			string template = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}";
			
			Regex ipReg = new Regex(template);
			MatchCollection ipMatches = ipReg.Matches(str);
			
			if (ipMatches.Count == 0) 
			{
				return null;
			}
			
			foreach (var el in ipMatches) 
			{
				result.Add(IPAddress.Parse(el.ToString()));
			}
			
			return result.Distinct().ToList();
		}
		
		/// <summary>
		/// Выделяет 1 IP адрес из входящей строки
		/// </summary>
		/// <param name="str">Входящая строка</param>
		/// <returns>IP адрес в формате - IPAddress, если адрес не найден - null</returns>
		public static IPAddress GetOneIp(string str)
		{
			string template = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}";
			
			Regex ipReg = new Regex(template);
			Match ipMatch = ipReg.Match(str);
			if (ipMatch.Success) 
			{
				return IPAddress.Parse(ipMatch.ToString());
			}
			else
			{
				return null;
			}
			
		}
		
		public static Dictionary<string, string> ConfigToDict(string source)
		{
			string pattern = @"(?<key>^\[.*\])(?<value>[^\[\]]*)";
			
			List<KeyValuePair<string, string>>	listConf = 
			( from Match m in Regex.Matches(source, pattern, RegexOptions.Multiline)
				 select new KeyValuePair<string, string> (
					m.Groups["key"].Value, 
					string.Format("{0}{1}",m.Groups["key"].Value, m.Groups["value"].Value)
				) ).ToList<KeyValuePair<string, string>>();
			
			if (listConf.Count !=0) 
			{
				listConf.Insert(0, new KeyValuePair<string, string>("[All sections]", source));
				
				return listConf.ToDictionary(x => x.Key, x => x.Value.Trim('\n','\r'));
			}
			else
			{
				return null;
			}
		}
		
		public static List<KeyValuePair<string, string>> ConfigToList(string source)
		{
			string pattern = @"(?<key>^\[.*\])(?<value>[^\[\]]*)";
			
			List<KeyValuePair<string, string>>	listConf = 
			( from Match m in Regex.Matches(source, pattern, RegexOptions.Multiline)
				 select new KeyValuePair<string, string> (
					m.Groups["key"].Value, 
					string.Format("{0}{1}",m.Groups["key"].Value, m.Groups["value"].Value)
				) ).ToList<KeyValuePair<string, string>>();
			
			if (listConf.Count !=0) 
			{
				listConf.Insert(0, new KeyValuePair<string, string>("[All sections]", source));
				
				return listConf;
			}
			else
			{
				return null;
			}
		}
		

	}
	
	
}
