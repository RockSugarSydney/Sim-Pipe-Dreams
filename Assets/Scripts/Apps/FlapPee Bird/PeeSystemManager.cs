using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeeSystemManager : MonoBehaviour
{
	public static PeeSystemManager instance;

	public GameObject peeSplash;
	public Transform splashParent;
	public Vector3 groundOffset;
	public float minScale, maxScale;

	private List<GameObject> splashPool;
	private List<ParticleCollisionEvent> collisionEvents;
	private ParticleSystem.Particle[] aliveParticles;
	private ParticleSystem peeParticleSystem;

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
		splashPool = new List<GameObject> ();
		collisionEvents = new List<ParticleCollisionEvent> ();
		peeParticleSystem = GetComponent<ParticleSystem> ();
		aliveParticles = new ParticleSystem.Particle [peeParticleSystem.main.maxParticles];
	}

	void LateUpdate ()
	{
		int numParticlesAlive = peeParticleSystem.GetParticles (aliveParticles);

		for (int i = 0; i < numParticlesAlive; i++)
		{
			float newRot = Vector3.Angle (Vector3.right, aliveParticles [i].velocity);
			aliveParticles [i].rotation = newRot;
		}
		peeParticleSystem.SetParticles (aliveParticles, numParticlesAlive);
	}

	void OnParticleCollision (GameObject other)
	{
		if (other.tag == "Ground")
		{
			float newScale = Random.Range (minScale, maxScale);
			int numCollisionEvents = peeParticleSystem.GetCollisionEvents (other, collisionEvents);

			for (int i = 0; i < numCollisionEvents; i++) 
			{
				GameObject splashPrefab;

				if (splashPool.Count > 0)
				{
					splashPrefab = splashPool [splashPool.Count - 1];
					splashPool.RemoveAt (splashPool.Count - 1);
					splashPrefab.SetActive (true);
				}
				else
				{
					splashPrefab = Instantiate (peeSplash, splashParent);
				}
				splashPrefab.transform.position = collisionEvents [i].intersection + groundOffset;
				splashPrefab.transform.localScale = new Vector3 (newScale, newScale, 1f);
			}
		}
	}

	public void ReturnToPool (GameObject splash)
	{
		splash.SetActive (false);
		splashPool.Add (splash);
	}

	public void CleanPool ()
	{
		peeParticleSystem.Clear ();

		for (int i = 0; i < splashParent.childCount; i++) 
		{
			if (splashParent.GetChild (i).gameObject.activeSelf) 
			{
				ReturnToPool (splashParent.GetChild (i).gameObject);
			}
		}
	}
}
