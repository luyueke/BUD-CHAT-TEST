using System;
using Basic.UndoRedo;
using Game.ECS;
using GameData;
using UI.UIPanels.GameEdit;
using UI.UIWidgets;
using UndoSystem;
using UnityEngine;
using UnityEngine.UI;

public class WaterCubeSubView : BasePropertyEditSubView
{
    [Header("Shape Setting")]
    public Toggle CubeToggle;
    public Toggle CylinderToggle;
    
    [Header("Speed Setting")]
    public Toggle SlowToggle;
    public Toggle MidToggle;
    public Toggle FastToggle;

    [Header("Oxygen Setting")] 
    public Toggle NoOxygenToggle;
    public Toggle HasOxygenToggle;
    
    [Header("Tile Setting")]
    public EditTilingView TilingView;

    private Action<PropModelShape> _changeShapeCallback;
    private Action<WaterSpeed> _changeSpeedCallback;
    private Action<int> _changeOxygenCallback;
   

    protected override void OnInit()
    {
        CubeToggle.onValueChanged.AddListener(OnCubeSelect);
        CylinderToggle.onValueChanged.AddListener(OnCylinderSelect);
        SlowToggle.onValueChanged.AddListener(OnSlowToggleChange);
        MidToggle.onValueChanged.AddListener(OnMidToggleChange);
        FastToggle.onValueChanged.AddListener(OnFastToggleChange);
        NoOxygenToggle.onValueChanged.AddListener(OnNoOxygenChange);
        HasOxygenToggle.onValueChanged.AddListener(OnHasOxygenChange);
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

    public void SetDefaultSpeed(WaterSpeed speed)
    {
        switch (speed)
        {
            case WaterSpeed.Slow:
                SlowToggle.SetIsOnWithoutNotify(true);
                break;
            case WaterSpeed.Mid:
                MidToggle.SetIsOnWithoutNotify(true);
                break;
            case WaterSpeed.Fast:
                FastToggle.SetIsOnWithoutNotify(true);
                break;
        }
    }

    public void SetDefaultOxygen(int value)
    {
        if (value == 0)
        {
            NoOxygenToggle.SetIsOnWithoutNotify(true);
        }
        else
        {
            HasOxygenToggle.SetIsOnWithoutNotify(true);
        }

    }

    public void SetTiling(Vector2 tileValue)
    {
        TilingView.SetTiling(tileValue);
    }

    public override void OnSelectEntity(SceneEntity entity)
    {
        base.OnSelectEntity(entity);
        TilingView.SetSelectEntity(entity);
    }

    public void AddShapeChangeListener(Action<PropModelShape> callBack)
    {
        _changeShapeCallback += callBack;
    }
    
    public void AddSpeedChangeListener(Action<WaterSpeed> callback)
    {
        _changeSpeedCallback += callback;
    }

    public void AddOxygenChangeListener(Action<int> callback)
    {
        _changeOxygenCallback += callback;
    }

    public void RemoveSpeedChangeListener(Action<WaterSpeed> callback)
    {
        _changeSpeedCallback -= callback;
    }
    
    public void RemoveShapeChangeListener(Action<PropModelShape> callBack)
    {
        _changeShapeCallback -= callBack;
    }
    
    public void RemoveOxygenChangeListener(Action<int> callback)
    {
        _changeOxygenCallback -= callback;
    }


    public void ClearAllListener()
    {
        TilingView.ClearTileChangeListener();
        _changeShapeCallback = null;
        _changeSpeedCallback = null;
        _changeOxygenCallback = null;
    }

    private void OnCubeSelect(bool isOn)
    {
        if (isOn)
        {
            _changeShapeCallback?.Invoke(PropModelShape.Cube);
        }
    }

    private void OnCylinderSelect(bool isOn)
    {
        if (isOn)
        {
            _changeShapeCallback?.Invoke(PropModelShape.Cylinder);
        }
    }
    
    private void OnSlowToggleChange(bool isOn)
    {
        if (isOn)
        {
            _changeSpeedCallback?.Invoke(WaterSpeed.Slow);
        }
    }

    private void OnMidToggleChange(bool isOn)
    {
        if (isOn)
        {
            _changeSpeedCallback?.Invoke(WaterSpeed.Mid);
        }
    }

    private void OnFastToggleChange(bool isOn)
    {
        if (isOn)
        {
            _changeSpeedCallback?.Invoke(WaterSpeed.Fast);
        }
    }

    private void OnNoOxygenChange(bool isOn)
    {
        if (isOn)
        {
            _changeOxygenCallback?.Invoke(0);
        }
    }

    private void OnHasOxygenChange(bool isOn)
    {
        if (isOn)
        {
            _changeOxygenCallback?.Invoke(1);
        }
    }

    private void OnDestroy()
    {
        ClearAllListener();
    }


    public void AddTileChangeListener(Action onTileAdd, Action onTileReduce)
    {
        TilingView.AddTileAddClickListenter(onTileAdd);
        TilingView.AddTileReduceClickListener(onTileReduce);
    }

    public void AddTileUndoListener(Action<Vector2> tileValue)
    {
        TilingView.AddUndoListener(tileValue);
    }

    public void SetTileSettingVisible(bool isEnable)
    {
        if (TilingView.gameObject != null)
        {
            TilingView.gameObject.SetActive(isEnable);
        }
    }
}