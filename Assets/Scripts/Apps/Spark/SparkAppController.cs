using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SparkAppController : MonoBehaviour 
{
	public static SparkAppController instance;

	[System.Serializable]
	public struct OnlineStatus
	{
		public Sprite sprite;
		public Color color;
	}

	public GameObject mainScreen, mainBottomBar;
	public GameObject[] sparkPanels;
	public Transform categoryPanel, chatDetailsPanel;
	public GameObject chatButton;

	[Header ("Chat Window")]
	public GameObject chatWindow;
	public GameObject[] chatPanels;
	public Image statusImage;
	public TextMeshProUGUI titleText, statusText;
	public GameObject senderPanel, respondentPanel, timestampPanel, newMessagePanel, messageText, messageTimeText, linkButton, mediaButton, audioButton, actionButton, messageButton;
	public Transform chatPanel, messageButtonsPanel;
	public GameObject sendMessagePanel, messageOptionsPanel, chatBottomBar;
	public float lerpRate;
	public OnlineStatus[] statusSprites;
	public Sprite[] actionSprites;

	private Button[] categoryButtons;
	private RectTransform chatTransform;
	private Vector2 finalWindowPosition;
	private bool isFading, isGravitating, isOpen;
	private float fadeTimer;

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
		categoryButtons = categoryPanel.GetComponentsInChildren<Button> ();

		PlayerController.instance.SwitchLayer ("Menu1");
		PlayerController.instance.SetSwitchableTabs (categoryButtons);
		PlayerController.instance.SetCapacitiveButtons (mainBottomBar.GetComponentsInChildren<Button> ());

		OpenSparkTab (1);
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
			chatTransform.anchoredPosition = Vector2.Lerp (chatTransform.anchoredPosition, finalWindowPosition, lerpRate);

			if (Vector2.Distance (chatTransform.anchoredPosition, finalWindowPosition) < 1f)
			{
				if (isOpen)
				{
					mainScreen.SetActive (false);
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

	public void OpenSparkTab (int index)
	{
		if (isFading) 
		{
			return;
		}
		FadePanels ();

		switch (index) 
		{
		case 0:
			PlayerController.instance.SetScrollableRect (sparkPanels [index].GetComponent<ScrollRect> ());
			break;
		case 1:
			PlayerController.instance.SetScrollableRect (null);
			break;
		case 2:
			PlayerController.instance.SetScrollableRect (sparkPanels [index].GetComponent<ScrollRect> ());
			break;
		default:
			break;
		}

		for (int i = 0; i < categoryButtons.Length; i++)
		{
			if (i == index)
			{
				categoryButtons [i].interactable = false;
				categoryButtons [i].transform.GetChild (0).gameObject.SetActive (true);
				sparkPanels [i].SetActive (true);
			} 
			else
			{
				categoryButtons [i].interactable = true;
				categoryButtons [i].transform.GetChild (0).gameObject.SetActive (false);
				sparkPanels [i].SetActive (false);
			}
		}

		PlayerController.instance.currentTabIndex = index;
	}

	public void OpenChatsTab (int index)
	{
		if (isFading) 
		{
			return;
		}
		FadePanels ();

		switch (index) 
		{
		case 0:
			break;
		case 1:
			break;
		default:
			break;
		}

		for (int i = 0; i < categoryButtons.Length; i++)
		{
			if (i == index)
			{
				categoryButtons [i].interactable = false;
				categoryButtons [i].transform.GetChild (0).gameObject.SetActive (true);
				chatPanels [i].SetActive (true);
			} 
			else
			{
				categoryButtons [i].interactable = true;
				categoryButtons [i].transform.GetChild (0).gameObject.SetActive (false);
				chatPanels [i].SetActive (false);
			}
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
