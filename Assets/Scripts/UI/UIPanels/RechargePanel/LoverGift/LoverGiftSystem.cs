using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public class LoverGiftSystem : GlobalInstance<LoverGiftSystem>
{
    public List<string> productIds = new List<string>();

    public override void Initialize()
    {
        base.Initialize();
#if UNITY_ANDROID
        productIds.Add("android_bud2026meiguiqingrenjietouxiangkuang30");
        productIds.Add("android_bud2026qingrenjietouxiangkuang30");
        productIds.Add("android_bud2026qingrenjieliaotianqipao68");
        productIds.Add("android_bud2026qingrenjiezhuyepifu128");
#else
        productIds.Add("ios_bud2026meiguiqingrenjietouxiangkuang30");
        productIds.Add("ios_bud2026qingrenjietouxiangkuang30");
        productIds.Add("ios_bud2026qingrenjieliaotianqipao68");
        productIds.Add("ios_bud2026qingrenjiezhuyepifu128");
#endif
    }




}