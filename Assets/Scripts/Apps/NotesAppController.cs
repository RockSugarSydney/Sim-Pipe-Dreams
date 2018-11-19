using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class NotesAppController : MonoBehaviour 
{
    public static NotesAppController instance;

    public class NoteInfo
    {
        public string title, body, dateCreated;
        public bool isCreated;
    }

	[XmlRoot ("Notebook")]
	public class Notebook
	{
		[XmlArray ("Notes")]
		[XmlArrayItem ("Note")]
		public List<NoteInfo> notes = new List<NoteInfo> ();
	}

    public GameObject notePage;
    public Transform notesPanel;

	private Notebook notebook = new Notebook ();


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
		ReadNotebook ();

		for (int i = 0; i < notebook.notes.Count; i++)
        {
			if (notebook.notes [i].isCreated) 
			{
				GameObject notePrefab = Instantiate (notePage, notesPanel);
				Text[] noteTexts = notePrefab.GetComponentsInChildren<Text> ();
				noteTexts [0].text = notebook.notes [i].title;
				noteTexts [1].text = notebook.notes [i].body;

				DateTime createdDate = DateTime.Parse (notebook.notes [i].dateCreated);
				noteTexts [2].text = createdDate.ToShortDateString ();
			}
        }
    }

	private void ReadNotebook ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(Notebook));
		string path = Path.Combine (Application.persistentDataPath, "Notebook.xml");

		if (File.Exists (path))
		{
			using (FileStream stream = new FileStream (path, FileMode.Open))
			{
				notebook = serializer.Deserialize (stream) as Notebook;
			}
		} 
		else 
		{
			TextAsset xml = Resources.Load ("Notebook") as TextAsset;
			notebook = serializer.Deserialize (new StringReader (xml.text)) as Notebook;
		}
	}

	private void WriteNotebook ()
	{
		XmlSerializer serializer = new XmlSerializer (typeof(Notebook));
		string path = Path.Combine (Application.persistentDataPath, "Notebook.xml");

		using (FileStream stream = new FileStream (path, FileMode.Create))
		{
			serializer.Serialize (stream, notebook);
		}
	}
}
