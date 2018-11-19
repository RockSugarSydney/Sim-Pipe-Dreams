using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GalleryAppController : MonoBehaviour 
{
    public static GalleryAppController instance;

    [System.Serializable]
    public struct MediaInfo
    {
        public string name;
        public MediaManager.MediaType type;
        public bool isInstalled;
    }

    [System.Serializable]
    public struct GalleryGroup
    {
        public string dateInstalled;
        public MediaInfo[] media;
    }

    [System.Serializable]
    public struct GalleryCategory
    {
        public string name;
        public GalleryGroup[] installDates;
    }

    [Header ("Gallery Screen")]
	public GameObject galleryScreen;
	public GameObject galleryBottomBar;
    public ScrollRect galleryScrollRect;
    public GameObject categoryButton;
    public GalleryCategory[] gallery;

    [Header ("Folder Screen")]
	public GameObject folderScreen;
	public GameObject folderBottomBar;
    public ScrollRect folderScrollRect;
    public GameObject mediaPanel, mediaButton, amountTextPanel;

    private List<GameObject> mediaPanels = new List<GameObject> ();


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

    void Start ()
    {
        CameraController.onMediaClose = OpenFolder;

		List<Button> generatedButtons = new List<Button> ();

        for (int i = 0; i < gallery.Length; i++)
        {
			CategoryButtonWrapper categoryWrapper = Instantiate (categoryButton, galleryScrollRect.content).GetComponent<CategoryButtonWrapper> ();
            bool foundImage = false;
            int categoryIndex = i;

            for (int j = 0; j < gallery [i].installDates.Length; j++)
            {
                for (int k = 0; k < gallery [i].installDates [j].media.Length; k++)
                {
                    if (gallery [i].installDates [j].media [k].isInstalled)
                    {
                        if (gallery [i].installDates [j].media [k].type == MediaManager.MediaType.Image)
                        {
                            categoryWrapper.mediaImage.sprite = MediaManager.instance.GetImageInfo (gallery [i].installDates [j].media [k].name).image;
                        }
                        else
                        {
                            categoryWrapper.mediaImage.sprite = MediaManager.instance.GetVideoInfo (gallery [i].installDates [j].media [k].name).thumbnail;
                        }
                        foundImage = true;
                        break;
                    }
                }

                if (foundImage)
                {
                    break;
                }
            }
                
            if (categoryWrapper.mediaImage.sprite.rect.width >= categoryWrapper.mediaImage.sprite.rect.height)
            {
                float newWidth = categoryWrapper.panelLayout.preferredHeight / categoryWrapper.mediaImage.sprite.rect.height * categoryWrapper.mediaImage.sprite.rect.width;
                categoryWrapper.mediaImage.rectTransform.sizeDelta = new Vector2 (newWidth, categoryWrapper.panelLayout.preferredHeight);
            }
            else
            {
                float newHeight = categoryWrapper.panelLayout.preferredWidth / categoryWrapper.mediaImage.sprite.rect.width * categoryWrapper.mediaImage.sprite.rect.height;
                categoryWrapper.mediaImage.rectTransform.sizeDelta = new Vector2 (categoryWrapper.panelLayout.preferredWidth, newHeight);
            }
            categoryWrapper.nameText.text = gallery [i].name;
            categoryWrapper.button.onClick.AddListener(() => {
                ListMedia (categoryIndex);
            });

			generatedButtons.Add (categoryWrapper.button);
        }

		PlayerController.instance.SwitchLayer ("Menu1");
		PlayerController.instance.SetScrollableRect (galleryScrollRect);
		PlayerController.instance.SetSelectableButtons (generatedButtons);
		PlayerController.instance.SetCapacitiveButtons (galleryBottomBar.GetComponentsInChildren<Button> ());
    }

    public void ListMedia (int categoryIndex)
    {
		List<Button> generatedButtons = new List<Button> ();

        for (int i = 0; i < gallery [categoryIndex].installDates.Length; i++)
        {
            MediaInfo[] currentGroup = gallery [categoryIndex].installDates [i].media;
            List<MediaInfo> installedMedia = new List<MediaInfo> ();

            for (int j = 0; j < currentGroup.Length; j++)
            {
                if (currentGroup [j].isInstalled)
                {
                    installedMedia.Add (currentGroup [j]);
                }
            }

            if (installedMedia.Count > 0)
            {
                GameObject panelPrefab;
                MediaButtonWrapper[] mediaButtons;

                if (i > mediaPanels.Count - 1)
                {
					panelPrefab = Instantiate (mediaPanel, folderScrollRect.content);
                    mediaPanels.Add (panelPrefab);
                }
                else 
                {
                    panelPrefab = mediaPanels [i];
                    panelPrefab.SetActive (true);
                }
                DateTime currentDate = DateTime.Parse (gallery [categoryIndex].installDates [i].dateInstalled);
                panelPrefab.GetComponentInChildren<TextMeshProUGUI> ().text = currentDate.ToString ("dd MMM");

                GridLayoutGroup panelGrid = panelPrefab.GetComponentInChildren<GridLayoutGroup> ();
                mediaButtons = panelGrid.GetComponentsInChildren<MediaButtonWrapper> (true);

                for (int j = 0; j < installedMedia.Count; j++)
                {
                    MediaButtonWrapper buttonWrapper;

                    if (j > mediaButtons.Length - 1)
                    {
                        buttonWrapper = Instantiate (mediaButton, panelGrid.transform).GetComponent<MediaButtonWrapper> (); 
                    }
                    else
                    {
                        buttonWrapper = mediaButtons [j];
                        buttonWrapper.button.onClick.RemoveAllListeners ();
                    }
                    string mediaName = installedMedia [j].name;
                    MediaManager.MediaType mediaType = installedMedia [j].type;

                    if (mediaType == MediaManager.MediaType.Image)
                    {
                        buttonWrapper.mediaImage.sprite = MediaManager.instance.GetImageInfo (mediaName).image;
						buttonWrapper.typeImage.transform.parent.gameObject.SetActive (false); 
                    }
                    else
                    {
                        buttonWrapper.mediaImage.sprite = MediaManager.instance.GetVideoInfo (mediaName).thumbnail;
						buttonWrapper.typeImage.transform.parent.gameObject.SetActive (true); 
                    }

                    if (buttonWrapper.mediaImage.sprite.rect.width >= buttonWrapper.mediaImage.sprite.rect.height)
                    {
                        float newWidth = panelGrid.cellSize.y / buttonWrapper.mediaImage.sprite.rect.height * buttonWrapper.mediaImage.sprite.rect.width;
                        buttonWrapper.mediaImage.rectTransform.sizeDelta = new Vector2 (newWidth, panelGrid.cellSize.y);
                    }
                    else
                    {
                        float newHeight = panelGrid.cellSize.x / buttonWrapper.mediaImage.sprite.rect.width * buttonWrapper.mediaImage.sprite.rect.height;
                        buttonWrapper.mediaImage.rectTransform.sizeDelta = new Vector2 (panelGrid.cellSize.x, newHeight);
                    }
                    buttonWrapper.button.onClick.AddListener(() => {
						MediaManager.instance.SelectMedia (mediaName, mediaType);
                        folderScreen.SetActive (false);
                    });
                    buttonWrapper.gameObject.SetActive (true);

					generatedButtons.Add (buttonWrapper.button);
                }

                //Hide unused media buttons
                for (int j = installedMedia.Count; j < mediaButtons.Length; j++)
                {
                    mediaButtons [j].gameObject.SetActive (false);
                }
            }
        }

        //Hide up unused media panels
        for (int i = gallery [categoryIndex].installDates.Length; i < mediaPanels.Count; i++)
        {
            mediaPanels [i].SetActive (false);
        }
//		GameObject amountPrefab = Instantiate (amountTextPanel, folderPanel);

        folderScreen.SetActive (true);
        galleryScreen.SetActive (false);

		PlayerController.instance.SwitchLayer ("Menu2");
		PlayerController.instance.SetScrollableRect (folderScrollRect);
		PlayerController.instance.SetSelectableButtons (generatedButtons);
		PlayerController.instance.SetCapacitiveButtons (folderBottomBar.GetComponentsInChildren<Button> ());
    }

    public void OpenFolder ()
    {
        folderScreen.SetActive (true);

		PlayerController.instance.SwitchLayer ("Menu2");
    }

    public void CloseFolder ()
    {
        galleryScreen.SetActive (true);
        folderScreen.SetActive (false);

		PlayerController.instance.SwitchLayer ("Menu1");
    }

}
