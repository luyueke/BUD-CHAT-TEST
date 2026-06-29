using System.Collections;
using System.Collections.Generic;
using Game;
using Game.GameSetting;
using UnityEngine;

public class GraphicsManager: GlobalInstance<GraphicsManager>
{
    private static Dictionary<GraphicsEnum, float> resolutionRit = new()
    {
        { GraphicsEnum.None, 1f },
        { GraphicsEnum.Low, 0.5f },
        { GraphicsEnum.Medium, 0.8f },
        { GraphicsEnum.High, 1f }
    };
    Vector2 screenResolution = Vector2.zero;
    public GraphicsManager()
    {
        screenResolution.x = Screen.currentResolution.width;
        screenResolution.y = Screen.currentResolution.height;
    }
    public void SetGraphics(int graphics)
    {
        GizmoController.CurRate = resolutionRit[(GraphicsEnum) graphics];
        Screen.SetResolution((int)(screenResolution.x * resolutionRit[(GraphicsEnum)graphics]), (int)(screenResolution.y * resolutionRit[(GraphicsEnum)graphics]), true);
    }

    public float GetCurResolutionRit() {
        return resolutionRit[(GraphicsEnum)GlobalSettingManager.Inst.GetGraphics()];
    }

}
