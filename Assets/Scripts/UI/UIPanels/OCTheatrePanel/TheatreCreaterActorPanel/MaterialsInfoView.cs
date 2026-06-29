using GameData.BaseInfo;
using Message;
using UnityEngine;
using UnityEngine.UI;


public class MaterialsInfoView : MonoBehaviour
{
    private static readonly string[] GenderNames = { "未知", "男", "女" };

    private Text txt_desc;
    private Text txt_title1;
    private Text txt_sex;
    private Text txt_personality;
    private Text txt_bg;
    private Text txt_Important_people;

    private void Awake()
    {
        txt_desc = GameObjectEx.FindComponentByName<Text>(transform, "txt_desc");
        txt_title1 = GameObjectEx.FindComponentByName<Text>(transform, "txt_title1");
        txt_sex = GameObjectEx.FindComponentByName<Text>(transform, "txt_sex");
        txt_personality = GameObjectEx.FindComponentByName<Text>(transform, "txt_personality");
        txt_bg = GameObjectEx.FindComponentByName<Text>(transform, "txt_bg");
        txt_Important_people = GameObjectEx.FindComponentByName<Text>(transform, "txt_Important_people");

        MessageHelper.AddListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, ActorCardInfoUpdate);
        MessageHelper.AddListener<object[]>(MessageName.OnSliderValueChanged, OnSliderValueChanged);
        ActorCardInfoUpdate(null);
    }

    public void ActorCardInfoUpdate(OCTheatreAvatarInfo actor)
    {
        OCTheatreAvatarInfo info;
        if(actor != null)
        {
            info = actor;
        }
        else{
           info = OCTheatreActorEditorDataManager.Inst.GetCurActor();
        }
        //txt_title1.text = info.name;
        string desc = info.desc;
        if(string.IsNullOrEmpty(desc))
            desc = "待输入！";
        else if(desc.Length>149)
            desc = info.desc.Remove(149) + "...";
        txt_desc.text = desc;
        string backgroundDes = info.backgroundDes;
        if(string.IsNullOrEmpty(backgroundDes))
            backgroundDes = "待输入！";
        else if(backgroundDes.Length > 13)
            backgroundDes = info.backgroundDes.Remove(13) + "...";
        
        txt_bg.text = $"背       景：  {backgroundDes}";

        int gender = info.gender;
        string sexStr = gender >= 0 && gender < GenderNames.Length ? GenderNames[gender] : "";
        txt_sex.text = $"性       别：  {sexStr}" ;

        string personalityStr = info.personalities != null ? string.Join("、", info.personalities) : "";
        if(string.IsNullOrEmpty(personalityStr))
            personalityStr = "待输入！";
        else if(personalityStr.Length > 13)
            personalityStr = personalityStr.Remove(13) + "...";
       
        txt_personality.text = $"性       格：  {personalityStr}";
        string importantPersonsStr = info.importantPersons != null ? string.Join("、", info.importantPersons) : "";
        if(string.IsNullOrEmpty(importantPersonsStr))
            importantPersonsStr = "待输入！";
        else if(importantPersonsStr.Length > 13)
            importantPersonsStr = importantPersonsStr.Remove(13) + "...";

        txt_Important_people.text = $"重要的人：  {importantPersonsStr}";

        
    }

    private void OnSliderValueChanged(object[] args)
    {
        int type = args[0] as int? ?? 1;
        Color m_color = args[1] as Color? ?? default;
        switch (type)
        {
            case 1:
                break;
            case 2:
                break;
            case 3:
                txt_desc.color = m_color;
                txt_sex.color = m_color;
                txt_personality.color = m_color;
                txt_bg.color = m_color;
                txt_Important_people.color = m_color;
            
                break;
            case 4:
                break;
            default:
                break;
        }
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, ActorCardInfoUpdate);
        MessageHelper.RemoveListener<object[]>(MessageName.OnSliderValueChanged, OnSliderValueChanged);
    }
}
