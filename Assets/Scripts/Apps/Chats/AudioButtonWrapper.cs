using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioButtonWrapper : MonoBehaviour 
{
    public static AudioButtonWrapper playingButtonWrapper;

    public MediaManager.SoundInfo playbackInfo;
	public Button playButton;
	public Sprite[] playSprites;

    private AudioButtonBarController playbackBar;
    private bool isPlaying;


    void Update ()
    {
        if (isPlaying && !playbackBar.GetIsSeeking ()) 
		{
			float playbackTime = MediaManager.instance.bgmPlaybackTime;
			int playbackMin = Mathf.FloorToInt (playbackTime / 60);
			int playbackSec = (int)(playbackTime % 60);

			float musicLength = MediaManager.instance.GetBGMClipLength ();
			float currentFillScale = playbackTime / musicLength;

			playbackBar.fillImage.localScale = new Vector3 (currentFillScale, 1f, 1f);
			playbackBar.handleImage.anchoredPosition = new Vector2 (playbackBar.fillImage.rect.width * currentFillScale, 0f);
			playbackBar.currentTimeText.text = string.Format ("{0:0}:{1:00}", playbackMin, playbackSec);
		}
    }

    public void InitializePlayback (MediaManager.SoundInfo newPlayback)
    {
        playbackInfo = newPlayback;

        float musicLength = playbackInfo.clip.length;
        int remainingMin = Mathf.FloorToInt (musicLength / 60);
        int remainingSec = (int)(musicLength % 60);

        playbackBar = GetComponentInChildren<AudioButtonBarController> ();  
        playbackBar.fillImage.localScale = new Vector3 (0f, 1f, 1f);
        playbackBar.handleImage.anchoredPosition = new Vector2 (0f, 0f);
        playbackBar.currentTimeText.text = string.Format ("{0:0}:{1:00}", remainingMin, remainingSec);
    }

    public void PlayAudio ()
    {
        if (!isPlaying)
        {
            float startTimeScale = playbackBar.handleImage.anchoredPosition.x / playbackBar.fillImage.rect.width;
            startTimeScale = Mathf.Clamp (startTimeScale, 0f, 0.999f);
            MediaManager.instance.PlayBGM (playbackInfo.name);
            MediaManager.instance.bgmPlaybackTime = playbackInfo.clip.length * startTimeScale;

            if (playingButtonWrapper != null)
            {
				playingButtonWrapper.playButton.image.sprite = playSprites [0];
                playingButtonWrapper.isPlaying = false;
            }
            playingButtonWrapper = this;
			playButton.image.sprite = playSprites [1];
            isPlaying = true;
        }
        else
        {
            MediaManager.instance.StopBGM ();
			playButton.image.sprite = playSprites [0];
            isPlaying = false;
        }
    }

    public bool GetIsPlaying ()
    {
        return isPlaying;
    }
}
