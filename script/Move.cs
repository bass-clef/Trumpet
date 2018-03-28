using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void StateMachineBehaviourCallBack(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);
public class Move : StateMachineBehaviour {
	[SerializeField] AvatarTarget targetBodyPart = AvatarTarget.Root;
	[SerializeField, Range(0, 1)] float start = 0, end = 1;

	public StateMachineBehaviourCallBack onStateExit;
	public CardInfo parentCard;

	[HeaderAttribute("match target")]
	public Vector3 matchPosition;		// 指定パーツが到達して欲しい座標
	public Quaternion matchRotation;	// 到達して欲しい回転

	[HeaderAttribute("Weights")]
	public Vector3 positionWeight = new Vector3(1, 0, 1);
	public float rotationWeight = 0;			// 回転に与えるウェイト。

	private MatchTargetWeightMask weightMask;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
	{
		weightMask = new MatchTargetWeightMask(positionWeight, rotationWeight);
	}

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
	{
		animator.MatchTarget(matchPosition, matchRotation, targetBodyPart, weightMask, start, end);
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (null != onStateExit) {
			onStateExit(animator, stateInfo, layerIndex);
		}
	}
}
