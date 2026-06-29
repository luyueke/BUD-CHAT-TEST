using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class PlantTreePanelPlayerItem : MonoBehaviour
{
    public RemoteImageBehaviour Head;
    public SuperTextMesh Name;
    public Image Frame;
    public Transform Trans_BottomEffect;
    public Transform Trans_TopEffect;
    public Text Count;
    public Image Seed;
    public RemoteImageBehaviour RemoteImage;
    [SerializeField] private Button headButton;

    public void SetData(ContestEntryInfo info) 
    {
        var creator = info.creator;
        Head.Load(creator.portraitUrl);
        //string nickStr = DataUtil.RemoveRichTextAndEmoji(creator.nickname);
        //Name.SetText(nickStr);
        Name.SetText(creator.nickname);

        Count.text = info.scoreInfo.score.ToString();

        Frame.gameObject.SetActive(false);
        Trans_BottomEffect.ClearChildren();
        Trans_TopEffect.ClearChildren();
        var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(creator.avatarFrame, this.gameObject);
        if (headCycleData != null)
        {
            Frame.gameObject.SetActive(true);
            Frame.sprite = headCycleData.Sp_HeadCycle;

            if (headCycleData.Effect_Bottom != null)
            {
                headCycleData.Effect_Bottom.Instantiate(Trans_BottomEffect);
            }

            if (headCycleData.Effect_Top != null)
            {
                headCycleData.Effect_Top.Instantiate(Trans_TopEffect);
            }
        }

        headButton.onClick.AddListener(() =>
        {
            if (creator == null)
            {
                return;
            }

            string uid = creator.uid;
            if (string.IsNullOrEmpty(uid))
            {
                return;
            }

            UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, uid);
        });

        var url = info.creationInfo.cover;
        if (url.StartsWith("http"))
        {
            RemoteImage.gameObject.SetActive(true);
            Seed.gameObject.SetActive(false);
            RemoteImage.Load(url);
        }
        else
        {
            RemoteImage.gameObject.SetActive(false);
            Seed.gameObject.SetActive(true);
            var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcPropSprite);
            var spriteAtlas = XAssetLoaderMgr.Inst.LoadResource<SpriteAtlas>(atlasPath, gameObject);
            Seed.sprite = spriteAtlas.GetSprite(url);
        }


    }
}