
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using EasySpreadsheet;


namespace Es
{
	[UnityEditor.CustomEditor(typeof(GameResDataTable))]
	public class GameResDataInspector : EsAssetInspector
	{
	}
}
#endif