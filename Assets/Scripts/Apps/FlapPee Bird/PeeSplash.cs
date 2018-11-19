using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeeSplash : MonoBehaviour 
{
	public float lifespan;

	private float lifespanTimer;


	void OnEnable ()
	{
		lifespanTimer = 0f;
	}

	void Update ()
	{
		if (lifespanTimer <= lifespan)
		{
			lifespanTimer += Time.deltaTime;

			if (lifespanTimer > lifespan) 
			{
				PeeSystemManager.instance.ReturnToPool (gameObject);
			}
		}
	}
}
