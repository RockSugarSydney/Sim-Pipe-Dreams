using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicAppController : MonoBehaviour 
{
	public static MusicAppController instance;
	public static bool musicOn;

	public class MusicInfo
	{
		public bool isAccessable;
		public string name, artist, album;
	}

	[XmlRoot ("MusicLibrary")]
	public class MusicLibrary
	{
		[XmlArray ("Library")]
		[XmlArrayItem ("Music")]
		public List<MusicInfo> library = new List<MusicInfo> ();
	}

    public GameObject libraryScreen;
	public Transform musicListPanel;
    public GameObject musicDetailsButton;

	[Header ("Playing Music Panel")]
    public Image playingAlbumImage;
    public Image playingButtonImage;
	public TextMeshProUGUI playingNameText, playingArtistText;
	public int charLimit;

	[Header ("Music Window")]
	public GameObject musicWindow;
	public Image albumImage, playButtonImage;
	public TextMeshProUGUI musicNameText, musicArtistText;
	public SeekBarController musicBar;

	private static MusicLibrary musicLibrary;
	private static int currentMusicIndex;


	void Awake ()
	{
//		if (instance == null) 
//		{
//			instance = this;
//		} 
//		else if (instance != this) 
//		{
//			Destroy (gameObject);
//		}
	}

	void Start ()
	{
		for (int i = 0; i < musicLibrary.library.Count; i++) 
		{
			if (musicLibrary.library [i].isAccessable)
			{
				if (MediaManager.instance.bgmClip == null) 
				{
					currentMusicIndex = i;
					MediaManager.instance.bgmClip = MediaManager.instance.GetAudioInfo (musicLibrary.library [i].name).clip;
				}
				GameObject detailsPrefab = Instantiate (musicDetailsButton, musicListPanel);
				MusicDetailsWrapper detailsWrapper = detailsPrefab.GetComponent<MusicDetailsWrapper> ();
				detailsWrapper.albumImage.sprite = MediaManager.instance.GetAudioInfo (musicLibrary.library [i].name).cover;
				detailsWrapper.nameText.text = musicLibrary.library [i].name;
				detailsWrapper.artistText.text = musicLibrary.library [i].artist;

				int index = i;
				detailsWrapper.playButton.onClick.AddListener (() => {
					SelectMusic (index);
				});
			}
		}
		UpdateMusicDetails ();
		TogglePlayButtonSprite ();
	}

	void Update ()
	{
		if (musicWindow.activeSelf && MediaManager.instance.IsBGMPlaying () && !musicBar.GetIsSeeking ()) 
		{
			UpdateMusicPlayback ();
		} 
	}

	public static void InitializeDatabase ()
	{
		ReadMusicLibrary ();
	}

	public static void CycleMusic (int next)
	{
		currentMusicIndex += next;

		if (currentMusicIndex < 0)
		{
			for (int i = musicLibrary.library.Count - 1; i > 0; i--) 
			{
				if (musicLibrary.library [i].isAccessable)
				{
					currentMusicIndex = i;
					break;
				}
			}
		} 
		else if (currentMusicIndex == musicLibrary.library.Count)
		{
			for (int i = 0; i < musicLibrary.library.Count; i++) 
			{
				if (musicLibrary.library [i].isAccessable)
				{
					currentMusicIndex = i;
					break;
				}
			}
		}
		MediaManager.instance.bgmPlaybackTime = 0f;
		MediaManager.instance.PlayBGM (musicLibrary.library [currentMusicIndex].name);
		musicOn = true;
	}

	public void SelectMusic (int index)
	{
		currentMusicIndex = index;

		MediaManager.instance.bgmPlaybackTime = 0f;
		MediaManager.instance.PlayBGM (musicLibrary.library [currentMusicIndex].name);
		musicOn = true;

		UpdateMusicDetails ();
		TogglePlayButtonSprite ();
		musicWindow.SetActive (true);
        libraryScreen.SetActive (false);
	}

	public void PlayMusic ()
	{
		MediaManager.instance.PlayBGM ();
		musicOn = MediaManager.instance.IsBGMPlaying ();

		TogglePlayButtonSprite ();
	}

	public void ChangeMusic (int next)
	{
		CycleMusic (next);
		UpdateMusicDetails ();
		TogglePlayButtonSprite ();
	}

	public void ToggleLoop ()
	{
		if (MediaManager.instance.bgmLoop)
		{
			MediaManager.instance.bgmLoop = false;
		}
		else 
		{
			MediaManager.instance.bgmLoop = true;
		}
	}

	public void OpenMusicWindow ()
	{
		UpdateMusicPlayback ();
		musicWindow.SetActive (true);
        libraryScreen.SetActive (false);
	}

	public void CloseMusicWindow ()
	{
        libraryScreen.SetActive (true);
		musicWindow.SetActive (false);
	}

	public void UpdateMusicPlayback ()
	{
		float playbackTime = MediaManager.instance.bgmPlaybackTime;
		int playbackMin = Mathf.FloorToInt (playbackTime / 60);
		int playbackSec = (int)(playbackTime % 60);

		float musicLength = MediaManager.instance.GetBGMClipLength ();
		float remainingTime = musicLength - playbackTime;
		int remainingMin = Mathf.FloorToInt (remainingTime / 60);
		int remainingSec = (int)(remainingTime % 60);

		float currentFillScale = playbackTime / musicLength;

		musicBar.fillImage.localScale = new Vector3 (currentFillScale, 1f, 1f);
		musicBar.handleImage.anchoredPosition = new Vector2 (musicBar.fillImage.rect.width * currentFillScale, 0f);
		musicBar.currentTimeText.text = string.Format ("{0:0}:{1:00}", playbackMin, playbackSec);
		musicBar.remainingTimeText.text = string.Format ("-{0:0}:{1:00}", remainingMin, remainingSec);
	}

	private void UpdateMusicDetails ()
	{
		Sprite albumArt = MediaManager.instance.GetAudioInfo (musicLibrary.library [currentMusicIndex].name).cover;

		playingAlbumImage.sprite = albumArt;
		playingNameText.text = CameraController.FilterString (musicLibrary.library [currentMusicIndex].name, charLimit);
		playingArtistText.text = CameraController.FilterString (musicLibrary.library [currentMusicIndex].artist, charLimit);
		albumImage.sprite = albumArt;
		musicNameText.text = musicLibrary.library [currentMusicIndex].name;
		musicArtistText.text = musicLibrary.library [currentMusicIndex].artist;
	}

	private void TogglePlayButtonSprite ()
	{
		if (MediaManager.instance.IsBGMPlaying ())
		{
			playingButtonImage.color = Color.red;
			playButtonImage.color = Color.red;
		} 
		else 
		{
			playingButtonImage.color = Color.green;
			playButtonImage.color = Color.green;
		}
	}

	private static void ReadMusicLibrary ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(MusicLibrary));
		string path = Path.Combine (Application.persistentDataPath, "MusicLibrary.xml");

		if (File.Exists (path))
		{
			using (FileStream stream = new FileStream (path, FileMode.Open))
			{
				musicLibrary = serializer.Deserialize (stream) as MusicLibrary;
			}
		} 
		else 
		{
			TextAsset xml = Resources.Load ("MusicLibrary") as TextAsset;
			musicLibrary = serializer.Deserialize (new StringReader (xml.text)) as MusicLibrary;
		}
	}

	private static void WriteMusicLibrary ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(MusicLibrary));
		string path = Path.Combine (Application.persistentDataPath, "MusicLibrary.xml");

		using (FileStream stream = new FileStream (path, FileMode.Create))
		{
			serializer.Serialize (stream, musicLibrary);
		}
	}
}
