using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SeekBarController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	public static SeekBarController instance;

	public RectTransform startPoint, fillImage, handleImage;
	public TextMeshProUGUI currentTimeText, remainingTimeText;
	public MediaManager.MediaType type;

	private PointerEventData currentPointer;
	private bool seeking;
	private float newScale;


	void Awake ()
	{
		if (instance == null) 
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy (gameObject);
		}
	}

	void Update ()
	{
		if (seeking) 
		{
			Vector3 newHandlePosition = Camera.main.ScreenToWorldPoint (currentPointer.position);
			newHandlePosition = startPoint.InverseTransformPoint (newHandlePosition);

			newScale = newHandlePosition.x / fillImage.rect.width;
			newScale = Mathf.Clamp (newScale, 0f, 0.999f);

			int playbackMin = 0;
			int playbackSec = 0;
			int remainingMin = 0;
			int remainingSec = 0;

			if (type == MediaManager.MediaType.Audio)
			{
				float playbackTime = MediaManager.instance.GetBGMClipLength () * newScale;
				playbackMin = Mathf.FloorToInt (playbackTime / 60);
				playbackSec = (int)(playbackTime % 60);

				float musicLength = MediaManager.instance.GetBGMClipLength ();
				float remainingTime = musicLength - playbackTime;
				remainingMin = Mathf.FloorToInt (remainingTime / 60);
				remainingSec = (int)(remainingTime % 60);
			} 
			else if (type == MediaManager.MediaType.Video)
			{
				float playbackTime = MediaManager.instance.GetVideoLength () * newScale;
				playbackMin = Mathf.FloorToInt (playbackTime / 60);
				playbackSec = (int)(playbackTime % 60);

				float videoLength = MediaManager.instance.GetVideoLength ();
				float remainingTime = videoLength - playbackTime;
				remainingMin = Mathf.FloorToInt (remainingTime / 60);
				remainingSec = (int)(remainingTime % 60);
			}
			fillImage.localScale = new Vector3 (newScale, 1f, 1f);
			handleImage.anchoredPosition = new Vector2 (fillImage.rect.width * newScale, 0f);
			currentTimeText.text = string.Format ("{0:0}:{1:00}", playbackMin, playbackSec);
			remainingTimeText.text = string.Format ("-{0:0}:{1:00}", remainingMin, remainingSec);
		}
	}

	public void OnPointerDown (PointerEventData eventData)
	{
		seeking = true;
		currentPointer = eventData;
	}

	public void OnPointerUp (PointerEventData eventData)
	{
		if (type == MediaManager.MediaType.Audio)
		{
			MediaManager.instance.bgmPlaybackTime = MediaManager.instance.GetBGMClipLength () * newScale;
		} 
		else if (type == MediaManager.MediaType.Video) 
		{
			MediaManager.instance.SetVideoFrame (newScale);
		}
		seeking = false;
		currentPointer = null;
	}

	public bool GetIsSeeking ()
	{
		return seeking;
	}
}
