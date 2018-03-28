using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

public enum StageStatus {
	top = -1, stage_helloWorld = 0, stage_variableDirection, stage_newType, stage_elementClass,
	stage_user
}
public class StageInfo {
	public Action changingStatusCallBack;
	public Action changedStatusCallBack;
	public int status = (int)StageStatus.top;
	public int toStatus = (int)StageStatus.top;
	public int maxStatus = (int)StageStatus.top;
	public int level = 0;
	public bool sceneLoading = false;

	public GameObject fadePanel;
	const float fadeSpeed = 0.05f;
	float fade;
	Color fadeIn = new Color(1, 1, 1, 0);
	Color fadeOut = new Color(1, 1, 1, 1);
	Color fadeStatus = Color.clear;

	public bool isStaging() {
		return status != toStatus;
	}
	public void toStage(int stageNum) {
		if ((int)maxStatus < stageNum) {
			Debug.Log("notthing stage "+ stageNum);
			return;
		}
		if (stageNum == (int)status) {
			return;
		}
		toStatus = stageNum;
		fade = 0f;
		fadeStatus = fadeOut;
		sceneLoading = false;
	}

	public void calc() {
		if (!sceneLoading && isStaging()) {
			fade += fadeSpeed * Time.deltaTime;
			fadePanel.GetComponent<Image>().color = Color.Lerp(
				fadePanel.GetComponent<Image>().color, fadeStatus, fade);
			if (fadeStatus == fadePanel.GetComponent<Image>().color) {
				if (fadeIn == fadeStatus) {
					status = toStatus;
				} else {
					changingStatusCallBack();
					fadeStatus = fadeIn;
					fade = 0f;
				}
			}
		}
	}
}
public class SaveData {
	public int maxStatus = 0;
	public int userData = 0;
}

[Serializable]
public class CardInfo {
	[NonSerialized]
	string[] CardKind = {"♤", "♧", "♡", "♢", "□", "○", ""};

	[NonSerialized]
	string[] CardDirection = {"←", "↑", "→", "↓", "□", "", "?"};

	[NonSerialized]
	string[] CardNumberText = {"", "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K"};

	[SerializeField]
	[HideInInspector]
	public int x;
	public int y;
	public string k;
	public string d;
	public int n;

	[NonSerialized]
	public Kind kind = Kind.blank;
	[NonSerialized]
	private Direction direction = Direction.throught;
	public Direction Direction {
		get {
			return direction;
		}
		set {
			direction = value;
			if (null != cardObject) {
				switch(direction) {
				case Direction.up:
					cardObject.transform.rotation = Quaternion.Euler(0, 0, 0);
					break;
				case Direction.right:
					cardObject.transform.rotation = Quaternion.Euler(0, 90, 0);
					break;
				case Direction.down:
					cardObject.transform.rotation = Quaternion.Euler(0, 180, 0);
					break;
				case Direction.left:
					cardObject.transform.rotation = Quaternion.Euler(0, 270, 0);
					break;
				}
			}
		}
	}
	[NonSerialized]
	public int number = 1;
	[NonSerialized]
	public int index = -1;
	[NonSerialized]
	public GameObject cardObject;

	public static bool operator ==(CardInfo a, CardInfo b){
		// 同一のインスタンス true
		if (System.Object.ReferenceEquals(a, b))
		{
			return true;
		}
		if (null == (object)a || null == (object)b) {
			return false;
		}
		// 向きの一致は見ない
		if (a.x == b.x && a.y == b.y && a.kind == b.kind && a.number == b.number) {
			return true;
		}
		return false;
	}
	public static bool operator !=(CardInfo a, CardInfo b) {
		return !(a == b);
	}

	public void deserialize() {
		kind = (Kind)Array.IndexOf(CardKind, k);
		direction = (Direction)Array.IndexOf(CardDirection, d);
		number = n < 1 ? 1 : n;
		index = 0;
		if (-1 == (int)kind) kind = Kind.blank;
		if (-1 == (int)direction) {
			if (Kind.blank != kind) {
				if (Kind.exit != kind && Kind.start != kind) {
					direction = Direction.cover;
				} else {
					direction = Direction.stop;
				}
			} else {
				direction = Direction.throught;
			}
		}
	}
	public void serialize() {
		k = CardKind[(int)kind];
		d = CardDirection[(int)direction];
		n = number;
	}

	public bool isMark() {
		return Kind.spade == kind || Kind.club == kind || Kind.heart == kind || Kind.dia == kind;
	}
	public bool isGate() {
		return Kind.start == kind || Kind.exit == kind;
	}
	public void toBlank() {
		kind = Kind.blank;
		direction = Direction.throught;
		cardObject = null;
		index = 0;
	}
	public void open() {
		if (Direction.cover == direction) {
			direction = Direction.throught;
		}
	}
	public void takeCard(Vector3 matchPosition) {
		Move m = cardObject.GetComponent<Animator>().GetBehaviour<Move>();
		m.parentCard = this;
		m.positionWeight = new Vector3(1, 0, 1);
		m.rotationWeight = 1;
		m.matchPosition = matchPosition;
		m.matchRotation.x = cardObject.transform.rotation.x;
		m.matchRotation.z = cardObject.transform.rotation.z;
		m.matchRotation.w = cardObject.transform.rotation.w;
		m.onStateExit = (Animator anim, AnimatorStateInfo si, int li) => {
			CardInfo parent = anim.GetBehaviour<Move>().parentCard;
			if (anim.GetBool("isRotating")) {
				if (Direction.cover == parent.Direction) {
					// 裏
					anim.SetBool("isOpening", true);
				}
			}
			anim.SetBool("isRotating", false);
			parent.open();
		};
		Open o = cardObject.GetComponent<Animator>().GetBehaviour<Open>();
		o.onStateExit = (Animator anim, AnimatorStateInfo si, int li) => {
			anim.SetBool("isOpening", false);
			anim.transform.rotation = Quaternion.Euler(0, 0, 0);
			anim.transform.tag = "card";
		};

		cardObject.GetComponent<Animator>().SetBool("isRotating", true);
		Debug.Log("\tis rotating");
	}	
	public void copyInfo(CardInfo from) {
		cardObject = from.cardObject;
		kind = from.kind;
		Direction = from.direction;
		index = from.index;
		number = from.number;
	}
	public void swapInfo(CardInfo from) {
		CardInfo ci = new CardInfo();
		ci.copyInfo(from);
		from.copyInfo(this);
		this.copyInfo(ci);
	}
	public void directionFromAngle(float angle) {
		int a = (int)(Mathf.Rad2Deg * angle) + 90;
		a = (a + 45) % 360;
		if (0 <= a && a < 90) {
			Direction = Direction.up;
		} else if (90 <= a && a < 180) {
			Direction = Direction.left;
		} else if (180 <= a && a < 270) {
			Direction = Direction.down;
		} else {
			Direction = Direction.right;
		}
	}
	public string getNumberText() {
		return CardNumberText[number];
	}
	public Vector2Int getDirectionTo() {
		Vector2Int vi = new Vector2Int(0, 0);
		switch(direction) {
		case Direction.left: vi.x = -1; break;
		case Direction.up: vi.y = 1; break;
		case Direction.right: vi.x = 1; break;
		case Direction.down: vi.y = -1; break;
		}
		return vi;
	}
}

[Serializable]
public class StageMapData {
	[SerializeField]
	public CardInfo[] map;
	public string name;
	public bool clear;

	[NonSerialized] public int width = 0;
	[NonSerialized] public int height = 0;
	[NonSerialized] public Dictionary<int, Dictionary<int, CardInfo[]>> pointToCard;
	[NonSerialized] public Dictionary<int, Dictionary<int, int>> pointCardCount;

	public static StageMapData CreateFromJson(string json) {
		StageMapData stageMapData = null;
		try {
			stageMapData = JsonUtility.FromJson<StageMapData>(json);
			stageMapData.pointToCard = new Dictionary<int, Dictionary<int, CardInfo[]>>();
			stageMapData.pointCardCount = new Dictionary<int, Dictionary<int, int>>();
			foreach(CardInfo ci in stageMapData.map) {
				ci.deserialize();
				stageMapData.createMap(ci);
			}
		} catch(Exception e) {
			Debug.Log(e.Message);
		}
		return stageMapData;
	}

	public void createMap(CardInfo newCard) {
		if (height <= newCard.y) {
			height = newCard.y+1;
		}
		if (width <= newCard.x){
			width = newCard.x+1;
		}
	}

	[NonSerialized]
	public Action<CardInfo> createMapPlane;

	public string toJson() {
		foreach(CardInfo ci in map) {
			ci.serialize();
		}
		return JsonUtility.ToJson(this);
	}
}

public class Times : IEquatable<Times> {
	int[] ints;

	public Times(params int[] ints){
		this.ints = new int[ints.Length];
		ints.CopyTo(this.ints, 0);
	}
	public int[] get() {
		return ints;
	}
	// Typeへ変換する
	public Type to<Type>(Func<int[], Type> convertFunc) {
		return convertFunc(ints);
	}
	// Typeから変換して格納
	public void from(Func<int[]> convertFunc) {
		ints = convertFunc();
	}

	public static bool operator ==(Times a, Times b){
		// 同一のインスタンス true
		if (System.Object.ReferenceEquals(a, b))
		{
			return true;
		}
		if (null == (object)a || null == (object)b) {
			return false;
		}

		// 配列の長さが違っても,原点を表す数値ならないものとして扱う
		int min = Math.Min(a.ints.Length, b.ints.Length);
		for (int i = 0; i < min; i++) {
			if (a.ints[i] != b.ints[i]) {
				return false;
			}
		}
		if (a.ints.Length != b.ints.Length) {
			Times max = a.ints.Length < b.ints.Length ? b : a;
			for (int i = min; i < max.ints.Length; i++) {
				if (0 != max.ints[i]) {
					return false;
				}
			}
		}
		return true;
	}
	public static bool operator !=(Times a, Times b) {
		return !(a == b);
	}

	public bool Equals(Times times) {
		return (this == times);
	}
	public override bool Equals(object obj) {
		return this.Equals(obj);
	}

	public override string ToString() {
		string s = "";
		for(int i=0; i<this.ints.Length; i++) {
			s += ""+ this.ints[i] +",";
		}
		return s;
	}

	public override int GetHashCode() {
		return this.ToString().GetHashCode();
	}
}
// 実態いれるやつ (無駄にX次元対応、4次元以上に置くことはあるのか？ｗｗｗ)
// Tree使え← : ランダムアクセス:O(1|N) : シーケンシャルアクセス:昇順:O(N) 降順:O(2N)
public class CardContinaer {
	Dictionary<Times, CardInfo> timesToCard;

	public CardContinaer() {
		timesToCard = new Dictionary<Times, CardInfo>();
	}

	int[] makeTop(int[] ints) {
		int[] newInts = new int[ints.Length + 1];
		ints.CopyTo(newInts, 0);
		int n = length(ints) - 1;
		newInts[ints.Length] = n < 0 ? 0 : n;
		return newInts;
	}

	// public void set(CardInfo cardInfo, params int[] ints) {
	// 	timesToCard[new Times(ints)] = cardInfo;
	// }
	public CardInfo get(params int[] ints) {
		return timesToCard[new Times(ints)];
	}
	public bool exist(params int[] ints) {
		return timesToCard.ContainsKey(new Times(ints));
	}

	// 一つ下の階層につけ加える
	public int push(CardInfo cardInfo, params int[] ints) {
		int[] newInts = new int[ints.Length + 1];
		ints.CopyTo(newInts, 0);

		for(newInts[ints.Length] = 0;; newInts[ints.Length]++) {
			Times newTimes = new Times(newInts);
			if (false == timesToCard.ContainsKey(newTimes)) {
				timesToCard[newTimes] = cardInfo;
				return newInts[ints.Length];
			}
		}
	}
	public CardInfo pop(params int[] ints) {
		int[] newInts = makeTop(ints);
		Times newTimes = new Times(newInts);
		if (false == timesToCard.ContainsKey(newTimes)) {
			return null;
		}
		CardInfo ci = timesToCard[newTimes];
		timesToCard.Remove(newTimes);
		return ci;
	}
	public CardInfo top(params int[] ints) {
		int[] newInts = makeTop(ints);
		Times newTimes = new Times(newInts);
		if (false == timesToCard.ContainsKey(newTimes)) {
			return null;
		}
		return timesToCard[newTimes];
	}

	public int enqueue(CardInfo cardInfo, params int[] ints) {
		return push(cardInfo, ints);
	}
	public CardInfo dequeue(params int[] ints) {
		int[] newInts = new int[ints.Length + 1];
		ints.CopyTo(newInts, 0);
		newInts[ints.Length] = 0;
		Times newTimes = new Times(newInts), prevTimes;
		if (false == timesToCard.ContainsKey(newTimes)) {
			return null;
		}
		CardInfo cardInfo = timesToCard[newTimes];
		while(true) {
			prevTimes = new Times(newInts);
			newInts[ints.Length]++;
			newTimes = new Times(newInts);
			if ( false == timesToCard.ContainsKey(newTimes) ) {
				timesToCard.Remove(prevTimes);
				break;
			}
			timesToCard[prevTimes] = timesToCard[newTimes];
		}
		return cardInfo;
	}

	public int length(params int[] ints) {
		int[] newInts = new int[ints.Length + 1];
		ints.CopyTo(newInts, 0);
		
		for (newInts[ints.Length] = 0;; newInts[ints.Length]++) {
			Times newTimes = new Times(newInts);
			if (false == timesToCard.ContainsKey(newTimes)) {
				return newInts[ints.Length];
			}
		}
	}

	public void clear(params int[] ints) {
		int[] newInts = new int[ints.Length + 1];
		ints.CopyTo(newInts, 0);
		
		while(true) {
			newInts[ints.Length]++;
			Times newTimes = new Times(newInts);
			if (false == timesToCard.ContainsKey(newTimes)) {
				break;
			}
			timesToCard.Remove(newTimes);
		}
	}

	// 昇順参照
	public void arrayMap(Func<CardInfo, bool> func, params int[] ints) {
		int[] newInts = new int[ints.Length + 1];
		ints.CopyTo(newInts, 0);
		
		while(true) {
			newInts[ints.Length]++;
			Times newTimes = new Times(newInts);
			if (false == timesToCard.ContainsKey(newTimes)) {
				break;
			}
			// 繰り返しの続行可否
			if (false == func(timesToCard[newTimes])) break;
		}
	}

	// 降順参照
	public void arrayReverseMap(Func<CardInfo, bool> func, params int[] ints) {
		int[] newInts = new int[ints.Length + 1];
		ints.CopyTo(newInts, 0);
		
		for(newInts[ints.Length] = length(ints) - 1; 0 <= newInts[ints.Length]; newInts[ints.Length]--) {
			Times newTimes = new Times(newInts);
			if (timesToCard.ContainsKey(newTimes)) {
				// 繰り返しの続行可否
				if (false == func(timesToCard[newTimes])) break;
			}
		}
	}
}

public class StageMain : MonoBehaviour {

	public GameObject mainObject;
	public GameObject mainCamera;
	public GameObject mainLight;
	public GameObject mainCanvas;
	public GameObject stageBase;
	public GameObject fadePanel;
	public GameObject warpPoint;
	public GameObject spawnPoint;
	public GameObject toPoint;
	public GameObject defaultSpawnPoint;
	public GameObject topSpawnPoint;

	public string[] stageMapDataText;
	public GameObject stagePlane;
	public GameObject cardObject;
	public GameObject[] cardTypeList;
	public StageMapData stageMapData;
	public StageInfo stageInfo;

	public CardContinaer cardStorage = new CardContinaer();	// 何次元入るかわからんけど将来性(笑)のために
	public CardContinaer cardQueue = new CardContinaer();	// 1次元のみを保持してキューとして使う

	DataManager<SaveData> dm;
	string titleText = "Ｔｒｕｍｐｅｔ", filePath;
	Vector3 diffWarpPoint;
	SaveData sd;

	void Awake() {
		DontDestroyOnLoad(this);
		DontDestroyOnLoad(mainObject);
		DontDestroyOnLoad(mainCamera);
		DontDestroyOnLoad(mainLight);
		DontDestroyOnLoad(mainCanvas);
		DontDestroyOnLoad(warpPoint);
		DontDestroyOnLoad(spawnPoint);
		DontDestroyOnLoad(toPoint);
		DontDestroyOnLoad(defaultSpawnPoint);
		DontDestroyOnLoad(topSpawnPoint);

		dm = new DataManager<SaveData>("savedata");

		stageInfo = new StageInfo();
		stageInfo.fadePanel = fadePanel;
		SceneManager.sceneLoaded += (Scene scene, LoadSceneMode sceneMode) => {
			if (!stageInfo.sceneLoading) {
				return;
			}
			stageInfo.sceneLoading = false;
			stageInfo.changedStatusCallBack();
		};

		stageInfo.changingStatusCallBack = () => {
			Debug.Log("Loading Stage...");
			transform.DetachChildren();
			foreach(GameObject child in GameObject.FindGameObjectsWithTag("stageChild")){
				Destroy(child);
			}
			if ((int)StageStatus.top == stageInfo.status) {
				SceneManager.UnloadScene("Title");
			}
			SceneManager.LoadScene("Stage");
			stageInfo.sceneLoading = true;

			stageMapData = StageMapData.CreateFromJson(stageMapDataText[stageInfo.toStatus]);
			cardStorage = new CardContinaer();
			cardQueue = new CardContinaer();
			Debug.Log("StageInfo: "+ stageMapData.width +"x"+ stageMapData.height);

			// ステージとカードを生成するときに呼ばれる/ぶやつ
			stageMapData.createMapPlane = (CardInfo ci) => {
				int index = cardStorage.push(ci, ci.x, ci.y);

				GameObject newStagePlane;
				newStagePlane = (GameObject)Instantiate(stagePlane);
				newStagePlane.transform.position = new Vector3(
					spawnPoint.transform.position.x + ci.y,
					0,
					spawnPoint.transform.position.z - ci.x
				);
				newStagePlane.gameObject.transform.GetComponent<StageChildData>().point.x = ci.x;
				newStagePlane.gameObject.transform.GetComponent<StageChildData>().point.y = ci.y;
				switch(ci.kind) {
				case Kind.spade: case Kind.club: case Kind.heart: case Kind.dia:
					GameObject newCard = Instantiate(cardObject);
					newCard.transform.position = newStagePlane.transform.position + new Vector3(0, 0.02f * index, 0);
					newCard.transform.GetComponent<StageChildData>().point.x = ci.x;
					newCard.transform.GetComponent<StageChildData>().point.y = ci.y;
					newCard.transform.GetComponent<StageChildData>().cardInfo = ci;
					newCard.transform.GetComponent<StageChildData>().prevPoint = null;
					GameObject newCardType = Instantiate(cardTypeList[(int)ci.kind]);
					newCardType.transform.SetParent(newCard.transform, false);
					newCard.transform.Find("Canvas").transform.Find("NumberText")
						.GetComponent<Text>().text = ci.getNumberText();
					ci.cardObject = newCard;
					ci.Direction = ci.Direction;
					
					switch(ci.Direction) {
					case Direction.cover:
						newCard.transform.rotation = Quaternion.Euler(0, 180, 180);
						break;
					case Direction.throught:
						newCard.transform.rotation = Quaternion.Euler(0, 180, 180);
						ci.takeCard(
							new Vector3(defaultSpawnPoint.transform.position.x - 4.5f, 0, -ci.x)
						);
						ci.Direction = Direction.cover;
						break;
					default:
						newCard.transform.GetComponent<Animator>().SetBool("isOpened", true);
						break;
					}
					break;
				case Kind.start: case Kind.exit:
					GameObject newGate = Instantiate(cardTypeList[(int)ci.kind]);
					newGate.transform.position = newStagePlane.transform.position + new Vector3(0, 0.01f, 0);
					ci.cardObject = newGate;
					break;
				}
			};			
		};
		stageInfo.changedStatusCallBack = () => {
			spawnPoint.transform.position = new Vector3(
				defaultSpawnPoint.transform.position.x - stageMapData.height/2,
				defaultSpawnPoint.transform.position.y,
				defaultSpawnPoint.transform.position.z + stageMapData.width/2
			);
			foreach(CardInfo ci in stageMapData.map) {
				stageMapData.createMapPlane(ci);
			}
			
			warpPoint.transform.position = spawnPoint.transform.position;
			mainObject.GetComponent<Main>().newStage();
		};
	}

	void Start () {
		loadData();
		loadTitle();
		diffWarpPoint = spawnPoint.transform.position - mainObject.transform.position;
	}
	
	void Update () {
		if (!stageInfo.isStaging() && Input.GetButton("Fire1")) {
			// タイトルのステージ選択
			RaycastHit hit;
			getObjectFromPoint(out hit);
			if (null != hit.collider) {
				TextMesh tm = hit.collider.GetComponent<TextMesh>();
				if (null != tm) {
					StageChildData scd = hit.collider.GetComponent<StageChildData>();
					if (null != scd) {
						if (Input.GetButtonDown("Fire1")) {
							int stage = scd.point.x;
							// ステージ変更
							stageInfo.toStage(stage);
							toPoint.transform.position = hit.transform.parent.transform.position + diffWarpPoint * 2;
							warpPoint.transform.position = toPoint.transform.position;
						} else {
							toPoint.transform.position = hit.point;
						}
					} else if (titleText == tm.text) {
						toPoint.transform.position = hit.point;
					}
					mainObject.GetComponent<Main>().roundPosition(toPoint.transform);
				}
			}
		}

		stageInfo.calc();
	}

	public void newStage() {
		SceneManager.UnloadScene("Stage");
		stageInfo.changingStatusCallBack();
	}

	public void clearStage() {
		int stageNumber = stageInfo.status;
		saveStageData(stageNumber);
		loadTitle();
		toPoint.transform.position = this.transform.GetChild(stageNumber).Find("Plane").position;
		mainObject.transform.position = toPoint.transform.position;
		mainObject.GetComponent<Main>().roundPosition(mainObject.transform);
	}
	public void loadTitle() {
		cardStorage = new CardContinaer();
		stageInfo.status = (int)StageStatus.top;
		stageInfo.toStatus = (int)StageStatus.top;
		stageInfo.level = 0;
		SceneManager.LoadScene("Title");
		for(int i=0; i<=(int)stageInfo.maxStatus; i++) {
			addStageBase();
		}
		mainObject.GetComponent<Main>().moveInfo.moveMode = MoveMode.free;
		spawnPoint.transform.position =	topSpawnPoint.transform.position;
		warpPoint.transform.position =	topSpawnPoint.transform.position;
	}

	public void loadData() {
		// システムデータ
		if (false == dm.exist()) {
			sd = new SaveData();
			dm.save(sd);
		}
		sd = dm.load();
		stageInfo.maxStatus = sd.maxStatus;//(int)StageStatus.stage_elementClass;

		// ステージデータ
		for (int i = (int)StageStatus.stage_user + sd.userData; 0 <= i; i--){
			DataManager<StageMapData> dmsm = new DataManager<StageMapData>("stage_"+ i);
			if (dmsm.exist()) {
				stageMapDataText[i] = dmsm.loadBinary();
			}
		}
	}
	public void saveStageData(int stage) {
		if (sd.maxStatus == stage) {
			if (stage < (int)StageStatus.stage_user + sd.userData - 1) {
				sd.maxStatus++;
				stageInfo.maxStatus++;
			}
			dm.save(sd);
		}
		stageMapData = StageMapData.CreateFromJson(stageMapDataText[stage]);
		stageMapData.clear = true;
		stageMapDataText[stage] = stageMapData.toJson();
		DataManager<StageMapData> dmsm = new DataManager<StageMapData>("stage_"+ stage);
		dmsm.saveBinary(stageMapDataText[stage]);
	}

	void addStageBase() {
		StageMapData smd = StageMapData.CreateFromJson(stageMapDataText[stageInfo.level]);
		GameObject newStageBase = (GameObject)Instantiate(stageBase);
		Transform t = newStageBase.transform.Find("Text");
		t.GetComponent<TextMesh>().text = smd.name;
		newStageBase.transform.SetParent(this.transform, false);
		newStageBase.transform.Translate(new Vector3(-1 * stageInfo.level, 0, 0));
		t.GetComponent<Collider>().transform.position += new Vector3(0, 0.1f, 0);
		t.GetComponent<StageChildData>().point = new Vector2Int(stageInfo.level, 0);
		if (smd.clear) {
			GameObject newGate = Instantiate(cardTypeList[(int)Kind.exit]);
			newGate.transform.SetParent(newStageBase.transform, false);
			newGate.transform.position = newStageBase.transform.Find("Plane").position + new Vector3(0, 0.01f, 0);
		}

		stageInfo.level++;
	}

	public static bool getObjectFromPoint(out RaycastHit rh) {
		rh = new RaycastHit();
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		return Physics.Raycast(ray.origin, ray.direction, out rh, Mathf.Infinity);
	}
}
