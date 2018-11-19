using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NotificationBarController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
	public Image notifierImage;
	public Text titleText, bodyText;
	public float swipeDistanceThreshold, swipeSpeedThreshold, scrollSensitivity, lerpRate, clickDistanceLimit, lifespan;

	private List<PointerEventData> pointerData = new List<PointerEventData> (); 
	private RectTransform rectTransform, parentTransform;
	private Vector2 initialPointerWorldPosition, finalMenuPosition, initClickPosition;
	private bool isInteracting, isGravitating, isOpen, isInteractable;
	private float touchDuration, initBarPosX, currentBarPosX, lifespanTimer;


	void Awake ()
	{
		rectTransform = GetComponent<RectTransform> ();
		parentTransform = transform.parent.GetComponent<RectTransform> ();
	}

	void Update ()
	{
		if (isInteracting) 
		{
			float scrollOffset = Camera.main.ScreenToWorldPoint (pointerData [0].position).x - initialPointerWorldPosition.x;
			currentBarPosX = initBarPosX + scrollOffset * scrollSensitivity;
			currentBarPosX = Mathf.Clamp (currentBarPosX, 0f, rectTransform.rect.width);
			rectTransform.anchoredPosition = new Vector2 (currentBarPosX, rectTransform.anchoredPosition.y);
			touchDuration += Time.deltaTime;
		} 
		else if (isGravitating)
		{
			rectTransform.anchoredPosition = Vector2.Lerp (rectTransform.anchoredPosition, finalMenuPosition, lerpRate);

			if (Vector2.Distance (rectTransform.anchoredPosition, finalMenuPosition) < 1f)
			{
				rectTransform.anchoredPosition = finalMenuPosition;
				isGravitating = false;

				if (isOpen) 
				{
					isInteractable = true;
				}
				else
				{
					CameraController.instance.ReceiveNotification ();
				}
			}
		}

		if (isInteractable)
		{
			if (lifespanTimer < lifespan)
			{
				lifespanTimer += Time.deltaTime;

				if (lifespanTimer >= lifespan) 
				{
					finalMenuPosition = new Vector2 (rectTransform.rect.width, rectTransform.anchoredPosition.y);
					isOpen = false;
					isInteractable = false;
					isInteracting = false;
					isGravitating = true;
				}
			}
		}
	}

	public void OnPointerDown (PointerEventData eventData)
	{
		if (isInteractable) 
		{
			if (pointerData.Count == 0) 
			{
				initialPointerWorldPosition = Camera.main.ScreenToWorldPoint (eventData.position);
				initBarPosX = rectTransform.anchoredPosition.x;
				currentBarPosX = initBarPosX;
				touchDuration = 0f;
				isInteracting = true;
			}
			initClickPosition = Camera.main.ScreenToWorldPoint (eventData.position);
		}
		pointerData.Add (eventData);
	}

	public void OnPointerUp (PointerEventData eventData)
	{
		if (isInteractable)
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
							initBarPosX = rectTransform.anchoredPosition.x;
							isInteracting = true;	
						}
						return;
					}
				}
			} 
			else if (swipeDistanceX >= swipeDistanceThreshold || swipeSpeedX >= swipeSpeedThreshold) 
			{
				int swipeDirection = (int)Mathf.Sign (pointerDisplacementX);

				if (swipeDirection < 0) 
				{
					finalMenuPosition = new Vector2 (0f, rectTransform.anchoredPosition.y);
					isOpen = true;
				} 
				else
				{
					finalMenuPosition = new Vector2 (rectTransform.rect.width, rectTransform.anchoredPosition.y);
					isOpen = false;
				}
				isGravitating = true;
			} 
			else if (!isGravitating) 
			{
				if (isOpen) 
				{
					finalMenuPosition = new Vector2 (0f, rectTransform.anchoredPosition.y);
				}
				else
				{
					finalMenuPosition = new Vector2 (rectTransform.rect.width, rectTransform.anchoredPosition.y);
				}
				isGravitating = true;
			}
			initialPointerWorldPosition = new Vector2 ();
			initBarPosX = 0f;
			currentBarPosX = initBarPosX;
			isInteracting = false;
		}
		pointerData.Clear ();
	}

	public void OnPointerClick (PointerEventData eventData)
	{
		Vector2 lastClickPosition = Camera.main.ScreenToWorldPoint (eventData.position);

		if (Vector2.Distance (lastClickPosition, initClickPosition) <= clickDistanceLimit)
		{
			finalMenuPosition = new Vector2 (rectTransform.rect.width, rectTransform.anchoredPosition.y);
			isInteractable = false;
			isOpen = false;
			isGravitating = true;

			CameraController.instance.OpenNotification ();
		}
		initClickPosition = new Vector2 ();
	}

	public void ShowNotification (float newLifespan)
	{
		lifespan = newLifespan;
		lifespanTimer = 0f;
		rectTransform.anchoredPosition = new Vector2 (rectTransform.rect.width, rectTransform.anchoredPosition.y);
		finalMenuPosition = new Vector2 (0f, rectTransform.anchoredPosition.y);
		isOpen = true;
		isGravitating = true;
	}
}
