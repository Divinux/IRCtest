using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Chat : MonoBehaviour {

	public List<string> chatHistory = new List<string>();
	
	public string currMsg = "";
	void OnGUI () 
	{
		GUILayout.BeginHorizontal(GUILayout.Width(250));
		currMsg = GUILayout.TextField(currMsg);
		if(GUILayout.Button("Send"))
		{
			if(!string.IsNullOrEmpty(currMsg.Trim()))
			{
				GetComponent<NetworkView>().RPC("ChatMessage", RPCMode.AllBuffered, new object[]{currMsg});
			}
		}
		GUILayout.EndHorizontal();
		
		foreach(string c in chatHistory)
		GUILayout.Label(c);
	}
	[RPC]
	public void ChatMessage(string msg)
	{
		chatHistory.Add(msg);
	}
}
