using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ImagePanelController : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public static bool isInteracting, isGravitating;

	public RectTransform contentPanel, imagesParent;
    public Image currentMediaImage, nextMediaImage;
	public float swipeDistanceThreshold, swipeSpeedThreshold, doubleTapWindow, doubleTapDistance, minZoom, maxZoom, zoomSpeed, scrollSensitivity, lerpRate;

    private ScrollRect parentScrollRect;
	private PointerEventData firstPointerData, secondPointerData, dragPointerData; 
    private Vector2 initialTapPos, initialPointerWorldPosition, finalPanelPosition;
    private bool isHovering, isPinching, isDoubleTapping, doubleTapTrigger;
	private float touchDuration, doubleTapTimer, initPanelPosX, currentPanelPosX;
	private float initialDistance, initialScale;


    void Awake ()
    {
        parentScrollRect = GetComponentInParent<ScrollRect> ();
    }

	void Update ()
    {
		if (GalleryAppController.instance != null && contentPanel.localScale.x == minZoom && !isPinching)
		{
			if (isInteracting)
			{
                float scrollOffset = Camera.main.ScreenToWorldPoint (firstPointerData.position).x - initialPointerWorldPosition.x;
                currentPanelPosX = initPanelPosX + scrollOffset * scrollSensitivity;
                currentPanelPosX = Mathf.Clamp (currentPanelPosX, -contentPanel.rect.width, contentPanel.rect.width);
                imagesParent.anchoredPosition = new Vector2 (currentPanelPosX, contentPanel.anchoredPosition.y);

                if (imagesParent.anchoredPosition.x < 0)
                {
                    float newXPos = currentMediaImage.rectTransform.anchoredPosition.x + currentMediaImage.rectTransform.rect.width;
                    nextMediaImage.rectTransform.anchoredPosition = new Vector2 (newXPos, nextMediaImage.rectTransform.anchoredPosition.y);
                } 
                else 
                {
                    float newXPos = currentMediaImage.rectTransform.anchoredPosition.x - currentMediaImage.rectTransform.rect.width;
                    nextMediaImage.rectTransform.anchoredPosition = new Vector2 (newXPos, nextMediaImage.rectTransform.anchoredPosition.y);
                }
                touchDuration += Time.deltaTime;
			} 
			else if (isGravitating)
			{
                imagesParent.anchoredPosition = Vector2.Lerp (imagesParent.anchoredPosition, finalPanelPosition, lerpRate);

				if (Vector2.Distance (imagesParent.anchoredPosition, finalPanelPosition) < 1f) 
                {
                    imagesParent.anchoredPosition = contentPanel.anchoredPosition;
					isGravitating = false;
                }
			}
		}

        if (!isGravitating)
        {
            if (isDoubleTapping)
            {
				Vector2 tapPosition = Camera.main.ScreenToWorldPoint (firstPointerData.position);
                Vector2 initCenterPos = contentPanel.InverseTransformPoint (tapPosition);
                float newScale = contentPanel.localScale.x + zoomSpeed;

                if (newScale > maxZoom)
                {
                    newScale = minZoom;
                }
                contentPanel.localScale = new Vector3 (newScale, newScale, 1f);

                Vector2 finalCenterPos = contentPanel.InverseTransformPoint (tapPosition);
                Vector2 offset = finalCenterPos - initCenterPos;
                contentPanel.localPosition += new Vector3 (offset.x, offset.y) * newScale;
                isDoubleTapping = false;
            }

            #if UNITY_EDITOR || UNTIY_STANDALONE
            if (isHovering && Input.GetAxis ("Mouse ScrollWheel") != 0)
            {
                Vector2 mousePosition = GetCurrentMouseWorldPosition ();
                Vector2 initCenterPos = contentPanel.InverseTransformPoint (mousePosition);
                float scrollSpeed = Input.GetAxis ("Mouse ScrollWheel");
                float newScale = contentPanel.localScale.x + scrollSpeed;

                if (newScale < minZoom) 
                {
                    newScale = minZoom;
                } 
                else if (newScale > maxZoom)
                {
                    newScale = maxZoom;
                }
                contentPanel.localScale = new Vector3 (newScale, newScale, 1f);

                Vector2 finalCenterPos = contentPanel.InverseTransformPoint (mousePosition);
                Vector2 offset = finalCenterPos - initCenterPos;
                contentPanel.localPosition += new Vector3 (offset.x, offset.y) * newScale;
            } 
            #elif UNITY_ANDROID || UNITY_IOS
            if (isPinching)
            {
		        float currentDistance = Vector2.Distance (Camera.main.ScreenToWorldPoint (firstPointerData.position), Camera.main.ScreenToWorldPoint (secondPointerData.position));
		        float difference = currentDistance - initialDistance;
		        float newScale = initialScale + difference;

		        if (newScale < minZoom) 
		        {
			        initialDistance = currentDistance;
			        newScale = minZoom;
			        initialScale = newScale;
		        } 
		        else if (newScale > maxZoom)
		        {
			        initialDistance = currentDistance;
			        newScale = maxZoom;
			        initialScale = newScale;
		        }
		        contentPanel.localScale = new Vector3 (newScale, newScale, 1f);
            }
            #endif
        }

        if (doubleTapTrigger) 
        {
            doubleTapTimer += Time.deltaTime;

            if (doubleTapTimer > doubleTapWindow)
            {
                doubleTapTrigger = false;
            }
        }
	}

	public void OnPointerEnter (PointerEventData eventData)
	{
		isHovering = true;
	}

	public void OnPointerExit (PointerEventData eventData)
	{
		isHovering = false;
	}

	public void OnPointerDown (PointerEventData eventData)
	{
		if (firstPointerData == null) 
		{
            if (GalleryAppController.instance != null && contentPanel.localScale.x == minZoom)
            {
                initPanelPosX = imagesParent.anchoredPosition.x;
                currentPanelPosX = initPanelPosX;
                touchDuration = 0f;
                isInteracting = true;
            }

			#if UNITY_ANDROID || UNITY_IOS
			if (secondPointerData != null) 
			{
				initialDistance = Vector2.Distance (Camera.main.ScreenToWorldPoint (eventData.position), Camera.main.ScreenToWorldPoint (secondPointerData.position));
				initialScale = contentPanel.localScale.x;
				isPinching = true;
			}
			#endif
            initialPointerWorldPosition = Camera.main.ScreenToWorldPoint (eventData.position);
			firstPointerData = eventData;
		}
		#if UNITY_ANDROID || UNITY_IOS
		else
		{
			initialDistance = Vector2.Distance (Camera.main.ScreenToWorldPoint (firstPointerData.position), Camera.main.ScreenToWorldPoint (eventData.position));
			initialScale = contentPanel.localScale.x;
			isPinching = true;
			secondPointerData = eventData;
		}
		#endif

        if (eventData.pointerId == -1 || eventData.pointerId == 0)
        {
            if (!doubleTapTrigger) 
            {
                initialTapPos = Camera.main.ScreenToWorldPoint (eventData.position);
                doubleTapTimer = 0f;
                isDoubleTapping = false;
                doubleTapTrigger = true;
            } 
            else if (Vector2.Distance (initialTapPos, Camera.main.ScreenToWorldPoint (eventData.position)) <= doubleTapDistance)
            {
                doubleTapTrigger = false;
                isDoubleTapping = true;
            }
        }
	}

	public void OnPointerUp (PointerEventData eventData)
    {
		if (eventData.pointerId == -1 || eventData.pointerId == 0)
		{
            if (GalleryAppController.instance != null && contentPanel.localScale.x == minZoom)
            {
                float pointerDisplacementX = Camera.main.ScreenToWorldPoint (firstPointerData.position).x - initialPointerWorldPosition.x;
                float swipeDistanceX = Mathf.Abs (pointerDisplacementX);
                float swipeSpeedX = swipeDistanceX / touchDuration;

                if (swipeDistanceX >= swipeDistanceThreshold || swipeSpeedX >= swipeSpeedThreshold) 
                {
                    int swipeDirection = (int)Mathf.Sign (pointerDisplacementX);

                    if (!isGravitating) 
                    {
                        finalPanelPosition = contentPanel.anchoredPosition + (Vector2.right * contentPanel.rect.width * swipeDirection);
                        isGravitating = true;
                    } 
                    else 
                    {
                        if (finalPanelPosition.x < 0 && swipeDirection > 0 || finalPanelPosition.x > 0 && swipeDirection < 0)
                        {
                            finalPanelPosition = contentPanel.anchoredPosition;
                        }
                        else 
                        {
                            finalPanelPosition = contentPanel.anchoredPosition + (Vector2.right * contentPanel.rect.width * swipeDirection);
                        }
                    }
                }
                else if (!isGravitating && Vector2.Distance (initialTapPos, Camera.main.ScreenToWorldPoint (eventData.position)) > doubleTapDistance)
                {
                    finalPanelPosition = new Vector2 (initPanelPosX, contentPanel.anchoredPosition.y);
                    isGravitating = true;
                }
                initPanelPosX = 0f;
                currentPanelPosX = initPanelPosX;
                isInteracting = false;
            }
			initialPointerWorldPosition = new Vector2 ();
			firstPointerData = null;
		}
		else 
		{
			secondPointerData = null;
		}
		isPinching = false;
	}

    public void OnBeginDrag (PointerEventData eventData)
    {
		if (dragPointerData == null) 
		{
			dragPointerData = eventData;
			parentScrollRect.OnBeginDrag (dragPointerData);
		}
    }

    public void OnDrag (PointerEventData eventData)
    {
		if (dragPointerData == null) 
		{
			dragPointerData = eventData;
			parentScrollRect.OnBeginDrag (dragPointerData);
		} 
		parentScrollRect.OnDrag (dragPointerData);
    }

    public void OnEndDrag (PointerEventData eventData)
    {
		if (dragPointerData != null)
		{
			if (eventData.pointerId == dragPointerData.pointerId)
			{
				parentScrollRect.OnEndDrag (dragPointerData);
				dragPointerData = null;
			}
		}
    }

	private Vector3 GetCurrentMouseWorldPosition (int touchIndex = 0)
	{
		#if UNITY_EDITOR || UNITY_STANDALONE
		return Camera.main.ScreenToWorldPoint (Input.mousePosition);
		#elif UNITY_ANDROID || UNITY_IOS
		return Camera.main.ScreenToWorldPoint (Input.GetTouch (touchIndex).position);
		#endif
	}
}
