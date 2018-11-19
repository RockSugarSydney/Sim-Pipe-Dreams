using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TopBarController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	public void OnPointerDown (PointerEventData eventData)
	{
		QuickAccessMenuManager.instance.OnDown (eventData);
	}

	public void OnPointerUp (PointerEventData eventData)
	{
		QuickAccessMenuManager.instance.OnUp (eventData);
	}
}
