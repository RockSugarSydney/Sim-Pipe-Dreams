using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class FriendsAppController : MonoBehaviour 
{
	public static FriendsAppController instance;

	public class Comment
	{
		public bool isPosted;
        public string username, displayName, timestamp, body;
	}

	public class Post
	{
		public bool isPosted, hasLiked, hasCommented;
        public string username, displayName, timestamp, body;
		public int likes, ghostComments;
		[XmlArray ("Comments")]
		[XmlArrayItem ("Comment")]
		public List<Comment> comments = new List<Comment> ();
	}

	[XmlRoot ("Feed")]
	public class Feed
	{
		[XmlArray ("Posts")]
		[XmlArrayItem ("Post")]
		public List<Post> posts = new List<Post> ();
	}

    public GameObject friendScreen, mainBottomBar;
	public ScrollRect feedScrollRect;
	public Transform categoryPanel, postListPanel;
	public GameObject categoryTab, postPanel, commentPanel, bodyText, mediaButton, locationPanel;
	public Color defaultColor, likeColor, commentColor;
    public Sprite[] likeSprites, commentSprites;

	[Header ("Profile")]
	public GameObject profileHeaderPanel;
	public Image backgroundImage;
	public Image profilePictureImage;
	public TextMeshProUGUI usernameText, descriptionText;

	[Header ("Comment Window")]
	public GameObject commentWindow;
	public TextMeshProUGUI commentTitleText, commentTimestampText;
	public ScrollRect commentScrollRect;
	public Transform commentListPanel;
	public PostDetailsWrapper selectedPostWrapper;
	public OnScreenKeyboard onScreenKeyboard;
    public GameObject commentBottomBar;
	public float lerpRate;

	private Feed feed = new Feed ();
	private Post selectedPost;
	private List<GameObject> postPanels = new List<GameObject> ();
	private List<GameObject> commentPanels = new List<GameObject> ();
    private Button[] categoryButtons;
	private RectTransform commentTransform;
	private Vector2 finalWindowPosition;
	private bool isFading, isGravitating, isOpen;
	private string currentFeed;
	private float fadeTimer;
	private int selectedPostIndex;

    private GraphicRaycaster m_Raycaster;
    private PointerEventData m_PointerEventData;
    private EventSystem m_EventSystem;


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
		commentTransform = commentWindow.GetComponent<RectTransform> ();
	}
		
	void Start ()
    {
		string[] splitString = CameraController.CurrentActivity.Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        categoryButtons = categoryPanel.GetComponentsInChildren<Button> ();

		PlayerController.instance.SwitchLayer ("Menu1");
		PlayerController.instance.SetSwitchableTabs (categoryButtons);
		PlayerController.instance.SetScrollableRect (feedScrollRect);
		PlayerController.instance.SetCapacitiveButtons (mainBottomBar.GetComponentsInChildren<Button> ());

		ReadFeed ();
		OpenTab (1);

		if (splitString.Length > 1)
		{
			int newPostIndex = 0;

			if (int.TryParse (splitString [1], out newPostIndex)) 
			{
				ShowComments (newPostIndex);
			}
		}
		CameraController.onMediaClose = OpenFriendScreen;

        //Fetch the Raycaster from the GameObject (the Canvas)
        m_Raycaster = GetComponent<GraphicRaycaster>();
        //Fetch the Event System from the Scene
        m_EventSystem = GetComponent<EventSystem>();
	}

	void Update ()
	{
		if (onScreenKeyboard.alphabetKeyboardPanel.activeSelf || onScreenKeyboard.symbolKeyboardPanel.activeSelf)
        {
            if (Input.GetButtonUp ("Fire1"))
            {
                bool hasFocus = false;

                //Set up the new Pointer Event
                m_PointerEventData = new PointerEventData(m_EventSystem);
                //Set the Pointer Event Position to that of the mouse position
                #if UNITY_EDITOR || UNITY_STANDALONE
                m_PointerEventData.position = Input.mousePosition;
                #elif UNITY_ANDROID || UNITY_IOS
                m_PointerEventData.position = Input.GetTouch (0).position;
                #endif

                //Create a list of Raycast Results
                List<RaycastResult> results = new List<RaycastResult>();

                //Raycast using the Graphics Raycaster and mouse click position
                m_Raycaster.Raycast(m_PointerEventData, results);

				Debug.Log (onScreenKeyboard.transform.TransformPoint (onScreenKeyboard.GetComponent<RectTransform> ().rect.min));
				Debug.Log (onScreenKeyboard.transform.TransformPoint (onScreenKeyboard.GetComponent<RectTransform> ().rect.max));

                //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
                foreach (RaycastResult result in results)
                {
					float keyboardMinYPos = 0f;
					float keyboardMaxYPos = 0f;
					Vector3 hitPosition = Camera.main.ScreenToWorldPoint (result.screenPosition);
					Vector3[] keyboardCornerPos = new Vector3[4];
					onScreenKeyboard.GetComponent<RectTransform> ().GetWorldCorners (keyboardCornerPos);

					for (int i = 0; i < keyboardCornerPos.Length; i++)
					{
						Debug.Log (keyboardCornerPos [i]);

						if (keyboardCornerPos [i].y < keyboardMaxYPos)
						{
							keyboardMinYPos = keyboardCornerPos [i].y;
							Debug.Log (keyboardMinYPos);
						}

						if (keyboardCornerPos [i].y > keyboardMinYPos)
						{
							keyboardMaxYPos = keyboardCornerPos [i].y;
							Debug.Log (keyboardMaxYPos);
						}
					}
						
					if (hitPosition.y >= keyboardMinYPos && hitPosition.y <= keyboardMaxYPos || result.gameObject.name == "CommentButton")
                    {
                        hasFocus = true;
                        break;
                    }
                }

                if (!hasFocus)
                {
                    OnScreenKeyboard.onEnter = null;
					onScreenKeyboard.inputField.text = "";
                    onScreenKeyboard.alphabetKeyboardPanel.SetActive (false);
					onScreenKeyboard.symbolKeyboardPanel.SetActive (false);
                    commentBottomBar.SetActive (true);

					PlayerController.instance.SwitchLayer ("Menu2");
                }
            }
        }

		if (isFading)
		{
			fadeTimer += Time.deltaTime;
			float currentAlpha = fadeTimer / CameraController.instance.GetAppliedCameraEffect ().duration;
			currentAlpha = Mathf.Clamp01 (currentAlpha);
			CameraController.instance.GetAppliedCameraEffect ().effectMaterial.SetFloat ("_NewAlpha", currentAlpha);

			if (fadeTimer > CameraController.instance.GetAppliedCameraEffect ().duration) 
			{
				CameraController.instance.RemoveCameraEffect ();
				isFading = false;
			}
		}

		if (isGravitating)
		{
			commentTransform.anchoredPosition = Vector2.Lerp (commentTransform.anchoredPosition, finalWindowPosition, lerpRate);

			if (Vector2.Distance (commentTransform.anchoredPosition, finalWindowPosition) < 1f)
			{
				if (isOpen)
				{
					friendScreen.SetActive (false);
				}
				else 
				{
					commentWindow.SetActive (false);
				}
				commentTransform.anchoredPosition = finalWindowPosition;
				isGravitating = false;
			}
		}
	}

	public void OpenTab (int index)
	{
		if (isFading) 
		{
			return;
		}
//		FadePanels ();

		switch (index) 
		{
		case 0:
			OpenProfile ();
			break;
		case 1:
            currentFeed = "All";
            OpenFeed (currentFeed);
			profileHeaderPanel.SetActive (false);
			break;
		default:
			break;
		}
		feedScrollRect.velocity = new Vector2 ();
		feedScrollRect.content.anchoredPosition = new Vector2 (0f, 0f); 

		for (int i = 0; i < categoryButtons.Length; i++)
		{
			if (i == index)
			{
				categoryButtons [i].interactable = false;
				categoryButtons [i].transform.GetChild (0).gameObject.SetActive (true);
			} 
			else
			{
				categoryButtons [i].interactable = true;
				categoryButtons [i].transform.GetChild (0).gameObject.SetActive (false);
			}
		}

		PlayerController.instance.currentTabIndex = index;
	}

    public void OpenFriendScreen ()
    {   
        friendScreen.SetActive (true);
		PlayerController.instance.SwitchLayer ("Menu1");
    }

    public void OpenCommentsWindow ()
    {
        commentWindow.SetActive (true);
		PlayerController.instance.SwitchLayer ("Menu2");
    }

	public void CloseCommentsWindow ()
	{
		if (onScreenKeyboard.alphabetKeyboardPanel.activeSelf || onScreenKeyboard.symbolKeyboardPanel.activeSelf)
		{
			onScreenKeyboard.alphabetKeyboardPanel.SetActive (false);
			onScreenKeyboard.symbolKeyboardPanel.SetActive (false);
		} 
		else 
		{
			PlayerController.instance.SwitchLayer ("Menu1");
			OpenFeed (currentFeed);

			finalWindowPosition = new Vector2 (commentTransform.rect.width, commentTransform.anchoredPosition.y);
			isOpen = false;
			isGravitating = true;
			friendScreen.SetActive (true);

            CameraController.onMediaClose = OpenFriendScreen;
		}
	}

	private void OpenProfile ()
	{
		profileHeaderPanel.SetActive (true);
        currentFeed = "fridakahlo";
        OpenFeed (currentFeed);
	}

	private void OpenFeed (string username)
	{
		List<Button> generatedButtons = new List<Button> ();
		int previousButtonIndex = 0;
		int usedPanels = 0;

		for (int i = 0; i < feed.posts.Count; i++)
		{
			if (feed.posts [i].isPosted && (feed.posts [i].username == username || username == "All"))
			{
				GameObject postPrefab;

				if (usedPanels > postPanels.Count - 1)
				{
					postPrefab = Instantiate (postPanel, postListPanel);
					postPanels.Add (postPrefab);
				}
				else 
				{
					postPrefab = postPanels [usedPanels];
					postPrefab.SetActive (true);
				}
				usedPanels++;

				PostDetailsWrapper postWrapper = postPrefab.GetComponent<PostDetailsWrapper> ();
				postWrapper.profileImage.sprite = MediaManager.instance.GetImageInfo (feed.posts [i].username).image;
                postWrapper.displayNameText.text = feed.posts [i].displayName;
				postWrapper.usernameText.text = "@" + feed.posts [i].username;

				DateTime postTime = DateTime.Parse (feed.posts [i].timestamp);
				postWrapper.timestampText.text = postTime.ToLongDateString ();

				for (int j = 0; j < postWrapper.bodyPanel.childCount; j++)
				{
					Destroy (postWrapper.bodyPanel.GetChild (j).gameObject);
				}
				string[] split = feed.posts [i].body.Split (new Char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

				for (int j = 0; j < split.Length; j++) 
				{
					if (split [j].Contains ("Image_")) 
					{
						Button buttonPrefab = Instantiate (mediaButton, postWrapper.bodyPanel).GetComponent<Button> ();
						string imageName = split [j].Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries) [1];
						buttonPrefab.image.sprite = MediaManager.instance.GetImageInfo (imageName).image;
						buttonPrefab.transform.GetChild (0).gameObject.SetActive (false); 
						buttonPrefab.onClick.AddListener (() => {
							MediaManager.instance.SelectMedia (imageName, MediaManager.MediaType.Image);
                            friendScreen.SetActive (false);
						});
					}
					else if (split [j].Contains ("Video_"))
					{
						Button buttonPrefab = Instantiate (mediaButton, postWrapper.bodyPanel).GetComponent<Button> ();
						string videoName = split [j].Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries) [1];
						buttonPrefab.image.sprite = MediaManager.instance.GetVideoInfo (videoName).thumbnail;
						buttonPrefab.transform.GetChild (0).gameObject.SetActive (true); 
						buttonPrefab.onClick.AddListener (() => {
							MediaManager.instance.SelectMedia (videoName, MediaManager.MediaType.Video);
                            friendScreen.SetActive (false);
						});
					}
					else
					{
						GameObject textPrefab = Instantiate (bodyText,  postWrapper.bodyPanel);
						textPrefab.GetComponentInChildren<TextMeshProUGUI> ().text = split [j];
					}
				}
				int index = i;

				if (!feed.posts [i].hasLiked)
				{
                    postWrapper.likeImage.sprite = likeSprites [0];
					postWrapper.likeButton.GetComponentInChildren<TextMeshProUGUI> ().color = defaultColor;
				}
				else
				{
                    postWrapper.likeImage.sprite = likeSprites [1];
                    postWrapper.likeButton.GetComponentInChildren<TextMeshProUGUI> ().color = likeColor;
				}
				postWrapper.likeButton.GetComponentInChildren<TextMeshProUGUI> ().text = feed.posts [i].likes.ToString ();
				postWrapper.likeButton.onClick.RemoveAllListeners ();
				postWrapper.likeButton.onClick.AddListener (() => {
					LikePost (postWrapper, index);
				});

				if (!feed.posts [i].hasCommented)
				{
                    postWrapper.commentImage.sprite = commentSprites [0];
					postWrapper.commentImage.color = defaultColor;
                    postWrapper.commentButton.GetComponentInChildren<TextMeshProUGUI> ().color = defaultColor;
				} 
				else 
				{
                    postWrapper.commentImage.sprite = commentSprites [1];
					postWrapper.commentImage.color = commentColor;
                    postWrapper.commentButton.GetComponentInChildren<TextMeshProUGUI> ().color = commentColor;
				}
                postWrapper.commentButton.GetComponentInChildren<TextMeshProUGUI> ().text = (feed.posts [i].comments.Count + feed.posts [i].ghostComments).ToString ();
				postWrapper.commentButton.onClick.RemoveAllListeners ();
				postWrapper.commentButton.onClick.AddListener (() => {
					ShowComments (index);
				});

				postWrapper.postButton.onClick.RemoveAllListeners ();
				postWrapper.postButton.onClick.AddListener (() => {
					ShowComments (index);
				});

				if (feed.posts [i] == selectedPost)
				{
					previousButtonIndex = usedPanels - 1;
				}
				generatedButtons.Add (postWrapper.postButton);
			}
		}

		for (int i = usedPanels; i < postPanels.Count; i++) 
		{
			postPanels [i].SetActive (false);
		}
		PlayerController.instance.SetSelectableButtons (generatedButtons , previousButtonIndex);
	}

	private void LikePost (PostDetailsWrapper likedPost, int postIndex)
	{
		Post selectedPost = feed.posts [postIndex];

		if (!selectedPost.hasLiked)
		{
            likedPost.likeImage.sprite = likeSprites [1];
			likedPost.likeImage.color = likeColor;
            likedPost.likeButton.GetComponentInChildren<TextMeshProUGUI> ().color = likeColor;
			selectedPost.likes++;
			selectedPost.hasLiked = true;
		} 
		else 
		{
            likedPost.likeImage.sprite = likeSprites [0];
			likedPost.likeImage.color = defaultColor;
            likedPost.likeButton.GetComponentInChildren<TextMeshProUGUI> ().color = defaultColor;
			selectedPost.likes--;
			selectedPost.hasLiked = false;
		}
        likedPost.likeButton.GetComponentInChildren<TextMeshProUGUI> ().text = selectedPost.likes.ToString ();
	}

	private void ShowComments (int postIndex)
	{
		selectedPost = feed.posts [postIndex];
		selectedPostIndex = postIndex;

		commentTitleText.text = selectedPost.displayName;
		commentTimestampText.text = selectedPost.timestamp;
			
		selectedPostWrapper.profileImage.sprite = MediaManager.instance.GetImageInfo (selectedPost.username).image;
        selectedPostWrapper.displayNameText.text = selectedPost.displayName;
		selectedPostWrapper.usernameText.text = "@" + selectedPost.username;

		DateTime postTime = DateTime.Parse (selectedPost.timestamp);
		selectedPostWrapper.timestampText.text = postTime.ToLongDateString ();

		for (int i = 0; i < selectedPostWrapper.bodyPanel.childCount; i++)
		{
			Destroy (selectedPostWrapper.bodyPanel.GetChild (i).gameObject);
		}

		List<Button> generatedButtons = new List<Button> ();

		string[] split = selectedPost.body.Split (new Char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

		for (int i = 0; i < split.Length; i++) 
		{
			if (split [i].Contains ("Image_")) 
			{
				Button buttonPrefab = Instantiate (mediaButton, selectedPostWrapper.bodyPanel).GetComponent<Button> ();
				string imageName = split [i].Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries) [1];
				buttonPrefab.image.sprite = MediaManager.instance.GetImageInfo (imageName).image;
				buttonPrefab.transform.GetChild (0).gameObject.SetActive (false); 
				buttonPrefab.onClick.AddListener (() => {
					MediaManager.instance.SelectMedia (imageName, MediaManager.MediaType.Image);
                    commentWindow.SetActive (false);
				});

				generatedButtons.Add (buttonPrefab);
			}
			else if (split [i].Contains ("Video_"))
			{
				Button buttonPrefab = Instantiate (mediaButton, selectedPostWrapper.bodyPanel).GetComponent<Button> ();
				string videoName = split [i].Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries) [1];
				buttonPrefab.image.sprite = MediaManager.instance.GetVideoInfo (videoName).thumbnail;
				buttonPrefab.transform.GetChild (0).gameObject.SetActive (true); 
				buttonPrefab.onClick.AddListener (() => {
					MediaManager.instance.SelectMedia (videoName, MediaManager.MediaType.Video);
                    commentWindow.SetActive (false);
				});

				generatedButtons.Add (buttonPrefab);
			}
			else
			{
				GameObject textPrefab = Instantiate (bodyText,  selectedPostWrapper.bodyPanel);
				textPrefab.GetComponentInChildren<TextMeshProUGUI> ().text = split [i];
			}
		}

		if (!selectedPost.hasLiked) 
		{
            selectedPostWrapper.likeImage.sprite = likeSprites [0];
            selectedPostWrapper.likeButton.GetComponentInChildren<TextMeshProUGUI> ().color = defaultColor;
		}
		else
		{
            selectedPostWrapper.likeImage.sprite = likeSprites [1];
            selectedPostWrapper.likeButton.GetComponentInChildren<TextMeshProUGUI> ().color = likeColor;
		}
        selectedPostWrapper.likeButton.GetComponentInChildren<TextMeshProUGUI> ().text = selectedPost.likes.ToString ();
		selectedPostWrapper.likeButton.onClick.RemoveAllListeners ();
		selectedPostWrapper.likeButton.onClick.AddListener (() => {
			LikePost (selectedPostWrapper, postIndex);
		});

		generatedButtons.Add (selectedPostWrapper.likeButton);

		if (!selectedPost.hasCommented) 
		{
            selectedPostWrapper.commentImage.sprite = commentSprites [0];
            selectedPostWrapper.commentButton.GetComponentInChildren<TextMeshProUGUI> ().color = defaultColor;
		}
		else
		{
            selectedPostWrapper.commentImage.sprite = commentSprites [1];
            selectedPostWrapper.commentButton.GetComponentInChildren<TextMeshProUGUI> ().color = commentColor;
		}
        selectedPostWrapper.commentButton.GetComponentInChildren<TextMeshProUGUI> ().text = (selectedPost.comments.Count + selectedPost.ghostComments).ToString ();
		selectedPostWrapper.commentButton.onClick.RemoveAllListeners ();
		selectedPostWrapper.commentButton.onClick.AddListener (() => {
			OpenKeyboard ();
		});

		generatedButtons.Add (selectedPostWrapper.commentButton);

		int usedPanels = 0;

		for (int i = 0; i < selectedPost.comments.Count; i++) 
		{
			if (selectedPost.comments [i].isPosted) 
			{
				GameObject commentPrefab;

				if (i > commentPanels.Count - 1)
				{
					commentPrefab = Instantiate (commentPanel, commentListPanel);
					commentPanels.Add (commentPrefab);
				} 
				else 
				{
					commentPrefab = commentPanels [i];
					commentPrefab.SetActive (true);
				}
				usedPanels++;

				CommentDetailsWrapper commentWrapper = commentPrefab.GetComponent<CommentDetailsWrapper> ();
				commentWrapper.profileImage.sprite = MediaManager.instance.GetImageInfo (selectedPost.comments [i].username).image;

				DateTime commentTimestamp = DateTime.Parse (selectedPost.comments [i].timestamp);
                commentWrapper.displayNameText.text = selectedPost.displayName;
				commentWrapper.usernameText.text = "@" + selectedPost.comments [i].username;
				commentWrapper.bodyText.text = selectedPost.comments [i].body;
			}
		}

		for (int i = usedPanels; i < commentPanels.Count; i++) 
		{
			commentPanels [i].SetActive (false);
		}
        onScreenKeyboard.inputField.text = "";
		onScreenKeyboard.alphabetKeyboardPanel.SetActive (false);
		onScreenKeyboard.symbolKeyboardPanel.SetActive (false);
        commentBottomBar.SetActive (true);

        if (!commentWindow.activeSelf)
        {
            commentTransform.anchoredPosition = new Vector2 (commentTransform.rect.width, commentTransform.anchoredPosition.y);
            finalWindowPosition = new Vector2 (0f, commentTransform.anchoredPosition.y);
            isOpen = true;
            isGravitating = true;
            commentWindow.SetActive (true);
        }
        CameraController.onMediaClose = OpenCommentsWindow;

		PlayerController.instance.SwitchLayer ("Menu2");
		PlayerController.instance.SetScrollableRect (commentScrollRect);
		PlayerController.instance.SetSelectableButtons (generatedButtons);
		PlayerController.instance.SetRightTriggerButton (selectedPostWrapper.commentButton);
		PlayerController.instance.SetCapacitiveButtons (commentBottomBar.GetComponentsInChildren<Button> ());
	}

    public void OpenKeyboard ()
	{
		if (!onScreenKeyboard.alphabetKeyboardPanel.activeSelf && !onScreenKeyboard.symbolKeyboardPanel.activeSelf)
		{
			PlayerController.instance.SwitchLayer ("Menu3");
			PlayerController.instance.SetNegativeButton (selectedPostWrapper.commentButton);

			OnScreenKeyboard.onEnter = PostComment;
			onScreenKeyboard.InitializeKeyboard ();
			commentBottomBar.SetActive (false);
		} 
		else if (onScreenKeyboard.alphabetKeyboardPanel.activeSelf || onScreenKeyboard.symbolKeyboardPanel.activeSelf)
		{
			OnScreenKeyboard.onEnter = null;
			onScreenKeyboard.inputField.text = "";
			onScreenKeyboard.alphabetKeyboardPanel.SetActive (false);
			onScreenKeyboard.symbolKeyboardPanel.SetActive (false);
			commentBottomBar.SetActive (true);

			PlayerController.instance.SwitchLayer ("Menu2");
		}
	}

	private void PostComment (string comment)
	{
		Comment newComment = new Comment ();
		newComment.isPosted = true;
		newComment.username = "Frida Kahlo";
		newComment.timestamp = DateTime.Now.ToString ();
		newComment.body = comment;

		Post selectedPost = feed.posts [selectedPostIndex];
		selectedPost.comments.Add (newComment);
		selectedPost.hasCommented = true;
		ShowComments (selectedPostIndex);
	}

	private void ReadFeed ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(Feed));
		string path = Path.Combine (Application.persistentDataPath, "Friends.xml");

		if (File.Exists (path))
		{
			using (FileStream stream = new FileStream (path, FileMode.Open))
			{
				feed = serializer.Deserialize (stream) as Feed;
			}
		} 
		else 
		{
			TextAsset xml = Resources.Load ("Friends") as TextAsset;
			feed = serializer.Deserialize (new StringReader (xml.text)) as Feed;
		}
	}

	private void UpdateFeed ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(Feed));
		string path = Path.Combine (Application.persistentDataPath, "Friends.xml");

		using (FileStream stream = new FileStream (path, FileMode.Create))
		{
			serializer.Serialize (stream, feed);
		}
	}

	private void FadePanels ()
	{
		if (CameraController.CurrentActivity == "Jabbr") 
		{
			if (CameraController.instance.ApplyCameraEffect ("FadeOut")) 
			{
				Camera.main.targetTexture = new RenderTexture (Camera.main.pixelWidth, Camera.main.pixelHeight, 16);
				Camera.main.Render ();
				CameraController.instance.GetAppliedCameraEffect ().effectMaterial.SetTexture ("_TargetTex", Camera.main.targetTexture);
				CameraController.instance.GetAppliedCameraEffect ().effectMaterial.SetFloat ("_NewAlpha", 0f);

				Camera.main.targetTexture = null;
				fadeTimer = 0f;
				isFading = true;
			}
		}
	}
}
