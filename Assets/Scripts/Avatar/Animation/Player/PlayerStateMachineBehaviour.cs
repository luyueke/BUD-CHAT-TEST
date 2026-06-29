using UnityEngine;

public class PlayerStateMachineBehaviour : StateMachineBehaviour
{
    private IUpdateState updateState;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (updateState == null)
        {
            updateState = animator.GetComponent<IUpdateState>();
        }

        updateState?.StateEnter(stateInfo, layerIndex);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (updateState == null)
        {
            updateState = animator.GetComponent<IUpdateState>();
        }

        updateState?.StateExit(stateInfo, layerIndex);
    }
}
