using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatDetailsWrapper : MonoBehaviour 
{
	[System.Serializable]
	public struct OnlineStatus
	{
		public Sprite sprite;
		public Color color;
	}
	public Button detailsButton;

	[Header ("Profile")]
	public Image profileImage;
//	public GameObject profileFrame;

	[Header ("Chat Information")]
	public TextMeshProUGUI contactNameText;
	public TextMeshProUGUI chatExcerptText;
	public Image statusImage;
	public GameObject notificationButton;
	public TextMeshProUGUI timeText;
    public OnlineStatus[] statusSprites;
}
