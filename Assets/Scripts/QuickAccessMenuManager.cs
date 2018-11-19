using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuickAccessMenuManager : MonoBehaviour 
{
	public static QuickAccessMenuManager instance;

	public RectTransform menuTransform;
	public RectTransform[] panelTransforms;
	public float swipeDistanceThreshold, swipeSpeedThreshold, scrollSensitivity, lerpRate;

	private List<PointerEventData> pointerData = new List<PointerEventData> (); 
	private Vector2[] finalMenuPosition;
	private Vector2 initialPointerWorldPosition;
	private bool isInteracting, isGravitating, isOpen;
	private float touchDuration, initPanelPosX, currentPanelPosX;
	private int interactionLevel;


	void Awake ()
	{
		if (instance == null)
		{
			instance = this;
		} 
		else if (instance != this)
		{
			Destroy (gameObject);
		}
	}

	void Update ()
	{
		if (isInteracting) 
		{
			for (int i = 0; i < interactionLevel; i++)
			{
				float scrollOffset = Camera.main.ScreenToWorldPoint (pointerData [0].position).x - initialPointerWorldPosition.x;
				currentPanelPosX = initPanelPosX + scrollOffset * scrollSensitivity;
				currentPanelPosX = Mathf.Clamp (currentPanelPosX, 0f, panelTransforms [i].rect.width);
				panelTransforms [i].anchoredPosition = new Vector2 (currentPanelPosX, panelTransforms [i].anchoredPosition.y);
			}
			touchDuration += Time.deltaTime;
		} 
		else if (isGravitating)
		{
			int panelAnchored = 0;

			for (int i = 0; i < interactionLevel; i++)
			{
				panelTransforms [i].anchoredPosition = Vector2.Lerp (panelTransforms [i].anchoredPosition, finalMenuPosition [i], lerpRate);

				if (Vector2.Distance (panelTransforms [i].anchoredPosition, finalMenuPosition [i]) < 1f) 
				{
					panelAnchored++;
					panelTransforms [i].anchoredPosition = finalMenuPosition [i];
				}
			}

			if (panelAnchored == interactionLevel) 
			{
				isGravitating = false;
			}
		}
	}

	public void OnDown (PointerEventData eventData)
	{
		if (pointerData.Count == 0) 
		{
			initialPointerWorldPosition = Camera.main.ScreenToWorldPoint (eventData.position);
			initPanelPosX = panelTransforms [0].anchoredPosition.x;
			currentPanelPosX = initPanelPosX;
			touchDuration = 0f;
			isInteracting = true;
		}

		if (!isGravitating) 
		{
			if (CameraController.CurrentActivity == "PhoneMenu" || isOpen && interactionLevel == 3)
			{
				interactionLevel = 3;
			} 
			else 
			{
				interactionLevel = 1;
			}
		}
		pointerData.Add (eventData);
	}

	public void OnUp (PointerEventData eventData)
	{
		float pointerDisplacementX = Camera.main.ScreenToWorldPoint (pointerData [0].position).x - initialPointerWorldPosition.x;
		float swipeDistanceX = Mathf.Abs (pointerDisplacementX);
		float swipeSpeedX = swipeDistanceX / touchDuration;

		if (pointerData.Count > 1) 
		{
			for (int i = 0; i < pointerData.Count; i++) 
			{
				if (pointerData [i].pointerId == eventData.pointerId) 
				{
					pointerData.RemoveAt (i);

					if (i == 0) 
					{
						initialPointerWorldPosition = Camera.main.ScreenToWorldPoint (pointerData [0].position);
						touchDuration = 0f;
						initPanelPosX = panelTransforms [0].anchoredPosition.x;
						isInteracting = true;	
					}
					return;
				}
			}
		} 
		else if (swipeDistanceX >= swipeDistanceThreshold || swipeSpeedX >= swipeSpeedThreshold) 
		{
			int swipeDirection = (int)Mathf.Sign (pointerDisplacementX);

			finalMenuPosition = new Vector2 [interactionLevel];

			if (swipeDirection < 0) 
			{
				for (int i = 0; i < interactionLevel; i++) 
				{
					finalMenuPosition [i] = new Vector2 (menuTransform.anchoredPosition.x, panelTransforms [i].anchoredPosition.y);
				}
				isOpen = true;
			} 
			else
			{
				for (int i = 0; i < interactionLevel; i++) 
				{
					finalMenuPosition [i] = new Vector2 (menuTransform.anchoredPosition.x + menuTransform.rect.width, panelTransforms [i].anchoredPosition.y);
				}
				isOpen = false;
			}
			isGravitating = true;
		} 
		else if (!isGravitating) 
		{
			finalMenuPosition = new Vector2 [interactionLevel];

			for (int i = 0; i < interactionLevel; i++)
			{
				if (isOpen) 
				{
					finalMenuPosition [i] = new Vector2 (menuTransform.anchoredPosition.x, panelTransforms [i].anchoredPosition.y);
				}
				else
				{
					finalMenuPosition [i] = new Vector2 (menuTransform.anchoredPosition.x + menuTransform.rect.width, panelTransforms [i].anchoredPosition.y);
				}
			}
			isGravitating = true;
		}
		initialPointerWorldPosition = new Vector2 ();
		initPanelPosX = 0f;
		currentPanelPosX = initPanelPosX;
		isInteracting = false;
		pointerData.Clear ();
	}

	public void ToggleMenu ()
	{
		if (!isGravitating) 
		{
			if (CameraController.CurrentActivity == "PhoneMenu" || isOpen && interactionLevel == 3)
			{
				interactionLevel = 3;
			} 
			else 
			{
				interactionLevel = 1;
			}

			if (!isOpen)
			{
				QuickAccessMenuManager.instance.SetMenuPosition (0);
			}
			else 
			{
				QuickAccessMenuManager.instance.SetMenuPosition (1);
			}
		}
	}

	public void ToggleMenu (int position)
	{
		instance.interactionLevel = 3;
		QuickAccessMenuManager.instance.SetMenuPosition (position);
	}

	public void SetMenuPosition (int position)
	{
		finalMenuPosition = new Vector2 [interactionLevel];

		if (position == 0) 
		{
			for (int i = 0; i < interactionLevel; i++)
			{
				finalMenuPosition [i] = new Vector2 (menuTransform.anchoredPosition.x, panelTransforms [i].anchoredPosition.y);
			}
			isOpen = true;
		}
		else
		{
			for (int i = 0; i < interactionLevel; i++)
			{
				finalMenuPosition [i] = new Vector2 (menuTransform.anchoredPosition.x + menuTransform.rect.width, panelTransforms [i].anchoredPosition.y);
			}
			isOpen = false;
		}
		initialPointerWorldPosition = new Vector2 ();
		initPanelPosX = 0f;
		currentPanelPosX = initPanelPosX;
		isInteracting = false;
		isGravitating = true;
		pointerData.Clear ();
	}

}
