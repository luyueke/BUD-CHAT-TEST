using System.Collections;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class LimitTimePropManager : GlobalInstance<LimitTimePropManager>
{
    public List<LimitTimePgc> limitTimePgc;
    public List<string> petTemplateList;
    public void RefashLimitTimeProps()
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.LimitedTimeBackpack,
            HttpMethod.GET,
            null,
            (content) => {
                LimitTimeBackpack limitTimeBackpack = JsonConvert.DeserializeObject<LimitTimeBackpack>(content);
                limitTimePgc = limitTimeBackpack.limitedTimePgcList;
                if (limitTimeBackpack.ugcTemplate != null)
                {
                    petTemplateList = limitTimeBackpack.ugcTemplate.petTemplateList;
                }
            },
            (error) => {
                
            });
    }

    public LimitTimePgc GetLimitTimePorp(string pgcId)
    {
        if (limitTimePgc==null)
        {
            return null;
        }
        return limitTimePgc.Find(x => x.pgcId == pgcId);
    }
    public bool CheckLimitPetProp(string pgcId)
    {
        if (petTemplateList==null)
        {
            return false;
        }
        return petTemplateList.Contains(pgcId);
    }
}

public class LimitTimeBackpack
{
    public LimitTimeUgcTemplate ugcTemplate;
    public List<LimitTimePgc> limitedTimePgcList;
}
public class LimitTimePgc
{
    public string pgcId;
    public int dataType;
    public string leftTime;
}
public class LimitTimeUgcTemplate
{
    public List<string> petTemplateList;
}