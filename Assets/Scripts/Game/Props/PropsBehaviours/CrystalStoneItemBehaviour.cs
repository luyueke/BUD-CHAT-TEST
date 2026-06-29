using System;
using Basic.Extensions;
using DG.Tweening;
using Game.Audio;
using Game.Base;
using GameData;
using UnityEngine;

/// <summary>
/// Author: pzkunn
/// Description: 冰晶宝石行为类
/// Date: 2022/10/21 13:15:36
/// </summary>
public enum IceGemMatType
{
    Bright, //亮色
    Dark //灰色
}
//todo:kingdom相关逻辑暂时被注释
public class CrystalStoneItemBehaviour : MonoBehaviour
{
    private Vector3 defPos = new Vector3(0, 0.3f, 0);
    private Vector3 defRot = new Vector3(-90, 0, 0);
    private Vector3 defSca = Vector3.one;

    public Animator animator;
    public GameObject effectGO;
    public GameObject glowGO;
    public GameObject glowEffect;
    public MeshRenderer cRenderer;
    public Material defMat;
    public Material repMat;

    public IceGemMatType matType;
    private NodeBaseBehaviour parentBehav;

    public void Init(NodeBaseBehaviour parent)
    {
        parentBehav = parent;
        // effectGO.SetActive(false);
        animator.enabled = false;
        animator.SetBool("isCollect", false);
        // GameUtils.RefreshMaterial(defMat);
        // GameUtils.RefreshMaterial(repMat);
        //考虑到从缓存池取出, 需要初始化
        ChangeIceGemMaterial(IceGemMatType.Bright);
    }

    public void OnChangeMode(GameMode mode, bool isChangeMat = true)
    {
        if (mode == GameMode.Edit)
        {
            animator.enabled = false;
            //还原模型位置
            animator.transform.localScale = defSca;
            animator.transform.localPosition = defPos;
            animator.transform.localRotation = Quaternion.Euler(defRot);
            //停止循环音效
            StopCrystalStoneLoop(parentBehav.gameObject);
        }
        else
        {
            animator.enabled = true;
            //复原动画
            PlayCollectAnim(false);
            //播放循环音效
            PlayCrystalStoneLoop(parentBehav.gameObject);
        }

        // effectGO.SetActive(false);

        if (isChangeMat)
        {
            // //版本50.1 -> 非大地图游玩模式下, 宝石显示灰透
            // if (mode == GameMode.Guest && !GlobalFieldController.IsDowntownEnter)
            // {
            //     ChangeIceGemMaterial(IceGemMatType.Dark);
            // }   
        }
    }

    //替换材质颜色, 游玩模式GetItems之后, 已收集过需要替换
    public void ChangeIceGemMaterial(IceGemMatType type)
    {
        matType = type;
        // cRenderer.material = type == IceGemMatType.Dark ? repMat : defMat;
        // glowGO.SetActive(type == IceGemMatType.Bright);
        // glowEffect.SetActive(type == IceGemMatType.Bright);
    }

    public void PlayCollectAnim(bool isCollect)
    {
        animator.SetBool("isCollect", isCollect);
        // animator.SetBool("isBright", matType == IceGemMatType.Bright && GlobalFieldController.IsDowntownEnter);
        animator.SetBool("isBright", false);
    }

    public void ActiveCollectEffect()
    {
        // effectGO.SetActive(true);
    }

    public void PlayOnDiscoverAnim()
    {
        //播放星星飞出动画(单次)
        transform.DOLocalRotate(new Vector3(0, 720, 0), 1, RotateMode.FastBeyond360);
        transform.DOLocalMoveY(3.14f, 1).SetEase(Ease.OutCubic);
        //星星飞出音效
        // AkSoundManager.Inst.PostEvent("Play_Stars_FlyOut", gameObject);
    }

    private void OnDisable()
    {
        //停止循环音效
        if (parentBehav && parentBehav.gameObject)
        {
            StopCrystalStoneLoop(parentBehav.gameObject);
        }
    }



    public void PlayCrystalStoneLoop(GameObject in_gameObjectID)
    {
    }

    public void StopCrystalStoneLoop(GameObject in_gameObjectID)
    {
    }
    
    public void PlayCrystalStonePickUp(GameObject in_gameObjectID)
    {
        AkSoundManager.Inst.PlayInteractable3DSound("PickUpStar", in_gameObjectID);
    }
    
}