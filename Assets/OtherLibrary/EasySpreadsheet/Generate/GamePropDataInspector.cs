
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using EasySpreadsheet;


namespace Es
{
	[UnityEditor.CustomEditor(typeof(GamePropDataTable))]
	public class GamePropDataInspector : EsAssetInspector
	{
	}
}
#endif