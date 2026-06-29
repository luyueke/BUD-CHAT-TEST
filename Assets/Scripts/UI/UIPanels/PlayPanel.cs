using Game.Base;
using GameData;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PlayPanel : BaseGamePlayPanel<PlayPanel>
{
    [SerializeField] private Button m_returnBtn;
    [SerializeField] private CButton emoBtn;

    private GameMode curGameMode = GameMode.Play;

    public override void OnCreate()
    {
        base.OnCreate();
        m_returnBtn.onClick.AddListener(ChangeEditMode);
        emoBtn.onClick.AddListener(OnEmoBtnClick);
    }
  
    private void ChangeEditMode()
    {   
        GameController.ChangeMode(GameMode.Edit, () =>
        {
            UIManager.Inst.OpenPanel(PanelId.GameEditModePanel);
            CloseSelf();
        });
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        if (args != null && args.Length > 0)
        {
            curGameMode = (GameMode)args[0];
        }

        if (curGameMode == GameMode.Play)
        {
            ShowPlayMode();
        }
        else
        {
            ShowGuestMode();
        }
    }

    private void ShowPlayMode()
    {
        emoBtn.gameObject.SetActive(true);
    }

    private void ShowGuestMode()
    {

    }

    private void OnEmoBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.EmoMenuPanel);
    }
}
