using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Es;
using UnityEngine;

public enum CharacterStyle
{
    Avatar,
    Pet
}

public class UgcPartDataManager : GlobalInstance<UgcPartDataManager>
{
   private Dictionary<string, Dictionary<int,UgcPartData>> ugcPartDatas = new Dictionary<string, Dictionary<int,UgcPartData>>();
   public UgcPartDataManager()
   {
      var ugcDatas =  Es.DataTables.GetUgcPartDataList().ToList();
      var patugcDatas =  Es.DataTables.GetPetUgcPartDataList();
      ugcDatas.AddRange(patugcDatas);
      for (var i = 0; i < ugcDatas.Count; i++)
      {
          var data = ugcDatas[i];
          if (!ugcPartDatas.ContainsKey(data.uId))
          {
              var temp = new Dictionary<int, UgcPartData>();
              temp.Add(data.ugcType,data);
              ugcPartDatas.Add(data.uId,temp);
          }
          else
          {
              var temp = ugcPartDatas[data.uId];
              if (!temp.ContainsKey(data.ugcType))
              {
                  temp.Add(data.ugcType,data);
              }
              else
              {
                  Debug.LogError($"ugcPartData {data.uId} has exist");
              }
          }
      }
   }

   public UgcPartData GetUgcPartDataOnFirst(string id)
   {
       if (ugcPartDatas.ContainsKey(id))
       {
           return ugcPartDatas[id].Values.First();
       }
       else
       {
           Debug.LogError($"get ugcPart Data {id} not exist ");
           return null;
       }
   }

   public List<UgcPartData> GetUgcPartDataList(string id)
   {
       if (ugcPartDatas.ContainsKey(id) && ugcPartDatas[id].Values.Count > 0)
       {
           return ugcPartDatas[id].Values.ToList();
       }
       else
       {
           Debug.LogError($"GetUgcPartDataList {id} not exist ");
           return new List<UgcPartData>();
       }
   }
   public int GetUgcPartCount(string id)
   {
       if (ugcPartDatas.ContainsKey(id) && ugcPartDatas[id].Values.Count > 0)
       {
           return ugcPartDatas[id].Count;
       }
       else
       {
           Debug.LogError($"GetUgcPartDataList {id} not exist ");
           return 0;
       }
   }

   public int GetUgcTextureCount(string id)
   {
       return GetUgcPartCount(id)*2;

   }
   public UgcPartData GetUgcPartData(string id, int ugcType)
   {
       if (ugcPartDatas.ContainsKey(id))
       {
           return ugcPartDatas[id][ugcType];
       }
       else
       {
           Debug.LogError($"get ugcPart Data {id} not exist ");
           return null;
       }
   }

   public UgcPartData GetPartDataByPartName(string tempId, string name)
   {
       var allPartsData = GetUgcPartDataList(tempId);
       foreach (var partData in allPartsData)
       {
           if (partData.partsName.Equals(name))
           {
               return partData;
           }
       }

       return null;
   }

}
