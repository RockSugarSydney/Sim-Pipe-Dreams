using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmailAppController : MonoBehaviour 
{
	public static EmailAppController instance;

	public class EmailInfo
	{
		public bool isSent, hasRead;
		public string senderName, senderEmail, receiverEmail, timestamp, subject, body;
	}

	[XmlRoot ("Inbox")]
	public class Inbox
	{
		[XmlArray ("Emails")]
		[XmlArrayItem ("Email")]
		public List<EmailInfo> emails = new List<EmailInfo> ();
	}

	[Header ("Main Screen")]
    public GameObject inboxScreen;
	public GameObject inboxBottomBar;
	public TextMeshProUGUI titleText;
	public ScrollRect emailDetailsScrollRect;
	public GameObject emailDetails;

	[Header ("Email Window")]
	public GameObject emailWindow;
	public GameObject emailBottomBar, bodyText, mediaButton;
	public TextMeshProUGUI senderTitleText, subjectText, dateText, timeText, fromEmailText, toEmailText;
	public ScrollRect emailScrollRect;
	public Transform emailBodyPanel;
	public float lerpRate;

	private Inbox inbox = new Inbox ();
	private List<GameObject> detailsButtons = new List<GameObject> ();
	private EmailInfo selectedEmail;
	private RectTransform emailTransform;
	private Vector2 finalWindowPosition;
	private bool isGravitating, isOpen;


	void Awake ()
	{
		if (instance != this) 
		{
			instance = this;
		}
		emailTransform = emailWindow.GetComponent<RectTransform> ();
	}

	void Start ()
	{
        string[] splitString = CameraController.CurrentActivity.Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
		CheckInbox ();

		PlayerController.instance.SwitchLayer ("Menu1");
		PlayerController.instance.SetScrollableRect (emailDetailsScrollRect);
		PlayerController.instance.SetCapacitiveButtons (inboxBottomBar.GetComponentsInChildren<Button> ());
       
		if (splitString.Length > 1)
		{
			int newMailIndex = 0;

            if (int.TryParse (splitString [1], out newMailIndex))
            {
                OpenEmail (newMailIndex);
            }
		} 
		else
		{
			RefreshInbox ();
		}
        CameraController.onMediaClose = OpenEmailWindow;

		ChatsAppController.ActivateTag ("Greg", "a1_main", true);
	}

	void Update ()
	{
		if (isGravitating)
		{
			emailTransform.anchoredPosition = Vector2.Lerp (emailTransform.anchoredPosition, finalWindowPosition, lerpRate);

			if (Vector2.Distance (emailTransform.anchoredPosition, finalWindowPosition) < 1f)
			{
				if (isOpen)
				{
					inboxScreen.SetActive (false);
				}
				else 
				{
					emailWindow.SetActive (false);
				}
				emailTransform.anchoredPosition = finalWindowPosition;
				isGravitating = false;
			}
		}
	}

	public void OpenEmail (int inboxIndex)
	{
		List<Button> generatedButtons = new List<Button> ();
		selectedEmail = inbox.emails [inboxIndex];

		if (!selectedEmail.hasRead) 
		{
			selectedEmail.hasRead = true;
		}
		DateTime emailTimestamp = DateTime.Parse (selectedEmail.timestamp);
        senderTitleText.text = selectedEmail.senderEmail;
		dateText.text = emailTimestamp.ToString ("dd MMMMM yyyy");
		timeText.text = emailTimestamp.ToString ("h:mm tt");
        fromEmailText.text = "From: " + selectedEmail.senderName;
        toEmailText.text = "To: " + selectedEmail.receiverEmail;
        subjectText.text = "Subject: " + selectedEmail.subject;

		for (int i = 0; i < emailBodyPanel.childCount; i++)
		{
			Destroy (emailBodyPanel.GetChild (i).gameObject);
		}
		string[] split = selectedEmail.body.Split (new Char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

		for (int i = 0; i < split.Length; i++) 
		{
			if (split [i].Contains ("Image_")) 
			{
				Button buttonPrefab = Instantiate (mediaButton, emailBodyPanel).GetComponent<Button> ();
				string imageName = split [i].Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries) [1];
				buttonPrefab.image.sprite = MediaManager.instance.GetImageInfo (imageName).image;
				buttonPrefab.transform.GetChild (0).gameObject.SetActive (false); 
				buttonPrefab.onClick.AddListener (() => {
					MediaManager.instance.SelectMedia (imageName, MediaManager.MediaType.Image);
                    emailWindow.SetActive (false);
				});

				generatedButtons.Add (buttonPrefab);
			}
			else if (split [i].Contains ("Video_"))
			{
				Button buttonPrefab = Instantiate (mediaButton, emailBodyPanel).GetComponent<Button> ();
				string videoName = split [i].Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries) [1];
				buttonPrefab.image.sprite = MediaManager.instance.GetVideoInfo (videoName).thumbnail;
				buttonPrefab.transform.GetChild (0).gameObject.SetActive (true); 
				buttonPrefab.onClick.AddListener (() => {
					MediaManager.instance.SelectMedia (videoName, MediaManager.MediaType.Video);
                    emailWindow.SetActive (false);
				});

				generatedButtons.Add (buttonPrefab);
			}
			else
			{
				GameObject textPrefab = Instantiate (bodyText, emailBodyPanel);
				textPrefab.GetComponent<TextMeshProUGUI> ().text = split [i];
			}
		}
		emailTransform.anchoredPosition = new Vector2 (emailTransform.rect.width, emailTransform.anchoredPosition.y);
		finalWindowPosition = new Vector2 (0f, emailTransform.anchoredPosition.y);
		isOpen = true;
		isGravitating = true;
		emailWindow.SetActive (true);

		emailScrollRect.velocity = new Vector2 ();
		emailScrollRect.content.anchoredPosition = new Vector2 (0f, 0f); 

		CameraController.CurrentActivity = "Mail_" + inboxIndex + "_" + selectedEmail.senderName;

		PlayerController.instance.SwitchLayer ("Menu2");
		PlayerController.instance.SetScrollableRect (emailScrollRect);
		PlayerController.instance.SetSelectableButtons (generatedButtons);
		PlayerController.instance.SetCapacitiveButtons (emailBottomBar.GetComponentsInChildren<Button> ());
	}

    public void OpenEmailWindow ()
    {
		emailWindow.SetActive (true);
		PlayerController.instance.SwitchLayer ("Menu2");
    }

	public void CloseEmail ()
	{
		PlayerController.instance.SwitchLayer ("Menu1");
		RefreshInbox ();

		finalWindowPosition = new Vector2 (emailTransform.rect.width, emailTransform.anchoredPosition.y);
		isOpen = false;
		isGravitating = true;
        inboxScreen.SetActive (true);
		CameraController.CurrentActivity = "Mail";
	}

	private void CheckInbox ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(Inbox));
		string path = Path.Combine (Application.persistentDataPath, "Inbox.xml");

		if (File.Exists (path))
		{
			using (FileStream stream = new FileStream (path, FileMode.Open))
			{
				inbox = serializer.Deserialize (stream) as Inbox;
			}
		} 
		else 
		{
			TextAsset xml = Resources.Load ("Inbox") as TextAsset;
			inbox = serializer.Deserialize (new StringReader (xml.text)) as Inbox;
		}
	}

	private void UpdateInbox ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(Inbox));
		string path = Path.Combine (Application.persistentDataPath, "Inbox.xml");

		using (FileStream stream = new FileStream (path, FileMode.Create))
		{
			serializer.Serialize (stream, inbox);
		}
	}

	private void RefreshInbox ()
	{
		List<Button> generatedButtons = new List<Button> ();
		int previousButtonIndex = 0;
		int usedButtons = 0;
		int unreadEmails = 0;

		for (int i = 0; i < inbox.emails.Count; i++)
		{
			if (inbox.emails [i].isSent)
			{
				GameObject detailsPrefab;

				if (usedButtons > detailsButtons.Count - 1) 
				{
					detailsPrefab = Instantiate (emailDetails, emailDetailsScrollRect.content);
					detailsButtons.Add (detailsPrefab);
				}
				else
				{
					detailsPrefab = detailsButtons [usedButtons];
					detailsPrefab.SetActive (true);
				}
				usedButtons++;

				EmailDetailsWrapper emailWrapper = detailsPrefab.GetComponent<EmailDetailsWrapper> ();

				if (inbox.emails [i].senderName.Length > 20) 
				{
					emailWrapper.senderText.text = inbox.emails [i].senderName.Substring (0, 20) + "...";
				}
				else 
				{
					emailWrapper.senderText.text = inbox.emails [i].senderName;
				}
				emailWrapper.subjectText.text = inbox.emails [i].subject;

				string[] split = inbox.emails [i].body.Split (new String[] { "(", ")", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
				string filteredString = "";

				for (int j = 0; j < split.Length; j++) 
				{
					if (!split [j].Contains ("Image_") && !split [j].Contains ("Video_")) 
					{
						filteredString += split [j] + " ";
					}
				}

				if (filteredString.Length > 50) 
				{
					emailWrapper.excerptText.text = filteredString.Substring (0, 50) + "...";
				}
				else 
				{
					emailWrapper.excerptText.text = filteredString;
				}

				DateTime emailTimestamp = DateTime.Parse (inbox.emails [i].timestamp);
				emailWrapper.timeText.text = emailTimestamp.ToShortTimeString ();

				if (!inbox.emails [i].hasRead)
				{
					unreadEmails++;

//                    Color newMessageColor = new Color ();
//                    ColorUtility.TryParseHtmlString ("#FFD902", out newMessageColor);
//                    emailWrapper.senderText.color = newMessageColor;
//                    emailWrapper.subjectText.color = Color.white;
//                    emailWrapper.excerptText.color = Color.white;
					emailWrapper.notificationImage.gameObject.SetActive (true);
				} 
				else
				{
//                    Color readMessageColor = new Color ();
//                    ColorUtility.TryParseHtmlString ("#858688", out readMessageColor);
//                    emailWrapper.senderText.color = readMessageColor;
//                    emailWrapper.subjectText.color = readMessageColor;
//                    emailWrapper.excerptText.color = readMessageColor;
					emailWrapper.notificationImage.gameObject.SetActive (false);
				}

				int inboxIndex = i;
				emailWrapper.detailsButton.onClick.RemoveAllListeners ();
				emailWrapper.detailsButton.onClick.AddListener (() => {
					OpenEmail (inboxIndex);
				});

				if (inbox.emails [i] == selectedEmail) 
				{
					previousButtonIndex = usedButtons - 1;
				}
				generatedButtons.Add (emailWrapper.detailsButton);
			}
		}

		for (int i = usedButtons; i < detailsButtons.Count; i++) 
		{
			detailsButtons [i].SetActive (false);
		}
		PlayerController.instance.SetSelectableButtons (generatedButtons, previousButtonIndex);

		if (unreadEmails > 0) 
		{
			titleText.text = "INBOX (" + unreadEmails + ")";
		} 
		else 
		{
			titleText.text = "INBOX";
		}
		PhoneMenuManager.UpdateNotification ("Email", unreadEmails);
	}
}
