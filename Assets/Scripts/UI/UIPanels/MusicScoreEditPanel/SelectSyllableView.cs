using System;
using System.Collections;
using System.Collections.Generic;
using Game.MusicalInstrument;
using GameData.BaseInfo;
using Network.Message;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;

public class SelectSyllableView : MonoBehaviour
{
   public CButton closeViewBtn;
   public CButton yesBtn;
   public Transform syllableContent;
   private List<SelectSyllableItem> _allSelectItems = new List<SelectSyllableItem>();
   public SelectSyllableItem longItem;
   public SelectSyllableItem emptyItem;
   private MusicScoreSyllableInfo curInfo;
   private int curId;
   private Action<int,MusicScoreSyllableInfo> _setSuccessCallBack;
   private bool canSelectLong;
   public GameObject yesBtnMask;

   public GameObject lowKeyIcon;
   
   public void Init()
   {
      closeViewBtn = GameObjectEx.FindComponentByName<CButton>(transform, "CloseBtn");
      
      syllableContent = GameObjectEx.FindChildByName(transform, "Content");
      closeViewBtn.onClick.AddListener(Close);
      yesBtn.onClick.AddListener(OnYesBtnClick);
      var items = syllableContent.GetComponentsInChildren<SelectSyllableItem>();
      for (int i = 0; i < items.Length; i++)
      {
         int syllableId = i + 1;//音标唯一id为1-22
         items[i].Init(()=>
         {
            OnItemClick(syllableId);
         },syllableId,new Color(0.07f, 0.07f, 0.07f, 0.12f));
         items[i].SetStatus(SelectSyllableItem.Status.canSelect);
         _allSelectItems.Add(items[i]);
      }
      longItem.Init(OnLongItemClick,0,new Color(0.85f, 0.82f, 1f, 1f));
      emptyItem.Init(OnEmptyItemClick,0,new Color(0.85f, 0.82f, 1f, 1f));
   }
   public void Open(int id,MusicScoreSyllableInfo info,Action<int,MusicScoreSyllableInfo> setSuccessCallBack,bool canSelectLong,int toneType)
   {
      curInfo = JsonConvert.DeserializeObject<MusicScoreSyllableInfo>(JsonConvert.SerializeObject(info));
      curId = id;
      this.canSelectLong = canSelectLong;
      gameObject.SetActive(true);
      SetViewItemsShow();
      _setSuccessCallBack = setSuccessCallBack;
      SetToneType(toneType);
   }

   public void SetToneType(int toneType)
   {
      bool isShowLowKeys = toneType == (int)ToneType.TwentyTwo;
      lowKeyIcon.SetActive(isShowLowKeys);
      for (int i = 15; i < _allSelectItems.Count; i++)
      {
         _allSelectItems[i].gameObject.SetActive(isShowLowKeys);
      }
   }
   private void SetViewItemsShow()
   {
      switch (curInfo.syllableType)
      {
         case (int)MusicScoreSyllableType.Syllables:
            for (int i = 0; i < _allSelectItems.Count; i++)
            {
               _allSelectItems[i].SetStatus(SelectSyllableItem.Status.canSelect);
            }
            if (curInfo.syllablesList!=null&&curInfo.syllablesList.Count>0)
            {
               for (int i = 0; i < curInfo.syllablesList.Count; i++)
               {
                  var item = _allSelectItems.Find(x => x.Id == curInfo.syllablesList[i]);
                  if (item!=null)
                  {
                     item.SetStatus(SelectSyllableItem.Status.Selected);
                  }
               }
            }
            longItem.SetStatus(SelectSyllableItem.Status.canSelect);
            emptyItem.SetStatus(SelectSyllableItem.Status.canSelect);
            break;
         case (int)MusicScoreSyllableType.Long:
            for (int i = 0; i < _allSelectItems.Count; i++)
            {
               _allSelectItems[i].SetStatus(SelectSyllableItem.Status.canSelect);
            }
            longItem.SetStatus(SelectSyllableItem.Status.Selected);
            emptyItem.SetStatus(SelectSyllableItem.Status.canSelect);
            break;
         case (int)MusicScoreSyllableType.Empty:
            for (int i = 0; i < _allSelectItems.Count; i++)
            {
               _allSelectItems[i].SetStatus(SelectSyllableItem.Status.canSelect);
            }
            longItem.SetStatus(SelectSyllableItem.Status.canSelect);
            emptyItem.SetStatus(SelectSyllableItem.Status.Selected);
            break;
      }
      if (curInfo.syllableType == (int)MusicScoreSyllableType.Syllables&&(curInfo.syllablesList==null||curInfo.syllablesList.Count==0))
      {
         yesBtn.interactable = false;
         yesBtnMask.SetActive(true);
      }
      else
      {
         yesBtn.interactable = true;
         yesBtnMask.SetActive(false);
      }
      
   }
   public void OnItemClick(int id)
   {
      if (curInfo.syllablesList==null)
      {
         curInfo.syllablesList = new List<int>();
      }
      if (curInfo.syllablesList.Contains(id))
      {
         curInfo.syllablesList.Remove(id);
      }
      else
      {
         if (curInfo.syllablesList.Count>=3)
         {
            TipPanel.ShowToast("最多支持3个音哦，可以点击对应音符取消选择");
            return;
         }
         else
         {
            curInfo.syllablesList.Add(id);
            var syllableIds = new List<int>();
            syllableIds.Add(id);
            MusicalInstrumentManager.Inst.PlaySingleSyllable(PreviewAudioType.TwoD, MusicalInstrumentUtils.GetDefaultToneInfo(), syllableIds, (int)LongShortType.Short, this.gameObject);
         }
      }
      curInfo.syllableType = (int)MusicScoreSyllableType.Syllables;
      SetViewItemsShow();
   }
   public void OnLongItemClick()
   {
      if (!canSelectLong)
      {
         TipPanel.ShowToast("第一个音不可以是连音哦");
         return;
      }
      if (curInfo.syllableType == (int)MusicScoreSyllableType.Long)
      {
         curInfo.syllableType = (int)MusicScoreSyllableType.Syllables;
      }
      else
      {
         if (curInfo.syllablesList!=null)
         {
            curInfo.syllablesList.Clear();
         }
         curInfo.syllableType = (int)MusicScoreSyllableType.Long;
      }
     
      SetViewItemsShow();
   }
   public void OnEmptyItemClick()
   {
      if (curInfo.syllableType == (int)MusicScoreSyllableType.Empty)
      {
         curInfo.syllableType = (int)MusicScoreSyllableType.Syllables;
      }
      else
      {
         if (curInfo.syllablesList != null)
         {
            curInfo.syllablesList.Clear();
         }

         curInfo.syllableType = (int)MusicScoreSyllableType.Empty;
      }

      SetViewItemsShow();
   }
   public void Close()
   {
      gameObject.SetActive(false);
   }

   public void OnYesBtnClick()
   {
      if (curInfo.syllablesList!=null&&curInfo.syllablesList.Count>0)
      {
         curInfo.syllablesList.Sort((x, y) => x>y?1:-1);
      }
      _setSuccessCallBack?.Invoke(curId,curInfo);
      Close();
   }
}
