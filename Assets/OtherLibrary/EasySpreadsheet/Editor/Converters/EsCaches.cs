using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace EasySpreadsheet
{
	/// <summary>
	/// EasySpreadsheet Caches
	/// </summary>
	public static class EsCaches
	{
		private static string CacheFile => Path.Combine(EsSettings.Instance.excelFilesPath, ".EasySpreadsheetCache");
		
		private static readonly Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>(128);

		private class FileInfo
		{
			public string scriptMd5;
			public string dataMd5;
		}


		public static void Reload()
		{
			LoadCache();
		}

		public static void Save()
		{
			SaveCache();
		}

		private static void LoadCache()
		{
			try
			{
				files.Clear();
				if (!File.Exists(CacheFile)) return;
				var fileStream = new StreamReader(CacheFile);
				string str1;
				while ((str1 = fileStream.ReadLine()) != null)
				{
					var array = str1.Split(';');
					if (array.Length != 3) continue;
					var file = array[0].Trim();
					var scriptMd5 = array[1].Trim();
					var dataMd5 = array[2].Trim();
					files[file] = new FileInfo(){scriptMd5 = scriptMd5, dataMd5 = dataMd5};
				}

				fileStream.Close();
			}
			catch (IOException e)
			{
				EsLog.Info(e.ToString());
			}
		}

		private static void SaveCache()
		{
			var fileNames = files.Keys.ToList();
			fileNames.Sort();
			
			var streamWriter = new StreamWriter(CacheFile, false);
			foreach (var fileName in fileNames)
				streamWriter.WriteLine(fileName + ";" + files[fileName].scriptMd5 + ";" + files[fileName].dataMd5);
			streamWriter.Close();
		}

		public static void Clear()
		{
			files.Clear();
			if (File.Exists(CacheFile))
				File.WriteAllText(CacheFile, "");
		}

		public static void UpdateScript(string file, string newMD5)
		{
			if (files.TryGetValue(file, out var ret))
			{
				ret.scriptMd5 = newMD5;
			}
			else
			{
				files.Add(file, new FileInfo(){scriptMd5 = newMD5});
			}
		}

		public static bool IsScriptChanged(string file, out string newMD5)
		{
			newMD5 = string.Empty;
			if (!File.Exists(file)) return true;
			newMD5 = CalFileMD5(file);
			var last = GetCachedMd5(file);
			if (last == null) return true;
			return last.scriptMd5 != newMD5;
		}
		
		public static void UpdateData(string file, string newMD5)
		{
			if (files.TryGetValue(file, out var ret))
			{
				ret.dataMd5 = newMD5;
			}
			else
			{
				files.Add(file, new FileInfo(){dataMd5 = newMD5});
			}
		}

		public static bool IsDataChanged(string file, out string newMD5)
		{
			newMD5 = string.Empty;
			if (!File.Exists(file)) return true;
			newMD5 = CalFileMD5(file);
			var last = GetCachedMd5(file);
			if (last == null) return true;
			return last.dataMd5 != newMD5;
		}

		private static FileInfo GetCachedMd5(string file)
		{
			files.TryGetValue(file, out var ret);
			return ret;
		}

		private static readonly System.Security.Cryptography.MD5CryptoServiceProvider s_md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
		private static readonly System.Text.StringBuilder sb = new System.Text.StringBuilder(128);

		private static string CalFileMD5(string file)
		{
			try
			{
				if (!File.Exists(file)) return string.Empty;

				//md5.Clear();
				sb.Clear();

				byte[] bytes = File.ReadAllBytes(file);
				byte[] retVal = s_md5.ComputeHash(bytes, 0, bytes.Length);
				for (int i = 0; i < retVal.Length; i++)
				{
					sb.Append(retVal[i].ToString("x2"));
				}

				return sb.ToString();
			}
			catch (Exception ex)
			{
				if (ex is System.IO.IOException)
					EsLog.Error($"Please Close {Path.GetFileName(file)} first.\n" + ex.ToString());
				else
					EsLog.Error(ex.ToString());
			}

			return string.Empty;
		}
	}
	
	
	public static class EsStringBuilderCache
	{
		private static readonly List<StringBuilder> stringBuilders = new List<StringBuilder>();

		public static void Reset()
		{
			stringBuilders.Clear();
		}
		
		public static StringBuilder Get()
		{
			if (stringBuilders.Count == 0)
				return new StringBuilder(1024);
			var first = stringBuilders[0];
			stringBuilders.RemoveAt(0);
			//EsLog.Error("get " + stringBuilders.Count.ToString());
			return first;
		}

		public static string Return(StringBuilder strb)
		{
			if (strb == null) return null;
			if (!stringBuilders.Contains(strb))
				stringBuilders.Add(strb);
			var str = strb.ToString();
			strb.Clear();
			//EsLog.Error(stringBuilders.Count.ToString());
			return str;
		}
	}
	
}