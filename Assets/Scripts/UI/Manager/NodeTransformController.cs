/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-07 16:01:29
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-07 16:12:21
 * @ Description: 业务代码的Gizmo的回调
 */


using Game;
using Game.Base;
using Game.Props.PropsController;
using Game.Props.PropsManagers;
using UnityEngine;

public class NodeTransformController : GameInstance<NodeTransformController> {
    public void OnDragUpdate(GameObject target, HandleMode handleMode) {
        switch (handleMode) {
            case HandleMode.Move:
                if (GlobalNodeManager.Inst.Get<SpawnPointManager>() != null) {
                    GlobalNodeManager.Inst.Get<SpawnPointManager>().OnDragMoveUpdate(target);
                }
                break;
            case HandleMode.Rotate:
                break;
            case HandleMode.Scale:
                break;
        }

    }

    public void OnDragBegin(GameObject target, HandleMode handleMode) {

    }

    public void OnDragEnd(GameObject target, HandleMode handleMode) {
        switch (handleMode) {
            case HandleMode.Move:
                if (MovementController.HasInstance) {
                    MovementController.Inst.OnDragMoveEnd(target);
                }
                break;
            case HandleMode.Rotate:
                break;
            case HandleMode.Scale:
                if (GlobalNodeManager.Inst.Get<PGCPlantManager>() != null) {
                    GlobalNodeManager.Inst.Get<PGCPlantManager>().OnDragMoveEnd(target);
                }

                break;
        }
    }
}
