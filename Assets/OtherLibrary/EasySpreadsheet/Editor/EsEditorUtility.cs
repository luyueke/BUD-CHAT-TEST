using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace EasySpreadsheet
{
	public static class EsEditorUtility
	{
		public static bool IsSpreadsheetFileSupported(string filePath)
		{
			if (string.IsNullOrEmpty(filePath))
				return false;
			var fileName = Path.GetFileName(filePath);
			if (fileName.Contains("~$"))// ignore temporary files
				return false;
			var lower = Path.GetExtension(filePath).ToLower();
			return lower == ".xlsx" || lower == ".xls" || lower == ".xlsm";
		}
		
		public static string SubstringSingle(string source, string startStr, string endStr)
		{
			Regex rg = new Regex("(?<=(" + startStr + "))[.\\s\\S]*?(?=(" + endStr + "))", RegexOptions.Multiline | RegexOptions.Singleline);
			return rg.Match(source).Value;
		}
 
		public static List<string> SubstringMultiple(string source, string startStr, string endStr)
		{
			Regex rg = new Regex("(?<=(" + startStr + "))[.\\s\\S]*?(?=(" + endStr + "))", RegexOptions.Multiline | RegexOptions.Singleline);
            
			MatchCollection matches = rg.Matches(source);
 
			List<string> resList=new List<string>();
 
			foreach (Match item in matches)
				resList.Add(item.Value);
 
			return resList;
		}
	}
}