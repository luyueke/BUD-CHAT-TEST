using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using Newtonsoft.Json;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class PartnerActorItem : MonoBehaviour
{
    [SerializeField] private Image ActorIcon;
    [SerializeField] private RemoteImageBehaviour ActorRemoteIcon;
    [SerializeField] private Text ActorName;

    public void SetData(characterInteraction data)
    {
        if (data == null) return;
        SetDataInternal(data.emoteId, data.isPgc == 1, data.ugcData);
    }

    public void SetData(pEmoteData data)
    {
        if (data == null) return;
        // pEmoteData 无 isPgc 字段，ugcData 为 null 时视为 PGC
        SetDataInternal(data.emoteId, data.ugcData == null, data.ugcData);
    }

    public void SetData(voiceCommands data)
    {
        if (data == null) return;
        SetDataInternal(data.emoteId, data.isPgc == 1, data.ugcData);
    }

    private void SetDataInternal(string emoteId, bool isPgc, UgcIdleData ugcData)
    {
        if (string.IsNullOrEmpty(emoteId) || emoteId == "leisure")
        {
            gameObject.SetActive(false);
            return;
        }
        if (isPgc)
        {
            ActorRemoteIcon.gameObject.SetActive(false);
            ActorIcon.gameObject.SetActive(true);
            ActorName.text = PgcUtils.GetEmoteName(emoteId) ?? string.Empty;
            var sprite = PgcUtils.LoadEmoteIcon(emoteId, gameObject);
            if (sprite != null) ActorIcon.sprite = sprite;
        }
        else
        {
            ActorIcon.gameObject.SetActive(false);
            ActorRemoteIcon.gameObject.SetActive(true);
            if (ugcData != null && !string.IsNullOrEmpty(ugcData.cover))
                ActorRemoteIcon.Load(ugcData.cover);
            AssetsDataManager.GetUgcAnimInfo(emoteId, (isSuccess, serverData) =>
            {
                if (!isSuccess || ActorName == null) return;
                ActorName.text = serverData.animInfo.name ?? string.Empty;
            });
        }
    }
}
