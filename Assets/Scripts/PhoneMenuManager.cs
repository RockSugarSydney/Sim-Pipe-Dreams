using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhoneMenuManager : MonoBehaviour 
{
    public static PhoneMenuManager instance;

    [System.Serializable]
    public struct AppInfo
    {
		public bool isInstalled;
        public string name;
        public Sprite icon;
		public int notification;
    }

    [System.Serializable]
    public struct AppList
    {
        public AppInfo[] installedApps;
    }
		
    [Header ("Menu Screen")]
	public GameObject menuScreen; 
	public TextMeshProUGUI timeText, dateText;

	[Header ("App List")]
	public GameObject appButton;
	public Transform[] menuPanels;
    public List<AppList> appList;

	private static List<AppList> applications;

	private List<AppButtonWrapper> appButtons = new List<AppButtonWrapper> ();


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

		if (applications == null)
		{
			applications = appList;
		}
    }

	void Start ()
	{
		PlayerController.instance.SwitchLayer ("Menu1");
		ListApps ();
		dateText.text = DateTime.Now.ToString ("ddd, dd MMMM");
	}

	void Update ()
	{
		timeText.text = DateTime.Now.ToString ("hh:mm tt");
	}

	public static void UpdateNotification (string appName, int amount)
	{
		for (int i = 0; i < applications.Count; i++)
		{
			for (int j = 0; j < applications [i].installedApps.Length; j++)
			{
				if (applications [i].installedApps [j].isInstalled)
				{
					if (applications [i].installedApps [j].name == appName) 
					{
						applications [i].installedApps [j].notification = amount;

						if (applications [i].installedApps [j].notification < 0) 
						{
							applications [i].installedApps [j].notification = 0;
						}
						break;
					}
				}
			}
		}
	}

	public void UpdateNotificationBadge (string appName)
	{
		int amount = 0;

		for (int i = 0; i < applications.Count; i++)
		{
			for (int j = 0; j < applications [i].installedApps.Length; j++)
			{
				if (applications [i].installedApps [j].isInstalled)
				{
					if (applications [i].installedApps [j].name == appName) 
					{
						amount = applications [i].installedApps [j].notification;
						break;
					}
				}
			}
		}

		for (int i = 0; i < appButtons.Count; i++)
		{
			if (appButtons [i].name == appName) 
			{
				appButtons [i].notificationText.text = amount.ToString ();
				appButtons [i].notificationBadge.SetActive (true);
				break;
			}
		}
	}

    public void OpenApp (GameObject selectedApp)
    {
        if (!ScrollingMenuController.isGravitating)
        {
            StartCoroutine (CameraController.instance.LoadApp (selectedApp));

			for (int i = 0; i < appButtons.Count; i++)
			{
				appButtons [i].button.interactable = false;
			}
			PlayerController.instance.isActive = false;
        }
    }

    private void ListApps ()
    {
		List<Button> generatedButtons = new List<Button> ();
		string previousApp = "";
		int installedApps = 0;
		int previousButtonIndex = 0;

		if (!String.IsNullOrEmpty (CameraController.PreviousActivity)) 
		{
			previousApp = CameraController.PreviousActivity.Split (new Char[] { '_' }, StringSplitOptions.RemoveEmptyEntries) [0];
		}

		for (int i = 0; i < applications.Count; i++)
        {
			Transform root = menuPanels [i];

			for (int j = 0; j < applications [i].installedApps.Length; j++)
            {
				AppInfo app = applications [i].installedApps [j];

				if (app.isInstalled)
				{
					GameObject buttonPrefab = Instantiate (appButton, root);
					AppButtonWrapper buttonWrapper = buttonPrefab.GetComponent<AppButtonWrapper> ();
					buttonWrapper.appIconImage.sprite = app.icon;
					buttonWrapper.appNameText.text = app.name;
					buttonPrefab.name = app.name;

					if (root.name == "BottomBar")
					{
						buttonWrapper.appNameText.gameObject.SetActive (false);
					}

					if (app.notification > 0) 
					{
						buttonWrapper.notificationText.text = app.notification.ToString ();
						buttonWrapper.notificationBadge.SetActive (true);
					}
					else
					{
						buttonWrapper.notificationBadge.SetActive (false);
					}
					buttonWrapper.button.onClick.AddListener (() => {
						OpenApp (buttonPrefab);
					});
					appButtons.Add (buttonWrapper);
					installedApps++;

					if (app.name == previousApp)
					{
						previousButtonIndex = installedApps - 1;
					}
					generatedButtons.Add (buttonPrefab.GetComponent<Button> ());
				}
				else
				{
					
				}
            }
        }
		PlayerController.instance.SetSelectableButtons (generatedButtons, previousButtonIndex);
    }

}
