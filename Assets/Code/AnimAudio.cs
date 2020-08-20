using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimAudio : StateMachineBehaviour
{
    [SerializeField]
    string audioCodeName;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.transform.parent.Find(audioCodeName).TryGetComponent(out AudioCode audioCode))
        {
            audioCode.PlayNextTake();
        }
    }
}
