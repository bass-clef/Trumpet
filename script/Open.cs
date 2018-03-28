using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Open : StateMachineBehaviour {
	public StateMachineBehaviourCallBack onStateExit;

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (null != onStateExit) {
			onStateExit(animator, stateInfo, layerIndex);
		}
	}
}
