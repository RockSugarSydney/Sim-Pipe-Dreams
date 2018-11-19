using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlappyPlayerController : MonoBehaviour 
{
	public static FlappyPlayerController instance;

	public GameObject dropObject;
	public Color dropColor;
	public Transform pool;
	public Vector2 flapVelocity;
	public float movementSpeed, flapAngle, rollRate, peeChance, spraySpeed, sprayDuration;
	public int sprayAmount;

	private List<GameObject> peePool = new List<GameObject> ();
	private Rigidbody2D birdRigidbody;
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
		birdRigidbody = GetComponent<Rigidbody2D> ();
		peeParticleSystem = GetComponentInChildren<ParticleSystem> ();
		peeParticleSystem.Stop ();
	}

	void Update ()
	{
		if (Input.GetButtonDown ("Fire1")) 
		{
			if (FlappyGameController.instance.currentGameState == FlappyGameController.GameState.Menu)
			{
				birdRigidbody.isKinematic = false;
				peeParticleSystem.Play ();
				FlappyGameController.instance.PlayGame ();
			} 
			else if (FlappyGameController.instance.currentGameState == FlappyGameController.GameState.Play) 
			{
				birdRigidbody.velocity = flapVelocity;
				transform.rotation = Quaternion.Euler (new Vector3 (0f, 0f, flapAngle));
//				SpawnDrop ();
			}
		}

		if (birdRigidbody.isKinematic == false) 
		{
			float newRoll = transform.rotation.eulerAngles.z - rollRate * Time.deltaTime;
			transform.rotation = Quaternion.Euler (new Vector3 (0f, 0f, newRoll)); 
		}
	}

	void OnTriggerEnter2D (Collider2D other)
	{
		if (other.tag == "Obstacle" && FlappyGameController.instance.currentGameState == FlappyGameController.GameState.Play) 
		{
			birdRigidbody.velocity = Vector2.zero;
			birdRigidbody.angularVelocity = 0f;
			FlappyGameController.instance.GameOver ();
//			StartCoroutine (SprayDrop ());
		}
	}

	void OnTriggerExit2D (Collider2D other)
	{
		if (other.tag == "Gap" && FlappyGameController.instance.currentGameState == FlappyGameController.GameState.Play) 
		{
			FlappyGameController.instance.AddScore ();
		}
	}

	void OnCollisionEnter2D (Collision2D col)
	{
		if (col.collider.tag == "Ground")
		{
			if (FlappyGameController.instance.currentGameState == FlappyGameController.GameState.Play)
			{
				birdRigidbody.velocity = Vector2.zero;
				birdRigidbody.angularVelocity = 0f;
				FlappyGameController.instance.GameOver ();
			}
			peeParticleSystem.Stop ();
			birdRigidbody.isKinematic = true;
		}
	}

	public void ReturnToPool (GameObject drop)
	{
		drop.SetActive (false);
		peePool.Add (drop);
	}

	public void CleanPool ()
	{
		for (int i = 0; i < pool.childCount; i++) 
		{
			if (pool.GetChild (i).gameObject.activeSelf) 
			{
				ReturnToPool (pool.GetChild (i).gameObject);
			}
		}
	}

	private void SpawnDrop ()
	{
		float randChance = Random.value;

		if (randChance <= peeChance) 
		{
			GameObject dropPrefab;

			if (peePool.Count > 0) 
			{
				dropPrefab = peePool [peePool.Count - 1];
				peePool.RemoveAt (peePool.Count - 1);
				dropPrefab.SetActive (true);
			}
			else 
			{
				dropPrefab = Instantiate (dropObject, pool);
			}
			dropPrefab.transform.position = transform.position;
//			dropPrefab.GetComponent<SpriteRenderer> ().color = dropColor;
			dropPrefab.GetComponent<Rigidbody2D> ().velocity = Vector2.zero;
		}
	}

	private IEnumerator SprayDrop ()
	{
		float sprayRate = sprayDuration / sprayAmount;
		float spawnTimer = 0;
		int dropSpawned = 0;

		while (dropSpawned < sprayAmount && FlappyGameController.instance.currentGameState == FlappyGameController.GameState.GameOver)
		{
			spawnTimer += Time.deltaTime;

			if (spawnTimer > sprayRate) 
			{
				GameObject dropPrefab;

				if (peePool.Count > 0) 
				{
					dropPrefab = peePool [peePool.Count - 1];
					peePool.RemoveAt (peePool.Count - 1);
					dropPrefab.SetActive (true);
				}
				else 
				{
					dropPrefab = Instantiate (dropObject, pool);
				}
				dropPrefab.transform.position = transform.position;
//				dropPrefab.GetComponent<SpriteRenderer> ().color = dropColor;

				Vector2 direction = Random.insideUnitCircle;
				dropPrefab.GetComponent<Rigidbody2D> ().velocity = direction * spraySpeed;

				dropSpawned++;
				spawnTimer = 0f;
			}
			yield return null;
		}
	}
}
