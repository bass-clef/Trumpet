using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSourceWrapper {
	public AudioSource content = new AudioSource();
	bool isPlaying;
	public bool IsPlaying {
		get { return isPlaying | content.isPlaying; }
		set { isPlaying = value; }
	}
	public int fadeOutFrameMax = 6;

	public IEnumerator delayPlayOneShot(float time, float waitTime) {
		isPlaying = true;
		yield return new WaitForSecondsRealtime(waitTime);
		play();
		yield return new WaitForSecondsRealtime(time);
		yield return stop();
	}

	public void play() {
		isPlaying = true;
		if (0 == content.volume) content.volume = 1.0f;
		content.Play();
	}
	public IEnumerator stop() {
		return fadeOut();
	}
	public IEnumerator fadeOut() {
		int fadeOutFrame = fadeOutFrameMax;
		while(0 < fadeOutFrame) {
			fadeOutFrame--;
			content.volume = fadeOutFrame / fadeOutFrameMax;
			yield return null;
		}
		content.Stop();
		isPlaying = false;
	}
}

public class AudioSourceManager {
	AudioSourceWrapper[] audioSourceList;

	public AudioSourceManager(int length, System.Action<AudioSourceWrapper> func) {
		System.Array.Resize(ref audioSourceList, 10);
		for (int i = 0; i < length; i++) {
			audioSourceList[i] = new AudioSourceWrapper();
			func(audioSourceList[i]);
		}
	}

	AudioSourceWrapper allocate() {
		int index = System.Array.FindIndex(audioSourceList,
			(AudioSourceWrapper audioSource) => {
				return false == audioSource.IsPlaying;
			}
		);
		return audioSourceList[index];
	}

	public void play(MonoBehaviour mono, float time, float waitTime, AudioClip audioClip, System.Action<AudioSource> func) {
		AudioSourceWrapper blank = allocate();
		func(blank.content);
		blank.content.clip = audioClip;
		mono.StartCoroutine( blank.delayPlayOneShot(time, waitTime) );
	}

	public void playAll(AudioClip audioClip, System.Action<AudioSource> func) {
		AudioSourceWrapper blank = allocate();
		func(blank.content);
		blank.content.clip = audioClip;
		blank.play();
	}
}
