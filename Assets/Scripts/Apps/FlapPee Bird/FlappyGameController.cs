using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlappyGameController : MonoBehaviour 
{
	public static FlappyGameController instance;

	[System.Serializable]
	public class PlayerStats
	{
		public string name;
		public float playtime;
		public int highscore, adsWatched;
	}

	[System.Serializable]
	public class Leaderboard
	{
		public List<PlayerStats> playerStatsList;
	}

	[System.Serializable]
	public struct SpriteSet
	{
		public Sprite[] backgroundSprites, pipeSprites, cloudSprites, mountainSprites, treeSprites, bushSprites, groundSprites;
	}

	public enum GameState
	{
		Menu,
		Play,
		Pause,
		GameOver
	};
	public GameState currentGameState;

	public static int currentSpriteSet;

	public SpriteSet[] spriteSets;

	[Header ("Splash Screen")]
	public GameObject splashScreen;
	public Image logoImage;
	public float startupDuration;

	private float startupTimer;

	[Header ("UI")]
	public TextMeshProUGUI scoreText;
	public GameObject menuPanel, inGameMenuPanel;

	[Header ("Leaderboard UI")]
	public GameObject leaderboardPanel;
	public GameObject leaderboardStartButton, leaderboardMenuButton, playerStatsPanel;
	public Transform leaderboardContent;

	private Leaderboard leaderboard;

	[Header ("Game Over UI")]
	public GameObject gameOverPanel;
	public TextMeshProUGUI finalScoreText, bestScoreText;
	public float cycleDuration;

	[Header ("Player Settings")]
	public GameObject playerBird;
	public Vector3 playerInitialPos;
	public float movementSpeed;

	private int currentScore;

	[Header ("Pipe Info")]
	public GameObject pipeObstacle;
	public Transform pipeParent;
	public Vector3 startingPos, outerBounds;
	public Vector3 spawnOffset;
	public float heightOffset, minHeight, maxHeight;
	public float minGap, maxGap;
	public float readyDuration, pipeMinSpawnInterval, pipeMaxSpawnInterval;

	private List<GameObject> obstaclePool = new List<GameObject> ();
	private float pipeCurrentSpawnInterval, pipeSpawnTimer;

	[Header ("Cloud Info")]
	public GameObject cloud;
	public Transform cloudParent;
	public float cloudMinSpawnHeight, cloudMaxSpawnHeight, cloudMinSpawnInterval, cloudMaxSpawnInterval, cloudMinScale, cloudMaxScale, cloudMinVelocity, cloudMaxVelocity;

	private List<GameObject> cloudPool = new List<GameObject> ();
	private float cloudCurrentSpawnInterval, cloudSpawnTimer;
	private int previousCloudSpriteIndex;

	[Header ("Environment Info")]
	public Transform environmentParent;
	public float groundHeight;

	[Header ("Mountain Info")]
	public GameObject mountain;
	public float mountainMinSpawnInterval, mountainMaxSpawnInterval, mountainMinScale, mountainMaxScale, mountainVelocity;

	private List<GameObject> mountainPool = new List<GameObject> ();
	private float mountainCurrentSpawnInterval, mountainSpawnTimer;
	private int previousMountainSpriteIndex;

	[Header ("Tree Info")]
	public GameObject tree;
	public float treeMinSpawnInterval, treeMaxSpawnInterval, treeMinScale, treeMaxScale, treeVelocity;

	private List<GameObject> treePool = new List<GameObject> ();
	private float treeCurrentSpawnInterval, treeSpawnTimer;
	private int previousTreeSpriteIndex;

	[Header ("Bush Info")]
	public GameObject bush;
	public float bushMinSpawnInterval, bushMaxSpawnInterval, bushMinScale, bushMaxScale, bushVelocity;

	private List<GameObject> bushPool = new List<GameObject> ();
	private float bushCurrentSpawnInterval, bushSpawnTimer;
	private int previousBushSpriteIndex;

	[Header ("Ground Info")]
	public SpriteRenderer[] groundTransform;

	private Vector3 pipeOuterBounds, groundOuterBounds;
	private float startTime, currentPlaytime;
	private int currentAdsWatched;


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
		StartCoroutine (StartApp ());
	}

	void Update ()
	{
		if (currentGameState == GameState.Play)
		{
			SpawnPipes ();
			SpawnClouds ();
			SpawnEnvironment ();
			MoveGround ();
		}
	}

	public void StartGame ()
	{
		if (currentGameState == GameState.Menu) 
		{
			if (leaderboardPanel.activeSelf) 
			{
				leaderboardPanel.SetActive (false);
				inGameMenuPanel.SetActive (true);
				playerBird.SetActive (true);
			} 
			else 
			{
				ShowLeaderboard (true);
				return;
			}
		}
		else if (currentGameState == GameState.GameOver)
		{
			CleanPool ();
			PeeSystemManager.instance.CleanPool ();

			gameOverPanel.SetActive (false);
			inGameMenuPanel.SetActive (true);
			currentGameState = GameState.Menu;
		}
		currentScore = 0;
		scoreText.text = currentScore.ToString ("N0");

		pipeCurrentSpawnInterval = readyDuration;
		pipeSpawnTimer = 0f;

//		cloudCurrentSpawnInterval = 0f;
		cloudSpawnTimer = 0f;
		previousCloudSpriteIndex = -1;

//		mountainCurrentSpawnInterval = 0f;
		mountainSpawnTimer = 0f;
		previousCloudSpriteIndex = -1;

//		treeCurrentSpawnInterval = 0f;
		treeSpawnTimer = 0f;
		previousCloudSpriteIndex = -1;

//		bushCurrentSpawnInterval = 0f;
		bushSpawnTimer = 0f;
		previousCloudSpriteIndex = -1;

		Vector3 playerSpawnPosition = Camera.main.ViewportToWorldPoint (playerInitialPos);
		playerBird.transform.position = new Vector3 (playerSpawnPosition.x, playerSpawnPosition.y);
		playerBird.transform.rotation = Quaternion.identity;
		playerBird.GetComponent<Rigidbody2D> ().velocity = Vector2.zero;
		playerBird.GetComponent<Rigidbody2D> ().isKinematic = true;

		pipeOuterBounds = Camera.main.ViewportToWorldPoint (outerBounds);
		groundOuterBounds = pipeOuterBounds;
		pipeOuterBounds -= spawnOffset;
	}

	public void PlayGame ()
	{
		for (int i = 0; i < groundTransform.Length; i++) 
		{
			groundTransform [i].GetComponent<Rigidbody2D> ().velocity = Vector2.left * movementSpeed;
		}

		for (int i = 0; i < environmentParent.childCount; i++) 
		{
			if (environmentParent.GetChild (i).name.Contains ("Mountain"))
			{
				environmentParent.GetChild (i).GetComponent<Rigidbody2D> ().velocity = Vector2.left * mountainVelocity;
			} 
			else if (environmentParent.GetChild (i).name.Contains ("Tree"))
			{
				environmentParent.GetChild (i).GetComponent<Rigidbody2D> ().velocity = Vector2.left * treeVelocity;
			}
			else if (environmentParent.GetChild (i).name.Contains ("Bush"))
			{
				environmentParent.GetChild (i).GetComponent<Rigidbody2D> ().velocity = Vector2.left * bushVelocity;
			}
		}
		startTime = Time.unscaledTime;
		currentGameState = GameState.Play;
	}

	public void GameOver ()
	{
		for (int i = 0; i < pipeParent.childCount; i++) 
		{
			pipeParent.GetChild (i).GetComponent<Rigidbody2D> ().velocity = Vector2.zero;
		}

		for (int i = 0; i < environmentParent.childCount; i++) 
		{
			environmentParent.GetChild (i).GetComponent<Rigidbody2D> ().velocity = Vector2.zero;
		}

		for (int i = 0; i < groundTransform.Length; i++) 
		{
			groundTransform [i].GetComponent<Rigidbody2D> ().velocity = Vector2.zero;
		}
		inGameMenuPanel.SetActive (false);
		gameOverPanel.SetActive (true);
		currentGameState = GameState.GameOver;

		currentPlaytime = (Time.unscaledTime - startTime) / 3600;
		UpdatePlayerStats ("Calex", currentScore, currentAdsWatched, currentPlaytime);
		currentAdsWatched = 0;

		StartCoroutine (CycleScore ());
	}

	public void ReturnToMenu ()
	{
		if (currentGameState == GameState.GameOver) 
		{
			CleanPool ();
			PeeSystemManager.instance.CleanPool ();
		}
		playerBird.SetActive (false);
		leaderboardPanel.SetActive (false);
		inGameMenuPanel.SetActive (false);
		gameOverPanel.SetActive (false);
		menuPanel.SetActive (true);
		currentGameState = GameState.Menu;
	}

	public void ShowLeaderboard (bool play)
	{
		GetLeaderboard ();

		for (int i = 0; i < leaderboardContent.childCount; i++) 
		{
			Destroy (leaderboardContent.GetChild (i).gameObject);
		}

		for (int i = 0; i < leaderboard.playerStatsList.Count; i++) 
		{
			PlayerStatsWrapper statsWrapper = Instantiate (playerStatsPanel, leaderboardContent).GetComponent<PlayerStatsWrapper> ();
			statsWrapper.nameText.text = leaderboard.playerStatsList [i].name;
			statsWrapper.highscoreText.text = leaderboard.playerStatsList [i].highscore.ToString ("N0");
			statsWrapper.playtimeText.text = leaderboard.playerStatsList [i].playtime.ToString ("N") + " Hours";
			statsWrapper.adsWatchedText.text = leaderboard.playerStatsList [i].adsWatched + " Ads";
		}
		menuPanel.SetActive (false);
		leaderboardStartButton.SetActive (play);
		leaderboardMenuButton.SetActive (!play);
		leaderboardPanel.SetActive (true);
	}

	public void AddScore ()
	{
		currentScore++;
		scoreText.text = currentScore.ToString ();
	}

	public void ReturnToPool (GameObject obj)
	{
		obj.SetActive (false);

		if (obj.name.Contains ("Obstacle")) 
		{
			obstaclePool.Add (obj);
		} 
		else if (obj.name.Contains ("Cloud"))
		{
			cloudPool.Add (obj);
		}
		else if (obj.name.Contains ("Mountain"))
		{
			mountainPool.Add (obj);
		}
		else if (obj.name.Contains ("Tree"))
		{
			treePool.Add (obj);
		}
		else if (obj.name.Contains ("Bush"))
		{
			bushPool.Add (obj);
		}
	}

	public void CleanPool ()
	{
		for (int i = 0; i < pipeParent.childCount; i++) 
		{
			if (pipeParent.GetChild (i).gameObject.activeSelf) 
			{
				ReturnToPool (pipeParent.GetChild (i).gameObject);
			}
		}
	} 

	public Vector3 GetOuterBounds ()
	{
		return pipeOuterBounds;
	}

	private IEnumerator StartApp ()
	{
		while (startupTimer < startupDuration) 
		{
			startupTimer += Time.deltaTime;

			if (!logoImage.gameObject.activeSelf) 
			{
				if (startupTimer >= startupDuration / 2) 
				{
					logoImage.gameObject.SetActive (true);
				}
			}
			yield return null;
		}
        menuPanel.SetActive (true);
		splashScreen.SetActive (false);
	}

	private IEnumerator CycleScore ()
	{
		float cycleSpeed = 0;
		float currentCycle = 0;

		finalScoreText.text = "0";
		bestScoreText.text = "0";

		yield return new WaitForSeconds (0.5f);

		while (currentGameState == GameState.GameOver) 
		{
			cycleSpeed = currentScore / cycleDuration * Time.deltaTime;
			currentCycle += cycleSpeed;
			finalScoreText.text = string.Format ("{0:N0}", Mathf.FloorToInt (currentCycle));

			if (currentCycle >= currentScore)
			{
				finalScoreText.text = string.Format ("{0:N0}", currentScore);
				PlayerStats playerStats = GetPlayerStats ("Calex");
				bestScoreText.text = playerStats.highscore.ToString ("N0");
				break;
			}
			yield return null;
		}
	}

	private void SpawnPipes ()
	{
		pipeSpawnTimer += Time.deltaTime;

		if (pipeSpawnTimer >= pipeCurrentSpawnInterval) 
		{
			GameObject pipePrefab;

			Vector3 playerPosition = Camera.main.WorldToViewportPoint (playerBird.transform.position);
			float newHeight = playerPosition.y + Random.Range (-heightOffset, heightOffset);
			newHeight = Mathf.Clamp (newHeight, minHeight, maxHeight);

			Vector3 spawnPosition = startingPos;
			spawnPosition = new Vector3 (spawnPosition.x, newHeight);
			spawnPosition = Camera.main.ViewportToWorldPoint (spawnPosition);
			spawnPosition += spawnOffset;
			spawnPosition = new Vector3 (spawnPosition.x, spawnPosition.y);

			if (obstaclePool.Count > 0) 
			{
				pipePrefab = obstaclePool [obstaclePool.Count - 1];
				obstaclePool.RemoveAt (obstaclePool.Count - 1);
				pipePrefab.SetActive (true);
			} 
			else
			{
				pipePrefab = Instantiate (pipeObstacle, pipeParent);
			}
			SpriteRenderer[] pipeRenderer = pipePrefab.GetComponentsInChildren<SpriteRenderer> ();
			int randSprite = Random.Range (0, spriteSets [currentSpriteSet].pipeSprites.Length);

			for (int i = 0; i < pipeRenderer.Length; i++) 
			{
				pipeRenderer [i].sprite = spriteSets [currentSpriteSet].pipeSprites [randSprite];
			}
			pipePrefab.transform.position = spawnPosition;

			PipeObstacle pipeInstance = pipePrefab.GetComponent<PipeObstacle> ();
			pipeInstance.SetupObstacle (Random.Range (minGap, maxGap));

			pipeCurrentSpawnInterval = Random.Range (pipeMinSpawnInterval, pipeMaxSpawnInterval);
			pipeSpawnTimer = 0f;
		}
	}

	private void SpawnClouds ()
	{
		cloudSpawnTimer += Time.deltaTime;

		if (cloudSpawnTimer >= cloudCurrentSpawnInterval) 
		{
			GameObject cloudPrefab;

			float spawnHeight = Random.Range (cloudMinSpawnHeight, cloudMaxSpawnHeight);

			Vector3 spawnPosition = startingPos;
			spawnPosition = new Vector3 (spawnPosition.x, spawnHeight);
			spawnPosition = Camera.main.ViewportToWorldPoint (spawnPosition);
			spawnPosition += spawnOffset;
			spawnPosition = new Vector3 (spawnPosition.x, spawnPosition.y);

			if (cloudPool.Count > 0) 
			{
				cloudPrefab = cloudPool [cloudPool.Count - 1];
				cloudPool.RemoveAt (cloudPool.Count - 1);
				cloudPrefab.SetActive (true);
			} 
			else
			{
				cloudPrefab = Instantiate (cloud, cloudParent);
			}
			cloudPrefab.transform.position = spawnPosition;

			int randSprite = Random.Range (0, spriteSets [currentSpriteSet].cloudSprites.Length);

			if (randSprite == previousCloudSpriteIndex) 
			{
				randSprite = (randSprite + 1) % spriteSets [currentSpriteSet].cloudSprites.Length;
			}
			cloudPrefab.GetComponent<SpriteRenderer> ().sprite = spriteSets [currentSpriteSet].cloudSprites [randSprite];
			previousCloudSpriteIndex = randSprite;

			float newScale = Random.Range (cloudMinScale, cloudMaxScale);
			cloudPrefab.transform.localScale = new Vector3 (newScale, newScale, 1f);

			float newVelocity = Random.Range (cloudMinVelocity, cloudMaxVelocity);
			cloudPrefab.GetComponent<Rigidbody2D> ().velocity = Vector2.left * newVelocity;

			cloudCurrentSpawnInterval = Random.Range (cloudMinSpawnInterval, cloudMaxSpawnInterval);
			cloudSpawnTimer = 0f;
		}
	}

	private void SpawnEnvironment ()
	{
		mountainSpawnTimer += Time.deltaTime;
		treeSpawnTimer += Time.deltaTime;
		bushSpawnTimer += Time.deltaTime;

		if (mountainSpawnTimer >= mountainCurrentSpawnInterval) 
		{
			GameObject mountainPrefab;

			Vector3 spawnPosition = startingPos;
			spawnPosition = Camera.main.ViewportToWorldPoint (spawnPosition);
			spawnPosition += spawnOffset;
			spawnPosition = new Vector3 (spawnPosition.x, groundHeight);

			if (mountainPool.Count > 0) 
			{
				mountainPrefab = mountainPool [mountainPool.Count - 1];
				mountainPool.RemoveAt (mountainPool.Count - 1);
				mountainPrefab.SetActive (true);
			} 
			else
			{
				mountainPrefab = Instantiate (mountain, environmentParent);
			}
			mountainPrefab.transform.position = spawnPosition;

			int randSprite = Random.Range (0, spriteSets [currentSpriteSet].mountainSprites.Length);

			if (randSprite == previousMountainSpriteIndex) 
			{
				randSprite = (randSprite + 1) % spriteSets [currentSpriteSet].mountainSprites.Length;
			}
			mountainPrefab.GetComponent<SpriteRenderer> ().sprite = spriteSets [currentSpriteSet].mountainSprites [randSprite];
			previousMountainSpriteIndex = randSprite;

			float newScale = Random.Range (mountainMinScale, mountainMaxScale);
			mountainPrefab.transform.localScale = new Vector3 (newScale, newScale, 1f);

			mountainPrefab.GetComponent<Rigidbody2D> ().velocity = Vector2.left * mountainVelocity;

			mountainCurrentSpawnInterval = Random.Range (mountainMinSpawnInterval, mountainMaxSpawnInterval);
			mountainSpawnTimer = 0f;
		}

		if (treeSpawnTimer >= treeCurrentSpawnInterval) 
		{
			GameObject treePrefab;

			Vector3 spawnPosition = startingPos;
			spawnPosition = Camera.main.ViewportToWorldPoint (spawnPosition);
			spawnPosition += spawnOffset;
			spawnPosition = new Vector3 (spawnPosition.x, groundHeight);

			if (treePool.Count > 0) 
			{
				treePrefab = treePool [treePool.Count - 1];
				treePool.RemoveAt (treePool.Count - 1);
				treePrefab.SetActive (true);
			} 
			else
			{
				treePrefab = Instantiate (tree, environmentParent);
			}
			treePrefab.transform.position = spawnPosition;

			int randSprite = Random.Range (0, spriteSets [currentSpriteSet].treeSprites.Length);

			if (randSprite == previousTreeSpriteIndex) 
			{
				randSprite = (randSprite + 1) % spriteSets [currentSpriteSet].treeSprites.Length;
			}
			treePrefab.GetComponent<SpriteRenderer> ().sprite = spriteSets [currentSpriteSet].treeSprites [randSprite];
			previousTreeSpriteIndex = randSprite;

			float newScale = Random.Range (treeMinScale, treeMaxScale);
			treePrefab.transform.localScale = new Vector3 (newScale, newScale, 1f);

			treePrefab.GetComponent<Rigidbody2D> ().velocity = Vector2.left * treeVelocity;

			treeCurrentSpawnInterval = Random.Range (treeMinSpawnInterval, treeMaxSpawnInterval);
			treeSpawnTimer = 0f;
		}

		if (bushSpawnTimer >= bushCurrentSpawnInterval) 
		{
			GameObject bushPrefab;

			Vector3 spawnPosition = startingPos;
			spawnPosition = Camera.main.ViewportToWorldPoint (spawnPosition);
			spawnPosition += spawnOffset;
			spawnPosition = new Vector3 (spawnPosition.x, groundHeight);

			if (bushPool.Count > 0) 
			{
				bushPrefab = bushPool [bushPool.Count - 1];
				bushPool.RemoveAt (bushPool.Count - 1);
				bushPrefab.SetActive (true);
			} 
			else
			{
				bushPrefab = Instantiate (bush, environmentParent);
			}
			bushPrefab.transform.position = spawnPosition;

			int randSprite = Random.Range (0, spriteSets [currentSpriteSet].bushSprites.Length);

			if (randSprite == previousBushSpriteIndex) 
			{
				randSprite = (randSprite + 1) % spriteSets [currentSpriteSet].bushSprites.Length;
			}
			bushPrefab.GetComponent<SpriteRenderer> ().sprite = spriteSets [currentSpriteSet].bushSprites [randSprite];
			previousBushSpriteIndex = randSprite;

			float newScale = Random.Range (bushMinScale, bushMaxScale);
			bushPrefab.transform.localScale = new Vector3 (newScale, newScale, 1f);

			bushPrefab.GetComponent<Rigidbody2D> ().velocity = Vector2.left * bushVelocity;

			bushCurrentSpawnInterval = Random.Range (bushMinSpawnInterval, bushMaxSpawnInterval);
			bushSpawnTimer = 0f;
		}
	}

	private void MoveGround ()
	{
		for (int i = 0; i < groundTransform.Length; i++)
		{
			if (groundTransform [i].bounds.max.x <= groundOuterBounds.x) 
			{
				int nextGroundIndex = (i + groundTransform.Length - 1) % groundTransform.Length;
				groundTransform [i].transform.position = groundTransform [nextGroundIndex].bounds.center + new Vector3 (groundTransform [nextGroundIndex].bounds.size.x, 0f);

				int randSprite = Random.Range (0, spriteSets [currentSpriteSet].groundSprites.Length);
				groundTransform [i].sprite = spriteSets [currentSpriteSet].groundSprites [randSprite];
			}
		}
	}

	private void GetLeaderboard ()
	{
		if (leaderboard == null) 
		{
			if (PlayerPrefs.HasKey ("Leaderboard"))
			{
				leaderboard = JsonUtility.FromJson<Leaderboard> (PlayerPrefs.GetString ("Leaderboard"));
			}
			else 
			{
				TextAsset json = Resources.Load ("Leaderboard") as TextAsset;
				leaderboard = JsonUtility.FromJson<Leaderboard> (json.text);
			}
		}
	}

	private PlayerStats GetPlayerStats (string name)
	{
		for (int i = 0; i < leaderboard.playerStatsList.Count; i++) 
		{
			if (leaderboard.playerStatsList [i].name == name)
			{
				return leaderboard.playerStatsList [i];
			}
		}
		return null;
	}

	private void UpdatePlayerStats (string name, int highscore, int adsWatched, float playtime)
	{
		PlayerStats updatedStats = null;
		bool newHighscore = false;

		for (int i = 0; i < leaderboard.playerStatsList.Count; i++) 
		{
			if (leaderboard.playerStatsList [i].name == name)
			{
				updatedStats = leaderboard.playerStatsList [i];
				updatedStats.adsWatched += adsWatched;
				updatedStats.playtime += playtime;

				if (updatedStats.highscore < highscore)
				{
					newHighscore = true;
					updatedStats.highscore = highscore;
					leaderboard.playerStatsList.RemoveAt (i);
				}
				break;
			}
		}

		if (updatedStats == null) 
		{
			updatedStats = new PlayerStats ();
			updatedStats.name = name;
			updatedStats.highscore = highscore;
			updatedStats.adsWatched = adsWatched;
			updatedStats.playtime = playtime;
			newHighscore = true;
		}

		if (newHighscore) 
		{
			for (int i = 0; i < leaderboard.playerStatsList.Count; i++) 
			{
				if (leaderboard.playerStatsList [i].highscore <= updatedStats.highscore)
				{
					leaderboard.playerStatsList.Insert (i, updatedStats);
					return;
				}
			}
			leaderboard.playerStatsList.Add (updatedStats);
		}
	}
}
