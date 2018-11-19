using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VloggrAppController : MonoBehaviour 
{
	public static VloggrAppController instance;

	public ScrollRect galleryScrollRect;
	public GameObject bottomBar;
	public GameObject vlogGroupPanel, vlogButton;


	void Start () 
	{
		PlayerController.instance.SetScrollableRect (galleryScrollRect);
		PlayerController.instance.SetCapacitiveButtons (bottomBar.GetComponentsInChildren<Button> ());
	}
}
