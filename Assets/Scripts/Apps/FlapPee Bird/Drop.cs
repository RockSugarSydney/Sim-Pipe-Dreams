using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drop : MonoBehaviour 
{
	public Sprite[] sprites;
	public float lifespan;

	private Rigidbody2D dropRigidbody;
	private float lifeTimer;


	void Awake ()
	{
		dropRigidbody = GetComponent<Rigidbody2D> ();
	}

	void Update ()
	{
		transform.rotation = Quaternion.FromToRotation (Vector2.right, dropRigidbody.velocity);
	}

	void OnBecameInvisible ()
	{
		if (gameObject.activeSelf) 
		{
			FlappyPlayerController.instance.ReturnToPool (gameObject);
		}
	}

	void OnTriggerEnter2D (Collider2D other)
	{
		
	}
}
