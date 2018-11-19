using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class AudioButtonBarController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler 
{
    public RectTransform startPoint, fillImage, handleImage;
    public TextMeshProUGUI currentTimeText;

    private AudioButtonWrapper audioButtonWrapper;
    private PointerEventData currentPointer;
    private bool seeking;
    private float newScale;

    void Awake ()
    {
        audioButtonWrapper = GetComponentInParent<AudioButtonWrapper> ();
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

            float playbackTime = audioButtonWrapper.playbackInfo.clip.length * newScale;
            playbackMin = Mathf.FloorToInt (playbackTime / 60);
            playbackSec = (int)(playbackTime % 60);

            fillImage.localScale = new Vector3 (newScale, 1f, 1f);
            handleImage.anchoredPosition = new Vector2 (fillImage.rect.width * newScale, 0f);
            currentTimeText.text = string.Format ("{0:0}:{1:00}", playbackMin, playbackSec);
        }
    }

    public void OnPointerDown (PointerEventData eventData)
    {
        seeking = true;
        currentPointer = eventData;
    }

    public void OnPointerUp (PointerEventData eventData)
    {
        if (audioButtonWrapper.GetIsPlaying ())
        {
            MediaManager.instance.bgmPlaybackTime = MediaManager.instance.GetBGMClipLength () * newScale;
        } 
        seeking = false;
        currentPointer = null;
    }

    public bool GetIsSeeking ()
    {
        return seeking;
    }
}
