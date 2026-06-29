using GameData.Base;
using UGCAsset;
using UGCAsset.Draft;

public class GameStudioUtils
{
    public static UgcBaseInfo GetBaseInfo(DraftListItem draftListItem)
    {
        if (draftListItem == null)
        {
            return null;
        }
        if (draftListItem.mapInfo != null)
        {
            return draftListItem.mapInfo;
        }
        else if (draftListItem.propInfo != null)
        {
            return draftListItem.propInfo;
        }
        else if (draftListItem.materialInfo != null)
        {
            return draftListItem.materialInfo;
        }
        else if (draftListItem.skinInfo != null)
        {
            return draftListItem.skinInfo;
        }
        else if (draftListItem.musicScoreInfo != null)
        {
            return draftListItem.musicScoreInfo;
        }
        else if (draftListItem.animInfo != null)
        {
            return draftListItem.animInfo;
        }
        else if (draftListItem.poseInfo != null)
        {
            return draftListItem.poseInfo;
        }
        else if (draftListItem.animMusicInfo != null)
        {
            return draftListItem.animMusicInfo;
        }
        else if (draftListItem.npc != null)
        {
            return draftListItem.npc;
        }
        else if (draftListItem.vehicleInfo != null)
        {
            return draftListItem.vehicleInfo;
        }
        else if (draftListItem.actorInfo != null)
        {
            return draftListItem.actorInfo;
        }
        else if (draftListItem.theatreInfo != null)
        {
            return draftListItem.theatreInfo;
        }
        return null;
    }

    public static MainViewType GetMainViewType(DraftListItem draftListItem)
    {
        if (draftListItem.propInfo != null)
        {
            return MainViewType.Prop;
        }
        else if (draftListItem.materialInfo != null)
        {
            return MainViewType.Material;
        }
        else if (draftListItem.skinInfo != null)
        {
            return MainViewType.Cloth;
        }
        return MainViewType.Game;
    }

    public static bool GetIsProp(DraftListItem draftListItem)
    {
        if (draftListItem.propInfo != null)
        {
            return true;
        }
        else if (draftListItem.skinInfo != null)
        {
            return draftListItem.skinInfo.isProp;
        }

        return false;
    }

    public static UploadStatus GetUploadStatus(DraftListItem draftListItem)
    {
        string infoId = GetBaseInfo(draftListItem).id;
        UploadStatus uploadStatus = UploadStatus.NotUpload;

        MainViewType mainViewType = GetMainViewType(draftListItem);

        if (mainViewType == MainViewType.Game)
        {
            MapDraftInfo draftInfo = MapAssetManager.Inst.GetDraftInfo(infoId);
            if (draftInfo != null)
            {
                uploadStatus = draftInfo.GetUploadStatus();
            }
        }
        else if (mainViewType == MainViewType.Prop)
        {
            PropDraftInfo draftInfo = PropAssetManager.Inst.GetDraftInfo(infoId);
            if (draftInfo != null)
            {
                uploadStatus = draftInfo.GetUploadStatus();
            }
        }
        else if (mainViewType == MainViewType.Material)
        {
            MaterialDraftInfo draftInfo = MaterialAssetManager.Inst.GetDraftInfo(infoId);
            if (draftInfo != null)
            {
                uploadStatus = draftInfo.GetUploadStatus();
            }
        }
        else if (mainViewType == MainViewType.Cloth)
        {
            SkinDraftInfo draftInfo = SkinAssetManager.Inst.GetDraftInfo(infoId);
            if (draftInfo != null)
            {
                uploadStatus = draftInfo.GetUploadStatus();
            }
        }

        return uploadStatus;
    }

    public static int GetDetailInteractNum(DraftListItem draftListItem)
    {
        MainViewType mainViewType = GetMainViewType(draftListItem);
        switch (mainViewType)
        {
            case MainViewType.Game:
                return draftListItem.interactInfo?.likeAmount ?? 0;
            case MainViewType.Cloth:
            case MainViewType.Prop:
            case MainViewType.Material:
                return draftListItem.interactInfo?.consumeAmount ?? 0;
        }

        return 0;
    }

}
