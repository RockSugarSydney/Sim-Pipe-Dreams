using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class LockScreenWrapper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler 
{
	public static bool isInteracting, isGravitating;

	public GameObject unlockPanel;
	public RectTransform informationPanel, contentTransform;
	public TextMeshProUGUI timeText, dateText, inputText;
	public float swipeDistanceThreshold, swipeSpeedThreshold, scrollSensitivity, lerpRate;

	private PointerEventData firstPointerData; 
	private Vector2 initialPointerWorldPosition, finalPanelPosition;
	private bool unlockPhone;
	private float touchDuration, initPanelPosY, currentPanelPosY;
	private float initialDistance;
	private string password;

	void Update ()
	{
		if (informationPanel.gameObject.activeSelf)
		{
			if (isInteracting) 
			{
				float scrollOffset = Camera.main.ScreenToWorldPoint (firstPointerData.position).y - initialPointerWorldPosition.y;
				currentPanelPosY = initPanelPosY + scrollOffset * scrollSensitivity;
				currentPanelPosY = Mathf.Clamp (currentPanelPosY, 0f, informationPanel.rect.height);
				contentTransform.anchoredPosition = new Vector2 (contentTransform.anchoredPosition.x, currentPanelPosY);

				touchDuration += Time.deltaTime;
			} 
			else if (isGravitating) 
			{
				contentTransform.anchoredPosition = Vector2.Lerp (contentTransform.anchoredPosition, finalPanelPosition, lerpRate);

				if (Vector2.Distance (contentTransform.anchoredPosition, finalPanelPosition) < 1f) 
				{
					contentTransform.anchoredPosition = finalPanelPosition;
					isGravitating = false;

					if (unlockPhone)
					{
						informationPanel.gameObject.SetActive (false);
						unlockPanel.SetActive (true);
					}
				}
			}
		}
	}

	public void OnPointerDown (PointerEventData eventData)
	{
		if (firstPointerData == null)
		{
			firstPointerData = eventData;
			initialPointerWorldPosition = Camera.main.ScreenToWorldPoint (eventData.position);
			initPanelPosY = informationPanel.anchoredPosition.y;
			currentPanelPosY = initPanelPosY;
			isInteracting = true;
			touchDuration = 0f;
		}
	}

	public void OnPointerUp (PointerEventData eventData)
	{
		float pointerDisplacementY = Camera.main.ScreenToWorldPoint (firstPointerData.position).y - initialPointerWorldPosition.y;
		float swipeDistanceY = Mathf.Abs (pointerDisplacementY);
		float swipeSpeedY = swipeDistanceY / touchDuration;

		if (swipeDistanceY >= swipeDistanceThreshold || swipeSpeedY >= swipeSpeedThreshold)
		{
			finalPanelPosition = new Vector2 (informationPanel.anchoredPosition.x, informationPanel.rect.height);
			unlockPhone = true;
			isGravitating = true;
		}
		else if (currentPanelPosY != 0f)
		{
			finalPanelPosition = new Vector2 ();
			isGravitating = true;
		}
		firstPointerData = null;
		initPanelPosY = 0f;
		currentPanelPosY = initPanelPosY;
		isInteracting = false;
	}

	public void InitLockScreen ()
	{
		inputText.text = "Input Password";
		password = "";
		unlockPanel.SetActive (false);
	}

	public void EnterPassword (string number)
	{
		if (number != "Delete")
		{
			password += number;
			inputText.text = "Input Password";
		}
		else if (password.Length > 0)
		{
			password = password.Remove (password.Length - 1);
		}

		if (password == "0123")
		{
			inputText.text = "Welcome";
			CameraController.instance.LockPhone (false);
		}
		else if (password.Length >= 4)
		{
			password = "";
			inputText.text = "<color=red>Wrong Password</color>";
		}
	}
}
