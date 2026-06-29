using System.Collections.Generic;


public class AvatarColorManager : GameInstance<AvatarColorManager>
{
    //颜色数据ID
    public static string HAIR_ALL = "10300001";
    public static string HAIR_COM = "10300002";
    public static string SKIN_ALL = "10300003";
    public static string SKIN_COM = "10300004";
    public static string FACESTYLE_ALL = "10300005";
    public static string FACESTYLE_COM = "10300006";
    public static string BROW_ALL = "10300007";
    public static string BROW_COM = "10300008";
    public static string GLASSES_ALL = "10300009";
    public static string GLASSES_COM = "10300010";
    public static string BAG_ALL = "10300011";
    public static string BAG_COM = "10300012";
    public static string EYE_ALL = "10300013";
    public static string EYE_COM = "10300014";
    public static string HAT_ALL = "10300015";
    public static string HAT_COM = "10300016";
    public static string EARRINGS_ALL = "10300017";
    public static string EARRINGS_COM = "10300018";
    public static string VISOR_ALL = "10300017";
    public static string VISOR_COM = "10300018";


    public static string PaletteKey = "PaletteId";

    public static List<string> GetColorList(string colorId)
    {
        return Es.DataTables.GetAvatarColorData(colorId).color;
    }
}