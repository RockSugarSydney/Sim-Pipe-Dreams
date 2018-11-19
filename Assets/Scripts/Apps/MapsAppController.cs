using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapsAppController : MonoBehaviour 
{
	public static MapsAppController instance;

	public Transform mapTransform;
	public float minZoom, maxZoom, zoomSpeed;

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

	public void Zoom (int direction)
	{
		float newScale = Mathf.Clamp (mapTransform.localScale.x + zoomSpeed * direction, minZoom, maxZoom);  
		mapTransform.localScale = new Vector3 (newScale, newScale, 1f);
	}
}
