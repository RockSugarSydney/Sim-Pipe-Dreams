using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollingMenuController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	public static bool isInteracting, isGravitating;

	public RectTransform bodyPanel, scrollingMenuPanel; 
	public float swipeDistanceThreshold, swipeSpeedThreshold, scrollSensitivity, lerpRate;

	private static int currentPageNumber;

	private List<PointerEventData> pointerData = new List<PointerEventData> (); 
	private Vector2 initialPointerWorldPosition, finalMenuPosition;
	private float touchDuration, initMenuPosX, currentMenuPosX;


	void Start ()
	{
		Vector2 newMenuPos = bodyPanel.anchoredPosition + (Vector2.right * bodyPanel.rect.width * currentPageNumber * -1);
		scrollingMenuPanel.anchoredPosition = newMenuPos;

		isInteracting = false;
		isGravitating = false;
	}

	void Update ()
	{
		if (isInteracting) 
		{
			float currentAnchorPositionX = bodyPanel.anchoredPosition.x - bodyPanel.rect.width * currentPageNumber;
			float xOffset = scrollingMenuPanel.anchoredPosition.x - currentAnchorPositionX;
			float pointerDisplacementX = Camera.main.ScreenToWorldPoint (pointerData [0].position).x - initialPointerWorldPosition.x;
			float minScrollLimit = 0f;
			float maxScrollLimit = 0f;
			int swipeDirection = 0;

			if (pointerDisplacementX != 0f)
			{
				swipeDirection = (int)Mathf.Sign (pointerDisplacementX);
			}

			if (currentPageNumber == 0 && swipeDirection > 0 && xOffset > 0f || currentPageNumber == scrollingMenuPanel.childCount - 1 && swipeDirection < 0 && xOffset < 0f) 
			{
				minScrollLimit = bodyPanel.anchoredPosition.x - bodyPanel.rect.width * currentPageNumber - bodyPanel.rect.width / 10;
				maxScrollLimit = bodyPanel.anchoredPosition.x - bodyPanel.rect.width * currentPageNumber + bodyPanel.rect.width / 10;
			} 
			else
			{
				minScrollLimit = bodyPanel.anchoredPosition.x - bodyPanel.rect.width * currentPageNumber - bodyPanel.rect.width;
				maxScrollLimit = bodyPanel.anchoredPosition.x - bodyPanel.rect.width * currentPageNumber + bodyPanel.rect.width;
			}
			currentMenuPosX = initMenuPosX + pointerDisplacementX * scrollSensitivity;
			currentMenuPosX = Mathf.Clamp (currentMenuPosX, minScrollLimit, maxScrollLimit);

			if (currentMenuPosX == minScrollLimit || currentMenuPosX == maxScrollLimit) 
			{
				initialPointerWorldPosition = Camera.main.ScreenToWorldPoint (pointerData [0].position);
				initMenuPosX = currentMenuPosX;
			}
			scrollingMenuPanel.anchoredPosition = new Vector2 (currentMenuPosX, bodyPanel.anchoredPosition.y);
			touchDuration += Time.deltaTime;
		} 
		else if (isGravitating)
		{
			scrollingMenuPanel.anchoredPosition = Vector2.Lerp (scrollingMenuPanel.anchoredPosition, finalMenuPosition, lerpRate);

			if (Vector2.Distance (scrollingMenuPanel.anchoredPosition, finalMenuPosition) < 1f) 
			{
				scrollingMenuPanel.anchoredPosition = finalMenuPosition;
				isGravitating = false;
			}
		}
	}

	public void OnPointerDown (PointerEventData eventData)
	{
		if (pointerData.Count == 0) 
		{
			initialPointerWorldPosition = Camera.main.ScreenToWorldPoint (eventData.position);
			initMenuPosX = scrollingMenuPanel.anchoredPosition.x;
			currentMenuPosX = initMenuPosX;
            touchDuration = 0f;
			isInteracting = true;
		}
		pointerData.Add (eventData);
	}

	public void OnPointerUp (PointerEventData eventData)
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
						initMenuPosX = scrollingMenuPanel.anchoredPosition.x;
						isInteracting = true;	
					}
					return;
				}
			}
		} 
		else if (swipeDistanceX >= swipeDistanceThreshold || swipeSpeedX >= swipeSpeedThreshold) 
		{
			int page = (int)Mathf.Sign (pointerDisplacementX) * -1;

			if (currentPageNumber + page >= 0 && currentPageNumber + page < scrollingMenuPanel.childCount)
			{ 
				currentPageNumber += page;
			}
			finalMenuPosition = bodyPanel.anchoredPosition + (Vector2.right * bodyPanel.rect.width * currentPageNumber * -1);
			isGravitating = true;
		} 
		else if (!isGravitating) 
		{
			finalMenuPosition = bodyPanel.anchoredPosition + (Vector2.right * bodyPanel.rect.width * currentPageNumber * -1);
			isGravitating = true;
		}
		initialPointerWorldPosition = new Vector2 ();
		initMenuPosX = 0f;
		currentMenuPosX = initMenuPosX;
		isInteracting = false;
		pointerData.Clear ();
	}
}
