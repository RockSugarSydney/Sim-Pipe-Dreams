using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MediaScreenWrapper : MonoBehaviour 
{
	public TextMeshProUGUI titleText;
	public GameObject bottomBar;

	[Header ("Image Panel")]
	public GameObject imagePanel;
    public RectTransform contentPanel;
	public Image currentMediaImage, nextMediaImage;

	[Header ("Video Panel")]
	public GameObject videoPanel;
	public GameObject videoControlPanel;
	public Button videoButton;
	public SeekBarController videoBar;


	void Start ()
	{
		PlayerController.instance.SetCapacitiveButtons (bottomBar.GetComponentsInChildren<Button> ());
	}

	void Update ()
	{
		if (CameraController.CurrentActivity == "MediaScreen") 
		{
			if (MediaManager.instance.GetVideoIsPlaying () && !videoBar.GetIsSeeking ()) 
			{
				float playbackTime = MediaManager.instance.GetVideoPlaybackTime ();
				int playbackMin = Mathf.FloorToInt (playbackTime / 60);
				int playbackSec = (int)(playbackTime % 60);

				float videoLength = MediaManager.instance.GetVideoLength ();
				float remainingTime = videoLength - playbackTime;
				int remainingMin = Mathf.FloorToInt (remainingTime / 60);
				int remainingSec = (int)(remainingTime % 60);

				float currentFillScale = playbackTime / videoLength;

				videoBar.fillImage.localScale = new Vector3 (currentFillScale, 1f, 1f);
				videoBar.handleImage.anchoredPosition = new Vector2 (videoBar.fillImage.rect.width * currentFillScale, 0f);
				videoBar.currentTimeText.text = string.Format ("{0:0}:{1:00}", playbackMin, playbackSec);
				videoBar.remainingTimeText.text = string.Format ("-{0:0}:{1:00}", remainingMin, remainingSec);
			}
		}
	}

	public void ResetVideoBar ()
	{
		videoBar.fillImage.localScale = new Vector3 (0f, 1f, 1f);
		videoBar.handleImage.anchoredPosition = new Vector2 (0f, 0f);
		videoBar.currentTimeText.text = "0:00";
		videoBar.remainingTimeText.text = "-0:00";
	}

	public void ToggleVideoControl ()
	{
		if (videoControlPanel.activeSelf)
		{
			videoControlPanel.SetActive (false);
		} 
		else 
		{
			videoControlPanel.SetActive (true);
		}
	}
}
