using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessageRestoreManager : MonoBehaviour 
{
	public TextMeshProUGUI subtitleText;
	public GameObject errorPanel, messageFragmentsPanel;
	public GameObject fragmentRow, fragmentButton;
	public RectTransform restoredMessageGrid, messageFragmentGrid;

	private List<GameObject> restoredFragments;
	private string[] originalMessage, currentRestoredMessage;


	void Start ()
	{
		InitMessageRestore ("You are the one that went out of line.");
	}

	public void InitMessageRestore (string message)
	{
		originalMessage = message.Split (new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		currentRestoredMessage = new string [originalMessage.Length];

		string[] shuffledMessage = originalMessage;

		for (int i = 0; i < shuffledMessage.Length; i++) 
		{
			int randIndex = UnityEngine.Random.Range (i, shuffledMessage.Length);
			string temp = shuffledMessage [i];

			shuffledMessage [i] = shuffledMessage [randIndex];
			shuffledMessage [randIndex] = temp;
		}
		RectTransform fragmentRowRectTransform = Instantiate (fragmentRow, messageFragmentGrid).GetComponent<RectTransform> ();

		for (int i = 0; i < shuffledMessage.Length; i++) 
		{
			GameObject fragmentPrefab = Instantiate (fragmentButton, fragmentRowRectTransform);
			fragmentButton.GetComponentInChildren<TextMeshProUGUI> ().text = shuffledMessage [i];
			fragmentPrefab.GetComponent<Button> ().onClick.AddListener (() => {
				Debug.Log (messageFragmentGrid.rect.width);
			});
			Debug.Log (messageFragmentGrid.rect.width);
			Debug.Log (fragmentPrefab.GetComponent<RectTransform> ().rect.width);
		}
		Debug.Log (messageFragmentGrid.rect.width);
	}

	public void AddFragment (string fragment)
	{
		for (int i = 0; i < currentRestoredMessage.Length; i++) 
		{
			if (string.IsNullOrEmpty (currentRestoredMessage [i])) 
			{
				currentRestoredMessage [i] = fragment;

				if (i == 0)
				{
					errorPanel.SetActive (false);
				}
				else if (i == currentRestoredMessage.Length - 1) 
				{
					CheckRestoration ();
				}
				break;
			} 
			else if (currentRestoredMessage [i] == fragment)
			{
				currentRestoredMessage [i] = string.Empty;

				if (i == 0)
				{
					errorPanel.SetActive (true);
				}
				break;
			}
		}
	}

	public void SkipRestore ()
	{
		for (int i = 0; i < originalMessage.Length; i++) 
		{
			
		}
	}

	private void CheckRestoration ()
	{
		for (int i = 0; i < originalMessage.Length; i++) 
		{
			if (originalMessage [i] != currentRestoredMessage [i])
			{
				break;
			}

			if (i == originalMessage.Length - 1) 
			{
				Debug.Log ("Message Restored");
			}
		}
	}

}
