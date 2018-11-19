using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour 
{
    public static CameraController instance;
	public static GameObject lockPrefab, mediaPrefab, callPrefab, notificationPrefab, keyboardPrefab;
	public static LockScreenWrapper lockWrapper;
	public static CallScreenWrapper callWrapper;
	public static MediaScreenWrapper mediaWrapper;
	public static NotificationBarController notificationController;
	public static OnScreenKeyboard keyboard;

    public delegate void CloseMediaScreen ();
    public static CloseMediaScreen onMediaClose;

    [System.Serializable]
    public class CameraEffect
    {
        public string name;
        public Material effectMaterial;
        public float duration;
    };

    public GameObject lockScreen, mediaScreen, callScreen, chatHandler, notificationBar, onScreenKeyboard;
	public Transform overlayCanvas;
    public CameraEffect[] cameraEffects;

	private static string currentActivity;
    private static string previousActivity;
	private static bool isLoading;
    
	private List<Canvas> transitionedCanvases;
    private Camera transitionCam;
	private CameraEffect appliedCameraEffect;
//  	private RenderTexture effectTexture, destTexture;
	private bool isTransitioning, isNotifying;
    private float effectTimer;

	[Header ("Notification")]
	public float lifespan;

	private struct Notification
	{
		public string name;
		public string message;
	}
	private Queue<Notification> scheduledNotifications = new Queue<Notification> ();
	private Notification currentNotification;

	private class TestClass
	{
		public string testString;
		public int testInt;
	}


    void Awake ()
    {
        if (instance == null)
        {
            instance = this;
			SceneManager.sceneLoaded += OnSceneLoaded;
			DontDestroyOnLoad (gameObject);
			DontDestroyOnLoad (overlayCanvas.gameObject);
        }
        else if (instance != this)
        {
			GetComponent<AudioListener> ().enabled = false;
			Destroy (overlayCanvas.gameObject);
        }
    }

	void Start ()
	{
		if (instance == this) 
		{
			currentActivity = SceneManager.GetActiveScene ().name;
//			LockPhone (true);
			InitializeApps ();
//			Invoke ("TestCall", 10f);
//			ChatsAppController.InitiateConversation ("Marco");
		}

//		string testString = "";
//		string[] splitString = testString.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
//		Debug.Log (splitString.Length);
//		for (int i = 0; i < splitString.Length; i++) 
//		{
//			Debug.Log (splitString [i]);
//		}
	}

    void Update ()
    {
        if (isTransitioning)
        {
            if (effectTimer <= appliedCameraEffect.duration)
            {
                effectTimer += Time.deltaTime;
                float currentCutoff = Mathf.Clamp01 (effectTimer / appliedCameraEffect.duration);
				appliedCameraEffect.effectMaterial.SetFloat ("_Cutoff", currentCutoff);
            }
            else
            {
				SceneManager.UnloadSceneAsync (SceneManager.GetActiveScene ());

				for (int i = 0; i < transitionedCanvases.Count; i++) 
				{
					transitionedCanvases [i].worldCamera = Camera.main;
				}
                Destroy (transitionCam.gameObject);
				appliedCameraEffect = null;
				isLoading = false;
                isTransitioning = false;
//              Debug.Log ("Done");
            }
        }
    }
        
    void OnRenderImage (RenderTexture src, RenderTexture dst)
    {
		if (appliedCameraEffect == null)
        {
            Graphics.Blit (src, dst);
        }
        else
        {
			Graphics.Blit (src, dst, appliedCameraEffect.effectMaterial);
//            #if UNITY_EDITOR || UNITY_STANDALONE
//            Graphics.Blit (src, dst, cameraEffect);
//            #elif UNITY_ANDROID || UNITY_IOS
//            Graphics.Blit (src, effectTexture, cameraEffect);
//            destTexture = dst;
//            #endif
        }
    }

//    #if UNITY_ANDROID || UNITY_IOS
//    void OnPostRender ()
//    {
//        if (cameraEffect != null)
//        {
//            Graphics.Blit (effectTexture, destTexture);
//        }
//    }
//    #endif

	void OnApplicationFocus (bool hasFocus)
	{
		if (hasFocus)
		{
//			LockPhone (true);
		}
	}

	public static string FilterString (string newString, int maxLength)
	{
		string[] splitString = Regex.Split (newString, @"<[^<>]+>|\n|\r");
		string filteredString = "";

		for (int j = 0; j < splitString.Length; j++) 
        {
			if (splitString [j] != String.Empty) 
			{
				filteredString += splitString [j] + " ";
			}
		}

		if (filteredString.Length > maxLength) 
		{
			return filteredString.Substring (0, maxLength) + "...";
		}
		else 
		{
			return filteredString;
		}
	}

	public IEnumerator LoadApp (string name)
	{
		if (!isTransitioning) 
		{
			string[] splitString = name.Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries); 
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync (splitString [0], LoadSceneMode.Additive);
			currentActivity = name;
			isLoading = true;

			QuickAccessMenuManager.instance.SetMenuPosition (1);

			while (!asyncLoad.isDone)
			{
				yield return null;
			}
			CameraController.instance.StartTransition (Vector3.zero, "OpenApp");
		}
	}

    public IEnumerator LoadApp (GameObject loadingApp)
    {
		if (!isTransitioning) 
		{
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync (loadingApp.name, LoadSceneMode.Additive);
			currentActivity = loadingApp.name;
			isLoading = true;

			QuickAccessMenuManager.instance.SetMenuPosition (1);

			while (!asyncLoad.isDone)
			{
				yield return null;
			}
			CameraController.instance.StartTransition (loadingApp.transform.position, "OpenApp");
		}  
    }

	public void CloseApp ()
	{
		if (!isLoading)
		{
			instance.StartCoroutine (instance.LoadPhoneMenu ());

			if (Shader.GetGlobalInt ("_VideoIsPlaying") == 1)
			{
				MediaManager.instance.StopVideo ();
			}
		}
	}

	public void OpenLink (string link)
	{
		StartCoroutine (LoadApp (link));
	}

    public void OpenMedia (string name, Sprite image, MediaManager.MediaType type)
    {
        if (mediaPrefab == null)
        {
            Transform canvas = GameObject.FindGameObjectWithTag ("Canvas").transform;
            mediaPrefab = Instantiate (mediaScreen, canvas);
			mediaWrapper = mediaPrefab.GetComponent<MediaScreenWrapper> ();
        }
        else
        {
            mediaPrefab.SetActive (true);
        }
		mediaWrapper.titleText.text = name;

        if (type == MediaManager.MediaType.Image)
        {
			mediaWrapper.currentMediaImage.sprite = image;
			mediaWrapper.contentPanel.localScale = Vector3.one;
			mediaWrapper.imagePanel.SetActive (true);
			mediaWrapper.videoPanel.SetActive (false);
        }
        else
        {
			mediaWrapper.videoButton.image.sprite = image;
			mediaWrapper.ResetVideoBar ();
			mediaWrapper.videoControlPanel.SetActive (true);
			mediaWrapper.imagePanel.SetActive (false);
			mediaWrapper.videoPanel.SetActive (true);
			MediaManager.instance.LoadVideo (name);
        }
		previousActivity = currentActivity;
		currentActivity = "MediaScreen";

		PlayerController.instance.SwitchLayer ("MediaMenu");
    }

    public void CloseMedia ()
    {
        mediaPrefab.SetActive (false);

        if (Shader.GetGlobalInt ("_VideoIsPlaying") == 1)
        {
            MediaManager.instance.StopVideo ();
        }
		ReturnToPreviousActivity ();
        onMediaClose ();
    }

	public void MakeCall (string phoneNumber, string callerID, ContactsAppController.CallType callType)
	{
		if (callPrefab == null)
		{
			Transform canvas = GameObject.FindGameObjectWithTag ("Canvas").transform;
			callPrefab = Instantiate (callScreen, canvas);
			callWrapper = callPrefab.GetComponent<CallScreenWrapper> ();
		}
		else
		{
			callPrefab.SetActive (true);
		}
		previousActivity = currentActivity;
		currentActivity = "CallScreen";
		callWrapper.ConnectCall (phoneNumber, callerID, callType);
	}
		
	public void LockPhone (bool locking)
	{
		if (locking)
		{
			if (lockPrefab == null)
			{
				Transform canvas = GameObject.FindGameObjectWithTag ("Canvas").transform;
				lockPrefab = Instantiate (lockScreen, canvas);
				lockWrapper = lockPrefab.GetComponent<LockScreenWrapper> ();
			} 
			else 
			{
				lockPrefab.SetActive (true);
			}
			previousActivity = currentActivity;
			currentActivity = "LockScreen";
			lockWrapper.InitLockScreen ();
		} 
		else 
		{
			lockPrefab.SetActive (false);
			ReturnToPreviousActivity ();
		}
	}

	public void ScheduleNotification (string name, string message)
	{
		Notification newNotification = new Notification ();
		newNotification.name = name;
		newNotification.message = message;
		scheduledNotifications.Enqueue (newNotification);

		if (!isNotifying) 
		{
			isNotifying = true;
			ReceiveNotification ();
		}
	}

	public void ReceiveNotification ()
	{
		if (scheduledNotifications.Count > 0)
		{
			currentNotification = scheduledNotifications.Dequeue ();
			string[] splitString = currentNotification.name.Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

			if (currentNotification.name != currentActivity)
			{
				if (notificationPrefab == null)
				{
					notificationPrefab = Instantiate (notificationBar, overlayCanvas);
					notificationController = notificationPrefab.GetComponent<NotificationBarController> ();
				}
				else 
				{
					notificationPrefab.SetActive (true);
				}

				if (scheduledNotifications.Count > 0) 
				{
					if (currentNotification.name != scheduledNotifications.Peek ().name) 
					{
						notificationController.ShowNotification (lifespan);
					} 
					else
					{
						notificationController.ShowNotification (lifespan* 1 / 2);
					}
				} 
				else
				{
					notificationController.ShowNotification (lifespan);
				}
				notificationController.notifierImage.sprite = MediaManager.instance.GetImageInfo (splitString [splitString.Length - 1]).image; 
				notificationController.titleText.text = splitString [splitString.Length - 1];
				notificationController.bodyText.text = FilterString (currentNotification.message, 50);

				if (currentActivity == "PhoneMenu")
				{
					PhoneMenuManager.instance.UpdateNotificationBadge (splitString [0]);
				}
			} 
			else 
			{
				ReceiveNotification ();
			}
		} 
		else 
		{
			notificationPrefab.SetActive (false);
			isNotifying = false;
		}
	}

	public void OpenNotification ()
	{
		StartCoroutine (LoadApp (currentNotification.name));
	}

	public void ShowKeyboard ()
	{
		if (keyboardPrefab == null) 
		{
			Transform canvas = GameObject.FindGameObjectWithTag ("Canvas").transform;
			keyboardPrefab = Instantiate (onScreenKeyboard, canvas);
			keyboard = keyboardPrefab.GetComponent<OnScreenKeyboard> ();
		} 
		else
		{
			keyboardPrefab.SetActive (true);
		}
	}

	public bool ApplyCameraEffect (string name)
	{
		if (appliedCameraEffect == null) 
		{
			for (int i = 0; i < cameraEffects.Length; i++)
			{
				if (cameraEffects [i].name == name)
				{
					appliedCameraEffect = cameraEffects [i];
					return true;
				}
			}
		}
		return false;
	}

	public void RemoveCameraEffect ()
	{
		appliedCameraEffect = null;
	}

	public CameraEffect GetAppliedCameraEffect ()
	{
		return appliedCameraEffect;
	}

	public static void ReturnToPreviousActivity ()
	{
		currentActivity = previousActivity;
	}

	public static string CurrentActivity
	{
		get
		{
			return currentActivity;
		}

		set
		{
			currentActivity = value;
			Debug.Log (currentActivity);
		}
	}

	public static string PreviousActivity
	{
		get
		{
			return previousActivity;
		}

		set
		{
			previousActivity = value;
		}
	}
		
    private void StartTransition (Vector3 startPos, string effectName)
    {
		if (ApplyCameraEffect (effectName))
        {
			appliedCameraEffect.effectMaterial.SetTexture ("_TargetTex", transitionCam.targetTexture);
			appliedCameraEffect.effectMaterial.SetVector ("_StartPos", Camera.main.WorldToViewportPoint (startPos));
			appliedCameraEffect.effectMaterial.SetFloat ("_Cutoff", 0f);
//          Debug.Log ("Effect started");
            effectTimer = 0f;
			isTransitioning = true;
        }
    }

	private IEnumerator LoadPhoneMenu ()
	{
		if (!isTransitioning)
		{
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync ("PhoneMenu", LoadSceneMode.Additive);
			previousActivity = currentActivity;
			currentActivity = "PhoneMenu";
			isLoading = true;

			QuickAccessMenuManager.instance.SetMenuPosition (1);

			while (!asyncLoad.isDone)
			{
				yield return null;
			}
			CameraController.instance.StartTransition (Vector3.zero, "OpenApp");
		}
	}

	private void OnSceneLoaded (Scene scene, LoadSceneMode mode)
	{
		GameObject[] loadedObjects = scene.GetRootGameObjects ();
		transitionedCanvases = new List<Canvas> ();

		for (int i = 0; i < loadedObjects.Length; i++)
		{
			if (loadedObjects [i].tag == "Canvas")
			{
				transitionedCanvases.Add (loadedObjects [i].GetComponent<Canvas> ());
			}

			if (loadedObjects [i].tag == "MainCamera")
			{
				transitionCam = loadedObjects [i].GetComponent<Camera> ();
				transitionCam.targetTexture = new RenderTexture (transitionCam.pixelWidth, transitionCam.pixelHeight, 16);
			}
		}
	}

	private void InitializeApps ()
	{
		ChatsAppController.InitializeDatabase ();
		ContactsAppController.IntializeDatabase ();
	}

	private void TestCall ()
	{
		ContactsAppController.DialNumber ("017-2306739", ContactsAppController.CallType.Incoming);
	}
}
