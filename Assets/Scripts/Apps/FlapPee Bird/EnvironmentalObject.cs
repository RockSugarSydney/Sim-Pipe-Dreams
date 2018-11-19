using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentalObject : MonoBehaviour 
{
	void OnBecameInvisible ()
	{
		FlappyGameController.instance.ReturnToPool (gameObject);
	}
}
