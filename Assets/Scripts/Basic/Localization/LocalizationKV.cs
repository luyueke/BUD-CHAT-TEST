using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Lang-en", menuName = "Custom Data/LocalizationData")]
public class LocalizationKV : ScriptableObject
{
    public List<LangKV> list;
}
