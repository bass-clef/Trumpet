using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEnd : StateMachineBehaviour {
	public System.Action<Animator, AnimatorStateInfo, int> onStateExit;

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		foreach(AnimatorControllerParameter child in animator.parameters) {
			switch(child.type) {
			case AnimatorControllerParameterType.Bool:
				animator.SetBool(child.name, false);
				break;
			}
		}
		if (null != onStateExit) {
			onStateExit(animator, stateInfo, layerIndex);
		}
	}
}
