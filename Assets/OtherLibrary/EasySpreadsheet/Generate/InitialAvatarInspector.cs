
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using EasySpreadsheet;


namespace Es
{
	[UnityEditor.CustomEditor(typeof(InitialAvatarTable))]
	public class InitialAvatarInspector : EsAssetInspector
	{
	}
}
#endif