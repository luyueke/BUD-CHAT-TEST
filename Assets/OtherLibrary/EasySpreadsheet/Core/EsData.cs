using System;
using UnityEngine;

namespace EasySpreadsheet
{
	/// <summary>
	///     One row in an excel sheet.
	/// </summary>
	[Serializable]
	public abstract class EsRowData : ISerializationCallbackReceiver
	{
		public object GetKeyFieldValue()
		{
			var keyField = EsUtility.GetRowDataKeyField(GetType());
			return keyField == null ? null : keyField.GetValue(this);
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			OnDeserialized();
		}

		protected abstract void OnDeserialized();
	}

	/// <summary>
	///     All RowData in an excel sheet
	/// </summary>
	public abstract class EsRowDataTable : ScriptableObject, ISerializationCallbackReceiver
	{
		public string SpreadsheetFileName;
		public string SpreadsheetSheetName;
		public string KeyFieldName;

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			OnDeserialized();
		}

		protected abstract void OnDeserialized();
	}

	/// <summary>
	/// Mark which field of class is key
	/// </summary>
	public class EsKeyFieldAttribute : Attribute
	{
	}

#if UNITY_EDITOR
	/// <summary>
	/// Simple Parser
	/// </summary>
	public static class EsParser
	{
		public static bool TryParse(string raw, out string ret)
		{
			ret = raw;
			return true;
		}

		public static bool TryParse(string raw, out int ret)
		{
			return int.TryParse(raw, out ret);
		}

		public static bool TryParse(string raw, out float ret)
		{
			return float.TryParse(raw, out ret);
		}

		public static bool TryParse(string raw, out double ret)
		{
			return double.TryParse(raw, out ret);
		}

		public static bool TryParse(string raw, out bool ret)
		{
			return bool.TryParse(raw, out ret);
		}
		public static bool TryParse(string raw, out long ret)
		{
			return long.TryParse(raw, out ret);
		}
    }
#endif

}
