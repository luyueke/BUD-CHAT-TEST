using System.Collections;
using System.Collections.Generic;
using Game.Base;
using GameData;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class TestPlayPanel  : BasePanel<TestPlayPanel>
{
    // Start is called before the first frame update
    public Button editBtn;


    public override void OnCreate()
    {
        base.OnCreate();
        editBtn.onClick.AddListener(OnEditModeClicked);
    }

    private void OnEditModeClicked()
    {
        GameController.ChangeMode(GameMode.Edit, () =>
        {
            UIManager.Inst.OpenPanel(PanelId.GameEditModePanel);
            CloseSelf();
        });
    }

}
