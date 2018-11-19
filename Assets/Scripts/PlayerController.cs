using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour 
{
	public static PlayerController instance;

	[System.Serializable]
	public class NavigationButtons 
	{
		public string layerName;
		public List<Button> selectableButtons;
		public Button[] switchableTabs;
		public Button[] capacitiveButtons;
		public Vector3[] scrollRectCorners;
		public ScrollRect scrollableRect;
		public Button selectedButton, negativeButton, previousButton, nextButton, leftTriggerButton, rightTriggerButton;
	}

	public bool isActive;
	public float scrollSensitivity, buttonDetectThreshold, cycleInterval;
	public int currentTabIndex;

	private List<NavigationButtons> navigationLayers = new List<NavigationButtons> ();
	private NavigationButtons activeLayer;
	private bool xAxisDown, xAxisUp = true, yAxisDown, yAxisUp = true, leftTriggerDown, rightTriggerDown;
	private float cycleTimer;


	void Awake ()
	{
		if (instance != this) 
		{
			if (instance != null) 
			{
				instance.isActive = false;
			}
			instance = this;
		}
		isActive = true;
	}

	void Update ()
	{
		if (isActive)
		{
			if (activeLayer.scrollableRect != null) 
			{
				if (activeLayer.selectedButton == null) 
				{
					if (activeLayer.scrollableRect.horizontal) 
					{
						if (Input.GetButton ("Horizontal") || Input.GetAxis ("Horizontal") != 0f)
						{
							float moveDirection = Input.GetAxis ("Horizontal") * -1;
							float newPosX = activeLayer.scrollableRect.content.anchoredPosition.x + scrollSensitivity * moveDirection;

							activeLayer.scrollableRect.content.anchoredPosition = new Vector2 (newPosX, activeLayer.scrollableRect.content.anchoredPosition.y);
						}

						if (Input.GetButtonUp ("Horizontal") || Input.GetAxis ("Horizontal") == 0f && !xAxisUp) 
						{
							if (activeLayer.selectableButtons != null)
							{
								for (int i = 0; i < activeLayer.selectableButtons.Count; i++) 
								{
									if (activeLayer.selectableButtons [i].transform.position.x > activeLayer.scrollRectCorners [0].x && activeLayer.selectableButtons [i].transform.position.x < activeLayer.scrollRectCorners [1].x)
									{
										activeLayer.selectedButton = activeLayer.selectableButtons [i];
										Debug.Log (activeLayer.selectedButton.gameObject.name);
										break;
									}
								}
							}
						}
					}
					else if (Input.GetButtonDown ("Horizontal") || Input.GetAxis ("Horizontal") != 0f && !xAxisDown) 
					{
						float moveDirection = Mathf.Sign (Input.GetAxis ("Horizontal"));
						float newPosY = activeLayer.scrollableRect.content.anchoredPosition.y + activeLayer.scrollableRect.GetComponent<RectTransform> ().rect.height * moveDirection;

						activeLayer.scrollableRect.content.anchoredPosition = new Vector2 (activeLayer.scrollableRect.content.anchoredPosition.x, newPosY);

						xAxisDown = true;
					}

					if (activeLayer.scrollableRect.vertical)
					{
						if (Input.GetButton ("Vertical") || Input.GetAxis ("Vertical") != 0f) 
						{
							float moveDirection = Input.GetAxis ("Vertical") * -1;
							float newPosY = activeLayer.scrollableRect.content.anchoredPosition.y + scrollSensitivity * moveDirection;

							activeLayer.scrollableRect.content.anchoredPosition = new Vector2 (activeLayer.scrollableRect.content.anchoredPosition.x, newPosY); 
						}

						if (Input.GetButtonUp ("Vertical") || Input.GetAxis ("Vertical") == 0f && !yAxisUp) 
						{
							if (activeLayer.selectableButtons != null) 
							{
								for (int i = 0; i < activeLayer.selectableButtons.Count; i++) 
								{
									if (activeLayer.selectableButtons [i].transform.position.y > activeLayer.scrollRectCorners [0].y && activeLayer.selectableButtons [i].transform.position.y < activeLayer.scrollRectCorners [1].y)
									{
										activeLayer.selectedButton = activeLayer.selectableButtons [i];
										Debug.Log (activeLayer.selectedButton.gameObject.name);
										break;
									}
								}
							}
						}
					} 
					else if (Input.GetButtonDown ("Vertical") || Input.GetAxis ("Vertical") != 0f && !yAxisDown) 
					{
						float moveDirection = Mathf.Sign (Input.GetAxis ("Vertical"));
						float newPosX = activeLayer.scrollableRect.content.anchoredPosition.x + activeLayer.scrollableRect.GetComponent<RectTransform> ().rect.width * moveDirection;

						activeLayer.scrollableRect.content.anchoredPosition = new Vector2 (newPosX, activeLayer.scrollableRect.content.anchoredPosition.y);

						yAxisDown = true;
					}
				}
				else
				{
					if (Input.GetButtonUp ("Horizontal") || Input.GetAxis ("Horizontal") == 0f && !xAxisUp) 
					{
						Button newSelectableButton = activeLayer.selectedButton;
						float moveDirection = Mathf.Sign (Input.GetAxis ("Horizontal"));

						if (moveDirection > 0) 
						{
							for (int i = 0; i < activeLayer.selectableButtons.Count; i++) 
							{
								if (activeLayer.selectableButtons [i].transform.position.x > activeLayer.selectedButton.transform.position.x) 
								{
									float currentDistance = Vector2.Distance (newSelectableButton.transform.position, activeLayer.selectedButton.transform.position);
									float newDistance = Vector2.Distance (activeLayer.selectableButtons [i].transform.position, activeLayer.selectedButton.transform.position);

									if (newDistance <= buttonDetectThreshold)
									{
										if (currentDistance == 0f || newDistance < currentDistance) 
										{
											newSelectableButton = activeLayer.selectableButtons [i];
										}
									}
								}
							}
						} 
						else
						{
							for (int i = 0; i < activeLayer.selectableButtons.Count; i++) 
							{
								if (activeLayer.selectableButtons [i].transform.position.x < activeLayer.selectedButton.transform.position.x) 
								{
									float currentDistance = Vector2.Distance (newSelectableButton.transform.position, activeLayer.selectedButton.transform.position);
									float newDistance = Vector2.Distance (activeLayer.selectableButtons [i].transform.position, activeLayer.selectedButton.transform.position);

									if (newDistance <= buttonDetectThreshold)
									{
										if (currentDistance == 0f || newDistance < currentDistance) 
										{
											newSelectableButton = activeLayer.selectableButtons [i];
										}
									}
								}
							}
						}

						if (newSelectableButton != activeLayer.selectedButton) 
						{
							activeLayer.selectedButton = newSelectableButton;

							if (activeLayer.selectedButton.transform.position.x < activeLayer.scrollRectCorners [0].x || activeLayer.selectedButton.transform.position.x > activeLayer.scrollRectCorners [1].x)
							{
								float yOffset = activeLayer.selectedButton.transform.position.y - activeLayer.scrollableRect.transform.position.y;
								activeLayer.scrollableRect.content.position = new Vector3 (activeLayer.scrollableRect.content.transform.position.x, activeLayer.scrollableRect.content.transform.position.y - yOffset, activeLayer.scrollableRect.content.transform.position.z);
							}
							Debug.Log (activeLayer.selectedButton.gameObject.name);
						} 
						else
						{
							Debug.Log (activeLayer.selectedButton.name + "left focus");
							activeLayer.selectedButton = null;
						}
						xAxisUp = true;
					}
					else if (Input.GetButtonUp ("Vertical") || Input.GetAxis ("Vertical") == 0f && !yAxisUp)
					{
						Button newSelectableButton = activeLayer.selectedButton;
						float moveDirection = Mathf.Sign (Input.GetAxis ("Vertical"));

						if (moveDirection > 0) 
						{
							for (int i = 0; i < activeLayer.selectableButtons.Count; i++) 
							{
								if (activeLayer.selectableButtons [i].transform.position.y > activeLayer.selectedButton.transform.position.y) 
								{
									float currentDistance = Vector2.Distance (newSelectableButton.transform.position, activeLayer.selectedButton.transform.position);
									float newDistance = Vector2.Distance (activeLayer.selectableButtons [i].transform.position, activeLayer.selectedButton.transform.position);

									if (newDistance <= buttonDetectThreshold)
									{
										if (currentDistance == 0f || newDistance < currentDistance) 
										{
											Debug.Log (newDistance);
											newSelectableButton = activeLayer.selectableButtons [i];
										}
									}
								}
							}
						} 
						else
						{
							for (int i = 0; i < activeLayer.selectableButtons.Count; i++) 
							{
								if (activeLayer.selectableButtons [i].transform.position.y < activeLayer.selectedButton.transform.position.y) 
								{
									float currentDistance = Vector2.Distance (newSelectableButton.transform.position, activeLayer.selectedButton.transform.position);
									float newDistance = Vector2.Distance (activeLayer.selectableButtons [i].transform.position, activeLayer.selectedButton.transform.position);

									if (newDistance <= buttonDetectThreshold)
									{
										if (currentDistance == 0f || newDistance < currentDistance) 
										{
											Debug.Log (newDistance);
											newSelectableButton = activeLayer.selectableButtons [i];
										}
									}
								}
							}
						}

						if (newSelectableButton != activeLayer.selectedButton) 
						{
							if (newSelectableButton.transform.position.y < activeLayer.scrollRectCorners [0].y || newSelectableButton.transform.position.y > activeLayer.scrollRectCorners [1].y)
							{
								float yOffset = newSelectableButton.transform.position.y - activeLayer.selectedButton.transform.position.y;
								activeLayer.scrollableRect.content.position = new Vector3 (activeLayer.scrollableRect.content.transform.position.x, activeLayer.scrollableRect.content.transform.position.y - yOffset, activeLayer.scrollableRect.content.transform.position.z);
							}
							activeLayer.selectedButton = newSelectableButton;
							Debug.Log (activeLayer.selectedButton.gameObject.name);
						} 
						else
						{
							Debug.Log (activeLayer.selectedButton.name + "left focus");
							activeLayer.selectedButton = null;
						}
						yAxisUp = true;
					}
				}
			} 
			else 
			{
				if (Input.GetButtonDown ("Horizontal") || Input.GetAxis ("Horizontal") != 0f && !xAxisDown) 
				{
					GetButtonHorizontal ();
					xAxisDown = true;
					cycleTimer = 0f;
				}
				else if (Input.GetButtonDown ("Vertical") || Input.GetAxis ("Vertical") != 0f && !yAxisDown)
				{
					GetButtonVertical ();
					yAxisDown = true;
					cycleTimer = 0f;
				}

				if (xAxisDown) 
				{
					cycleTimer += Time.deltaTime;

					if (cycleTimer >= cycleInterval) 
					{
						GetButtonHorizontal ();
						cycleTimer = 0f;
					}
				}
				else if (yAxisDown) 
				{
					cycleTimer += Time.deltaTime;

					if (cycleTimer >= cycleInterval) 
					{
						GetButtonVertical ();
						cycleTimer = 0f;
					}
				}
			}

			if (Input.GetButtonDown ("Submit")) 
			{
				if (activeLayer.selectedButton != null)
				{
					activeLayer.selectedButton.onClick.Invoke ();
				}
			} 
			else if (Input.GetButtonDown ("Fire2"))
			{
				if (activeLayer.capacitiveButtons != null) 
				{
					if (activeLayer.capacitiveButtons.Length >= 1)
					{
						activeLayer.capacitiveButtons [0].onClick.Invoke ();
					}
				}
			} 
			else if (Input.GetButtonDown ("Fire3")) 
			{
				if (activeLayer.capacitiveButtons != null) 
				{
					if (activeLayer.capacitiveButtons.Length >= 2)
					{
						activeLayer.capacitiveButtons [1].onClick.Invoke ();
					}
				}
			}
			else if (Input.GetButtonDown ("Fire4")) 
			{
				if (activeLayer.capacitiveButtons != null)
				{
					if (activeLayer.capacitiveButtons.Length >= 3) 
					{
						activeLayer.capacitiveButtons [2].onClick.Invoke ();
					}
				}
				else if (activeLayer.negativeButton != null)
				{
					activeLayer.negativeButton.onClick.Invoke ();
				}
			}
			else if (Input.GetButtonDown ("Cycle Tab")) 
			{
				if (activeLayer.switchableTabs != null) 
				{
					float cycleDirection = Input.GetAxis ("Cycle Tab");

					if (cycleDirection > 0) 
					{
						int newIndex = currentTabIndex + 1;

						if (newIndex != activeLayer.switchableTabs.Length)
						{
							activeLayer.switchableTabs [newIndex].onClick.Invoke ();
						}
					}
					else 
					{
						int newIndex = currentTabIndex - 1;

						if (newIndex != -1)
						{
							activeLayer.switchableTabs [newIndex].onClick.Invoke ();
						}
					}
				} 
				else 
				{
					float cycleDirection = Input.GetAxis ("Cycle Tab");

					if (cycleDirection > 0) 
					{
						if (activeLayer.nextButton != null)
						{
							activeLayer.nextButton.onClick.Invoke ();
						}
					}
					else if (activeLayer.previousButton != null)
					{
						activeLayer.previousButton.onClick.Invoke ();
					}
				}
			}
			else if (Input.GetButtonDown ("Cancel")) 
			{
				QuickAccessMenuManager.instance.ToggleMenu ();
			}
			else if (Input.GetButtonDown ("Left Trigger") || Input.GetAxis ("Left Trigger") != 0f && !leftTriggerDown) 
			{
				if (activeLayer.leftTriggerButton != null)
				{
					activeLayer.leftTriggerButton.onClick.Invoke ();
				}
			}
			else if (Input.GetButtonDown ("Right Trigger") || Input.GetAxis ("Right Trigger") != 0f && !rightTriggerDown) 
			{
				if (activeLayer.rightTriggerButton != null) 
				{
					activeLayer.rightTriggerButton.onClick.Invoke ();
				}
			}

			if (Input.GetAxis ("Horizontal") == 0f)
			{
				if (xAxisDown)
				{
					xAxisDown = false;
				}
			}
			else if (!xAxisDown) 
			{
				xAxisDown = true;
			}

			if (Input.GetAxis ("Vertical") == 0f) 
			{
				if (yAxisDown) 
				{
					yAxisDown = false;
				}
			}
			else if (!yAxisDown)
			{
				yAxisDown = true;
			}

			if (Input.GetAxisRaw ("Horizontal") == 0f) 
			{
				if (!xAxisUp) 
				{
					xAxisUp = true;
				}
			}
			else if (xAxisUp)
			{
				xAxisUp = false;
			}

			if (Input.GetAxisRaw ("Vertical") == 0f)
			{
				if (!yAxisUp)
				{
					yAxisUp = true;
				}
			}
			else if (yAxisUp)
			{
				yAxisUp = false;
			}

			if (Input.GetAxis ("Left Trigger") == 0f)
			{
				if (leftTriggerDown)
				{
					leftTriggerDown = false;
				}
			}
			else if (!leftTriggerDown) 
			{
				leftTriggerDown = true;
			}

			if (Input.GetAxis ("Right Trigger") == 0f)
			{
				if (rightTriggerDown)
				{
					rightTriggerDown = false;
				}
			}
			else if (!rightTriggerDown) 
			{
				rightTriggerDown = true;
			}
		}
	}

	public void SetScrollableRect (ScrollRect newScrollRect)
	{
		activeLayer.scrollableRect = newScrollRect;

		if (activeLayer.scrollableRect != null) 
		{
			StartCoroutine (GetWorldCorners ());
		}
	}

	public void SetSelectableButtons (List<Button> newSelectableButtons, int previousIndex = 0)
    {
		if (newSelectableButtons != null) 
		{
			if (newSelectableButtons.Count != 0)
			{
				activeLayer.selectableButtons = newSelectableButtons;

				if (previousIndex == 0)
				{
					if (activeLayer.scrollableRect == null) 
					{
						activeLayer.selectedButton = activeLayer.selectableButtons [0];
					}
					else 
					{
						StartCoroutine (LocateButton ());
					}
				} 
				else
				{
					activeLayer.selectedButton = activeLayer.selectableButtons [previousIndex];
				}
			}
			else 
			{
				activeLayer.selectableButtons = null;
				activeLayer.selectedButton = null;
			}
		}
		else 
		{
			activeLayer.selectableButtons = newSelectableButtons;
			activeLayer.selectedButton = null;
		}
    }

	public void AddSelectableButtons (Button newSelectableButton, string layerName)
	{
		for (int i = 0; i < navigationLayers.Count; i++) 
		{
			if (navigationLayers [i].layerName == layerName)
			{
				navigationLayers [i].selectableButtons.Add (newSelectableButton);
				break;
			}
		}
	}

	public void SetSwitchableTabs (Button[] newSwitchableTabs)
	{
		activeLayer.switchableTabs = newSwitchableTabs;
	}

	public void SetCapacitiveButtons (Button[] newCapacitiveButtons)
	{
		activeLayer.capacitiveButtons = newCapacitiveButtons;
	}

	public void SetNegativeButton (Button newButton)
	{
		activeLayer.negativeButton = newButton;
	}

	public void SetLeftTriggerButton (Button newButton)
	{
		activeLayer.leftTriggerButton = newButton;
	}

	public void SetRightTriggerButton (Button newButton)
	{
		activeLayer.rightTriggerButton = newButton;
	}

	public void SwitchLayer (string name)
	{
		activeLayer = null;

		for (int i = 0; i < navigationLayers.Count; i++)
		{
			if (navigationLayers [i].layerName == name) 
			{
				activeLayer = navigationLayers [i];
				break;
			}
		}

		if (activeLayer == null) 
		{
			NavigationButtons newLayer = new NavigationButtons ();
			newLayer.layerName = name;
			navigationLayers.Add (newLayer);
			activeLayer = newLayer;
		}
	}

	public NavigationButtons GetActiveLayer ()
	{
		return activeLayer;
	}

	private IEnumerator GetWorldCorners ()
	{
		yield return null;

		activeLayer.scrollRectCorners = new Vector3 [4];
		activeLayer.scrollableRect.GetComponent<RectTransform> ().GetWorldCorners (activeLayer.scrollRectCorners);
	}

	private IEnumerator LocateButton ()
	{
		yield return null;

		for (int i = 0; i < activeLayer.selectableButtons.Count; i++)
		{
			if (activeLayer.selectableButtons [i].transform.position.x > activeLayer.scrollRectCorners [0].x && activeLayer.selectableButtons [i].transform.position.x < activeLayer.scrollRectCorners [3].x && activeLayer.selectableButtons [i].transform.position.y > activeLayer.scrollRectCorners [0].y && activeLayer.selectableButtons [i].transform.position.y < activeLayer.scrollRectCorners [1].y)
			{
				activeLayer.selectedButton = activeLayer.selectableButtons [i];
				break;
			}
		}
	}

	private void GetButtonHorizontal ()
	{
		Button newSelectableButton = activeLayer.selectedButton;
		float moveDirection = Mathf.Sign (Input.GetAxis ("Horizontal"));

		if (moveDirection > 0) 
		{
			for (int i = 0; i < activeLayer.selectableButtons.Count; i++) 
			{
				if (activeLayer.selectableButtons [i].transform.position.x > activeLayer.selectedButton.transform.position.x) 
				{
					float currentDistance = Vector2.Distance (newSelectableButton.transform.position, activeLayer.selectedButton.transform.position);
					float newDistance = Vector2.Distance (activeLayer.selectableButtons [i].transform.position, activeLayer.selectedButton.transform.position);

					if (currentDistance == 0f || newDistance < currentDistance) 
					{
						newSelectableButton = activeLayer.selectableButtons [i];
					}
				}
			}
		} 
		else
		{
			for (int i = 0; i < activeLayer.selectableButtons.Count; i++) 
			{
				if (activeLayer.selectableButtons [i].transform.position.x < activeLayer.selectedButton.transform.position.x) 
				{
					float currentDistance = Vector2.Distance (newSelectableButton.transform.position, activeLayer.selectedButton.transform.position);
					float newDistance = Vector2.Distance (activeLayer.selectableButtons [i].transform.position, activeLayer.selectedButton.transform.position);

					if (currentDistance == 0f || newDistance < currentDistance) 
					{
						newSelectableButton = activeLayer.selectableButtons [i];
					}
				}
			}
		}
		activeLayer.selectedButton = newSelectableButton;
		Debug.Log (activeLayer.selectedButton.gameObject.name);
	}

	private void GetButtonVertical ()
	{
		Button newSelectableButton = activeLayer.selectedButton;
		float moveDirection = Mathf.Sign (Input.GetAxis ("Vertical"));

		if (moveDirection > 0) 
		{
			for (int i = 0; i < activeLayer.selectableButtons.Count; i++) 
			{
				if (activeLayer.selectableButtons [i].transform.position.y > activeLayer.selectedButton.transform.position.y) 
				{
					float currentDistance = Vector2.Distance (newSelectableButton.transform.position, activeLayer.selectedButton.transform.position);
					float newDistance = Vector2.Distance (activeLayer.selectableButtons [i].transform.position, activeLayer.selectedButton.transform.position);

					if (currentDistance == 0f || newDistance < currentDistance) 
					{
						newSelectableButton = activeLayer.selectableButtons [i];
					}
				}
			}
		} 
		else
		{
			for (int i = 0; i < activeLayer.selectableButtons.Count; i++) 
			{
				if (activeLayer.selectableButtons [i].transform.position.y < activeLayer.selectedButton.transform.position.y) 
				{
					float currentDistance = Vector2.Distance (newSelectableButton.transform.position, activeLayer.selectedButton.transform.position);
					float newDistance = Vector2.Distance (activeLayer.selectableButtons [i].transform.position, activeLayer.selectedButton.transform.position);

					if (currentDistance == 0f || newDistance < currentDistance) 
					{
						newSelectableButton = activeLayer.selectableButtons [i];
					}
				}
			}
		}
		activeLayer.selectedButton = newSelectableButton;
		Debug.Log (activeLayer.selectedButton.gameObject.name);
	}
}
