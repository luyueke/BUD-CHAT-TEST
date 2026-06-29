using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIGame.Base
{
    public class LocationContainer : MonoBehaviour
    {
        [Header("归属区域")]
        [SerializeField] private ELocation _localtion;
        [Header("照片数量")]
        [SerializeField] private int _itemCount;
        [Header("照片模板")]
        [SerializeField] private AIGamePicUpLoadBtn _ItemPrefab;

        public void InitData(MapInfo mapInfo)
        {
            if (mapInfo == null)
            {
                LoggerUtils.LogError("MapInfo is null");
                return;
            }

            // 初始化当前位置的照片数据
            if (mapInfo.gameSetting.aIGameConfig.hospitalPhotos[(int)_localtion] == null)
            {
                mapInfo.gameSetting.aIGameConfig.hospitalPhotos[(int)_localtion] = new HospitalPhotoData
                {
                    urls = new List<string>()
                };
            }

            // 确保urls列表有足够的容量
            var locationPhotos = mapInfo.gameSetting.aIGameConfig.hospitalPhotos[(int)_localtion];
            while (locationPhotos.urls.Count < _itemCount)
            {
                locationPhotos.urls.Add(string.Empty);
            }
            locationPhotos.location = (int)_localtion;

            // 创建UI按钮
            for (int i = 0; i < _itemCount; i++)
            {
                var item = Instantiate(_ItemPrefab.gameObject, transform);
                item.gameObject.SetActive(true);
                item.transform.SetParent(transform);
                AIGamePicUpLoadBtn btn = item.GetComponent<AIGamePicUpLoadBtn>();
                btn.InitData(mapInfo);
                btn.InitData(_localtion, i);
            }
            gameObject.SetActive(_itemCount>0);
        }
    }
}