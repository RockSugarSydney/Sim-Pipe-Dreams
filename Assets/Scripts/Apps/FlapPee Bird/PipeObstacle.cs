using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeObstacle : MonoBehaviour 
{
	public GameObject topPipe, bottomPipe, gap;

	private Rigidbody2D obstacleRigidbody;


	void Awake ()
	{
		obstacleRigidbody = GetComponent<Rigidbody2D> ();
	}

	void Update ()
	{
		if (transform.position.x < FlappyGameController.instance.GetOuterBounds ().x) 
		{
			FlappyGameController.instance.ReturnToPool (gameObject);
		}
	}

	public void SetupObstacle (float gapWidth)
	{
		float halfGap = gapWidth / 2;
		topPipe.transform.localPosition = new Vector3 (0f, halfGap, 0f);
		bottomPipe.transform.localPosition = new Vector3 (0f, -halfGap, 0f);
		gap.transform.localScale = new Vector3 (gap.transform.localScale.x, gapWidth, 1f);
		obstacleRigidbody.velocity = Vector2.left * FlappyGameController.instance.movementSpeed;
	}
}
