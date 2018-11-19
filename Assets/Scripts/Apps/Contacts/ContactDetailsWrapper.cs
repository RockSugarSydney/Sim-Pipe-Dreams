using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContactDetailsWrapper : MonoBehaviour
{
	[System.Serializable]
	public struct CallType
	{
		public Sprite sprite;
		public Color color;
	}

	[Header ("Profile")]
	public Image profileImage;

	[Header ("Call Information")]
	public Button detailsButton;
	public TextMeshProUGUI contactNameText, callTimeText;
	public Image callStatusImage;
	public Image callImage;
	public TextMeshProUGUI callCategoryText;
	public CallType[] callSprites;
}
