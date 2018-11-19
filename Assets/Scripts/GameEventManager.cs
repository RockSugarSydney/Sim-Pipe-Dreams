using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEventManager : MonoBehaviour
{
	public static GameEventManager instance;

	public class Flag
	{
		public string name;
		public bool hasRaised;
	}

	public class GameEvent
	{
		public string name;
		public bool hasTriggered;
		public Flag[] prerequisiteFlags;
		public string[] actions;
	}

	private List<GameEvent> gameEvents = new List<GameEvent> ();


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

	public void CheckFlags (string name)
	{
		List<GameEvent> raisedEvents = new List<GameEvent> ();

		for (int i = 0; i < gameEvents.Count; i++)
		{
			for (int j = 0; j < gameEvents [i].prerequisiteFlags.Length; j++)
			{
				if (gameEvents [i].prerequisiteFlags [j].name == name) 
				{
					gameEvents [i].prerequisiteFlags [j].hasRaised = true;
					raisedEvents.Add (gameEvents [i]);
				}
			}
		}

		for (int i = 0; i < raisedEvents.Count; i++) 
		{
			int raisedFlagCounter = 0;

			for (int j = 0; j < raisedEvents [i].prerequisiteFlags.Length; j++) 
			{
				if (raisedEvents [i].prerequisiteFlags [j].hasRaised) 
				{
					raisedFlagCounter++;
				}
			}

			if (raisedFlagCounter == raisedEvents [i].prerequisiteFlags.Length)
			{
				TriggerEvent (raisedEvents [i]);
				raisedEvents [i].hasTriggered = true;
			}
		}
	}

	private void TriggerEvent (GameEvent triggeredEvent)
	{
		for (int i = 0; i < triggeredEvent.actions.Length; i++)
		{
			if (triggeredEvent.actions [i].Contains ("Phone"))
			{
				
			} 
			else if (triggeredEvent.actions [i].Contains ("Chats")) 
			{
//				ChatsAppController.HandleEvent (triggeredEvent.actions [i]);
			}
			else if (triggeredEvent.actions [i].Contains ("Gallery")) 
			{

			}
			else if (triggeredEvent.actions [i].Contains ("Email")) 
			{

			}
			else if (triggeredEvent.actions [i].Contains ("Spark")) 
			{

			}
			else if (triggeredEvent.actions [i].Contains ("Jabbr")) 
			{

			}
			else if (triggeredEvent.actions [i].Contains ("Vloggr")) 
			{

			}
			else if (triggeredEvent.actions [i].Contains ("Surfer")) 
			{

			}
			else if (triggeredEvent.actions [i].Contains ("Music")) 
			{

			}
			else if (triggeredEvent.actions [i].Contains ("FlapPee Bird")) 
			{

			}
		}
		CheckFlags (triggeredEvent.name);
	}
}
