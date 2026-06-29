using UnityEngine;

public interface IUpdateState
{
    void StateEnter(AnimatorStateInfo stateInfo, int layerIndex);

    void StateExit(AnimatorStateInfo stateInfo, int layerIndex);
}