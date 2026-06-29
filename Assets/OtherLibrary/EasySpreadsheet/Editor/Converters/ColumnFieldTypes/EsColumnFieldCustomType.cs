
using System;

namespace EasySpreadsheet
{
	/// <summary>
	/// Custom Type
	/// Format is {int a, float b, string c}
	/// </summary>
	public class EsColumnFieldCustomType : EsColumnField
	{
		private readonly string fieldName;
		private readonly string fieldType;
		private readonly string propertyName;

		private EsConverter.PredefinedType _typeInfo;

		public EsColumnFieldCustomType(EsConverter.PredefinedType typeInfo, EsConverter conv, int columnIndex, string rawColumnName, string rawColumnType):
			base(conv, columnIndex, rawColumnName, rawColumnType)
		{
			_typeInfo = typeInfo;
			propertyName = rawColumnName.Trim();
			fieldName = "_" + propertyName;
		}
		
		public override string GetDeclarationLines()
		{
			var stringBuilder = EsStringBuilderCache.Get();
			stringBuilder.Append("\t\t[SerializeField]\n");
			stringBuilder.AppendFormat("\t\tprivate {0} {1};\n", _typeInfo.fullName, fieldName);
			if (EsSettings.Instance.writable)
				stringBuilder.AppendFormat("\t\tpublic {0} {1}\n\t\t{{\n\t\t\tget => {2};\n#if UNITY_EDITOR\n\t\t\tset {{ {2} = value; }}\n#endif\n\t\t}}\n\n", _typeInfo.fullName, propertyName, fieldName);
			else
				stringBuilder.AppendFormat("\t\tpublic {0} {1} => {2};\n\n", _typeInfo.fullName, propertyName, fieldName);
			
			return EsStringBuilderCache.Return(stringBuilder);
		}

		public override string GetParseLines()
		{
			var stringBuilder = EsStringBuilderCache.Get();
			stringBuilder.AppendFormat("\t\t\t{0} = new {1}();\n", fieldName, _typeInfo.fullName);
			if (_typeInfo.isUnityType)
				stringBuilder.AppendFormat("\t\t\tUnityTypesParse.Parse(ref {0}, cells[column++]);\n", fieldName);
			else
				stringBuilder.AppendFormat("\t\t\t{0}.Parse(cells[column++]);\n", fieldName);
			
			return EsStringBuilderCache.Return(stringBuilder);
		}
	}
	
	public class EsColumnFieldCustomTypeArray : EsColumnField
	{
		private readonly string fieldName;
		private readonly string fieldType;
		private readonly string propertyName;

		private EsConverter.PredefinedType _typeInfo;

		public EsColumnFieldCustomTypeArray(EsConverter.PredefinedType typeInfo, EsConverter conv, int columnIndex, string rawColumnName, string rawColumnType):
			base(conv, columnIndex, rawColumnName, rawColumnType)
		{
			_typeInfo = typeInfo;
			propertyName = rawColumnName.Trim();
			fieldName = "_" + propertyName;
		}
		
		public override string GetDeclarationLines()
		{
			var stringBuilder = EsStringBuilderCache.Get();
			stringBuilder.Append("\t\t[SerializeField]\n");
			stringBuilder.AppendFormat("\t\tprivate List<{0}> {1};\n", _typeInfo.fullName, fieldName);
			stringBuilder.AppendFormat("\t\tpublic List<{0}> {1} => {2};\n\n", _typeInfo.fullName, propertyName, fieldName);
			
			return EsStringBuilderCache.Return(stringBuilder);
		}

		public override string GetParseLines()
		{
			var stringBuilder = EsStringBuilderCache.Get();
			
			string tempArray = fieldName + "_parts_";
			stringBuilder.AppendFormat($"\t\t\tstring[] {tempArray} = string.IsNullOrEmpty(cells[column]) ? \n\t\t\tnew string[0] : cells[column].Split('{ArraySeprator}');\n");
			stringBuilder.Append("\t\t\tcolumn++;\n");
			stringBuilder.Append($"\t\t\t{fieldName} = new List<{_typeInfo.fullName}>({tempArray}.Length);\n");
			stringBuilder.Append($"\t\t\tfor(int i = 0; i < {tempArray}.Length; i++)\n");
			stringBuilder.Append("\t\t\t{\n");
			stringBuilder.Append($"\t\t\t\tvar _temp_ = new {_typeInfo.fullName}();\n");
			if (_typeInfo.isUnityType)
				stringBuilder.AppendFormat($"\t\t\t\tUnityTypesParse.Parse(ref _temp_, {tempArray}[i]);\n");
			else
				stringBuilder.Append($"\t\t\t\t_temp_.Parse({tempArray}[i]);\n");
			stringBuilder.Append($"\t\t\t\t{fieldName}.Add(_temp_);\n");
			stringBuilder.Append("\t\t\t}\n");
			
			return EsStringBuilderCache.Return(stringBuilder);
		}
	}
	
	/// <summary>
	/// 自定义类型的Dictionary
	/// 需要keyArray valueArray用来序列化
	/// </summary>
	public class EsColumnFieldCustomTypeDictionary : EsColumnField
	{
		private readonly string fieldName;
		private readonly string propertyName;
		private readonly string keyType;
		private readonly string valueType;
		
		private readonly string fieldType;
		private EsConverter.PredefinedType _typeInfo;

		public EsColumnFieldCustomTypeDictionary(EsConverter.PredefinedType typeInfo, EsConverter conv, int columnIndex, string rawColumnName, string rawColumnType):
			base(conv, columnIndex, rawColumnName, rawColumnType)
		{
			_typeInfo = typeInfo;
			propertyName = rawColumnName.Trim();
			fieldName = "_" + propertyName;
			int startIndex = rawColumnType.IndexOf('<');
			int sepIndex = rawColumnType.IndexOf(',');
			//int endIndex = rawColumnType.IndexOf('>');
			keyType = rawColumnType.Substring(startIndex+1, sepIndex - startIndex - 1).Trim();
			//valueType = rawColumnType.Substring(sepIndex + 1, endIndex - sepIndex - 1).Trim();
			valueType = _typeInfo.fullName;
		}
		
		public override string GetDeclarationLines()
		{
			var stringBuilder = EsStringBuilderCache.Get();
			stringBuilder.Append("\t\t[SerializeField]\n");
			stringBuilder.AppendFormat("\t\tprivate List<{0}> {1}Keys;\n", keyType, fieldName);
			stringBuilder.Append("\t\t[SerializeField]\n");
			stringBuilder.AppendFormat("\t\tprivate List<{0}> {1}Values;\n", valueType, fieldName);
			stringBuilder.AppendFormat("\t\tprivate Dictionary<{0},{1}> {2};\n", keyType, valueType, fieldName);
			stringBuilder.AppendFormat("\t\tpublic Dictionary<{0},{1}> {2} => {3};\n\n", keyType, valueType, propertyName, fieldName);
			return EsStringBuilderCache.Return(stringBuilder);
		}

		public override string GetParseLines()
		{
			var stringBuilder = EsStringBuilderCache.Get();
			
			stringBuilder.AppendFormat("\t\t\tstring {0}RawData = cells[column++];\n", fieldName);
			stringBuilder.AppendFormat("\t\t\tstring[] {0}Pairs = {0}RawData.Split('{1}');\n", fieldName, ArraySeprator);
			stringBuilder.AppendFormat("\t\t\t{0}Keys = new List<{1}>();\n", fieldName, keyType);
			stringBuilder.AppendFormat("\t\t\t{0}Values = new List<{1}>();\n", fieldName, valueType);
			stringBuilder.AppendFormat("\t\t\tfor (int i = 0; i < {0}Pairs.Length; ++i)\n", fieldName);
			stringBuilder.Append("\t\t\t{\n");
			stringBuilder.AppendFormat("\t\t\t\tstring {0}Pair = {0}Pairs[i];\n", fieldName);
			stringBuilder.AppendFormat("\t\t\t\tstring[] {0}Pairs2 = {0}Pair.Split(':');\n", fieldName);
			stringBuilder.AppendFormat("\t\t\t\tif ({0}Pairs2.Length < 2) continue;\n", fieldName);
			stringBuilder.AppendFormat("\t\t\t\tEsParser.TryParse({0}Pairs2[0], out {1} {0}K);\n", fieldName, keyType);
			stringBuilder.Append($"\t\t\t\tvar _temp_ = new {valueType}();\n");
			if (_typeInfo.isUnityType)
				stringBuilder.AppendFormat($"\t\t\t\tUnityTypesParse.Parse(ref _temp_, {fieldName}Pairs2[1]);\n");
			else
				stringBuilder.Append($"\t\t\t\t_temp_.Parse({fieldName}Pairs2[1]);\n");
			stringBuilder.AppendFormat("\t\t\t\t{0}Keys.Add({0}K);\n", fieldName);
			stringBuilder.AppendFormat("\t\t\t\t{0}Values.Add(_temp_);\n", fieldName);
			stringBuilder.Append("\t\t\t}\n");
			
			return EsStringBuilderCache.Return(stringBuilder);
		}

		public override string GetAfterSerializedLines()
		{
			var stringBuilder = EsStringBuilderCache.Get();
			stringBuilder.AppendFormat("\t\t\t{0} = new Dictionary<{1}, {2}>({0}Keys.Count);\n", fieldName, keyType, valueType);
			stringBuilder.AppendFormat("\t\t\tfor (int i = 0; i < {0}Keys.Count; ++i)\n", fieldName);
			stringBuilder.Append("\t\t\t{\n");
			stringBuilder.AppendFormat("\t\t\t\tvar k = {0}Keys[i];\n", fieldName);
			stringBuilder.AppendFormat("\t\t\t\tvar v = {0}Values[i];\n", fieldName);
			stringBuilder.AppendFormat("\t\t\t\tif ({0}.ContainsKey(k))\n", fieldName);
			stringBuilder.AppendFormat("\t\t\t\t\tEsLog.Error(\"Dictionary {0} already contains key \" + k + \".\");\n", fieldName);
			stringBuilder.Append("\t\t\t\telse\n");
			stringBuilder.AppendFormat("\t\t\t\t\t{0}.Add(k, v);\n", fieldName);
			stringBuilder.Append("\t\t\t}\n");
			return EsStringBuilderCache.Return(stringBuilder);
		}
	}
	
}