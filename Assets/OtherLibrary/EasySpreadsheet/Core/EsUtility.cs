using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EasySpreadsheet
{
	public static class EsConstant
	{
		public const string Version = "5.0.1";

		public const string FieldNameFlag = "##name";
		public const string FieldTypeFlag = "##type";
		public const string HeaderFlag = "##";
		public const int HeaderRowRange = 5;
		public const int FieldStartColumn = 1;
		public const string DefaultSeperator = ";";
		public const string PredefinedTypesFile = "_PredefinedTypes.xlsx";
		
		public const string DataTablesName = "DataTables";
	}
	
	public static class EsLog
	{
		public static void Info(string message)
		{
			Debug.Log("[EasySpreadsheet] " + message);
		}
		
		public static void Warning(string message)
		{
			Debug.LogWarning("[EasySpreadsheet] " + message);
		}

		public static void Error(string message)
		{
			Debug.LogError("[EasySpreadsheet] " + message);
		}
	}
	
	public static class EsUtility
	{
		public static FieldInfo GetRowDataKeyField(Type rowDataType)
		{
			var fields = rowDataType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			var keyField = (from fieldInfo in fields let attrs = fieldInfo.GetCustomAttributes(typeof(EsKeyFieldAttribute), false) 
				where attrs.Length > 0 select fieldInfo).FirstOrDefault();
			return keyField;
		}
	}
}