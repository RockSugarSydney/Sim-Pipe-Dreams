using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContactsAppController : MonoBehaviour 
{
	public static ContactsAppController instance;

	public enum CallType
	{
		Incoming,
		Outgoing,
		Missed,
		Rejected
	};

	public class Call
	{
		public string phoneNumber;
		public CallType type;
		public string timestamp;
	}

	[XmlRoot ("CallHistory")]
	public class CallHistory
	{
		[XmlArray ("CallsList")]
		[XmlArrayItem ("Call")]
		public List<Call> callsList = new List<Call> ();
	}

	public class ContactDetail
	{
		public string name, detail;
	}

	public class Contact
	{
		public bool hasContact, favourite;
		public string name;
		[XmlArray ("DetailsList")]
		[XmlArrayItem ("Detail")]
		public ContactDetail[] detailsList;
	}

	[XmlRoot ("Phonebook")]
	public class Phonebook
	{
		[XmlArray ("ContactsList")]
		[XmlArrayItem ("Contact")]
		public List<Contact> contactsList = new List<Contact> ();
	}

    public GameObject contactScreen, contactBottomBar;
	public TextMeshProUGUI titleText;
	public ScrollRect contactsScrollRect;
	public Transform categoryPanel;
	public GameObject contactDetailsButton, alphabetGroupPanel;

	[Header ("Dialer")]
	public GameObject dialerPanel;
	public GameObject numpad;
	public TextMeshProUGUI inputText;
	public int maxLength;

	[Header ("Contact Info Window")]
	public GameObject infoWindow;
	public GameObject infoBottomBar;
	public GameObject profileDetailsButton;
	public Transform profileDetailsPanel;
	public Image profileImage;
	public TextMeshProUGUI contactName;
	public float lerpRate;

	private static Phonebook phonebook = new Phonebook ();
	private static CallHistory callHistory = new CallHistory ();

	private Button[] categoryButtons;
	private RectTransform contactInfoTransform;
	private Vector2 finalWindowPosition;
	private bool isFading, isGravitating, isOpen, newCall;
	private string previousMenu = "";
	private float fadeTimer;


	void Awake ()
	{
		if (instance != this) 
		{
			instance = this;
		}
		contactInfoTransform = infoWindow.GetComponent<RectTransform> ();
	}

	void Start ()
	{
		categoryButtons = categoryPanel.GetComponentsInChildren<Button> ();

		PlayerController.instance.SwitchLayer ("Menu1");
		PlayerController.instance.SetSwitchableTabs (categoryButtons);
		PlayerController.instance.SetCapacitiveButtons (contactBottomBar.GetComponentsInChildren<Button> ());

		OpenTab (3);
	}

	void Update ()
	{
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
			contactInfoTransform.anchoredPosition = Vector2.Lerp (contactInfoTransform.anchoredPosition, finalWindowPosition, lerpRate);

			if (Vector2.Distance (contactInfoTransform.anchoredPosition, finalWindowPosition) < 1f)
			{
				if (isOpen)
				{
					contactScreen.SetActive (false);
				}
				else 
				{
					infoWindow.SetActive (false);
				}
				contactInfoTransform.anchoredPosition = finalWindowPosition;
				isGravitating = false;
			}
		}
	}

	public static void IntializeDatabase ()
	{
		ReadPhonebook ();
		ReadCallHistory ();
	}

	public static void DialNumber (string phoneNumber, CallType callType)
	{
		Contact dialedContact = SearchPhonebook (phoneNumber);
		string contactName = "";

		if (dialedContact != null) 
		{
			contactName = dialedContact.name;
		} 
		CameraController.instance.MakeCall (phoneNumber, contactName, callType);
	}

	public static void RegisterCall (string calledNumber, CallType callType)
	{
		Debug.Log ("register call");
		Call newCall = new Call ();
		newCall.phoneNumber = calledNumber;
		newCall.timestamp = DateTime.Now.ToString ();
		newCall.type = callType;

		callHistory.callsList.Add (newCall);

		if (CameraController.CurrentActivity == "Contacts_History") 
		{
			instance.ListCallHistory ();
		}
		else if (instance != null)
		{
			instance.newCall = true;
			Debug.Log ("new call");
		}
	}

	public void OpenTab (int index)
	{
		if (isFading) 
		{
			return;
		}
		FadePanels ();

		switch (index) 
		{
		case 0:
			ListFavourites ();
			break;
		case 1:
			ListCallHistory ();
			break;
		case 2:
			ListContacts ();
			break;
		case 3:
			OpenDialer ();
			break;
		default:
			break;
		}
		contactsScrollRect.velocity = new Vector2 ();
		contactsScrollRect.content.anchoredPosition = new Vector2 (0f, 0f); 

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

	public void OpenDialer ()
	{
		titleText.text = "Dial";
		inputText.text = "";
		contactsScrollRect.gameObject.SetActive (false);
		dialerPanel.SetActive (true);
		CameraController.CurrentActivity = "Contacts_Dial";

		List<Button> numpadButtons = new List<Button> ();
		numpad.GetComponentsInChildren (numpadButtons);
		PlayerController.instance.SetScrollableRect (null);
		PlayerController.instance.SetSelectableButtons (numpadButtons);
	}

	public void EnterNumber (string number)
	{
		if (number != "Delete")
		{
			if (inputText.text.Length < maxLength)
			{
				inputText.text += number;
			}
		}
		else if (inputText.text != "")
		{
			inputText.text = inputText.text.Remove (inputText.text.Length - 1);
		}
	}

	public void DialNumber ()
	{
		DialNumber (inputText.text, CallType.Outgoing);
		inputText.text = "";
	}

	public void OpenContactInfo (string name)
	{
		Contact selectedContact = SearchPhonebook (name);

		List<Button> generatedButtons = new List<Button> ();

		for (int i = 0; i < profileDetailsPanel.childCount; i++)
		{
			Destroy (profileDetailsPanel.GetChild (i).gameObject);
		}

		if (selectedContact != null)
		{
			profileImage.sprite = MediaManager.instance.GetImageInfo (name).image;

			for (int i = 0; i < selectedContact.detailsList.Length; i++) 
			{
				GameObject detailsPrefab = Instantiate (profileDetailsButton, profileDetailsPanel);
				ProfileDetailsWrapper detailsWrapper = detailsPrefab.GetComponent<ProfileDetailsWrapper> ();
				string callCategory = selectedContact.detailsList [i].name;
				callCategory = callCategory.Substring (callCategory.IndexOf ('_') + 1);
				detailsWrapper.detailNameText.text = callCategory;
				detailsWrapper.detailText.text = selectedContact.detailsList [i].detail;

				if (selectedContact.detailsList [i].name.Contains ("Phone_")) 
				{
					string phoneNumber = selectedContact.detailsList [i].detail;
					detailsWrapper.detailsButton.onClick.AddListener (() => {
						DialNumber (phoneNumber, CallType.Outgoing);
					});
					detailsWrapper.detailsButton.interactable = true;
				} 
				else
				{
					detailsWrapper.detailsButton.interactable = false;
				}

				generatedButtons.Add (detailsWrapper.detailsButton);
			}
		}
		else 
		{
			profileImage.sprite = MediaManager.instance.GetImageInfo ("UnknownContact").image;

			GameObject detailsPrefab = Instantiate (profileDetailsButton, profileDetailsPanel);
			ProfileDetailsWrapper detailsWrapper = detailsPrefab.GetComponent<ProfileDetailsWrapper> ();
			detailsWrapper.detailNameText.text = "Unknown";
			detailsWrapper.detailText.text = name;

			detailsWrapper.detailsButton.onClick.AddListener (() => {
				DialNumber (name, CallType.Outgoing);
			});

			generatedButtons.Add (detailsWrapper.detailsButton);
		}
		contactName.text = name;

		contactInfoTransform.anchoredPosition = new Vector2 (contactInfoTransform.rect.width, contactInfoTransform.anchoredPosition.y);
		finalWindowPosition = new Vector2 (0f, contactInfoTransform.anchoredPosition.y);
		isOpen = true;
		isGravitating = true;
		infoWindow.SetActive (true);
		previousMenu = CameraController.CurrentActivity;
		CameraController.CurrentActivity = "Contacts_ContactInfo";

		PlayerController.instance.SwitchLayer ("Menu2");
		PlayerController.instance.SetSelectableButtons (generatedButtons);
		PlayerController.instance.SetCapacitiveButtons (infoBottomBar.GetComponentsInChildren<Button> ());
	}

	public void CloseContactInfo ()
	{
		finalWindowPosition = new Vector2 (contactInfoTransform.rect.width, contactInfoTransform.anchoredPosition.y);
		isOpen = false;
		isGravitating = true;
		contactScreen.SetActive (true);

		PlayerController.instance.SwitchLayer ("Menu1");

		if (previousMenu == "Contacts_History" && newCall) 
		{
			ListCallHistory ();
			contactsScrollRect.velocity = new Vector2 ();
			contactsScrollRect.content.anchoredPosition = new Vector2 (0f, 0f); 
			Debug.Log ("update history");
		}
	}
		
	private void ClearList ()
	{
		for (int i = 0; i < contactsScrollRect.content.childCount; i++) 
		{
			Destroy (contactsScrollRect.content.GetChild (i).gameObject);
		}
	}

	private void ListCallHistory ()
	{
		titleText.text = "Recents";
		ClearList ();

		List<Button> generatedButtons = new List<Button> ();

		for (int i = callHistory.callsList.Count - 1; i >= 0; i--) 
		{
			GameObject callPrefab = Instantiate (contactDetailsButton, contactsScrollRect.content);
			ContactDetailsWrapper detailsWrapper = callPrefab.GetComponent<ContactDetailsWrapper> ();
			Contact callerInfo = SearchPhonebook (callHistory.callsList [i].phoneNumber);
			string callerName;

			if (callerInfo != null)
			{
				detailsWrapper.contactNameText.text = callerInfo.name;
				callerName = callerInfo.name;

				for (int j = 0; j < callerInfo.detailsList.Length; j++) 
				{
					if (callerInfo.detailsList [j].name.Contains ("Phone_")) 
					{
						if (callerInfo.detailsList [j].detail == callHistory.callsList [i].phoneNumber) 
						{
							string callCategory = callerInfo.detailsList [j].name;
							callCategory = callCategory.Substring (callCategory.IndexOf ('_') + 1);
							detailsWrapper.callCategoryText.text = callCategory;
							break;
						}
					}
				}
			}
			else 
			{
				detailsWrapper.contactNameText.text = callHistory.callsList [i].phoneNumber;
				callerName = callHistory.callsList [i].phoneNumber;
				detailsWrapper.callCategoryText.text = "Unknown";
			}
			detailsWrapper.callStatusImage.sprite = detailsWrapper.callSprites [(int)callHistory.callsList [i].type].sprite;
			detailsWrapper.callStatusImage.color = detailsWrapper.callSprites [(int)callHistory.callsList [i].type].color;

			DateTime callTimestamp = DateTime.Parse (callHistory.callsList [i].timestamp);
			detailsWrapper.callTimeText.text = callTimestamp.ToShortTimeString ();

			detailsWrapper.detailsButton.onClick.AddListener (() => {
				OpenContactInfo (callerName);
			});

			generatedButtons.Add (detailsWrapper.detailsButton);
		}
		contactsScrollRect.gameObject.SetActive (true);
		dialerPanel.SetActive (false);
		newCall = false;
		CameraController.CurrentActivity = "Contacts_History";

		PlayerController.instance.SetScrollableRect (contactsScrollRect);
		PlayerController.instance.SetSelectableButtons (generatedButtons);
	}

	private void ListFavourites ()
	{
		titleText.text = "Favourites";
		ClearList ();

		List<Button> generatedButtons = new List<Button> ();

		for (int i = 0; i < phonebook.contactsList.Count; i++) 
		{
			if (phonebook.contactsList [i].hasContact && phonebook.contactsList [i].favourite)
			{
				GameObject detailsPrefab = Instantiate (contactDetailsButton, contactsScrollRect.content);
				ContactDetailsWrapper detailsWrapper = detailsPrefab.GetComponent<ContactDetailsWrapper> ();
				detailsWrapper.contactNameText.text = phonebook.contactsList [i].name;
				detailsWrapper.contactNameText.fontSize = 60f;
				detailsWrapper.callTimeText.transform.parent.gameObject.SetActive (false);

				for (int j = 0; j < phonebook.contactsList [i].detailsList.Length; j++) 
				{
					if (phonebook.contactsList [i].detailsList [j].name.Contains ("Phone_")) 
					{
						string callCategory = phonebook.contactsList [i].detailsList [j].name;
						callCategory = callCategory.Substring (callCategory.IndexOf ('_') + 1);
						detailsWrapper.callCategoryText.text = callCategory;
						break;
					}
				}

				string contactName = phonebook.contactsList [i].name;
				detailsWrapper.detailsButton.onClick.AddListener (() => {
					OpenContactInfo (contactName);
				});

				generatedButtons.Add (detailsWrapper.detailsButton);
			}
		}
		contactsScrollRect.gameObject.SetActive (true);
		dialerPanel.SetActive (false);
		CameraController.CurrentActivity = "Contacts_Favourites";

		PlayerController.instance.SetScrollableRect (contactsScrollRect);
		PlayerController.instance.SetSelectableButtons (generatedButtons);
	}

	private void ListContacts ()
	{
		titleText.text = "Contacts";

		ClearList ();
		char alphabet = 'A';

		List<Button> generatedButtons = new List<Button> ();

		for (int i = 0; i < phonebook.contactsList.Count; i++)
		{
			if (phonebook.contactsList [i].hasContact) 
			{
				if (phonebook.contactsList [i].name.StartsWith (alphabet.ToString ()))
				{
					GameObject alphabetPrefab = Instantiate (alphabetGroupPanel, contactsScrollRect.content);
					alphabetPrefab.GetComponentInChildren<TextMeshProUGUI> ().text = alphabet.ToString ();
					alphabet++;
				} 
				else if (alphabet < phonebook.contactsList [i].name.ToCharArray () [0]) 
				{
					GameObject alphabetPrefab = Instantiate (alphabetGroupPanel, contactsScrollRect.content);
					alphabet = phonebook.contactsList [i].name.ToCharArray () [0];
					alphabetPrefab.GetComponentInChildren<TextMeshProUGUI> ().text = alphabet.ToString ();
				}
				GameObject detailsPrefab = Instantiate (contactDetailsButton, contactsScrollRect.content);
				ContactDetailsWrapper detailsWrapper = detailsPrefab.GetComponent<ContactDetailsWrapper> ();
				detailsWrapper.contactNameText.text = phonebook.contactsList [i].name;
				detailsWrapper.contactNameText.fontSize = 60f;
				detailsWrapper.callTimeText.transform.parent.gameObject.SetActive (false);
				detailsWrapper.callCategoryText.transform.parent.gameObject.SetActive (false);

				string contactName = phonebook.contactsList [i].name;
				detailsWrapper.detailsButton.onClick.AddListener (() => {
					OpenContactInfo (contactName);
				});

				generatedButtons.Add (detailsWrapper.detailsButton);
			}
		}
		contactsScrollRect.gameObject.SetActive (true);
		dialerPanel.SetActive (false);
		CameraController.CurrentActivity = "Contacts";

		PlayerController.instance.SetScrollableRect (contactsScrollRect);
		PlayerController.instance.SetSelectableButtons (generatedButtons);
	}

	private static void ReadPhonebook ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(Phonebook));
		string path = Path.Combine (Application.persistentDataPath, "Phonebook.xml");

		if (File.Exists (path))
		{
			using (FileStream stream = new FileStream (path, FileMode.Open))
			{
				phonebook = serializer.Deserialize (stream) as Phonebook;
			}
		} 
		else 
		{
			TextAsset xml = Resources.Load ("Phonebook") as TextAsset;
			phonebook = serializer.Deserialize (new StringReader (xml.text)) as Phonebook;
		}
	}

	private static void UpdatePhonebook ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(Phonebook));
		string path = Path.Combine (Application.persistentDataPath, "Phonebook.xml");

		using (FileStream stream = new FileStream (path, FileMode.Create))
		{
			serializer.Serialize (stream, phonebook);
		}
	}

	private static Contact SearchPhonebook (string keyword)
	{
		for (int i = 0; i < phonebook.contactsList.Count; i++)
		{
			if (phonebook.contactsList [i].name == keyword) 
			{
				if (phonebook.contactsList [i].hasContact)
				{
					return phonebook.contactsList [i];
				}
				else 
				{
					return null;
				}
			} 
			else 
			{
				for (int j = 0; j < phonebook.contactsList [i].detailsList.Length; j++) 
				{
					if (phonebook.contactsList [i].detailsList [j].name.Contains ("Phone_")) 
					{
						if (phonebook.contactsList [i].detailsList [j].detail == keyword) 
						{
							if (phonebook.contactsList [i].hasContact)
							{
								return phonebook.contactsList [i];
							}
							else 
							{
								return null;
							}
						}
					}
				}
			}
		}
		return null;
	}

	private static void ReadCallHistory ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(CallHistory));
		string path = Path.Combine (Application.persistentDataPath, "CallHistory.xml");

		if (File.Exists (path))
		{
			using (FileStream stream = new FileStream (path, FileMode.Open))
			{
				callHistory = serializer.Deserialize (stream) as CallHistory;
			}
		} 
		else 
		{
			TextAsset xml = Resources.Load ("CallHistory") as TextAsset;
			callHistory = serializer.Deserialize (new StringReader (xml.text)) as CallHistory;
		}
	}

	private static void UpdateCallHistory ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(CallHistory));
		string path = Path.Combine (Application.persistentDataPath, "CallHistory.xml");

		using (FileStream stream = new FileStream (path, FileMode.Create))
		{
			serializer.Serialize (stream, callHistory);
		}
	}

	private void FadePanels ()
	{
		if (CameraController.CurrentActivity.Contains ("Contacts")) 
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
