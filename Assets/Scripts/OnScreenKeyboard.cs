using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OnScreenKeyboard : MonoBehaviour 
{
	public delegate void SubmitEntry (string result);
	public static SubmitEntry onEnter;

	public TMP_InputField inputField;
	public GameObject alphabetKeyboardPanel, symbolKeyboardPanel;
	public float fullstopWindow;

	private TextMeshProUGUI[] buttonText;
	private string entry;
	private bool uppercase, doubleSpace;
	private float doubleSpaceTimer;


	void Awake ()
	{
		buttonText = alphabetKeyboardPanel.GetComponentsInChildren<TextMeshProUGUI> ();
	}

	void Update ()
	{
		if (doubleSpace && doubleSpaceTimer < fullstopWindow) 
		{
			doubleSpaceTimer += Time.deltaTime;

			if (doubleSpaceTimer >= fullstopWindow) 
			{
				doubleSpace = false;
			}
		}
	}

    public void InitializeKeyboard ()
    {
        entry = "";
		uppercase = true;
        ChangeLetterCase ();
		SwitchKeyboard (0);
    }

	public void InputKeyboard (string input)
	{
		switch (input)
		{
		case "Shift":
			if (alphabetKeyboardPanel.activeSelf) 
			{
				uppercase = !uppercase;
				ChangeLetterCase ();
			}
			else if (symbolKeyboardPanel.activeSelf)
			{
				uppercase = false;
				ChangeLetterCase ();
				SwitchKeyboard (0);
			}
			break;
		case "Space":
			if (!doubleSpace || !Regex.IsMatch (entry, @"[^\.\s]\s{1}$"))
			{
				entry += " ";

				if (Regex.IsMatch (entry, @"\.\s*$") && !uppercase) 
				{
					uppercase = true;
					ChangeLetterCase ();
				}
				else if (Regex.IsMatch (entry, @"\S") && !Regex.IsMatch (entry, @"\.\s*$") && uppercase) 
				{
					uppercase = false;
					ChangeLetterCase ();
				} 
				doubleSpaceTimer = 0f;
			}
			else
			{
				entry = entry.Insert (entry.Length - 1, ".");

				if (!uppercase) 
				{
					uppercase = true;
					ChangeLetterCase ();
				}
			}
			doubleSpace = !doubleSpace;
			break;
		case "Delete":
			if (entry.Length > 0)
			{
				entry = entry.Remove (entry.Length - 1);

				if (Regex.IsMatch (entry, @"\.\s+$") && !uppercase || entry.Length == 0 && !uppercase) 
				{
					uppercase = true;
					ChangeLetterCase ();
				} 
				else if (Regex.IsMatch (entry, @"\S$") && uppercase) 
				{
					uppercase = false;
					ChangeLetterCase ();
				}
				doubleSpace = false;
			}
			break;
		case "Enter":
			if (Regex.IsMatch (entry, @"\S")) 
			{
				onEnter (entry);
				entry = "";

				if (!uppercase)
				{
					uppercase = true;
					ChangeLetterCase ();
				}
				doubleSpace = false;
			}
			break;
		default:
			if (uppercase) 
			{
				entry += input;
				uppercase = false;
				ChangeLetterCase ();
			} 
			else 
			{
				entry += input.ToLower ();
			}
			doubleSpace = false;
			break;
		}
		inputField.text = entry;
	}

	public void SwitchKeyboard (int keyboard)
	{
		List<Button> keyboardButtons = new List<Button> ();

		switch (keyboard) 
		{
		case 0:
			alphabetKeyboardPanel.SetActive (true);
			symbolKeyboardPanel.SetActive (false);

			alphabetKeyboardPanel.GetComponentsInChildren (keyboardButtons);
			break;
		case 1:
			alphabetKeyboardPanel.SetActive (false);
			symbolKeyboardPanel.SetActive (true);

			symbolKeyboardPanel.GetComponentsInChildren (keyboardButtons);
			break;
		default:
			break;
		}
		PlayerController.instance.SetSelectableButtons (keyboardButtons);
	}

	public string Entry
	{
		get 
		{
			return entry;
		}

		set
		{
			entry = value;
		}
	}

	private void ChangeLetterCase ()
	{
		for (int i = 0; i < buttonText.Length; i++) 
		{
			if (Regex.IsMatch (buttonText [i].text, @"[a-zA-Z]") && buttonText [i].text.Length < 2)
			{
				if (uppercase) 
				{
					buttonText [i].text = buttonText [i].text.ToUpper ();
				} 
				else
				{
					buttonText [i].text = buttonText [i].text.ToLower ();
				}
			}
		}
	}
}
