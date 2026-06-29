using AIGame.Base;
using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIGame.Base
{
    public class PictureSettingContent : SettingContentBase
    {
        // Start is called before the first frame update
        [SerializeField] private List<LocationContainer> _containers;

        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);

            if (mapInfo.gameSetting.aIGameConfig == null)
            {
                mapInfo.gameSetting.aIGameConfig = new AIGameConfig();
            }

            // 确保 hospitalPhotos 数组已初始化
            while (mapInfo.gameSetting.aIGameConfig.hospitalPhotos.Count<_containers.Count)
            {
                mapInfo.gameSetting.aIGameConfig.hospitalPhotos.Add(new HospitalPhotoData());
            }

            for (int i = 0; i < _containers.Count; i++)
            {
                _containers[i].InitData(mapInfo);
            }
        }
    }
}