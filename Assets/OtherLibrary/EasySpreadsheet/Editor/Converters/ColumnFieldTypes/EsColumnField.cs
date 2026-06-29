
namespace EasySpreadsheet
{
	/// <summary>
	/// Abstract Column field
	/// </summary>
	public abstract class EsColumnField
	{
		protected EsConverter Converter;
		
		protected readonly int rawColumnIndex;
		protected readonly string rawFieldName;
		public readonly string rawFieldType;
		public bool IsKeyField { get; private set; }
		public string KeyFieldType { get; private set; }
		public string KeyFieldName { get; private set; }

		public readonly string ArraySeprator = EsConstant.DefaultSeperator;


		protected EsColumnField(EsConverter conv, int columnIndex, string rawColumnName, string rawColumnType)
		{
			Converter = conv;
			rawColumnIndex = columnIndex;
			rawFieldName = rawColumnName;
			rawFieldType = rawColumnType;
			if (Converter.FieldParser.IsKeyColumn(rawColumnName, rawColumnType))
				SetAsKeyField();
			if (rawFieldName.Contains("sep="))
				ArraySeprator = EsEditorUtility.SubstringSingle(rawFieldName, "(sep=", ")");
		}
		
		public void SetAsKeyField()
		{
			IsKeyField = true;
			KeyFieldType = rawFieldType;
			int index = rawFieldName.IndexOf(":");
			KeyFieldName = index < 0 ? rawFieldName : rawFieldName.Substring(0, index);
		}
		
		public abstract string GetDeclarationLines();

		public abstract string GetParseLines();

		public virtual string GetAfterSerializedLines()
		{
			return null;
		}
		
	}

}