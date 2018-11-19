using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CallScreenWrapper : MonoBehaviour 
{
	public Image profileImage;
	public TextMeshProUGUI headerCallerText, contactNameText, callStatusText;
	public Button dialerButton, declineButton, acceptButton, muteButton;
	public float minWaitTime, maxWaitTime;

	[Header ("Dialer")]
	public GameObject dialerPanel;
	public GameObject numpad;
	public TextMeshProUGUI inputText;
	public int maxLength;

	private ContactsAppController.CallType currentCallType;
	private string contactingNumber, recordingID, previousLayer;
	private bool onHold, onCall, isAvailable;
	private float waitTimer, currentWaitTime;

		
	void Update ()
	{
		if (onHold) 
		{
			waitTimer += Time.deltaTime;

			if (waitTimer > currentWaitTime)
			{
				if (currentCallType == ContactsAppController.CallType.Incoming)
				{
					DropCall ();
				} 
				else if (currentCallType == ContactsAppController.CallType.Outgoing) 
				{
					AcceptCall ();
				}
			}
		}

		if (onCall) 
		{
			float playbackTime = MediaManager.instance.bgmPlaybackTime;
			int playbackMin = Mathf.FloorToInt (playbackTime / 60);
			int playbackSec = (int)(playbackTime % 60);

			callStatusText.text = string.Format ("{0:0}:{1:00}", playbackMin, playbackSec);
		}
	}

	public void ConnectCall (string phoneNumber, string callerID, ContactsAppController.CallType callType)
	{
		List<Button> callButtons = new List<Button> ();

		if (callerID != "") 
		{
			profileImage.sprite = MediaManager.instance.GetImageInfo (callerID).image;
			headerCallerText.text = callerID;
			contactNameText.text = callerID;
			CheckCallAvailability (callerID);
		}
		else 
		{
			profileImage.sprite = MediaManager.instance.GetImageInfo ("UnknownContact").image;
			headerCallerText.text = phoneNumber;
			contactNameText.text = phoneNumber;
			CheckCallAvailability (phoneNumber);
		}
		callStatusText.text = "Calling";
		contactingNumber = phoneNumber;

		if (callType == ContactsAppController.CallType.Incoming) 
		{
			currentWaitTime = maxWaitTime;
			dialerButton.gameObject.SetActive (false);
			declineButton.interactable = true;
			declineButton.gameObject.SetActive (true);
			acceptButton.interactable = true;
			acceptButton.gameObject.SetActive (true);
			muteButton.gameObject.SetActive (false);

			callButtons.Add (declineButton);
			callButtons.Add (acceptButton);
		}
		else if (callType == ContactsAppController.CallType.Outgoing)
		{
			if (isAvailable)
			{
				currentWaitTime = Random.Range (minWaitTime, maxWaitTime);
			}
			else
			{
				currentWaitTime = maxWaitTime;
			}
			dialerButton.interactable = true;
			dialerButton.gameObject.SetActive (true);
			declineButton.interactable = true;
			declineButton.gameObject.SetActive (true);
			acceptButton.gameObject.SetActive (false);
			muteButton.interactable = true;
			muteButton.gameObject.SetActive (true);

			callButtons.Add (dialerButton);
			callButtons.Add (declineButton);
			callButtons.Add (muteButton);
		}
		currentCallType = callType;
		waitTimer = 0f;
		onHold = true;
		MediaManager.instance.SwapToCall (true);

		previousLayer = PlayerController.instance.GetActiveLayer ().layerName;
		PlayerController.instance.SwitchLayer ("CallMenu");
		PlayerController.instance.SetSelectableButtons (callButtons);
	}

	public void AcceptCall ()
	{
		if (currentCallType == ContactsAppController.CallType.Incoming) 
		{
			MediaManager.instance.PlayBGM (recordingID);
			onHold = false;
			onCall = true;

			dialerButton.interactable = true;
			dialerButton.gameObject.SetActive (true);
			acceptButton.interactable = false;
			acceptButton.gameObject.SetActive (false);
			muteButton.interactable = true;
			muteButton.gameObject.SetActive (true);

			List<Button> callButtons = new List<Button> ();
			callButtons.Add (dialerButton);
			callButtons.Add (declineButton);
			callButtons.Add (muteButton);
			PlayerController.instance.SetSelectableButtons (callButtons);
		}
		else if (currentCallType == ContactsAppController.CallType.Outgoing)
		{
			if (isAvailable) 
			{
				MediaManager.instance.PlayBGM (recordingID);
				onHold = false;
				onCall = true;
			} 
			else 
			{
				callStatusText.text = "No Answer";
				declineButton.interactable = false;
				onHold = false;
				onCall = false;
				StartCoroutine (CloseCall ());
			}
		}
	}

	public void DropCall ()
	{
		if (onHold)
		{
			if (currentCallType == ContactsAppController.CallType.Incoming) 
			{
				if (waitTimer <= currentWaitTime)
				{
					currentCallType = ContactsAppController.CallType.Rejected;
				} 
				else
				{
					currentCallType = ContactsAppController.CallType.Missed;
				}
				acceptButton.interactable = false;
			}
		}
		callStatusText.text = "Call Dropped";
		declineButton.interactable = false;
		onHold = false;
		onCall = false;
		MediaManager.instance.StopBGM ();
		StartCoroutine (CloseCall ());
	}

	public void OpenDialer ()
	{
		dialerPanel.SetActive (!dialerPanel.activeSelf);

		if (dialerPanel.activeSelf)
		{
			inputText.text = "";

			List<Button> numpadButtons = new List<Button> ();
			numpad.GetComponentsInChildren (numpadButtons);
			PlayerController.instance.SetSelectableButtons (numpadButtons);
			PlayerController.instance.SetNegativeButton (dialerButton);
		} 
		else
		{
			List<Button> callButtons = new List<Button> ();
			callButtons.Add (dialerButton);
			callButtons.Add (declineButton);
			callButtons.Add (muteButton);
			PlayerController.instance.SetSelectableButtons (callButtons);
			PlayerController.instance.SetNegativeButton (null);
		}
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

	public void MuteCall ()
	{
		MediaManager.instance.ToggleSound ("BGM");
	}

	private void CheckCallAvailability (string key)
	{
		recordingID = "Emil's Shop";
		isAvailable = true;
	}

	private void EndCall ()
	{
		callStatusText.text = "Call Ended";
		declineButton.interactable = false;
		onCall = false;
		StartCoroutine (CloseCall ());
	}

	private IEnumerator CloseCall ()
	{
		yield return new WaitForSeconds (1f);
		MediaManager.instance.SwapToCall (false);
		gameObject.SetActive (false);

		CameraController.ReturnToPreviousActivity ();
		PlayerController.instance.SwitchLayer (previousLayer);
		ContactsAppController.RegisterCall (contactingNumber, currentCallType);
	}
}
