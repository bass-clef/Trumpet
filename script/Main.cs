using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Kind {
	spade = 0, club, heart, dia,
	start, exit, blank
}
public enum Direction {
	left = 0, up, right, down,
	throught, stop, cover
}
/* [カードの状態]
 * {Direction}	{Kind}	状態
 * stop			[mark]	表,置かれた状態
 * throught		[mark]	表,取れる状態
 * cover		[mark]	裏
 * throught		blank	板
 */ 

public enum MoveStatus {
	stop = 0, check_to_point, moving
}
public enum MoveMode {
	free = 0, stage, card_effect
}
public class MoveInfo {
	public System.Action<MoveStatus> changeMoveStatus;
	private MoveStatus moveStatus;
	public MoveMode moveMode;

	public MoveStatus MoveStatus {
		get { return moveStatus; }
		set {
			if (null != changeMoveStatus && moveStatus != value) changeMoveStatus(value);
			moveStatus = value;
		}
	}
}

public class Main : MonoBehaviour {

	public GameObject warpPoint;
	public GameObject spawnPoint;
	public GameObject toPoint;
	public GameObject plane;
	public GameObject stageBaseParent;
	public GameObject defaultSpawnPoint;
	public AudioClip se_trumpet, se_blank;

	public bool pause, fallout;
	public Vector2Int myPos;
	public CardInfo myInfo;
	public MoveInfo moveInfo = new MoveInfo();
	public bool debug = false;

	StageMain stageMain;
	float cubeSizeHalf;
	Vector3 cameraPos = new Vector3(-0.5f, 6.5f, -3.5f), defaultPos;
	AudioSourceManager audioManager;

	public bool Fallout {
		get {
			return fallout;
		}
		set {
			fallout = value;
			if (fallout) {
				transform.GetComponent<Animator>().enabled = false;
				transform.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
				transform.GetComponent<Rigidbody>().useGravity = true;
				transform.GetComponent<Rigidbody>().isKinematic = false;

			} else {
				transform.GetComponent<Animator>().enabled = true;
				transform.GetComponent<Rigidbody>().constraints = defaultConstraints;
				transform.GetComponent<Rigidbody>().useGravity = false;
				transform.GetComponent<Rigidbody>().isKinematic = true;
			}
		}
	}

	void Awake() {
		moveInfo.MoveStatus = MoveStatus.stop;
	}

	// Use this for initialization
	void Start () {
		cubeSizeHalf = transform.localScale.y / 2f;
		stageMain = stageBaseParent.GetComponent<StageMain>();
		defaultPos = transform.position;
		moveInfo.changeMoveStatus = changeMoveStatus;

		// SlideMoveアニメーション終わりのほげほげ
		foreach(var behaviour in transform.GetComponent<Animator>().GetBehaviours<AnimationEnd>()) {
			behaviour.onStateExit = (Animator animator, AnimatorStateInfo stateInfo, int layerIndex) => {
				moveInfo.MoveStatus = MoveStatus.check_to_point;
				transform.GetComponent<Rigidbody>().constraints = defaultConstraints;
			};
		}

		// AudioSource複数作成
		audioManager = new AudioSourceManager(10, (AudioSourceWrapper audioSource) => {
			audioSource.content = gameObject.AddComponent<AudioSource>();
		});
	}
	
	// Update is called once per frame
	void Update () {
		if (MoveMode.free == moveInfo.moveMode) {
			Camera.main.transform.position = new Vector3(
				transform.position.x + cameraPos.x,
				Camera.main.transform.position.y, Camera.main.transform.position.z
			);
		}
		calcTrumpet();
		calcMove();
	}

	// TrumpetMain
	public GameObject cardTook;
	private RaycastHit prevHit;
	private Vector3 mousePos;
	public CardInfo toci, fromci;
	const float tookCardDistance = 2f;
	void calcTrumpet() {
		if (MoveMode.stage != moveInfo.moveMode || false == pause) {
			return;
		}
		if (Input.GetButtonDown("Fire1")) {
			StageMain.getObjectFromPoint(out prevHit);
			mousePos = Input.mousePosition;
			if (null != cardTook) {
				// カードの下から探す
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				Physics.Raycast(cardTook.transform.position, ray.direction, out prevHit, Mathf.Infinity);
			}
		}
		if (null != cardTook) {
			// cardTookをマウス座標へ
			Vector3 p = Input.mousePosition;
			p.z = Camera.main.transform.position.y - tookCardDistance;
			cardTook.transform.position = Camera.main.ScreenToWorldPoint(p);
			if (null != prevHit.transform)
			if (Input.GetButton("Fire1")) {
				// 座標
				cardTook.transform.position = new Vector3(
					prevHit.transform.position.x, cubeSizeHalf, prevHit.transform.position.z
				);
				// 向き
				ternCardAngle(cardTook.GetComponent<StageChildData>().cardInfo);
			}
		}
		if (Input.GetButtonUp("Fire1")) {
			if (null == prevHit.transform) {
				// 何もない場所 : カード置く
				if (null != cardTook) {
					rollbackTookCard();
					Vector3 p = Input.mousePosition;
					p.z = Camera.main.transform.position.y;
					placeTookCard( Camera.main.ScreenToWorldPoint(p) );
				}
			} else if ("gate" == prevHit.transform.tag) {
				// gete : タイトルへ
				stageMain.loadTitle();
			} else if (this.transform == prevHit.transform) {
				// mainObject : 次の目的へ
				nextPlane();
			} else if (null != prevHit.transform && null != prevHit.transform.GetComponent<StageChildData>()) {
				if (null == cardTook) {
					// 持つ
					if ("card" == prevHit.transform.tag) {
						cardTook = prevHit.transform.gameObject;
						cardTook.GetComponent<Animator>().SetBool("isEmission", true);
					}
				} else {
					// (下ろしてから) 置く or 重ねる
					rollbackTookCard();
					ternCardAngle(cardTook.GetComponent<StageChildData>().cardInfo);
					placeCard(
						cardTook.GetComponent<StageChildData>().cardInfo,
						prevHit.transform.GetComponent<StageChildData>().point
					);
					placeTookCard(prevHit.transform.position);
				}
			}
		}
	}
	// カードを指定座標に置く
	void placeCard(CardInfo cardInfo, Vector2Int point) {
		Debug.Log("move card ["+ cardInfo.x +"x"+ cardInfo.y +"] -> ["+ point.x +"x"+ point.y +"]");
		cardInfo.x = point.x;
		cardInfo.y = point.y;
		cardTook.GetComponent<StageChildData>().point = point;
		cardTook.GetComponent<StageChildData>().prevPoint = new Times(point.x, point.y);
		stageMain.cardStorage.push(cardInfo, point.x, point.y);
	}
	void placeTookCard(Vector3 position) {
		if (null == cardTook) {
			return;
		}
		cardTook.transform.position = position;
		cardTook.GetComponent<Animator>().SetBool("isEmission", false);
		cardTook.transform.Translate(0, 0.02f, 0);
		cardTook = null;
	}
	void ternCardAngle(CardInfo ci) {
		if (cubeSizeHalf < Vector3.Distance(Input.mousePosition, mousePos)) {
			float angle = Mathf.Atan2(
				mousePos.y - Input.mousePosition.y,
				mousePos.x - Input.mousePosition.x
			);
			ci.directionFromAngle(angle);
		} else {
			ci.Direction = Direction.up;
		}
	}
	void rollbackTookCard() {
		Times times = cardTook.GetComponent<StageChildData>().prevPoint;
		if (null == times) {
			return;
		}
		// 置いていた場所から無へ
		Vector2Int prevPoint = times.to<Vector2Int>(
			(int[] ints) => {
				return new Vector2Int(ints[0], ints[1]);
			}
		);
		stageMain.cardStorage.pop(prevPoint.x, prevPoint.y);
		cardTook.GetComponent<StageChildData>().prevPoint = null;
		Debug.Log("rollback from ["+ prevPoint.x +"x"+ prevPoint.y +"]");
	}

	RigidbodyConstraints defaultConstraints = RigidbodyConstraints.FreezeAll & RigidbodyConstraints.FreezeAll ^ RigidbodyConstraints.FreezePositionY;
	public void newStage() {
		Camera.main.transform.position = defaultPos + cameraPos;
		toPoint.transform.position = spawnPoint.transform.position;
		transform.rotation = Quaternion.Euler(0, 0, 0);
		transform.position = spawnPoint.transform.position;
		moveInfo.MoveStatus = MoveStatus.stop;
		moveInfo.moveMode = MoveMode.stage;
		myInfo.Direction = stageMain.cardStorage.top(0, 0).Direction;
		myInfo.kind = stageMain.cardStorage.top(0, 0).kind;
		pause = true;
		myPos.x = 0;
		myPos.y = 0;
		cardTook = null;
		fromci = null;
		toci = null;
		Fallout = false;
	}

	// 次の何かまで先読み
	void nextPlane() {
		Vector2Int vi = myInfo.getDirectionTo();
		Debug.LogFormat("nextPlane {0}x{1} + {2}x{3}", myPos.x, myPos.y, vi.x, vi.y);
		while(true) {
			myPos += vi;
			toPoint.transform.position = spawnPoint.transform.position + new Vector3(myPos.y, 0, -myPos.x);
			
			CardInfo ci = stageMain.cardStorage.top(myPos.x, myPos.y);
			if (null == ci) {
				Debug.Log("\tnext fall out ["+ myPos.x +"x"+ myPos.y +"]");
				Fallout = true;
				pause = true;
				break;
			} else {
				if (ci.isMark() || ci.isGate()) {
					Debug.Log("\tnext "+ vi.x +"x"+ vi.y +":"+ ci.x +"x"+ ci.y +":"+ ci.Direction +":"+ ci.kind);
					break;
				}
			}
			if (MoveMode.card_effect == moveInfo.moveMode || 0 < stageMain.cardQueue.length()) {
				Debug.Log("\tcard effect mode");
				break;
			}
		}
	}

	// カードの効果を発揮
	bool effectCard(ref CardInfo cardInfo, int count) {
		Debug.LogFormat("effect card {0}x{1} {2} {3} {4}", cardInfo.x, cardInfo.y, cardInfo.Direction, cardInfo.kind, cardInfo.number);
		switch(cardInfo.Direction) {
		case Direction.cover:
			Debug.Log("\tis cover card.");
			cardInfo.takeCard(
				new Vector3(defaultSpawnPoint.transform.position.x - 4.5f, 0, transform.position.z)
			);
			stageMain.cardQueue.dequeue();
			pause = true;
			return false;
		}

		if (null != cardInfo.cardObject) {
			cardInfo.cardObject.tag = "coverCard";
			Destroy(cardInfo.cardObject);
		}
		switch(cardInfo.kind) {
		case Kind.blank:
		case Kind.start:
			Debug.Log("\tis blank");
			pause = false;
			break;
		case Kind.exit:
			// クリア
			Debug.Log("\tis gate");
			stageMain.clearStage();
			pause = true;
			return false;
		case Kind.spade: {
				myInfo.Direction = cardInfo.Direction;
				Vector2Int vi = myInfo.getDirectionTo();
				switch(cardInfo.number) {
				case 1: case 2: case 3: case 4:
					if (0 == count) {
						playTrumpetPos(new Vector2Int(myPos.x + vi.x, myPos.y + vi.y - 2), waitTime: 1.5f);
					}
					newBlankCardPlane(myPos.x + vi.x * (count+1), myPos.y + vi.y * (count+1));
					if (count+1 == cardInfo.number) {
						cardInfo = null;
					}
					break;
				}
			}
			break;
		case Kind.club: {
				Vector2Int vi = cardInfo.getDirectionTo();
				switch(cardInfo.number) {
				case 1: case 2:
					if (0 == count) {
						playTrumpetPos(new Vector2Int(myPos.x + vi.x - 2, myPos.y + vi.y - 2));
						playTrumpetPos(new Vector2Int(myPos.x + vi.x + 2, myPos.y + vi.y - 2));
						newBlankCardPlane(myPos.x + vi.x, myPos.y + vi.y);
						newBlankCardPlane(myPos.x + vi.x*2, myPos.y + vi.y*2);
						newBlankCardPlane(myPos.x + vi.x - vi.y, myPos.y + vi.y + vi.x);
						newBlankCardPlane(myPos.x + vi.x + vi.y, myPos.y + vi.y - vi.x);
					} else if (1 == count) {
						newBlankCardPlane(myPos.x + vi.x*3, myPos.y + vi.y*3);
						newBlankCardPlane(myPos.x + vi.x - vi.y, myPos.y + vi.y*2 + vi.x);
						newBlankCardPlane(myPos.x + vi.x + vi.y, myPos.y + vi.y*2 - vi.x);
						newBlankCardPlane(myPos.x + vi.x - vi.y*2, myPos.y + vi.y + vi.x);
						newBlankCardPlane(myPos.x + vi.x + vi.y*2, myPos.y + vi.y - vi.x);
					}
					break;
				}
			}
			break;
		case Kind.heart:
			moveInfo.moveMode = MoveMode.card_effect;
			break;
		}
		return true;
	}
	void newBlankCardPlane(int x, int y) {
		if (null != stageMain.cardStorage.top(x, y)) {
			// 追加する座標に既に何かあると追加しない
			return;
		}

		switch(moveInfo.moveMode) {
		case MoveMode.free:
			GameObject newPlane = (GameObject)Instantiate(plane);
			newPlane.transform.rotation = Quaternion.Euler(0, 0, 0);
			newPlane.transform.position = transform.position + (movePoint - transform.position) * 2 + new Vector3(0, cubeSizeHalf, 0);
			newPlane.transform.position = new Vector3(newPlane.transform.position.x, 0, newPlane.transform.position.z);
			break;
		default:
			CardInfo newCardInfo = new CardInfo();
			newCardInfo.x = x;
			newCardInfo.y = y;
			stageMain.stageMapData.createMapPlane(newCardInfo);
			break;
			
		}
	}
	
	void movedToPlane() {
		CardInfo ci = null;
		switch(moveInfo.moveMode) {
		case MoveMode.free:
			return;
		case MoveMode.stage:
		case MoveMode.card_effect:
			Debug.Log("moveToPlane mode : "+ moveInfo.moveMode);
			moveInfo.moveMode = MoveMode.stage;
			stageMain.cardStorage.arrayReverseMap((CardInfo cardInfo) => {
				Debug.Log("\tenqueue card "+ cardInfo.x +"x"+ cardInfo.y +":"+ cardInfo.Direction +":"+ cardInfo.kind);
				stageMain.cardQueue.enqueue(cardInfo);
				return true;
			}, myPos.x, myPos.y);
			stageMain.cardStorage.clear(myPos.x, myPos.y);
			break;
		}

		ci = stageMain.cardQueue.dequeue();
		if (null == ci) {
			Debug.Log("card is null");
			return;
		}

		// 表だと効果を発揮 キューがなくなるか、効果による中断、停止、キューの読み込み停止
		Debug.Log("dequeues ["+ stageMain.cardQueue.length() +"]");
		bool loopContinue = true;
		while(null != ci) {
			for (int count = 0; count < ci.number; count++) {
				loopContinue = effectCard(ref ci, count);
				if (null == ci) break;
			}
			if (null == ci || false == loopContinue || MoveMode.stage != moveInfo.moveMode) {
				Debug.Log("\tstop dequeue");
				break;
			}
			ci = stageMain.cardQueue.dequeue();
		}

		if (loopContinue) {
			// 何かのカードによって取得が止まった
			// :Direction.cover
			nextPlane();
		}
	}

	void respawnPlane() {
		stageMain.newStage();
	}

	// 指定された地点への移動の計算
	Vector3 movePoint, rotateAxis;
	float falloutY = -50f;
	void calcMove() {
		switch(moveInfo.moveMode) {
		case MoveMode.card_effect:
			slideMove();
			break;
		case MoveMode.free:
		case MoveMode.stage:
			rotateMove();
			break;
		}
	}
	// ずれて移動
	const float explosionforce = 200f;
	void slideMove() {
		float distance = Vector3.Distance(toPoint.transform.position, transform.position);
		float xDistance = toPoint.transform.position.x - transform.position.x,
			zDistance = toPoint.transform.position.z - transform.position.z;
		switch(moveInfo.MoveStatus) {
		case MoveStatus.stop:
			if (cubeSizeHalf*2 <= distance) {
				if (true == fallout) {
					moveInfo.moveMode = MoveMode.stage;
					break;
				}
				moveInfo.MoveStatus = MoveStatus.check_to_point;
			}
			break;
			
		case MoveStatus.check_to_point:
			if (cubeSizeHalf < Mathf.Abs(xDistance)) {
				transform.GetComponent<Animator>().SetBool("isMove_x", xDistance < 0);
				transform.GetComponent<Animator>().SetBool("isMove_X", 0 < xDistance);
				transform.GetComponent<Rigidbody>().constraints &= transform.GetComponent<Rigidbody>().constraints ^ RigidbodyConstraints.FreezePositionX;
			} else if (cubeSizeHalf < Mathf.Abs(zDistance)) {
				transform.GetComponent<Animator>().SetBool("isMove_z", zDistance < 0);
				transform.GetComponent<Animator>().SetBool("isMove_Z", 0 < zDistance);
				transform.GetComponent<Rigidbody>().constraints &= transform.GetComponent<Rigidbody>().constraints ^ RigidbodyConstraints.FreezePositionZ;
			} else {
				moveInfo.MoveStatus = MoveStatus.stop;
				roundPosition(transform);
				movedToPlane();
				break;
			}
			roundPosition(transform);
			moveInfo.MoveStatus = MoveStatus.moving;
			break;

		case MoveStatus.moving:
			break;
		}
	}
	// 回転して移動
	const float cubeAngleSpeed = 15f, cubeAngleMax = 90f;
	float cubeSumAngle = 0f;
	void rotateMove() {
		if (true == fallout) {
			// 落下
			Vector2Int vi = myInfo.getDirectionTo();
			transform.GetComponent<Rigidbody>().AddForceAtPosition(
				new Vector3(vi.y, -1, -vi.x),
				transform.position + new Vector3(0, cubeSizeHalf*50, 0)
			);
			// 底判定
			if (MoveMode.free != moveInfo.moveMode && transform.position.y < falloutY) {
				respawnPlane();
			}
			return;
		}

		switch(moveInfo.MoveStatus) {
		case MoveStatus.stop:
			float distance = Vector3.Distance(toPoint.transform.position, transform.position);
			if (cubeSizeHalf*2 <= distance) {
				moveInfo.MoveStatus = MoveStatus.check_to_point;
			}
			break;
			
		case MoveStatus.check_to_point:
			float sign,
				xDistance = toPoint.transform.position.x - transform.position.x,
				zDistance = toPoint.transform.position.z - transform.position.z;
			
			if (cubeSizeHalf < Mathf.Abs(xDistance)) {
				sign = (xDistance < 0) ? -1 : 1;
				movePoint = transform.position + new Vector3(cubeSizeHalf * sign, -cubeSizeHalf, 0f);
				rotateAxis = new Vector3(0, 0, -sign);
			} else if (cubeSizeHalf < Mathf.Abs(zDistance)) {
				sign = (zDistance < 0) ? -1 : 1;
				movePoint = transform.position + new Vector3(0f, -cubeSizeHalf, cubeSizeHalf * sign);
				rotateAxis = new Vector3(sign, 0, 0);
			} else {
				moveInfo.MoveStatus = MoveStatus.stop;
				movedToPlane();
				roundPosition(transform);
				break;
			}
			if (MoveMode.free == moveInfo.moveMode) {
				// freeモードなら動くと足場を自動で設置
				Vector3 p = defaultPos - transform.position;
				newBlankCardPlane((int)p.z, -(int)p.x);
			}
			moveInfo.MoveStatus = MoveStatus.moving;
			cubeSumAngle = 0f;
			break;

		case MoveStatus.moving:
			if (cubeSumAngle < cubeAngleMax) {
				cubeSumAngle += cubeAngleSpeed;
				transform.RotateAround(movePoint, rotateAxis, cubeAngleSpeed);
			} else {
				moveInfo.MoveStatus = MoveStatus.check_to_point;
				roundPosition(transform);
			}
			break;
		}
	}
	// 移動イベントを拾う
	Vector3 pitchBasePoint = new Vector3(-1, 0, 9);
	void changeMoveStatus(MoveStatus toms) {
		switch(toms) {
		case MoveStatus.moving:
			Vector3 pos;
			if (MoveMode.free == moveInfo.moveMode) {
				pos = toPoint.transform.position - stageBaseParent.transform.position - pitchBasePoint;
			} else {
				pos = new Vector3(myPos.y - 2, 0, -myPos.x);
			}
			playTrumpetPos(new Vector2Int(-(int)pos.z, (int)pos.x));
			break;
		}
	}
	// 座標に応じて音を鳴らす
	const float playTime = 1f / 8f;
	void playTrumpetPos(Vector2Int pt, float time = 1.0f, float waitTime = 0.0f) {
		audioManager.play(this, playTime * time, playTime * waitTime, se_trumpet, (AudioSource audioSource) => {
			pt.y = (int)Mathf.Repeat(pt.y + 2, 6) - 2;
			audioSource.pitch = 1f;
			audioSource.pitch *= Mathf.Pow(2, 1f / 12f * pt.x);
			if (-3 <= pt.y && pt.y < 0) {
				audioSource.pitch *= 1 / Mathf.Pow(2, Mathf.Abs(pt.y));
			} else {
				audioSource.pitch *= Mathf.Pow(2, Mathf.Abs(pt.y));
			}
		});
		
		Debug.LogFormat("play {0}:{1}", pt.x, pt.y);
	}
	// 誤差を丸める
	public void roundPosition(Transform target) {
		int cx = (int)target.position.x, cz = (int)target.position.z;
		float xSign = 0.5f * (cx < 0 ? -1f : 1f), zSign = 0.5f * (cz < 0 ? -1f : 1f);
		cx = (int)Mathf.Round(target.position.x - xSign);
		cz = (int)Mathf.Round(target.position.z - zSign);
		target.position = new Vector3(cx + xSign, cubeSizeHalf, cz + zSign);
		target.rotation = Quaternion.Euler(0, 0, 0);
	}
}
