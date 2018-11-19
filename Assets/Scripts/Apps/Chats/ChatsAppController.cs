using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ChatsAppController : MonoBehaviour
{
	public static ChatsAppController instance;

	[System.Serializable]
	public class UserMessageModel
	{
		public bool is_hidden;
		public string message;
	}

	[System.Serializable]
	public class SenderMessageModel
	{
		public bool hasRead;

		public string message;
		public string origin = "sender";

		public string date_text = "auto";
		public string time_text = "auto";

		public float idle_time;
		public float typing_time;

		public string trigger;
		public float trigger_time;
	}

	[System.Serializable]
	public class ConversationModel
	{
		public string tags;
		public string condition;

		public UserMessageModel[] user_messages;
		public int selectedMessageIndex = -1;
		public string timestamp;

		public bool is_user_ignored;

		public SenderMessageModel[] sender_messages;
		public int senderMessageIndex = -1;

		public string tags_to_unlock;
		public string tags_to_lock;

		public string trigger;
	}

    [System.Serializable]
    public class ConditionTag
    {
        public string name;
        public bool isActive, isUnlocked;
    }

	[System.Serializable]
	public class DialogueTreeModel
	{
		public string identifier;

		public ConversationModel[] conversations;
		public List<ConditionTag> activatedTags = new List<ConditionTag> ();
	}

	public enum MessageType
	{
		Send,
		Receive
	};

	[System.Serializable]
	public struct OnlineStatus
	{
		public Sprite sprite;
		public Color color;
	}

	public class Choice
	{
		public string text;
		public int result;
	}

	public class Message
	{
		public bool hasRead;
		public string timestamp;
		public MessageType type;
		public int selectedChoiceIndex;
		public List<Choice> choices = new List<Choice> ();
	}

	[System.Serializable]
	public class Contact
	{
		public bool hasContact = true, isAvailable;
		public string contactName, profileImagePath, dialogueTreePath;
		public int lastMessageIndex;
	}

	[System.Serializable]
	public class Chat
	{
		public Contact contactInfo;
		public DialogueTreeModel dialogTree;
	}

    public GameObject conversationScreen, conversationBottomBar;
	public ScrollRect chatDetailsScrollRect;
    public GameObject chatButton;

	[Header ("Chat Window")]
	public GameObject chatWindow;
    public Image statusImage;
	public TextMeshProUGUI titleText, statusText;
	public GameObject senderPanel, respondentPanel, timestampPanel, newMessagePanel, messageText, messageTimeText, linkButton, mediaButton, audioButton, actionButton, messageButton;
	public ScrollRect chatScrollRect, replyScrollRect;
	public GameObject sendMessagePanel, messageOptionsPanel, chatBottomBar;
	public float lerpRate;
	public OnlineStatus[] statusSprites;
	public Sprite[] actionSprites;

	private static List<Chat> chatsList = new List<Chat> ();

	private List<GameObject> chatButtons = new List<GameObject> ();
	private List<Button> messageButtons = new List<Button> ();
	private List<ConversationModel> availableConversations = new List<ConversationModel> ();
	private Chat selectedChat;
	private RectTransform chatTransform;
	private Vector2 finalWindowPosition;
	private bool isGravitating, isOpen;

    private GraphicRaycaster m_Raycaster;
    private PointerEventData m_PointerEventData;
    private EventSystem m_EventSystem;


	void Awake ()
	{
        if (instance != this)
		{
            instance = this;
		}
		chatTransform = chatWindow.GetComponent<RectTransform> ();
	}

	void Start ()
	{
		string[] splitString = CameraController.CurrentActivity.Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

		PlayerController.instance.SwitchLayer ("Menu1");
		PlayerController.instance.SetScrollableRect (chatDetailsScrollRect);
		PlayerController.instance.SetCapacitiveButtons (conversationBottomBar.GetComponentsInChildren<Button> ());

		if (splitString.Length > 1)
		{
			OpenChat (splitString [1]);
		} 
		else
		{
			ListConversations ();
		}
        CameraController.onMediaClose = OpenChatWindow;

        //Fetch the Raycaster from the GameObject (the Canvas)
        m_Raycaster = GetComponent<GraphicRaycaster>();
        //Fetch the Event System from the Scene
        m_EventSystem = GetComponent<EventSystem>();

//		StartCoroutine (TestDownload ());
	}

	private IEnumerator TestDownload ()
	{
		using (WWW www = new WWW ("https://simtools-214211.appspot.com/sheetparser?type=game31.model.DialogueTreeModel&document=1-3MuGr89UDFQujjB3e97ILdI5NdSOUI3Pe_jybqfQjo&sheet=331108729"))
		{
			yield return www;
			Debug.Log (www.text);

			Chat newChat = new Chat ();
			newChat.contactInfo = new Contact ();
			newChat.contactInfo.contactName = "Greg 2";
			newChat.dialogTree = JsonUtility.FromJson<DialogueTreeModel> (www.text);
			Debug.Log (newChat.dialogTree.conversations [0].tags);
		}
	}

	void Update ()
	{
        if (messageOptionsPanel.activeSelf)
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

                //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
                foreach (RaycastResult result in results)
                {
                    if (result.gameObject == sendMessagePanel || result.gameObject == messageOptionsPanel)
                    {
                        hasFocus = true;
                        break;
                    }
                }

                if (!hasFocus)
                {
                    messageOptionsPanel.SetActive (false);
                    chatBottomBar.SetActive (true);

					PlayerController.instance.SwitchLayer ("Menu2");
                }
            }
        }

		if (isGravitating)
		{
			chatTransform.anchoredPosition = Vector2.Lerp (chatTransform.anchoredPosition, finalWindowPosition, lerpRate);

			if (Vector2.Distance (chatTransform.anchoredPosition, finalWindowPosition) < 1f)
			{
				if (isOpen)
				{
					conversationScreen.SetActive (false);
				}
				else 
				{
					chatWindow.SetActive (false);
				}
				chatTransform.anchoredPosition = finalWindowPosition;
				isGravitating = false;
			}
		}
	}

	public static void InitializeDatabase ()
	{
		List<Contact> storedContacts = GetContactList ();

		for (int i = 0; i < storedContacts.Count; i++)
		{
			TextAsset json = Resources.Load ("Chats/" + storedContacts [i].contactName) as TextAsset;

			if (json != null) 
			{
				Chat recordedChat = new Chat ();
				recordedChat.contactInfo = storedContacts [i];
				recordedChat.dialogTree = JsonUtility.FromJson<DialogueTreeModel> (json.text);
				chatsList.Add (recordedChat);

				ActivateTag (storedContacts [i].contactName, "main", true); 
			}
		}
	}
		
	public static void ActivateTag (string contactName, string tag, bool unlock)
	{
		for (int i = 0; i < chatsList.Count; i++) 
		{
			if (chatsList [i].contactInfo.contactName == contactName)
			{
				bool tagFound = false;

				for (int j = 0; j < chatsList [i].dialogTree.activatedTags.Count; j++) 
				{
					if (chatsList [i].dialogTree.activatedTags [j].name == tag)
					{
						chatsList [i].dialogTree.activatedTags [j].isUnlocked = unlock;
						tagFound = true;
						break;
					}
				}

				if (!tagFound)
				{
					ChatsAppController.ConditionTag newTag = new ChatsAppController.ConditionTag ();
					newTag.name = tag;
					newTag.isUnlocked = unlock;
					chatsList [i].dialogTree.activatedTags.Add (newTag);
				}

				foreach (ConversationModel conversation in chatsList [i].dialogTree.conversations)
				{
					string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					bool isIdle = false;
					int satisfiedConditions = 0;

					foreach (string conditionTag in conditionTags)
					{
						for (int j = 0; j < chatsList [i].dialogTree.activatedTags.Count; j++)
						{
							if (chatsList [i].dialogTree.activatedTags [j].name == conditionTag.Trim ())
							{
								if (chatsList [i].dialogTree.activatedTags [j].name.Contains ("!"))
								{
									if (!chatsList [i].dialogTree.activatedTags [j].isUnlocked)
									{
										satisfiedConditions++;
										break; 
									}
								}
								else if (chatsList [i].dialogTree.activatedTags [j].isUnlocked)
								{
									satisfiedConditions++;
									break;
								}

								if (chatsList [i].dialogTree.activatedTags [j].name == "IDLE")
								{
									isIdle = true;
								}
							}
						}
					}

					if (isIdle)
					{

					}
					else if (satisfiedConditions == conditionTags.Length)
					{
						if (conversation.user_messages.Length == 0) 
						{
							bool past = true;

							for (int j = 0; j < conversation.sender_messages.Length; j++)
							{
								if (conversation.sender_messages [j].idle_time != 0f && conversation.sender_messages [j].typing_time != 0f)
								{
									past = false;
									break;
								} 
								else 
								{
									conversation.sender_messages [j].hasRead = true;
								}
							}

							if (past) 
							{
								string[] unlockTags = conversation.tags_to_unlock.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
								string[] lockTags = conversation.tags_to_lock.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
								string[] activeTags = conversation.tags_to_lock.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

								for (int j = 0; j < unlockTags.Length; j++) 
								{
									bool found = false;

									for (int k = 0; k < chatsList [i].dialogTree.activatedTags.Count; k++) 
									{
										if (chatsList [i].dialogTree.activatedTags [k].name == unlockTags [j].Trim ())
										{
											chatsList [i].dialogTree.activatedTags [k].isUnlocked = true;
											found = true;
											break;
										}
									}

									if (!found)
									{
										ConditionTag newTag = new ConditionTag ();
										newTag.name = unlockTags [j].Trim ();
										newTag.isUnlocked = true;
										chatsList [i].dialogTree.activatedTags.Add (newTag);
									}
								}

								for (int j = 0; j < lockTags.Length; j++) 
								{
									bool found = false;

									for (int k = 0; k < chatsList [i].dialogTree.activatedTags.Count; k++) 
									{
										if (chatsList [i].dialogTree.activatedTags [k].name == lockTags [j].Trim ())
										{
											chatsList [i].dialogTree.activatedTags [k].isUnlocked = false;
											found = true;
											break;
										}
									}

									if (!found)
									{
										ConditionTag newTag = new ConditionTag ();
										newTag.name = lockTags [j].Trim ();
										newTag.isUnlocked = false;
										chatsList [i].dialogTree.activatedTags.Add (newTag);
									}
								}

								for (int j = 0; j < activeTags.Length; j++) 
								{
									bool found = false;

									for (int k = 0; k < chatsList [i].dialogTree.activatedTags.Count; k++) 
									{
										if (chatsList [i].dialogTree.activatedTags [k].name == activeTags [j].Trim ())
										{
											chatsList [i].dialogTree.activatedTags [k].isActive = true;
											found = true;
											break;
										}
									}

									if (!found)
									{
										ConditionTag newTag = new ConditionTag ();
										newTag.name = activeTags [j].Trim ();
										newTag.isActive = true;
										chatsList [i].dialogTree.activatedTags.Add (newTag);
									}
								}
								conversation.senderMessageIndex = conversation.sender_messages.Length - 1;
							}
						}
					}
				}

				List<ConversationModel> nextConversations = new List<ConversationModel> ();

				foreach (ConversationModel conversation in chatsList [i].dialogTree.conversations) 
				{
					string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					bool isIdle = false;
					int satisfiedConditions = 0;

					foreach (string conditionTag in conditionTags)
					{
						for (int j = 0; j < chatsList [i].dialogTree.activatedTags.Count; j++) 
						{
							if (chatsList [i].dialogTree.activatedTags [j].name == conditionTag.Trim ())
							{
								if (chatsList [i].dialogTree.activatedTags [j].name.Contains ("!")) 
								{
									if (!chatsList [i].dialogTree.activatedTags [j].isUnlocked)
									{
										satisfiedConditions++;
										break; 
									}
								} 
								else if (chatsList [i].dialogTree.activatedTags [j].isUnlocked)
								{
									satisfiedConditions++;
									break;
								}

								if (chatsList [i].dialogTree.activatedTags [j].name == "IDLE")
								{
									isIdle = true;
								}
							}
						}
					}

					if (isIdle) 
					{

					} 
					else if (satisfiedConditions == conditionTags.Length) 
					{
						if (conversation.user_messages.Length != 0 || conversation.sender_messages.Length != 0)
						{
							nextConversations.Add (conversation);
						} 
						else 
						{
							string[] unlockTags = conversation.tags_to_unlock.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
							string[] lockTags = conversation.tags_to_lock.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

							for (int j = 0; j < unlockTags.Length; j++) 
							{
								bool found = false;

								for (int k = 0; k < chatsList [i].dialogTree.activatedTags.Count; k++) 
								{
									if (chatsList [i].dialogTree.activatedTags [k].name == unlockTags [j].Trim ())
									{
										chatsList [i].dialogTree.activatedTags [k].isUnlocked = true;
										found = true;
										break;
									}
								}

								if (!found)
								{
									ConditionTag newTag = new ConditionTag ();
									newTag.name = unlockTags [j].Trim ();
									newTag.isUnlocked = true;
									chatsList [i].dialogTree.activatedTags.Add (newTag);
								}
							}

							for (int j = 0; j < lockTags.Length; j++) 
							{
								bool found = false;

								for (int k = 0; k < chatsList [i].dialogTree.activatedTags.Count; k++) 
								{
									if (chatsList [i].dialogTree.activatedTags [k].name == lockTags [j].Trim ())
									{
										chatsList [i].dialogTree.activatedTags [k].isUnlocked = false;
										found = true;
										break;
									}
								}

								if (!found)
								{
									ConditionTag newTag = new ConditionTag ();
									newTag.name = lockTags [j].Trim ();
									newTag.isUnlocked = false;
									chatsList [i].dialogTree.activatedTags.Add (newTag);
								}
							}
						}
					}
				}

				if (nextConversations.Count != 0)
				{
					foreach (ConversationModel conversation in nextConversations)
					{
						if (conversation.user_messages.Length != 0)
						{
							chatsList [i].contactInfo.isAvailable = true;

							if (ChatsAppController.instance != null)
							{
								ChatsAppController.instance.UpdateAvailability (chatsList [i].contactInfo.contactName, 1);
							}
							return;
						} 
					}
						
					foreach (ConversationModel conversation in nextConversations)
					{
						if (conversation.sender_messages.Length != 0)
						{
							InitiateConversation (chatsList [i].contactInfo.contactName);
							chatsList [i].contactInfo.isAvailable = true;

							if (ChatsAppController.instance != null) 
							{
								ChatsAppController.instance.UpdateAvailability (chatsList [i].contactInfo.contactName, 1);
							}
							return;
						}
					}
				}
				break;
			}
		}
	}

	public static void InitiateConversation (string contactName)
	{
		for (int i = 0; i < chatsList.Count; i++)
		{
			if (chatsList [i].contactInfo.contactName == contactName) 
			{
				ChatHandler handlerPrefab = Instantiate (CameraController.instance.chatHandler).GetComponent<ChatHandler> ();
				handlerPrefab.StartConversation (chatsList [i]);
				break;
			}
		}
	}

	public static void HandleEvent (string action)
	{
		string[] actionInfo = action.Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
		string methodName = actionInfo [1];
		string parameter = actionInfo [2];

		if (methodName == "InitConvo")
		{
			InitiateConversation (parameter);
		}
		else if (methodName == "AddContact")
		{
			AddContact (parameter);
		}
		else if (methodName == "SetAvailability")
		{
			SetAvailability (parameter);
		}
	}

	public void OpenChat (string selectedContactName)
	{
		DateTime chatDate = new DateTime ();
		string previousMessageOrigin = "";
		bool hasNewMessage = false;

		titleText.text = selectedContactName;

		PlayerController.instance.SwitchLayer ("Menu2");

		for (int i = 0; i < chatsList.Count; i++)
		{
			if (chatsList [i].contactInfo.contactName == selectedContactName) 
			{
				selectedChat = chatsList [i];
			}
		}

        if (selectedChat.contactInfo.isAvailable)
        {
            statusImage.sprite = statusSprites [1].sprite;
			statusImage.color = statusSprites [1].color;
            statusText.text = "Online";
        }
        else
        {
			statusImage.sprite = statusSprites [0].sprite;
			statusImage.color = statusSprites [0].color;
            statusText.text = "Offline";
        }

		for (int i = 0; i < chatScrollRect.content.childCount; i++)
		{
			Destroy (chatScrollRect.content.GetChild (i).gameObject);
		}

		List<Button> generatedButtons = new List<Button> ();

		foreach (ConversationModel conversation in selectedChat.dialogTree.conversations)
		{
			string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			int satisfiedConditions = 0;

			foreach (string conditionTag in conditionTags) 
			{
                for (int i = 0; i < selectedChat.dialogTree.activatedTags.Count; i++)
                {
                    if (selectedChat.dialogTree.activatedTags [i].name == conditionTag.Trim () && selectedChat.dialogTree.activatedTags [i].isActive)
                    {
                        satisfiedConditions++;
                        break;
                    }
                }
			}

			if (satisfiedConditions == conditionTags.Length)
			{
				MessageWrapper messageWrapper;
				DateTime messageTime = new DateTime ();

				if (conversation.user_messages.Length != 0)
				{
					Button newButton;
					messageWrapper = Instantiate (senderPanel, chatScrollRect.content).GetComponent<MessageWrapper> ();

					if (!DateTime.TryParse (conversation.timestamp, out messageTime))
					{
						messageTime = DateTime.Now;
					}

					if (messageTime.Date > chatDate.Date) 
					{
						chatDate = messageTime;

						GameObject timestampPrefab = Instantiate (timestampPanel, chatScrollRect.content);
						timestampPrefab.GetComponentInChildren<TextMeshProUGUI> ().text = chatDate.ToString ("dd MMMMM yyyy");

						previousMessageOrigin = "";
					}

					if (previousMessageOrigin == "" || previousMessageOrigin != "user") 
					{
						previousMessageOrigin = "user";
					}
					newButton = FormatMessageBubble (conversation.user_messages [conversation.selectedMessageIndex].message, "user", messageTime, messageWrapper);

					if (newButton != null) 
					{
						generatedButtons.Add (newButton);
					}
				}

				for (int i = 0; i <= conversation.senderMessageIndex; i++)
				{
					Button newButton;
					string messageTimeString = conversation.sender_messages [i].date_text + " " + conversation.sender_messages [i].time_text;

					if (!DateTime.TryParse (messageTimeString, out messageTime))
					{
						messageTime = DateTime.Now;
					}

					if (messageTime.Date > chatDate.Date) 
					{
						chatDate = messageTime;
		
						GameObject timestampPrefab = Instantiate (timestampPanel, chatScrollRect.content);
						timestampPrefab.GetComponentInChildren<TextMeshProUGUI> ().text = chatDate.ToString ("dd MMMMM yyyy");
						
						previousMessageOrigin = "";
					}

					if (!conversation.sender_messages [i].hasRead)
		            {
		                if (!hasNewMessage)
		                {
							GameObject newMessagePrefab = Instantiate (newMessagePanel, chatScrollRect.content);
		                    hasNewMessage = true;
		                }
						conversation.sender_messages [i].hasRead = true;
		            }

					if (conversation.sender_messages [i].origin == "user") 
					{
						messageWrapper = Instantiate (senderPanel, chatScrollRect.content).GetComponent<MessageWrapper> ();
					}
					else
					{
						messageWrapper = Instantiate (respondentPanel, chatScrollRect.content).GetComponent<MessageWrapper> ();
					}
		
					if (previousMessageOrigin == "" || conversation.sender_messages [i].origin != previousMessageOrigin)
					{
						previousMessageOrigin = conversation.sender_messages [i].origin;
					}
					newButton = FormatMessageBubble (conversation.sender_messages [i].message, conversation.sender_messages [i].origin, messageTime, messageWrapper);

					if (newButton != null) 
					{
						generatedButtons.Add (newButton);
					}
				}
			}
		}
        
		if (selectedChat.contactInfo.isAvailable) 
		{
			List<ConversationModel> nextConversations = new List<ConversationModel> ();

			foreach (ConversationModel conversation in selectedChat.dialogTree.conversations) 
			{
				string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				bool isIdle = false;
				int satisfiedConditions = 0;

				foreach (string conditionTag in conditionTags)
				{
					for (int i = 0; i < selectedChat.dialogTree.activatedTags.Count; i++)
					{
						if (selectedChat.dialogTree.activatedTags [i].name == conditionTag.Trim ()) 
						{
							if (selectedChat.dialogTree.activatedTags [i].name.Contains ("!")) 
							{
								if (!selectedChat.dialogTree.activatedTags [i].isUnlocked) 
								{
									satisfiedConditions++;
									break; 
								}
							}
							else if (selectedChat.dialogTree.activatedTags [i].isUnlocked) 
							{
								satisfiedConditions++;
								break;
							}

							if (selectedChat.dialogTree.activatedTags [i].name == "IDLE") 
							{
								isIdle = true;
							}
						}
					}
				}

				if (isIdle) 
				{

				}
				else if (satisfiedConditions == conditionTags.Length) 
				{
					nextConversations.Add (conversation);
				}
			}

			if (nextConversations.Count != 0)
			{
				availableConversations.Clear ();

				foreach (ConversationModel conversation in nextConversations)
				{
					if (conversation.selectedMessageIndex != -1) 
					{
						availableConversations.Clear ();
						break;
					} 
					else if (conversation.user_messages.Length != 0)
					{
						availableConversations.Add (conversation);
					} 
				}

				if (availableConversations.Count != 0) 
				{
					int conversationCounter = 0;

					foreach (ConversationModel conversation in availableConversations) 
					{
						for (int i = 0; i < conversation.user_messages.Length; i++) 
						{
							Button messagePrefab;
							int choiceNumber = conversationCounter;

							if (conversationCounter > messageButtons.Count - 1) 
							{
								messagePrefab = Instantiate (messageButton, replyScrollRect.content).GetComponent<Button> ();
								messageButtons.Add (messagePrefab);
							}
							else
							{
								messagePrefab = messageButtons [conversationCounter];
								messagePrefab.gameObject.SetActive (true);
							}
							messagePrefab.GetComponentInChildren<TextMeshProUGUI> ().text = conversation.user_messages [i].message;
							messagePrefab.onClick.RemoveAllListeners ();
							messagePrefab.onClick.AddListener (() => {
								SendMessage (choiceNumber);
							});
							conversationCounter++;
						}
					}

					for (int i = messageButtons.Count - 1; i >= conversationCounter; i--)
					{
						messageButtons [i].gameObject.SetActive (false);
					}
					sendMessagePanel.GetComponent<Button> ().interactable = true;

					PlayerController.instance.SetRightTriggerButton (sendMessagePanel.GetComponent<Button> ());
				}
				else
				{
					OpenSendMessagePanel (false);
				}
			} 
			else
			{
				OpenSendMessagePanel (false);
			}
		}
		else 
		{
			OpenSendMessagePanel (false);
		}
		messageOptionsPanel.SetActive (false);
		chatBottomBar.SetActive (true);

		chatScrollRect.velocity = new Vector2 ();
		chatScrollRect.content.anchoredPosition = new Vector2 (0f, 0f);

		chatTransform.anchoredPosition = new Vector2 (chatTransform.rect.width, chatTransform.anchoredPosition.y);
		finalWindowPosition = new Vector2 (0f, chatTransform.anchoredPosition.y);
		isOpen = true;
		isGravitating = true;
		chatWindow.SetActive (true);
		CameraController.CurrentActivity = "Chats_" + selectedContactName;

		PlayerController.instance.SetScrollableRect (chatScrollRect);
		PlayerController.instance.SetSelectableButtons (generatedButtons);
		PlayerController.instance.SetCapacitiveButtons (chatBottomBar.GetComponentsInChildren<Button> ());
	}

	public void CloseChat ()
	{
		PlayerController.instance.SwitchLayer ("Menu1");
		ListConversations ();

		finalWindowPosition = new Vector2 (chatTransform.rect.width, chatTransform.anchoredPosition.y);
		isOpen = false;
		isGravitating = true;
        conversationScreen.SetActive (true);
		CameraController.CurrentActivity = "Chats";
	}

    public void OpenChatWindow ()
    {
        chatWindow.SetActive (true);
		PlayerController.instance.SwitchLayer ("Menu2");
    }

	public void ShowMessageOptions ()
	{
		if (!messageOptionsPanel.activeSelf)
		{
			List<Button> replyButtons = new List<Button> ();
			replyScrollRect.content.GetComponentsInChildren<Button> (replyButtons);

			messageOptionsPanel.SetActive (true);
			chatBottomBar.SetActive (false);

			PlayerController.instance.SwitchLayer ("Menu3");
			PlayerController.instance.SetScrollableRect (replyScrollRect);
			PlayerController.instance.SetSelectableButtons (replyButtons);
			PlayerController.instance.SetNegativeButton (sendMessagePanel.GetComponent<Button> ());
		}
		else
		{
			messageOptionsPanel.SetActive (false);
			chatBottomBar.SetActive (true);

			PlayerController.instance.SwitchLayer ("Menu2");
		}
	}

	public void SendMessage (int number)
	{
		ConversationModel lastConversation = null;
		ConversationModel currentConversation = null;
		int counter = 0;
		int conversationNumber = 0;
		int messageNumber = 0;

		foreach (ConversationModel conversation in selectedChat.dialogTree.conversations)
		{ 
			string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			int satisfiedConditions = 0;

			foreach (string conditionTag in conditionTags) 
			{
				for (int i = 0; i < selectedChat.dialogTree.activatedTags.Count; i++) 
				{
					if (selectedChat.dialogTree.activatedTags [i].name == conditionTag.Trim () && selectedChat.dialogTree.activatedTags [i].isActive)
					{
						satisfiedConditions++;
						break;
					}
				}
			}

			if (satisfiedConditions == conditionTags.Length)
			{
				lastConversation = conversation;
			}
		}

		for (int i = 0; i < availableConversations.Count; i++) 
		{
			for (int j = 0; j < availableConversations [i].user_messages.Length; j++) 
			{
				if (counter == number)
				{
					conversationNumber = i;
					messageNumber = j;
				}
				counter++;
			}
		}
		currentConversation = availableConversations [conversationNumber];
		currentConversation.selectedMessageIndex = messageNumber;

		if (lastConversation == null)
		{
			lastConversation = currentConversation;
		}

		if (!currentConversation.is_user_ignored)
		{
			MessageWrapper messageWrapper;
			Button newButton;
			DateTime previousMessageTime = new DateTime ();
			DateTime currentMessageTime = new DateTime ();
			bool newDate = false;

			if (lastConversation.senderMessageIndex != -1) 
			{
				if (!DateTime.TryParse (lastConversation.sender_messages [lastConversation.senderMessageIndex].date_text, out previousMessageTime))
				{
					previousMessageTime = DateTime.Now;
					lastConversation.sender_messages [lastConversation.senderMessageIndex].date_text = DateTime.Now.ToLongDateString ();
					lastConversation.sender_messages [lastConversation.senderMessageIndex].time_text = DateTime.Now.ToLongTimeString ();
				}
			}
			else if (lastConversation.selectedMessageIndex != -1)
			{
				if (!DateTime.TryParse (lastConversation.timestamp, out previousMessageTime))
				{
					previousMessageTime = DateTime.Now;
					lastConversation.timestamp = DateTime.Now.ToString ();
				}
			}

			if (!DateTime.TryParse (currentConversation.timestamp, out currentMessageTime))
			{
				currentMessageTime = DateTime.Now;
				currentConversation.timestamp = DateTime.Now.ToString ();
			}

			if (currentMessageTime.Date > previousMessageTime.Date) 
			{
				GameObject timestampPrefab = Instantiate (timestampPanel, chatScrollRect.content);
				timestampPrefab.GetComponentInChildren<TextMeshProUGUI> ().text = currentMessageTime.Date.ToString ("dd MMMMM yyyy");

				newDate = true;
			}
			messageWrapper = Instantiate (senderPanel, chatScrollRect.content).GetComponent<MessageWrapper> ();
			newButton = FormatMessageBubble (currentConversation.user_messages [currentConversation.selectedMessageIndex].message, "user", currentMessageTime, messageWrapper);

			if (newButton != null) 
			{
				PlayerController.instance.AddSelectableButtons (newButton, "Menu2");
			}
		}

		string[] activeTags = currentConversation.tags.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

		for (int i = 0; i < activeTags.Length; i++) 
		{
			bool found = false;

			for (int j = 0; j < selectedChat.dialogTree.activatedTags.Count; j++) 
			{
				if (selectedChat.dialogTree.activatedTags [j].name == activeTags [i].Trim ())
				{
					selectedChat.dialogTree.activatedTags [j].isActive = true;
					found = true;
					break;
				}
			}

			if (!found)
			{
				ChatsAppController.ConditionTag newTag = new ChatsAppController.ConditionTag ();
				newTag.name = activeTags [i].Trim ();
				newTag.isActive = true;
				selectedChat.dialogTree.activatedTags.Add (newTag);
			}
		}

		if (currentConversation.sender_messages.Length == 0) 
		{
			UpdateTags (currentConversation);
		}
			
		List<ConversationModel> nextConversations = new List<ConversationModel> ();

		foreach (ConversationModel conversation in selectedChat.dialogTree.conversations) 
		{
			string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			bool isIdle = false;
			int satisfiedConditions = 0;

			foreach (string conditionTag in conditionTags)
			{
				for (int i = 0; i < selectedChat.dialogTree.activatedTags.Count; i++) 
				{
					if (selectedChat.dialogTree.activatedTags [i].name == conditionTag.Trim ())
					{
						if (selectedChat.dialogTree.activatedTags [i].name.Contains ("!")) 
						{
							if (!selectedChat.dialogTree.activatedTags [i].isUnlocked)
							{
								satisfiedConditions++;
								break; 
							}
						} 
						else if (selectedChat.dialogTree.activatedTags [i].isUnlocked)
						{
							satisfiedConditions++;
							break;
						}

						if (selectedChat.dialogTree.activatedTags [i].name == "IDLE")
						{
							isIdle = true;
						}
					}
				}
			}

			if (isIdle) 
			{

			} 
			else if (satisfiedConditions == conditionTags.Length) 
			{
				if (conversation.user_messages.Length != 0 || conversation.sender_messages.Length != 0)
				{
					nextConversations.Add (conversation);
				} 
				else 
				{
					UpdateTags (conversation);
				}
			}
		}

		if (nextConversations.Count != 0) 
		{
			availableConversations.Clear ();

			foreach (ConversationModel conversation in nextConversations) 
			{
				if (conversation.selectedMessageIndex != -1)
				{
					availableConversations.Clear ();
					break;
				} 
				else if (conversation.user_messages.Length != 0)
				{
					availableConversations.Add (conversation);
				} 
			}

			if (availableConversations.Count != 0) 
			{
				int conversationCounter = 0;

				foreach (ConversationModel conversation in availableConversations)
				{
					for (int i = 0; i < conversation.user_messages.Length; i++) 
					{
						Button messagePrefab;
						int choiceNumber = conversationCounter;

						if (conversationCounter > messageButtons.Count - 1)
						{
							messagePrefab = Instantiate (messageButton, replyScrollRect.content).GetComponent<Button> ();
							messageButtons.Add (messagePrefab);
						}
						else 
						{
							messagePrefab = messageButtons [conversationCounter];
							messagePrefab.gameObject.SetActive (true);
						}
						messagePrefab.GetComponentInChildren<TextMeshProUGUI> ().text = conversation.user_messages [i].message;
						messagePrefab.onClick.RemoveAllListeners ();
						messagePrefab.onClick.AddListener (() => {
							SendMessage (choiceNumber);
						});
						conversationCounter++;
					}
				}

				for (int i = messageButtons.Count - 1; i >= conversationCounter; i--)
				{
					messageButtons [i].gameObject.SetActive (false);
				}
				sendMessagePanel.GetComponent<Button> ().interactable = true;
			}
			else
			{
				foreach (ConversationModel conversation in nextConversations) 
				{
					if (conversation.sender_messages.Length != 0) 
					{
						InitiateConversation (selectedChat.contactInfo.contactName);
						break;
					}
				}
				PlayerController.instance.SwitchLayer ("Menu2");
				OpenSendMessagePanel (false);
			}
		}
		else
		{
			selectedChat.contactInfo.isAvailable = false;
			UpdateAvailability (selectedChat.contactInfo.contactName, 0);
			PlayerController.instance.SwitchLayer ("Menu2");
			OpenSendMessagePanel (false);
		}
	}

	public void ReceiveMessage (int number)
	{
		ConversationModel lastConversation = null;
		ConversationModel currentConversation = null;
		Button newButton;
		string previousMessageOrigin = "";

		List<ConversationModel> currentConversations = new List<ConversationModel> ();

		foreach (ConversationModel conversation in selectedChat.dialogTree.conversations)
		{
			string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			bool isIdle = false;
			int satisfiedConditions = 0;

			foreach (string conditionTag in conditionTags) 
			{
				for (int i = 0; i < selectedChat.dialogTree.activatedTags.Count; i++)
				{
					if (selectedChat.dialogTree.activatedTags [i].name == conditionTag.Trim ()) 
					{
						if (selectedChat.dialogTree.activatedTags [i].name.Contains ("!")) 
						{
							if (!selectedChat.dialogTree.activatedTags [i].isUnlocked)
							{
								satisfiedConditions++;
								break; 
							}
						} 
						else if (selectedChat.dialogTree.activatedTags [i].isUnlocked)
						{
							satisfiedConditions++;
							break;
						}

						if (selectedChat.dialogTree.activatedTags [i].name == "IDLE")
						{
							isIdle = true;
						}
					}
				}
			}

			if (isIdle)
			{

			}
			else if (satisfiedConditions == conditionTags.Length) 
			{
				currentConversations.Add (conversation);
			}
		}

		if (currentConversations.Count != 0) 
		{
			foreach (ConversationModel conversation in currentConversations)
			{
				if (conversation.user_messages.Length != 0)
				{
					if (conversation.selectedMessageIndex != -1 && conversation.sender_messages.Length != 0)
					{
						currentConversation = conversation;
						break;
					}
				}
				else if (conversation.sender_messages.Length != 0)
				{
					currentConversation = conversation;
					break;
				}
			}
		}

		if (number == 0)
		{
			foreach (ConversationModel conversation in selectedChat.dialogTree.conversations)
			{ 
				string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				int satisfiedConditions = 0;

				foreach (string conditionTag in conditionTags)
				{
					for (int i = 0; i < selectedChat.dialogTree.activatedTags.Count; i++) 
					{
						if (selectedChat.dialogTree.activatedTags [i].name == conditionTag.Trim () && selectedChat.dialogTree.activatedTags [i].isActive)
						{
							satisfiedConditions++;
							break;
						}
					}
				}

				if (satisfiedConditions == conditionTags.Length)
				{
					lastConversation = conversation;
				}
			}

			if (lastConversation == null) 
			{
				lastConversation = currentConversation;
			}
		}
		else
		{
			lastConversation = currentConversation;
		}

		MessageWrapper messageWrapper;
		DateTime previousMessageTime = new DateTime ();
		DateTime currentMessageTime = new DateTime ();
		bool newDate = false;

		if (lastConversation.sender_messages.Length != 0 && lastConversation.senderMessageIndex != -1)
		{
			if (number == 0) 
			{
				if (!DateTime.TryParse (lastConversation.sender_messages [lastConversation.senderMessageIndex].date_text, out previousMessageTime))
				{
					previousMessageTime = DateTime.Now;
					lastConversation.sender_messages [lastConversation.senderMessageIndex].date_text = DateTime.Now.ToLongDateString ();
					lastConversation.sender_messages [lastConversation.senderMessageIndex].time_text = DateTime.Now.ToLongTimeString ();
				}
			}
			else
			{
				if (!DateTime.TryParse (lastConversation.sender_messages [number - 1].date_text, out previousMessageTime))
				{
					previousMessageTime = DateTime.Now;
					lastConversation.sender_messages [number - 1].date_text = DateTime.Now.ToLongDateString ();
					lastConversation.sender_messages [number - 1].time_text = DateTime.Now.ToLongTimeString ();
				}
			}
		} 
		else if (lastConversation.user_messages.Length != 0 && lastConversation.selectedMessageIndex != -1) 
		{
			if (!DateTime.TryParse (lastConversation.timestamp, out previousMessageTime))
			{
				previousMessageTime = DateTime.Now;
				lastConversation.timestamp = DateTime.Now.ToString ();
			}
		}

		string currentTimeString = currentConversation.sender_messages [number].date_text + " " + currentConversation.sender_messages [number].time_text;

		if (!DateTime.TryParse (currentTimeString, out currentMessageTime))
		{
			currentMessageTime = DateTime.Now;
			currentConversation.sender_messages [number].date_text = DateTime.Now.ToLongDateString ();
			currentConversation.sender_messages [number].time_text = DateTime.Now.ToLongTimeString ();
		}

		if (currentMessageTime.Date > previousMessageTime.Date) 
		{
			GameObject timestampPrefab = Instantiate (timestampPanel, chatScrollRect.content);
			timestampPrefab.GetComponentInChildren<TextMeshProUGUI> ().text = currentMessageTime.Date.ToString ("dd MMMMM yyyy");

			newDate = true;
		}

		if (currentConversation.sender_messages [number].origin == "user") 
		{
			messageWrapper = Instantiate (senderPanel, chatScrollRect.content).GetComponent<MessageWrapper> ();
		}
		else
		{
			messageWrapper = Instantiate (respondentPanel, chatScrollRect.content).GetComponent<MessageWrapper> ();
		}
		newButton = FormatMessageBubble (currentConversation.sender_messages [number].message, currentConversation.sender_messages [number].origin, currentMessageTime, messageWrapper);

		if (newButton != null) 
		{
			PlayerController.instance.AddSelectableButtons (newButton, "Menu2");
		}

		if (!currentConversation.sender_messages [number].hasRead)
		{
			currentConversation.sender_messages [number].hasRead = true;
		}

		if (selectedChat.contactInfo.isAvailable)
		{
			statusText.text = "Online";
		} 
		else
		{
			statusText.text = "Offline";
		}

		if (number == currentConversation.sender_messages.Length - 1) 
		{
			UpdateTags (currentConversation);

			List<ConversationModel> nextConversations = new List<ConversationModel> ();

			foreach (ConversationModel conversation in selectedChat.dialogTree.conversations) 
			{
				string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				bool isIdle = false;
				int satisfiedConditions = 0;

				foreach (string conditionTag in conditionTags)
				{
					for (int i = 0; i < selectedChat.dialogTree.activatedTags.Count; i++)
					{
						if (selectedChat.dialogTree.activatedTags [i].name == conditionTag.Trim ()) 
						{
							if (selectedChat.dialogTree.activatedTags [i].name.Contains ("!"))
							{
								if (!selectedChat.dialogTree.activatedTags [i].isUnlocked)
								{
									satisfiedConditions++;
									break; 
								}
							}
							else if (selectedChat.dialogTree.activatedTags [i].isUnlocked) 
							{
								satisfiedConditions++;
								break;
							}

							if (selectedChat.dialogTree.activatedTags [i].name == "IDLE")
							{
								isIdle = true;
							}
						}
					}
				}

				if (isIdle) 
				{

				}
				else if (satisfiedConditions == conditionTags.Length) 
				{
					if (conversation.user_messages.Length != 0 || conversation.sender_messages.Length != 0)
					{
						nextConversations.Add (conversation);
					} 
					else
					{
						UpdateTags (conversation);
					}
				}
			}

			if (nextConversations.Count != 0)
			{
				availableConversations.Clear ();

				foreach (ConversationModel conversation in nextConversations)
				{
					if (conversation.selectedMessageIndex != -1) 
					{
						availableConversations.Clear ();
						break;
					} 
					else if (conversation.user_messages.Length != 0) 
					{
						availableConversations.Add (conversation);
					} 
				}

				if (availableConversations.Count != 0) 
				{
					int conversationCounter = 0;

					foreach (ConversationModel conversation in availableConversations)
					{
						for (int i = 0; i < conversation.user_messages.Length; i++) 
						{
							Button messagePrefab;
							int choiceNumber = conversationCounter;

							if (conversationCounter > messageButtons.Count - 1)
							{
								messagePrefab = Instantiate (messageButton, replyScrollRect.content).GetComponent<Button> ();
								messageButtons.Add (messagePrefab);
							} 
							else
							{
								messagePrefab = messageButtons [conversationCounter];
								messagePrefab.gameObject.SetActive (true);
							}
							messagePrefab.GetComponentInChildren<TextMeshProUGUI> ().text = conversation.user_messages [i].message;
							messagePrefab.onClick.RemoveAllListeners ();
							messagePrefab.onClick.AddListener (() => {
								SendMessage (choiceNumber);
							});
							conversationCounter++;
						}
					}

					for (int i = messageButtons.Count - 1; i >= conversationCounter; i--) 
					{
						messageButtons [i].gameObject.SetActive (false);
					}
					sendMessagePanel.GetComponent<Button> ().interactable = true;

					PlayerController.instance.SetRightTriggerButton (sendMessagePanel.GetComponent<Button> ());
				} 
				else 
				{
					OpenSendMessagePanel (false);
				}
			} 
			else 
			{
				OpenSendMessagePanel (false);
			}
		}
		else 
		{
			OpenSendMessagePanel (false);
		}
	}

	public void UpdateAvailability (string contactName, int availability)
	{
		if (CameraController.CurrentActivity == "Chats")
		{
			for (int i = 0; i < chatDetailsScrollRect.content.childCount; i++)
			{
				ChatDetailsWrapper chatWrapper = chatDetailsScrollRect.content.GetChild (i).GetComponent<ChatDetailsWrapper> ();

				if (chatWrapper.contactNameText.text == contactName && chatWrapper.notificationButton.GetComponentInChildren<Text> ().text == "0") 
				{
					chatWrapper.statusImage.sprite = chatWrapper.statusSprites [availability].sprite;
					chatWrapper.statusImage.color = chatWrapper.statusSprites [availability].color;
					break;
				}
			}
		}
		else if (CameraController.CurrentActivity == "Chats_" + contactName)
		{
			statusImage.sprite = statusSprites [availability].sprite;
			statusImage.color = statusSprites [availability].color;

			if (availability == 1) 
			{
				statusText.text = "Online";
			} 
			else 
			{
				statusText.text = "Offline";
			}
		}
	}

	private void ListConversations ()
	{
		List<Button> generatedButtons = new List<Button> ();
		int previousButtonIndex = 0;
		int usedButtons = 0;

		for (int i = 0; i < chatsList.Count; i++)
		{
			if (chatsList [i].contactInfo.hasContact)
			{
				GameObject buttonPrefab;

				if (usedButtons > chatButtons.Count - 1)
				{
					buttonPrefab = Instantiate (chatButton, chatDetailsScrollRect.content);
					chatButtons.Add (buttonPrefab);
				} 
				else 
				{
					buttonPrefab = chatButtons [usedButtons];
					buttonPrefab.SetActive (true);
				}
				usedButtons++;

				ChatDetailsWrapper chatWrapper = buttonPrefab.GetComponent<ChatDetailsWrapper> ();
				chatWrapper.profileImage.sprite = MediaManager.instance.GetImageInfo (chatsList [i].contactInfo.contactName).image;
				chatWrapper.contactNameText.text = chatsList [i].contactInfo.contactName;

				if (chatsList [i].dialogTree.activatedTags.Count != 0) 
				{
					ConversationModel lastConversation = new ConversationModel ();

					foreach (ConversationModel conversation in chatsList [i].dialogTree.conversations)
					{ 
						string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
						int satisfiedConditions = 0;

                        foreach (string conditionTag in conditionTags) 
						{
							for (int j = 0; j < chatsList [i].dialogTree.activatedTags.Count; j++) 
							{
								if (chatsList [i].dialogTree.activatedTags [j].name == conditionTag.Trim () && chatsList [i].dialogTree.activatedTags [j].isActive)
								{
									satisfiedConditions++;
									break;
								}
							}
						}

						if (satisfiedConditions == conditionTags.Length)
						{
							lastConversation = conversation;
						}
					}
					string lastConversationExcerpt = "";
					string lastConversationTimestamp = "";

					if (lastConversation.senderMessageIndex != -1) 
					{
						lastConversationExcerpt = CameraController.FilterString (lastConversation.sender_messages [lastConversation.senderMessageIndex].message, 40);

						DateTime messageTimestamp = new DateTime ();
						string messageTimeString = lastConversation.sender_messages [lastConversation.senderMessageIndex].date_text + " " + lastConversation.sender_messages [lastConversation.senderMessageIndex].time_text;

						if (!DateTime.TryParse (messageTimeString, out messageTimestamp))
						{
							messageTimestamp = DateTime.Now;
							lastConversation.sender_messages [lastConversation.senderMessageIndex].date_text = DateTime.Now.ToLongDateString ();
							lastConversation.sender_messages [lastConversation.senderMessageIndex].time_text = DateTime.Now.ToLongTimeString ();
						}
						lastConversationTimestamp = messageTimestamp.ToString ("hh:mm tt");
					}
					else if (lastConversation.selectedMessageIndex != -1) 
					{
						lastConversationExcerpt = CameraController.FilterString (lastConversation.user_messages [lastConversation.selectedMessageIndex].message, 40);

						DateTime messageTimestamp = new DateTime ();

						if (!DateTime.TryParse (lastConversation.timestamp, out messageTimestamp))
						{
							messageTimestamp = DateTime.Now;
							lastConversation.timestamp = DateTime.Now.ToString ();
						}
						lastConversationTimestamp = messageTimestamp.ToString ("hh:mm tt");
					}
					else
					{
						lastConversationExcerpt = "";
						lastConversationTimestamp = "";
					}
					chatWrapper.chatExcerptText.text = lastConversationExcerpt;
					chatWrapper.timeText.text = lastConversationTimestamp;
				}
				else 
				{
					chatWrapper.chatExcerptText.text = "";
					chatWrapper.timeText.text = "";
				}
				int unreadMessages = 0;
				int nextMessageIndex = 0;

				foreach (ConversationModel conversation in chatsList [i].dialogTree.conversations)
				{ 
					string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					int satisfiedConditions = 0;

					foreach (string conditionTag in conditionTags) 
					{
						for (int j = 0; j < chatsList [i].dialogTree.activatedTags.Count; j++) 
						{
							if (chatsList [i].dialogTree.activatedTags [j].name == conditionTag.Trim () && chatsList [i].dialogTree.activatedTags [j].isActive)
							{
								satisfiedConditions++;
								break;
							}
						}
					}

					if (satisfiedConditions == conditionTags.Length)
					{
						for (int j = 0; j <= conversation.senderMessageIndex; j++)
						{
							if (!conversation.sender_messages [j].hasRead)
							{
								unreadMessages++;
							}
						}
					}
				}

                if (unreadMessages > 0)
                {
                    chatWrapper.statusImage.gameObject.SetActive (false);
                    chatWrapper.notificationButton.GetComponentInChildren<TextMeshProUGUI> ().text = unreadMessages.ToString ();
                    chatWrapper.notificationButton.SetActive (true);
                }
                else if (chatsList [i].contactInfo.isAvailable)
                {
					chatWrapper.statusImage.sprite = chatWrapper.statusSprites [1].sprite;
					chatWrapper.statusImage.color = chatWrapper.statusSprites [1].color;
                    chatWrapper.statusImage.gameObject.SetActive (true);
                    chatWrapper.notificationButton.GetComponentInChildren<TextMeshProUGUI> ().text = "0";
                    chatWrapper.notificationButton.SetActive (false);
                }
                else
                {
					chatWrapper.statusImage.sprite = chatWrapper.statusSprites [0].sprite;
					chatWrapper.statusImage.color = chatWrapper.statusSprites [0].color;
                    chatWrapper.statusImage.gameObject.SetActive (true);
                    chatWrapper.notificationButton.GetComponentInChildren<TextMeshProUGUI> ().text = "0";
                    chatWrapper.notificationButton.SetActive (false); 
                }
				string contactName = chatsList [i].contactInfo.contactName;

                chatWrapper.detailsButton.onClick.RemoveAllListeners ();
				chatWrapper.detailsButton.onClick.AddListener (() => {
					OpenChat (contactName);
				});

				if (chatsList [i] == selectedChat) 
				{
					previousButtonIndex = usedButtons - 1;
				}
				generatedButtons.Add (chatWrapper.detailsButton);
			}
		}

		for (int i = usedButtons; i < chatButtons.Count; i++) 
		{
			chatButtons [i].SetActive (false);
		}
		PlayerController.instance.SetSelectableButtons (generatedButtons, previousButtonIndex);
	}

	private Button FormatMessageBubble (string message, string origin, DateTime messageTime, MessageWrapper messageBubble)
	{
		string[] split = Regex.Split (message, @"(<link=[^<>]+>.*</link>)|<(image=[^<>]+|video=[^<>]+|audio=[^<>]+)>|<\w+time=[^<>]+>");
		Button newButton = null;

		for (int i = 0; i < split.Length; i++) 
		{
			if (split [i] != String.Empty) 
			{
				if (split [i].Contains ("link="))
				{
					LinkButtonWrapper linkButtonWrapper = Instantiate (linkButton, messageBubble.messagePanel).GetComponent<LinkButtonWrapper> ();
					string[] linkInfo = split [i].Split (new Char[] {'<', '>'}, StringSplitOptions.RemoveEmptyEntries);

					for (int j = 0; j < linkInfo.Length; j++) 
					{
						if (linkInfo [j].Contains ("link="))
						{
							string address = linkInfo [j].Split (new Char[] { '=' }, StringSplitOptions.RemoveEmptyEntries) [1];
							linkButtonWrapper.GetComponent<Button> ().onClick.AddListener (() => {
								CameraController.instance.OpenLink (address);
							});
						}
						else if (linkInfo [j].Contains ("image="))
						{
							string imageName = linkInfo [j].Split (new Char[] { '=' }, StringSplitOptions.RemoveEmptyEntries) [1];
							linkButtonWrapper.linkImage.sprite = MediaManager.instance.GetImageInfo (imageName).image;
							linkButtonWrapper.linkImage.gameObject.SetActive (true);
						}
						else if (!linkInfo [j].Contains ("/link"))
						{
							linkButtonWrapper.linkText.text = linkInfo [j];
							linkButtonWrapper.linkText.gameObject.SetActive (true);
						}
					}
					newButton = linkButtonWrapper.GetComponent<Button> ();
				}
				else if (split [i].Contains ("photoroll://")) 
				{
					Button buttonPrefab = Instantiate (mediaButton, messageBubble.messagePanel).GetComponent<Button> ();
//					string imageName = split [i].Split (new Char[] { '=' }, StringSplitOptions.RemoveEmptyEntries) [1];
//					buttonPrefab.image.sprite = MediaManager.instance.GetImageInfo (imageName).image;
//					buttonPrefab.transform.GetChild (0).gameObject.SetActive (false); 
//					buttonPrefab.onClick.AddListener (() => {
//						MediaManager.instance.SelectMedia (imageName, MediaManager.MediaType.Image);
//						chatWindow.SetActive (false);
//					});

					Button actionButtonPrefab = Instantiate (actionButton, messageBubble.transform).GetComponent<Button> ();
					actionButtonPrefab.image.sprite = actionSprites [0];
//					actionButtonPrefab.onClick.AddListener (() => {
//						MediaManager.instance.SelectMedia (imageName, MediaManager.MediaType.Image);
//						chatWindow.SetActive (false);
//					});
					newButton = buttonPrefab;
				}
				else if (split [i].Contains ("video="))
				{
					Button buttonPrefab = Instantiate (mediaButton, messageBubble.messagePanel).GetComponent<Button> ();
					string videoName = split [i].Split (new Char[] { '=' }, StringSplitOptions.RemoveEmptyEntries) [1];
					buttonPrefab.image.sprite = MediaManager.instance.GetVideoInfo (videoName).thumbnail;
					buttonPrefab.transform.GetChild (0).gameObject.SetActive (true); 
					buttonPrefab.onClick.AddListener (() => {
						MediaManager.instance.SelectMedia (videoName, MediaManager.MediaType.Video);
						chatWindow.SetActive (false);
					});

					Button actionButtonPrefab = Instantiate (actionButton, messageBubble.transform).GetComponent<Button> ();
					actionButtonPrefab.image.sprite = actionSprites [1];
					actionButtonPrefab.onClick.AddListener (() => {
						MediaManager.instance.SelectMedia (videoName, MediaManager.MediaType.Video);
						chatWindow.SetActive (false);
					});
					newButton = buttonPrefab;
				}
				else if (split [i].Contains ("audio="))
				{
					AudioButtonWrapper audioButtonWrapper = Instantiate (audioButton, messageBubble.messagePanel).GetComponent<AudioButtonWrapper> ();
                    string audioName = split [i].Split (new Char[] { '=' }, StringSplitOptions.RemoveEmptyEntries) [1];
					audioButtonWrapper.InitializePlayback (MediaManager.instance.GetAudioInfo (audioName));

					Button actionButtonPrefab = Instantiate (actionButton, messageBubble.transform).GetComponent<Button> ();
					actionButtonPrefab.image.sprite = actionSprites [2];
					actionButtonPrefab.onClick.AddListener (() => {
						audioButtonWrapper.PlayAudio ();
					});
					newButton = audioButtonWrapper.playButton;
				}
				else
				{
					GameObject textPrefab = Instantiate (messageText, messageBubble.messagePanel);
					textPrefab.GetComponent<TextMeshProUGUI> ().text = split [i];

					if (origin == "user") 
					{
						textPrefab.GetComponent<TextMeshProUGUI> ().color = Color.black;
					}
				}
				GameObject messageTimePrefab = Instantiate (messageTimeText, messageBubble.messagePanel);
				messageTimePrefab.GetComponent<TextMeshProUGUI> ().text = messageTime.ToString ("hh:mm tt");
			}
		}
		return newButton;
	}

	private void UpdateTags (ConversationModel currentConversation)
	{
		string[] unlockTags = currentConversation.tags_to_unlock.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		string[] lockTags = currentConversation.tags_to_lock.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

		for (int i = 0; i < unlockTags.Length; i++) 
		{
			bool found = false;

			for (int j = 0; j < selectedChat.dialogTree.activatedTags.Count; j++) 
			{
				if (selectedChat.dialogTree.activatedTags [j].name == unlockTags [i].Trim ())
				{
					selectedChat.dialogTree.activatedTags [j].isUnlocked = true;
					found = true;
					break;
				}
			}

			if (!found)
			{
				ConditionTag newTag = new ConditionTag ();
				newTag.name = unlockTags [i].Trim ();
				newTag.isUnlocked = true;
				selectedChat.dialogTree.activatedTags.Add (newTag);
			}
		}

		for (int i = 0; i < lockTags.Length; i++) 
		{
			bool found = false;

			for (int j = 0; j < selectedChat.dialogTree.activatedTags.Count; j++) 
			{
				if (selectedChat.dialogTree.activatedTags [j].name == lockTags [i].Trim ())
				{
					selectedChat.dialogTree.activatedTags [j].isUnlocked = false;
					found = true;
					break;
				}
			}

			if (!found)
			{
				ConditionTag newTag = new ConditionTag ();
				newTag.name = lockTags [i].Trim ();
				newTag.isUnlocked = false;
				selectedChat.dialogTree.activatedTags.Add (newTag);
			}
		}
	}

	private void OpenSendMessagePanel (bool open)
	{
		sendMessagePanel.GetComponent<Button> ().interactable = open;
		messageOptionsPanel.SetActive (open);
		chatBottomBar.SetActive (!open);

		if (open)
		{
			PlayerController.instance.SetRightTriggerButton (sendMessagePanel.GetComponent<Button> ());
		} 
		else
		{
			PlayerController.instance.SetRightTriggerButton (null);
		}
	}

	private static void AddContact (string name)
	{
		for (int i = 0; i < chatsList.Count; i++) 
		{
			if (chatsList [i].contactInfo.contactName == name) 
			{
				chatsList [i].contactInfo.hasContact = true;
			}
		}
	}

	private static void SetAvailability (string name)
	{
		for (int i = 0; i < chatsList.Count; i++) 
		{
			if (chatsList [i].contactInfo.contactName == name) 
			{
				chatsList [i].contactInfo.isAvailable = true;
			}
		}
	}

	private static List<Contact> GetContactList ()
	{
//		XmlSerializer serializer = new XmlSerializer (typeof(Contacts));
//		string path = Path.Combine (Application.persistentDataPath, "Chats.xml");
//
//		if (File.Exists (path))
//		{
//			using (FileStream stream = new FileStream (path, FileMode.Open))
//			{
//				return serializer.Deserialize (stream) as Contacts;
//			}
//		} 
//		else 
//		{
//			TextAsset xml = Resources.Load ("Chats/Chats") as TextAsset;
//			return serializer.Deserialize (new StringReader (xml.text)) as Contacts;
//		}
		List<Contact> contactList = new List<Contact> ();
		TextAsset csv = Resources.Load ("Chats/" + "Chat Info") as TextAsset;

		if (csv != null) 
		{
			string[] splitText = csv.text.Split (new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 1; i < splitText.Length; i++)
			{
				string[] contactInfo = splitText [i].Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				Contact contact = new Contact ();
				contact.contactName = contactInfo [0];
				int.TryParse (contactInfo [1], out contact.lastMessageIndex);

				contactList.Add (contact);
			}
		}
		return contactList;
	}

	private static void UpdateContact (Contact updatedContactInfo)
	{
//		XmlSerializer serializer = new XmlSerializer (typeof(Contacts));
//		string path = Path.Combine (Application.persistentDataPath, "Chats.xml");
//
//		using (FileStream stream = new FileStream (path, FileMode.Create))
//		{
//			serializer.Serialize (stream, chats);
//		}
	}

	private static List<Message> GetChatMessages (string contactName)
	{
//		XmlSerializer serializer = new XmlSerializer (typeof(ChatHistory));
//		string path = Path.Combine (Application.persistentDataPath, contactName + ".xml");
//
//		if (File.Exists (path))
//		{
//			using (FileStream stream = new FileStream (path, FileMode.Open))
//			{
//				return serializer.Deserialize (stream) as ChatHistory;
//			}
//		} 
//		else 
//		{
//			TextAsset xml = Resources.Load ("Chats/" + contactName) as TextAsset;
//			return serializer.Deserialize (new StringReader (xml.text)) as ChatHistory;
//		}

		TextAsset csv = Resources.Load ("Chats/" + contactName) as TextAsset;
		string replacedText = Regex.Replace (csv.text, @"""""", "<d_quotation>");
        replacedText = Regex.Replace (replacedText, @"(?s)(?<=<body>[^""]*)\n(?=[^""]*</body>)", "<newline>");
		string[] splitText = replacedText.Split (new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

		List<Message> chatMessages = new List<Message> ();

		for (int i = 1; i < splitText.Length; i++)
		{
            string messageString = Regex.Replace (splitText [i], @"(?<=<body>[^""]*),(?=[^""]*</body>)", "<comma>");
			string[] messageInfo = messageString.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			Message message = new Message ();
			message.timestamp = messageInfo [0];
			message.type = (MessageType)Enum.Parse (typeof (MessageType), messageInfo [1]);

			string choiceString = Regex.Replace (messageInfo [2], @"(?<=<body>)?<newline>(?=</body>)?", "");
            choiceString = choiceString.Replace ("\r", "");
			choiceString = choiceString.Replace ("\"", "");
			choiceString = choiceString.Replace ("<d_quotation>", "\"");
			choiceString = choiceString.Replace ("<comma>", ",");
			string[] choices = Regex.Split (choiceString, @"</?body>");

			for (int j = 0; j < choices.Length; j++)
			{
				if (choices [j] != String.Empty)
				{
					Choice choice = new Choice ();
					string[] choiceInfo = choices [j].Split (new String[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
					choice.text = choiceInfo [0];
					int.TryParse (choiceInfo [1], out choice.result);

					message.choices.Add (choice);
				}
			}
			chatMessages.Add (message);
		}
		return chatMessages;
	}

	private static void UpdateChatMessages (string contactName, List<Message> updatedChatMessages)
	{
//		XmlSerializer serializer = new XmlSerializer (typeof(ChatHistory));
//		string path = Path.Combine (Application.persistentDataPath, contactName + ".xml");
//
//		using (FileStream stream = new FileStream (path, FileMode.Create))
//		{
//			serializer.Serialize (stream, updatedChatHistory);
//		}
	}
}
