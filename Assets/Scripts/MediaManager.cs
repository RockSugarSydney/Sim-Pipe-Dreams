using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class MediaManager : MonoBehaviour 
{
    public static MediaManager instance;

    public enum MediaType
    {
        Image,
        Audio,
        Video
    };

    [System.Serializable]
    public struct SoundInfo
    {
        public string name;
        public Sprite cover;
        public AudioClip clip;
    }

    [System.Serializable]
    public struct ImageInfo
    {
        public string name;
        public Sprite image;
    }

    [System.Serializable]
    public struct VideoInfo
    {
        public string name;
        public Sprite thumbnail;
        public VideoClip clip;
    }
    public SoundInfo[] soundLibrary;
    public ImageInfo[] imageLibrary;
    public VideoInfo[] videoLibrary;

    private static AudioSource bgmSource, sfxSource;
    private static VideoPlayer videoPlayer;

	private AudioClip previousClip;
	private bool wasMusicPlaying, videoIsSeeking;
	private float previousPlaybackSec;

    void Awake ()
    {
        if (instance == null)
        {
            instance = this;
			DontDestroyOnLoad (gameObject);

			bgmSource = GetComponents<AudioSource> ()[0];
			sfxSource = GetComponents<AudioSource> ()[1];

			videoPlayer = GetComponent<VideoPlayer> ();
			videoPlayer.prepareCompleted += VideoLoaded;
			videoPlayer.loopPointReached += VideoEnded;
			videoPlayer.seekCompleted += SeekComplete;
        }
        else if (instance != this)
        {
            Destroy (gameObject);
        }
    }

    void Start ()
    {
        Shader.SetGlobalInt ("_VideoIsPlaying", 0);
    }

    void Update ()
    {
		if (MusicAppController.musicOn) 
		{
			float playbackTime = bgmSource.time;
			float musicLength = bgmSource.clip.length;
			float currentPlaybackScale = playbackTime / musicLength;

			if (!bgmSource.loop) 
			{
				if (currentPlaybackScale > 0.999f)
				{
					MusicAppController.CycleMusic (1);
				}
			}
		}

        if (videoPlayer.isPlaying)
        {
            Shader.SetGlobalTexture ("_VideoPlaybackTex", videoPlayer.texture);
        }
    }

    void OnApplicationFocus (bool hasFocus)
    {
        if (hasFocus && videoPlayer.isPlaying)
        {
            PlayVideo ();
        }
    }

    public void ToggleSound (string source)
    {
        PlayerPrefs.SetInt (source, 1 - PlayerPrefs.GetInt (source));

        if (source == "BGM")
        {
            bgmSource.volume = PlayerPrefs.GetInt (source);
        }
        else
        {
            sfxSource.volume = PlayerPrefs.GetInt (source);
        }
    }

	public void PlayBGM ()
	{
		if (bgmSource.isPlaying) 
		{
			bgmSource.Pause ();
		}
		else 
		{
			bgmSource.Play ();
		}
	}

    public void PlayBGM (string name)
    {
        for (int i = 0; i < soundLibrary.Length; i++)
        {
            if (soundLibrary [i].name == name)
            {
                bgmSource.clip = soundLibrary [i].clip;
                bgmSource.Play ();
            }
        }
    }

	public void StopBGM ()
	{
		bgmSource.Stop ();
	}

	public void SwapToCall (bool start)
	{
		if (start)
		{
			if (bgmSource.isPlaying) 
			{
				previousClip = bgmSource.clip;
				previousPlaybackSec = bgmSource.time;
				bgmSource.time = 0f;
				bgmSource.Stop ();
				wasMusicPlaying = true;
				MusicAppController.musicOn = false;
			}
		}
		else if (wasMusicPlaying)
		{
			bgmSource.clip = previousClip;
			bgmSource.time = previousPlaybackSec;
			bgmSource.Play ();
			wasMusicPlaying = false;
			MusicAppController.musicOn = true;
		}
		else 
		{
			bgmSource.Stop ();
		}
	}

	public AudioClip bgmClip
	{
		get
		{
			return bgmSource.clip;
		}

		set 
		{
			bgmSource.clip = value;
		}
	}

	public float bgmPlaybackTime
	{
		get
		{
			return bgmSource.time;
		}

		set 
		{
			bgmSource.time = value;
		}
	}

	public bool bgmLoop 
	{
		get
		{
			return bgmSource.loop;
		}

		set
		{
			bgmSource.loop = value;
		}
	}

	public float GetBGMClipLength ()
	{
		return bgmSource.clip.length;
	}

	public bool IsBGMPlaying ()
	{
		return bgmSource.isPlaying;
	}

    public void PlaySFX (string name)
    {
        for (int i = 0; i < soundLibrary.Length; i++)
        {
            if (soundLibrary[i].name == name)
            {
                sfxSource.PlayOneShot (soundLibrary [i].clip);
            }
        }
    }

    public void LoadVideo (string name)
    {
        for (int i = 0; i < videoLibrary.Length; i++)
        {
            if (videoLibrary[i].name == name)
            {
                videoPlayer.clip = videoLibrary [i].clip;
                videoPlayer.SetTargetAudioSource (0, bgmSource);
                videoPlayer.Prepare ();
            }
        }
    }

    public void PlayVideo ()
    {
		if (!videoIsSeeking) 
		{
			if (!videoPlayer.isPlaying)
			{
				videoPlayer.Play ();
				Shader.SetGlobalInt ("_VideoIsPlaying", 1);
			}
			else
			{
				videoPlayer.Pause ();
			}
		}
    }

    public void StopVideo ()
    {
		if (wasMusicPlaying) 
		{
			bgmSource.Play ();
			wasMusicPlaying = false;
			MusicAppController.musicOn = true;
		}
        videoPlayer.Stop ();
        Shader.SetGlobalInt ("_VideoIsPlaying", 0);
    }

	public float GetVideoPlaybackTime ()
	{
		return videoPlayer.frame / videoPlayer.frameRate;
	}

	public float GetVideoLength ()
	{
		return videoPlayer.frameCount / videoPlayer.frameRate;
	}

	public bool GetVideoIsPlaying ()
	{
		return videoPlayer.isPlaying;
	}

	public bool GetVideoIsSeeking ()
	{
		return videoIsSeeking;
	}

	public void SetVideoFrame (float newScale)
	{
		videoPlayer.frame = (long)(videoPlayer.frameCount * newScale);
		videoPlayer.Pause ();
		videoIsSeeking = true;
	}

	public void SelectMedia (string name, MediaType type)
	{
		string mediaName = "";
		Sprite mediaImage = null;

		if (type == MediaType.Image)
		{
			ImageInfo selectedImage = GetImageInfo (name);
			mediaName = selectedImage.name;
			mediaImage = selectedImage.image;
		}
		else
		{
			VideoInfo selectedVideo = GetVideoInfo (name);
			mediaName = selectedVideo.name;
			mediaImage = selectedVideo.thumbnail;
		}
		CameraController.instance.OpenMedia (mediaName, mediaImage, type);
	}

    public ImageInfo GetImageInfo (string name)
    {
        for (int i = 0; i < imageLibrary.Length; i++)
        {
            if (imageLibrary [i].name == name)
            {
                return imageLibrary [i];
            }
        }
        return new ImageInfo ();
    }

    public SoundInfo GetAudioInfo (string name)
    {
        for (int i = 0; i < soundLibrary.Length; i++)
        {
            if (soundLibrary [i].name == name)
            {
                return soundLibrary [i];
            }
        }
        return new SoundInfo ();
    }

    public VideoInfo GetVideoInfo (string name)
    {
        for (int i = 0; i < videoLibrary.Length; i++)
        {
            if (videoLibrary [i].name == name)
            {
                return videoLibrary [i];
            }
        }
        return new VideoInfo ();
    }

    private void VideoLoaded (VideoPlayer source)
    {
		if (bgmSource.isPlaying) 
		{
			bgmSource.Pause ();
			wasMusicPlaying = true;
			MusicAppController.musicOn = false;
		}
		videoPlayer.Play ();
		CameraController.mediaWrapper.videoControlPanel.SetActive (false);
        Shader.SetGlobalInt ("_VideoIsPlaying", 1);
    }

    private void VideoEnded (VideoPlayer source)
    {
        if (!videoPlayer.isLooping)
        {
            Shader.SetGlobalInt ("_VideoIsPlaying", 0);
        }
    }

	private void SeekComplete (VideoPlayer source)
	{
		videoPlayer.Play ();
		videoIsSeeking = false;
	}
}
