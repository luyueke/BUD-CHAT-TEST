using System;

namespace EasySpreadsheet
{
	public static class EsType
	{
		public const string INT = "int";
		public const string FLOAT = "float";
		public const string DOUBLE = "double";
		public const string LONG = "long";
		public const string STRING = "string";
		public const string BOOL = "bool";
	}
	
	public class EsColumnFieldParser
	{
		private readonly EsConverter _converter;
		
		private const string KEY_FLAG = ":key";
		private static readonly string[] basicTypes = { EsType.INT, EsType.FLOAT, EsType.DOUBLE, EsType.LONG, EsType.STRING, EsType.BOOL };


		public EsColumnFieldParser(EsConverter conv)
		{
			_converter = conv;
		}
		
		public EsColumnField Parse(int columnIndex, string rawColumnName, string rawColumnType)
		{
			try
			{
				rawColumnType = rawColumnType.Trim();

				if (IsBasicType(rawColumnType))
					return new EsColumnFieldBasicType(_converter, columnIndex, rawColumnName, rawColumnType);
				if (IsBasicTypeArray(rawColumnType))
					return new EsColumnFieldBasicTypeArray(_converter, columnIndex, rawColumnName, rawColumnType);
				if (IsBasicTypeDictionary(rawColumnType))
					return new EsColumnFieldBasicTypeDictionary(_converter, columnIndex, rawColumnName, rawColumnType);
				
				if (IsCustomType(rawColumnType, out var type1))
					return new EsColumnFieldCustomType(type1, _converter, columnIndex, rawColumnName, rawColumnType);
				if (IsCustomTypeArray(rawColumnType, out var typeArr))
					return new EsColumnFieldCustomTypeArray(typeArr, _converter, columnIndex, rawColumnName, rawColumnType);
				if (IsCustomTypeDictionary(rawColumnType, out var typeDict))
					return new EsColumnFieldCustomTypeDictionary(typeDict, _converter, columnIndex, rawColumnName, rawColumnType);
				
				if (!string.IsNullOrEmpty(rawColumnName) && !string.IsNullOrEmpty(rawColumnType))
					EsLog.Error($"Failed to parse column \"{rawColumnName}\" with type \"{rawColumnType}\".");
			}
			catch (Exception e)
			{
				EsLog.Error(e.ToString());
			}

			return null;
		}
		
		public bool IsSupportedType(string rawColumnType)
		{
			if (string.IsNullOrEmpty(rawColumnType)) return false;
			
			var rawType = rawColumnType.Trim();
			
			if (IsBasicType(rawType)) return true;
			if (IsBasicTypeArray(rawType)) return true;
			if (IsBasicTypeDictionary(rawType)) return true;
			if (IsCustomType(rawType, out _)) return true;
			if (IsCustomTypeArray(rawType, out _)) return true;
			if (IsCustomTypeDictionary(rawType, out _)) return true;
			
			return false;
		}

		public bool IsKeyColumn(string rawColumnName, string rawColumnType)
		{
			string lowerTrimmedName = rawColumnName.ToLower().Trim();
			if (!lowerTrimmedName.EndsWith(KEY_FLAG))
				return false;
			if (!rawColumnType.Equals("int") && !rawColumnType.Equals("string"))
			{
				EsLog.Error(
					$"Only columns with type int or string can be key column, but {rawColumnName}'s type is {rawColumnType}.");
				return false;
			}
			return true;
		}
		
		public bool IsBasicType(string rawType)
		{
			rawType = rawType.Trim();
			foreach (var type in basicTypes)
				if (rawType.Equals(type))
					return true;
			return false;
		}

		private bool IsBasicTypeArray(string rawType)
		{
			if (!rawType.Contains("[]"))
				return false;
			int startIndex = rawType.IndexOf('[');
			string typeName = rawType.Substring(0, startIndex).Trim();
			if (string.IsNullOrEmpty(typeName))
				return false;
			foreach (var type in basicTypes)
				if (typeName.Equals(type))
					return true;
			return false;
		}

		// Format is <key type,value type>
		private bool IsBasicTypeDictionary(string rawType)
		{
			rawType = rawType.Trim();
			if (!rawType.StartsWith("<") || !rawType.EndsWith(">"))
				return false;
			int startIndex = rawType.IndexOf('<');
			int sepIndex = rawType.IndexOf(',');
			int endIndex = rawType.IndexOf('>');
			if (!(startIndex == 0 && sepIndex > 0 && endIndex > sepIndex))
				return false;
			string valueType = rawType.Substring(sepIndex + 1, endIndex - sepIndex - 1).Trim();
			foreach (var type in basicTypes)
				if (valueType.Equals(type))
					return true;
			return false;
		}
		
		// Format is PredefinedType
		private bool IsCustomType(string rawType, out EsConverter.PredefinedType typeInfo)
		{
			rawType = rawType.Trim();
			return _converter.PredefinedTypes.TryGetValue(rawType, out typeInfo);
		}

		// Format is PredefinedType[]
		private bool IsCustomTypeArray(string rawType, out EsConverter.PredefinedType typeInfo)
		{
			typeInfo = null;
			if (!rawType.Contains("[]")) return false;
			int startIndex = rawType.IndexOf("[]");
			string typeName = rawType.Substring(0, startIndex).Trim();
			if (string.IsNullOrEmpty(typeName))
				return false;
			return _converter.PredefinedTypes.TryGetValue(typeName, out typeInfo);
		}
		
		// Format is <key-type, PredefinedType>
		private bool IsCustomTypeDictionary(string rawType, out EsConverter.PredefinedType typeInfo)
		{
			typeInfo = null;
			rawType = rawType.Trim();
			if (!rawType.StartsWith("<") || !rawType.EndsWith(">"))
				return false;
			int startIndex = rawType.IndexOf('<');
			int sepIndex = rawType.IndexOf(',');
			int endIndex = rawType.IndexOf('>');
			if (!(startIndex == 0 && sepIndex > 0 && endIndex > sepIndex))
				return false;
			string typeName = rawType.Substring(sepIndex + 1, endIndex - sepIndex - 1).Trim();
			return _converter.PredefinedTypes.TryGetValue(typeName, out typeInfo);
		}
	}
	
}