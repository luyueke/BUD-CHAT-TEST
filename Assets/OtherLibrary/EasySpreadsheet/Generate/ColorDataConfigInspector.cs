
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using EasySpreadsheet;


namespace Es
{
	[UnityEditor.CustomEditor(typeof(ColorDataConfigTable))]
	public class ColorDataConfigInspector : EsAssetInspector
	{
	}
}
#endif