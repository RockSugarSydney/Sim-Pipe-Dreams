using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ImageRestoreManager : MonoBehaviour 
{
	public enum FragmentCondition {
		Empty,
		Corrupted,
		Recovered
	};

	public struct ImageFragment
	{
		public FragmentCondition[] fragmentConditions;
	}

	[System.Serializable]
	public struct CorruptedImage
	{
		public string name;
		public ImageFragment[] fragments;
	}
	public TextMeshProUGUI subtitleText;
	public Image restoreImage;
	public GameObject errorPanel, imageFragmentsPanel;
	public GameObject imageFragmentButton;
	public RectTransform imageFragmentGrid;
	public CorruptedImage[] corruptedImages;

	[Header ("Progress Bar")]
	public TextMeshProUGUI progressText;
	public TextMeshProUGUI orderText;
	public RectTransform fillImage;

	private Image[] restoreImageFragments;
	private FragmentCondition[] restorationProgress;
	private List<int> restorationOrder = new List<int> ();
	private CorruptedImage currentRestoringImage;


	void Start ()
	{
		restoreImageFragments = restoreImage.GetComponentsInChildren<Image> ();
		restorationProgress = new FragmentCondition [restoreImageFragments.Length];
	}

	public void ApplyFragment (int index)
	{
		restorationOrder.Add (index);
	}

	public void SkipRestore ()
	{

	}
}
