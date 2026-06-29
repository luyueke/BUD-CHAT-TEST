using System;
using GameData;
using UI.UIPanels.GameEdit;
using UnityEngine;
using UnityEngine.UI;

public class ChangeShapeSubView : BasePropertyEditSubView
{
    [Header("Shape Setting")]
    public Toggle CubeToggle;

    public Toggle CylinderToggle;

    [Header("Tile Setting")]
    public GameObject TileSettingGo;    
    public Button AddTileBtn;
    public Button ReduceTileBtn;

    private Action<PropModelShape> ChangeShapeCallback;
    private Action AddTileClickCallback;
    private Action ReduceTileClickCallback;

    protected override void OnInit()
    {
        CubeToggle.onValueChanged.AddListener(OnCubeSelect);
        CylinderToggle.onValueChanged.AddListener(OnCylinderSelect);
        AddTileBtn.onClick.AddListener(OnAddTileClick);
        ReduceTileBtn.onClick.AddListener(OnReduceTileClick);
    }

    public void SetDefaultShape(PropModelShape value)
    {
        switch (value)
        {
            case PropModelShape.Cube:
                CubeToggle.SetIsOnWithoutNotify(true);
                break;
            case PropModelShape.Cylinder:
                CylinderToggle.SetIsOnWithoutNotify(true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }
    }

    public void AddShapeChangeListener(Action<PropModelShape> callBack)
    {
        ChangeShapeCallback += callBack;
    }

    public void RemoveShapeChangeListener(Action<PropModelShape> callBack)
    {
        ChangeShapeCallback -= callBack;
    }

    public void ClearAllListener()
    {
        ChangeShapeCallback = null;
        AddTileClickCallback = null;
        ReduceTileClickCallback = null;
    }

    private void OnCubeSelect(bool isOn)
    {
        if (isOn)
        {
            ChangeShapeCallback?.Invoke(PropModelShape.Cube);
        }
    }

    private void OnCylinderSelect(bool isOn)
    {
        if (isOn)
        {
            ChangeShapeCallback?.Invoke(PropModelShape.Cylinder);
        }
    }

    private void OnDestroy()
    {
        ClearAllListener();
    }

    private void OnAddTileClick()
    {
        AddTileClickCallback?.Invoke();
    }

    private void OnReduceTileClick()
    {
        ReduceTileClickCallback?.Invoke();
    }

    public void AddTileChangeListener(Action onTileAdd, Action onTileReduce)
    {
        this.AddTileClickCallback = onTileAdd;
        this.ReduceTileClickCallback = onTileReduce;
    }

    public void SetTileSettingVisible(bool isEnable)
    {
        if (TileSettingGo)
        {
            TileSettingGo.SetActive(isEnable);
        }
    }
}