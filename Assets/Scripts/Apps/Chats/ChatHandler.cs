using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatHandler : MonoBehaviour
{
    public enum BotState
    {
        Idle,
        Thinking,
        Typing
    }

	[System.Serializable]
	public struct TypingSpeed
	{
		public int maxCharacters;
		public float typingTime;
	}

	public ChatsAppController.Chat participatingChat;
    public float minWait, maxWait;
	public TypingSpeed[] typingSpeeds;
    public float typingTimeVariance;

    private BotState currentBotState;
	private ChatsAppController.ConversationModel lastConversation, currentConversation;
	private ChatDetailsWrapper handlingChatWrapper;
	private MessageWrapper handlingMessageWrapper;
    private float thinkingTimer, currentThinkingTime, typingTimer, currentTypingTime;
	private int currentMessageIndex;


    void Awake ()
    {
        DontDestroyOnLoad (gameObject);
    }

    void Update ()
    {
        if (currentBotState == BotState.Thinking)
        {
            thinkingTimer += Time.deltaTime;

            if (thinkingTimer > currentThinkingTime)
            {
                currentBotState = BotState.Typing;
            }
        }
        else if (currentBotState == BotState.Typing)
        {
            typingTimer += Time.deltaTime;

			if (typingTimer > currentTypingTime)
			{
				if (CameraController.CurrentActivity == "Chats")
				{
					if (handlingChatWrapper == null) 
					{
						for (int i = 0; i < ChatsAppController.instance.chatDetailsScrollRect.content.childCount; i++)
						{
							ChatDetailsWrapper chatWrapper = ChatsAppController.instance.chatDetailsScrollRect.content.GetChild (i).GetComponent<ChatDetailsWrapper> ();

							if (chatWrapper.contactNameText.text == participatingChat.contactInfo.contactName) 
							{
								handlingChatWrapper = chatWrapper;
								chatWrapper.chatExcerptText.text = CameraController.FilterString (currentConversation.sender_messages [currentMessageIndex].message, 40);
								break;
							}
						}
					}
					else 
					{
						handlingChatWrapper.chatExcerptText.text = CameraController.FilterString (currentConversation.sender_messages [currentMessageIndex].message, 40);
					}
					int unreadMessages = 0;
					int.TryParse (handlingChatWrapper.notificationButton.GetComponentInChildren<TextMeshProUGUI> ().text, out unreadMessages);
					unreadMessages++;

                    handlingChatWrapper.statusImage.gameObject.SetActive (false);
					handlingChatWrapper.notificationButton.GetComponentInChildren<TextMeshProUGUI> ().text = unreadMessages.ToString ();
					handlingChatWrapper.notificationButton.SetActive (true);

					DateTime messageTimestamp = new DateTime ();
					string messageTimeString = currentConversation.sender_messages [currentMessageIndex].date_text + " " + currentConversation.sender_messages [currentMessageIndex].time_text;

					if (!DateTime.TryParse (messageTimeString, out messageTimestamp))
					{
						messageTimestamp = DateTime.Now;
						currentConversation.sender_messages [currentMessageIndex].date_text = DateTime.Now.ToLongDateString ();
						currentConversation.sender_messages [currentMessageIndex].time_text = DateTime.Now.ToLongTimeString ();
					}
					handlingChatWrapper.timeText.text = messageTimestamp.ToShortTimeString ();

					if (currentMessageIndex == currentConversation.sender_messages.Length - 1) 
					{
						UpdateTags ();
					}
					CameraController.instance.ScheduleNotification ("Chats_" + participatingChat.contactInfo.contactName, currentConversation.sender_messages [currentMessageIndex].message);
				} 
				else if (CameraController.CurrentActivity == "Chats_" + participatingChat.contactInfo.contactName) 
				{
					if (handlingMessageWrapper != null) 
					{
						Destroy (handlingMessageWrapper.gameObject);
					}
					ChatsAppController.instance.ReceiveMessage (currentMessageIndex);
				}
				else
				{
					if (currentMessageIndex == currentConversation.sender_messages.Length - 1) 
					{
						UpdateTags ();
					}
					CameraController.instance.ScheduleNotification ("Chats_" + participatingChat.contactInfo.contactName, currentConversation.sender_messages [currentMessageIndex].message);
				}
				currentBotState = BotState.Idle;
				ContinueConversation ();
			}
			else if (ChatsAppController.instance != null)
			{
				if (CameraController.CurrentActivity == "Chats") 
				{
					if (handlingChatWrapper == null) 
					{
						for (int i = 0; i < ChatsAppController.instance.chatDetailsScrollRect.content.childCount; i++)
						{
							ChatDetailsWrapper chatWrapper = ChatsAppController.instance.chatDetailsScrollRect.content.GetChild (i).GetComponent<ChatDetailsWrapper> ();

							if (chatWrapper.contactNameText.text == participatingChat.contactInfo.contactName) 
							{
								handlingChatWrapper = chatWrapper;
								handlingChatWrapper.chatExcerptText.text = "<color=#FFD902>" + participatingChat.contactInfo.contactName + " is typing..." + "</color>";
								break;
							}
						}
					}
					else 
					{
						handlingChatWrapper.chatExcerptText.text = "<color=#FFD902>" + participatingChat.contactInfo.contactName + " is typing..." + "</color>";
					}
				}
				else if (CameraController.CurrentActivity == "Chats_" + participatingChat.contactInfo.contactName) 
				{
					if (handlingMessageWrapper == null) 
					{
						if (currentConversation.sender_messages [currentMessageIndex].origin != "user") 
						{
							handlingMessageWrapper = Instantiate (ChatsAppController.instance.respondentPanel, ChatsAppController.instance.chatScrollRect.content).GetComponent<MessageWrapper> ();
                            ChatsAppController.instance.statusText.text = "<color=#FFD902>" + participatingChat.contactInfo.contactName + " is typing..." + "</color>";
						}
						else
						{
							handlingMessageWrapper = Instantiate (ChatsAppController.instance.senderPanel, ChatsAppController.instance.chatScrollRect.content).GetComponent<MessageWrapper> ();
						}

						GameObject textPrefab = Instantiate (ChatsAppController.instance.messageText, handlingMessageWrapper.messagePanel);
						textPrefab.GetComponent<TextMeshProUGUI> ().text = "typing typing typing....";

						if (currentConversation.sender_messages [currentMessageIndex].origin == "user") 
						{
							textPrefab.GetComponent<TextMeshProUGUI> ().color = Color.black;
						}
					}
				}
			}
        }
    }

//	void OnApplicationQuit ()
//	{
//        bool hasFinishedConversation = false;
//
//        while (!hasFinishedConversation) 
//		{
//            if (!participatingChat.contactInfo.isAvailable || lastMessage.type != conversationSide || lastMessage.choices [lastMessage.selectedChoiceIndex].result == participatingChat.messages.Count)
//            {
//                hasFinishedConversation = true;
//            }
//            else
//            {
//                Debug.Log (lastMessage.choices [lastMessage.selectedChoiceIndex].text);
//                participatingChat.contactInfo.lastMessageIndex = lastMessage.choices [lastMessage.selectedChoiceIndex].result;
//                lastMessage = participatingChat.messages [participatingChat.contactInfo.lastMessageIndex];
//            }
//		}
//	}

	public void StartConversation (ChatsAppController.Chat newChat)
	{
		participatingChat = newChat;

		List<ChatsAppController.ConversationModel> nextConversations = new List<ChatsAppController.ConversationModel> ();

		foreach (ChatsAppController.ConversationModel conversation in participatingChat.dialogTree.conversations)
		{
			string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			bool isIdle = false;
			int satisfiedConditions = 0;

			foreach (string conditionTag in conditionTags)
			{
				for (int i = 0; i < participatingChat.dialogTree.activatedTags.Count; i++)
				{
					if (participatingChat.dialogTree.activatedTags [i].name == conditionTag.Trim ()) 
					{
						if (participatingChat.dialogTree.activatedTags [i].name.Contains ("!")) 
						{
							if (!participatingChat.dialogTree.activatedTags [i].isUnlocked) 
							{
								satisfiedConditions++;
								break; 
							}
						}
						else if (participatingChat.dialogTree.activatedTags [i].isUnlocked) 
						{
							satisfiedConditions++;
							break;
						}

						if (participatingChat.dialogTree.activatedTags [i].name == "IDLE") 
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
			foreach (ChatsAppController.ConversationModel conversation in nextConversations) 
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
			currentThinkingTime = currentConversation.sender_messages [currentMessageIndex].idle_time;
			currentTypingTime = currentConversation.sender_messages [currentMessageIndex].typing_time;
			thinkingTimer = 0f;
			typingTimer = 0f;
			currentBotState = BotState.Thinking;
		} 
		else 
		{
			participatingChat.contactInfo.isAvailable = false;

			if (ChatsAppController.instance != null)
			{
				ChatsAppController.instance.UpdateAvailability (participatingChat.contactInfo.contactName, 0);
			}
			Destroy (gameObject);
		}
	}

	public void ContinueConversation ()
	{
		if (currentMessageIndex == 0) 
		{
			string[] activeTags = currentConversation.tags.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 0; i < activeTags.Length; i++) 
			{
				bool found = false;

				for (int j = 0; j < participatingChat.dialogTree.activatedTags.Count; j++) 
				{
					if (participatingChat.dialogTree.activatedTags [j].name == activeTags [i].Trim ())
					{
						participatingChat.dialogTree.activatedTags [j].isActive = true;
						found = true;
						break;
					}
				}

				if (!found)
				{
					ChatsAppController.ConditionTag newTag = new ChatsAppController.ConditionTag ();
					newTag.name = activeTags [i].Trim ();
					newTag.isActive = true;
					participatingChat.dialogTree.activatedTags.Add (newTag);
				}
			}
		}
		currentConversation.senderMessageIndex = currentMessageIndex;
		currentMessageIndex++;

		if (currentMessageIndex == currentConversation.sender_messages.Length) 
		{
			List<ChatsAppController.ConversationModel> nextConversations = new List<ChatsAppController.ConversationModel> ();

			foreach (ChatsAppController.ConversationModel conversation in participatingChat.dialogTree.conversations) 
			{
				string[] conditionTags = conversation.tags.Split (new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				bool isIdle = false;
				int satisfiedConditions = 0;

				foreach (string conditionTag in conditionTags)
				{
					for (int j = 0; j < participatingChat.dialogTree.activatedTags.Count; j++) 
					{
						if (participatingChat.dialogTree.activatedTags [j].name == conditionTag.Trim ())
						{
							if (participatingChat.dialogTree.activatedTags [j].name.Contains ("!")) 
							{
								if (!participatingChat.dialogTree.activatedTags [j].isUnlocked)
								{
									satisfiedConditions++;
									break; 
								}
							} 
							else if (participatingChat.dialogTree.activatedTags [j].isUnlocked)
							{
								satisfiedConditions++;
								break;
							}

							if (participatingChat.dialogTree.activatedTags [j].name == "IDLE")
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

							for (int k = 0; k < participatingChat.dialogTree.activatedTags.Count; k++) 
							{
								if (participatingChat.dialogTree.activatedTags [k].name == unlockTags [j].Trim ())
								{
									participatingChat.dialogTree.activatedTags [k].isUnlocked = true;
									found = true;
									break;
								}
							}

							if (!found)
							{
								ChatsAppController.ConditionTag newTag = new ChatsAppController.ConditionTag ();
								newTag.name = unlockTags [j].Trim ();
								newTag.isUnlocked = true;
								participatingChat.dialogTree.activatedTags.Add (newTag);
							}
						}

						for (int j = 0; j < lockTags.Length; j++) 
						{
							bool found = false;

							for (int k = 0; k < participatingChat.dialogTree.activatedTags.Count; k++) 
							{
								if (participatingChat.dialogTree.activatedTags [k].name == lockTags [j].Trim ())
								{
									participatingChat.dialogTree.activatedTags [k].isUnlocked = false;
									found = true;
									break;
								}
							}

							if (!found) 
							{
								ChatsAppController.ConditionTag newTag = new ChatsAppController.ConditionTag ();
								newTag.name = lockTags [j].Trim ();
								newTag.isUnlocked = false;
								participatingChat.dialogTree.activatedTags.Add (newTag);
							}
						}
					}
				}
			}

			if (nextConversations.Count != 0)
			{
				currentConversation = null;

				foreach (ChatsAppController.ConversationModel conversation in nextConversations) 
				{
					if (conversation.user_messages.Length == 0 && conversation.sender_messages.Length != 0)
					{
						currentConversation = conversation;
						currentMessageIndex = 0;
						currentThinkingTime = currentConversation.sender_messages [currentMessageIndex].idle_time;
						currentTypingTime = currentConversation.sender_messages [currentMessageIndex].typing_time;
						thinkingTimer = 0f;
						typingTimer = 0f;
						currentBotState = BotState.Thinking;
						break;
					}
				}

				if (currentConversation == null) 
				{
					Destroy (gameObject);
				}
			} 
			else 
			{
				participatingChat.contactInfo.isAvailable = false;

				if (ChatsAppController.instance != null)
				{
					ChatsAppController.instance.UpdateAvailability (participatingChat.contactInfo.contactName, 0);
				}
				Destroy (gameObject);
			}
		} 
		else 
		{
			currentThinkingTime = currentConversation.sender_messages [currentMessageIndex].idle_time;
			currentTypingTime = currentConversation.sender_messages [currentMessageIndex].typing_time;
			thinkingTimer = 0f;
			typingTimer = 0f;
			currentBotState = BotState.Thinking;
		}
	}

	private void UpdateTags ()
	{
		string[] unlockTags = currentConversation.tags_to_unlock.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		string[] lockTags = currentConversation.tags_to_lock.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

		for (int i = 0; i < unlockTags.Length; i++) 
		{
			bool found = false;

			for (int j = 0; j < participatingChat.dialogTree.activatedTags.Count; j++) 
			{
				if (participatingChat.dialogTree.activatedTags [j].name == unlockTags [i].Trim ())
				{
					participatingChat.dialogTree.activatedTags [j].isUnlocked = true;
					found = true;
					break;
				}
			}

			if (!found)
			{
				ChatsAppController.ConditionTag newTag = new ChatsAppController.ConditionTag ();
				newTag.name = unlockTags [i].Trim ();
				newTag.isUnlocked = true;
				participatingChat.dialogTree.activatedTags.Add (newTag);
			}
		}

		for (int i = 0; i < lockTags.Length; i++) 
		{
			bool found = false;

			for (int j = 0; j < participatingChat.dialogTree.activatedTags.Count; j++) 
			{
				if (participatingChat.dialogTree.activatedTags [j].name == lockTags [i].Trim ())
				{
					participatingChat.dialogTree.activatedTags [j].isUnlocked = false;
					found = true;
					break;
				}
			}

			if (!found)
			{
				ChatsAppController.ConditionTag newTag = new ChatsAppController.ConditionTag ();
				newTag.name = lockTags [i].Trim ();
				newTag.isUnlocked = false;
				participatingChat.dialogTree.activatedTags.Add (newTag);
			}
		}
	}
}
