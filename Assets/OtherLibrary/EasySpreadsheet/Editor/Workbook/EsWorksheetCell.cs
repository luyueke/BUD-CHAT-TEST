
namespace EasySpreadsheet
{
	public class EsWorksheetCell
	{
		public readonly int column;
		public readonly int row;
		public string value;

		public EsWorksheetCell(int row, int column, string value)
		{
			this.row = row;
			this.column = column;
			this.value = string.IsNullOrEmpty(value) ? string.Empty : value;
		}
	}
}