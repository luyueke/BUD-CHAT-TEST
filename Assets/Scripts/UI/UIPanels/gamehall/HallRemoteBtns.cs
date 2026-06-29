using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class HallRemoteBtns : MonoBehaviour
{
    public CButton Btn;

    public Text Txt;

    public RemoteImageBehaviour RemoteImage;

    public GameObject ResDot;

    List<HallRemoteBtn> WebtoolNewsDatas;

    int index;
    bool isnew;
    private void Awake()
    {
        Btn.onClick.AddListener(OnBtn);
        gameObject.SetActive(false);

        isnew = false;
        var time = SaveGameUtil.Inst.GetStrByPlayerPrefs(SaveGameUtil.NewDayTime);
        if (!string.IsNullOrEmpty(time))
        {
            isnew = TimeTools.IsNewWeek(long.Parse(time), TimeTools.DateTimeToSeconds(DateTime.Now));
        }
        else
        {
            isnew = true;
        }

        if (isnew)
        {
            SaveGameUtil.Inst.SetStrByPlayerPrefs(SaveGameUtil.NewDayTime, TimeTools.DateTimeToSeconds(DateTime.Now).ToString());
            SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveGameUtil.HallRemoteBtns, 0);
        }

        ListReq((bo) =>{
            if (bo)
            {
                if (WebtoolNewsDatas != null && WebtoolNewsDatas.Count > 0)
                {
                    index = SaveGameUtil.Inst.GetIntByPlayerPrefs(SaveGameUtil.HallRemoteBtns);
                    Show();
                }
            }
        });
    }

    void Show() 
    {
        Txt.text = "";
        if (index < WebtoolNewsDatas.Count)
        {
            ResDot.gameObject.SetActive(isnew);
            isnew = false;
            var data = WebtoolNewsDatas[index];
            RemoteImage.Load(data.iconUrl, true, (ca,bo) => {
                gameObject.SetActive(true);
                Txt.text = data.name;
                //switch (data.sort)
                //{
                //    case 1: Txt.text = "音符乐队"; break;
                //    case 2: Txt.text = "音符热气球"; break;
                //    case 3: Txt.text = "堇色暗香令"; break;
                //    case 4: Txt.text = "铃阙巡礼"; break;
                //    case 5: Txt.text = "喵币礼包"; break;
                //    case 6: Txt.text = "心动玫瑰礼包"; break;
                //    case 7: Txt.text = "马年新春礼包"; break;
                //    case 8: Txt.text = "音符布丁"; break;
                //    case 9: Txt.text = "Umi滑雪板"; break;
                //    case 10: Txt.text = "Y2K极冻狂潮"; break;
                //    case 11: Txt.text = "锦时迎春季"; break;
                //    case 12: Txt.text = "暗夜冥羽"; break;
                //    case 13: Txt.text = "魔法砰砰砰"; break;
                //    case 14: Txt.text = "星夜童话季"; break;
                //    case 15: Txt.text = "戏韵贺岁"; break;
                //    case 16: Txt.text = "粉韵花辇"; break;
                //    default: Txt.text = ""; break;
                //}
            });
        }
    }

    void OnBtn()
    {
        if (WebtoolNewsDatas != null && index < WebtoolNewsDatas.Count)
        {
            var data = WebtoolNewsDatas[index];
            if (data != null)
            {
                WebtoolNewsSkipManager.Inst.HandleSkip(data);
            }
 

            index++;
            SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveGameUtil.HallRemoteBtns, index);
            if (index >= WebtoolNewsDatas.Count)
            {
                index = 0;
            }
            Show();
        }
    }

    public void ListReq(Action<bool> ac) {
        LobbyInfoManager.Inst.GetLobbyInfo((data) => {
            if (data != null  && data.carouselList != null && data.carouselList.Count > 0)
            {
                data.carouselList.Sort((x,y) => { return x.sort.CompareTo(y.sort); });
                WebtoolNewsDatas = data.carouselList;
                ac?.Invoke(true);
            }
        });


     
    }


}