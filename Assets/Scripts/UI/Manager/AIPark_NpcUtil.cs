using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIPark_NpcUtil
    {
        public static string GetCata(string id, Image img)
        {
            if (id == AccountDataManager.Inst.UserInfo.uid)
            {
                return AccountDataManager.Inst.UserInfo.portraitUrl;
            }

            if (Enum.TryParse(id, out ParkNpcRoleType type))
            {
                img.sprite = PgcUtils.LoadNpcHeadIcon(Es.DataTables.GetPgcNpcConfig((int)type).CataPath, img.gameObject);
                img.gameObject.SetActive(true);
                return "";
            }

            if (AIParkGameUgcsetTool.gameConfig != null && AIParkGameUgcsetTool.gameConfig.npcData != null)
            {
                foreach (var item in AIParkGameUgcsetTool.gameConfig.npcData)
                {
                    if (id == item.id)
                    {
                        return item.cover;
                    }
                }
            }

            return "";
        }
        public static string GetHead(string id, Image img)
        {
            if (id == AccountDataManager.Inst.UserInfo.uid || id == ((int)ParkNpcRoleType.self).ToString())
            {
                return AccountDataManager.Inst.UserInfo.portraitUrl;
            }

            if (Enum.TryParse(id, out ParkNpcRoleType type))
            {
                img.sprite = PgcUtils.LoadNpcHeadIcon(Es.DataTables.GetPgcNpcConfig((int)type).HeadPath, img.gameObject);
                img.gameObject.SetActive(true);
                return "";
            }

            if (AIParkGameUgcsetTool.gameConfig != null && AIParkGameUgcsetTool.gameConfig.npcData != null)
            {
                foreach (var item in AIParkGameUgcsetTool.gameConfig.npcData)
                {
                    if (id == item.id)
                    {
                        return item.cover;
                    }
                }
            }

            return "";
        }

        public static string GetName(string id)
        {
            if (id == AccountDataManager.Inst.UserInfo.uid || id == ((int)ParkNpcRoleType.self).ToString())
            {
                return AccountDataManager.Inst.UserInfo.nickname;
            }
            if (AIParkUtils.Inst.ParkGameData.isPgcEnter)
            {
                if (Enum.TryParse(id, out ParkNpcRoleType type))
                {
                    if (type == ParkNpcRoleType.self)
                    {
                        return AccountDataManager.Inst.UserInfo.nickname;
                    }
                    var cfg = Es.DataTables.GetPgcNpcConfig((int)type);
                    if (cfg == null)
                    {
                        return "";
                    }
                    return cfg.Name;
                }
                return "";
            }


            if (AIParkGameUgcsetTool.gameConfig != null && AIParkGameUgcsetTool.gameConfig.npcData != null)
            {
                foreach (var item in AIParkGameUgcsetTool.gameConfig.npcData)
                {
                    if (id == item.id)
                    {
                        return item.name;
                    }
                }
            }

            return "";
        }

        public static string GetNpcId(string npcName)
        {
            if (npcName == AccountDataManager.Inst.UserInfo.nickname)
            {
                return ((int)ParkNpcRoleType.self).ToString();
            }
            if (AIParkUtils.Inst.ParkGameData.isPgcEnter)
            {
                foreach (var item in Enum.GetValues(typeof(ParkNpcRoleType)))
                {
                    var cfg = Es.DataTables.GetPgcNpcConfig((int)item);
                    if (cfg != null)
                    {
                        if (cfg.Name == npcName)
                        {
                            return ((int)item).ToString();
                        }
                    }
                }
            }else{
                if(AIParkGameUgcsetTool.gameConfig != null && AIParkGameUgcsetTool.gameConfig.npcData != null){
                    foreach(var item in AIParkGameUgcsetTool.gameConfig.npcData){
                        if(item.name == npcName){
                            return item.id;
                        }
                    }
                }
            }
            return "";
        }

        public static string GetDesc(string id)
        {
            if (id == AccountDataManager.Inst.UserInfo.uid)
            {
                return AccountDataManager.Inst.UserInfo.nickname;
            }

            if (Enum.TryParse(id, out ParkNpcRoleType type))
            {
                return Es.DataTables.GetPgcNpcConfig((int)type).Desc;
            }

            if (AIParkGameUgcsetTool.gameConfig != null && AIParkGameUgcsetTool.gameConfig.npcData != null)
            {
                foreach (var item in AIParkGameUgcsetTool.gameConfig.npcData)
                {
                    if (id == item.id)
                    {
                        return item.desc;
                    }
                }
            }

            return "";
        }

        public static string GetPlot(string id)
        {
            if (id == AccountDataManager.Inst.UserInfo.uid)
            {
                return AccountDataManager.Inst.UserInfo.nickname;
            }

            if (Enum.TryParse(id, out ParkNpcRoleType type))
            {
                return Es.DataTables.GetPgcNpcConfig((int)type).Plot;
            }

            if (AIParkGameUgcsetTool.gameConfig != null && AIParkGameUgcsetTool.gameConfig.npcData != null)
            {
                foreach (var item in AIParkGameUgcsetTool.gameConfig.npcData)
                {
                    if (id == item.id)
                    {
                        return item.plot;
                    }
                }
            }

            return "";
        }

        public static string GetAvatar(string id)
        {
            if (id == AccountDataManager.Inst.UserInfo.uid)
            {
                return AccountDataManager.Inst.UserInfo.nickname;
            }

            if (AIParkUtils.Inst.ParkGameData.isPgcEnter)
            {
                return AIParkUtils.Inst.CreateAvatarJsonByParkNpcType(id);
            }


            if (AIParkGameUgcsetTool.gameConfig != null && AIParkGameUgcsetTool.gameConfig.npcData != null)
            {
                foreach (var item in AIParkGameUgcsetTool.gameConfig.npcData)
                {
                    if (id == item.id)
                    {
                        return item.npcAvatarJson;
                    }
                }
            }

            return "";
        }

        public static List<string> GetAllNpcNames(List<string> roleIdList)
        {
            List<string> allNpcNames = new();
            foreach (var roleId in roleIdList)
            {
                allNpcNames.Add(GetName(roleId));
            }
            return allNpcNames;
        }



        public static List<string> GetAllNpcAvatars(List<string> roleIdList)
        {
            List<string> allNpcAvatars = new();
            foreach (var roleId in roleIdList)
            {
                allNpcAvatars.Add(GetAvatar(roleId));
            }
            return allNpcAvatars;
        }
    }
}