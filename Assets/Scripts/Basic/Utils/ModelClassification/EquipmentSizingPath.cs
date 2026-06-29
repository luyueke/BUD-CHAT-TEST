using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentSizingPath
{
    public static string httpUrl = "https://cdn.joinbudapp.com/";

    public static string specialFileName = "equipmentSpecial.json";

    public static string fpsFileName = "equipmentFPS.json";

    public static string specialUrlDir_Master = "EquipmentSizing/Master/";

    public static string specialUrlDir_Alpha = "EquipmentSizing/Alpha/";

    public static string specialUrlDir_Prod = "EquipmentSizing/Prod/";

    public static string equipmentSpecialUrl_Master
    {
        get
        {
            return httpUrl+ specialUrlDir_Master+ specialFileName;
        }
    }

    public static string equipmentSpecialUrl_Alpha
    {
        get
        {
            return httpUrl + specialUrlDir_Alpha + specialFileName;
        }
    }

    public static string equipmentSpecialUrl_Prod
    {
        get
        {
            return httpUrl + specialUrlDir_Prod + specialFileName;
        }
    }
}
