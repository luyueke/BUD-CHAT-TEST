
namespace EasySpreadsheet
{
	/// <summary>
	/// Simple column types eg int a, float b, string c...
	/// </summary>
	public class EsColumnFieldBasicType : EsColumnField
	{
		private readonly string fieldName;
		private readonly string fieldType;
		private readonly string propertyName;

		public EsColumnFieldBasicType(EsConverter conv, int columnIndex, string rawColumnName, string rawColumnType):
			base(conv, columnIndex, rawColumnName, rawColumnType)
		{
			propertyName = IsKeyField ? rawColumnName.Split(':')[0].Trim() : rawColumnName.Trim();
			fieldName = "_" + propertyName;
			fieldType = rawColumnType.Trim();
		}
		
		public override string GetDeclarationLines()
		{
			var stringBuilder = EsStringBuilderCache.Get();
			if (IsKeyField)
				stringBuilder.Append("\t\t[EsKeyField]\n");
			stringBuilder.Append("\t\t[SerializeField]\n");
			stringBuilder.AppendFormat("\t\tprivate {0} {1};\n", fieldType, fieldName);
			if (EsSettings.Instance.writable)
				stringBuilder.AppendFormat("\t\tpublic {0} {1}\n\t\t{{\n\t\t\tget => {2};\n#if UNITY_EDITOR\n\t\t\tset {{ {2} = value; }}\n#endif\n\t\t}}\n\n", fieldType, propertyName, fieldName);
			else
				stringBuilder.AppendFormat("\t\tpublic {0} {1} => {2};\n\n", fieldType, propertyName, fieldName);
			
			return EsStringBuilderCache.Return(stringBuilder);
		}
		
		public override string GetParseLines()
		{
			return "\t\t\tEsParser.TryParse(cells[column++], out " + fieldName + ");\n";
		}
	}

	/// <summary>
	/// Array types eg List int a, List float b, List string c...
	/// </summary>
	public class EsColumnFieldBasicTypeArray : EsColumnField
	{
		private readonly string fieldName;
		private readonly string fieldType;
		private readonly string propertyName;

		public EsColumnFieldBasicTypeArray(EsConverter conv, int columnIndex, string rawColumnName, string rawColumnType):
			base(conv, columnIndex, rawColumnName, rawColumnType)
		{
			propertyName = rawColumnName.Trim();
			fieldName = "_" + propertyName;
			int startIndex = rawColumnType.IndexOf('[');
			fieldType = rawColumnType.Substring(0, startIndex).Trim();
		}
		
		public override string GetDeclarationLines()
		{
			var stringBuilder = EsStringBuilderCache.Get();
			stringBuilder.Append("\t\t[SerializeField]\n");
			stringBuilder.AppendFormat("\t\tprivate List<{0}> {1};\n", fieldType, fieldName);
			stringBuilder.AppendFormat("\t\tpublic List<{0}> {1} => {2};\n\n", fieldType, propertyName, fieldName);
			
			return EsStringBuilderCache.Return(stringBuilder);
		}

		public override string GetParseLines()
		{
			var stringBuilder = EsStringBuilderCache.Get();
			string tempArray = fieldName + "_parts_";
			stringBuilder.AppendFormat($"\t\t\tstring[] {tempArray} = string.IsNullOrEmpty(cells[column]) ? \n\t\t\tnew string[0] : cells[column].Split('{ArraySeprator}');\n");
			stringBuilder.Append("\t\t\tcolumn++;\n");
			stringBuilder.Append($"\t\t\t{fieldName} = new List<{fieldType}>({tempArray}.Length);\n");
			stringBuilder.Append($"\t\t\tfor(int i = 0; i < {tempArray}.Length; i++)\n");
			stringBuilder.Append("\t\t\t{\n");
			stringBuilder.Append($"\t\t\t\tEsParser.TryParse({tempArray}[i], out {fieldType} _temp_);\n");
			stringBuilder.Append($"\t\t\t\t{fieldName}.Add(_temp_);\n");
			stringBuilder.Append("\t\t\t}\n");
			return EsStringBuilderCache.Return(stringBuilder);
		}
	}
	
	/// <summary>
	/// 基础类型的Dictionary
	/// 需要keyArray valueArray用来序列化
	/// </summary>
	public class EsColumnFieldBasicTypeDictionary : EsColumnField
	{
		private readonly string fieldName;
		private readonly string propertyName;
		private readonly string keyType;
		private readonly string valueType;

		public EsColumnFieldBasicTypeDictionary(EsConverter conv, int columnIndex, string rawColumnName, string rawColumnType):
			base(conv, columnIndex, rawColumnName, rawColumnType)
		{
			propertyName = rawColumnName.Trim();
			fieldName = "_" + propertyName;
			int startIndex = rawColumnType.IndexOf('<');
			int sepIndex = rawColumnType.IndexOf(',');
			int endIndex = rawColumnType.IndexOf('>');
			keyType = rawColumnType.Substring(startIndex+1, sepIndex - startIndex - 1).Trim();
			valueType = rawColumnType.Substring(sepIndex + 1, endIndex - sepIndex - 1).Trim();
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
			stringBuilder.AppendFormat("\t\t\t\tEsParser.TryParse({0}Pairs2[1], out {1} {0}V);\n", fieldName, valueType);
			stringBuilder.AppendFormat("\t\t\t\t{0}Keys.Add({0}K);\n", fieldName);
			stringBuilder.AppendFormat("\t\t\t\t{0}Values.Add({0}V);\n", fieldName);
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