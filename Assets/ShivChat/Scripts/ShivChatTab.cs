using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;

[CLSCompliant(false)]
public class ShivChatTab : MonoBehaviour {

	public string tabName = "General";
	public string channel = "#ShivChatTest";
	public string channelPassword = "";
	public bool isDefault = false;
	private Text textArea;
	private Text userArea;
	private ShivChat chatSystem; 
	private string ta_temp;
	private string ua_temp;
	private bool update_ta = false;
	private bool update_ua = false;
	private string userListcolor;

	void Start(){
		//find all necessary components
		chatSystem = this.transform.parent.parent.parent.GetComponent<ShivChat> ();
		userListcolor = chatSystem.userListColor;
		Transform tempChild = this.transform.FindChild ("Components");
		this.transform.FindChild("Button").FindChild("Text").GetComponent<Text>().text = tabName;
		textArea = tempChild.FindChild ("MSGArea").FindChild ("Viewport").FindChild ("Content").GetComponent<Text> ();
		userArea = tempChild.FindChild ("UsersArea").FindChild ("Viewport").FindChild ("Content").GetComponent<Text> ();
		if (!isDefault)
			tempChild.gameObject.SetActive (false);	
	}

	void Update(){
		//Thread safe workaround, i hate it
		if (update_ta) {
			textArea.text = ta_temp;
			update_ta = false;
		}
		if (update_ua) {
			userArea.text = ua_temp;
			update_ua = false;
		}
	}
	//write message to chat
	public void ToTextArea(string text){
		ta_temp += "\n\r" + text;
		update_ta = true;
	}
	//write text to input field
	public void ToUserArea(string text){
		if (text.Contains ("@"))
			text=text.Replace("@",string.Empty);	
		ua_temp +="<color="+userListcolor+">" + "\n\r" + text +"</color>";
		update_ua = true;		
	}
	//delete text from input field
	public void RemoveFromUserArea(string text){
		if (ua_temp.Contains ("\n\r" + text)) {
			ua_temp=ua_temp.Replace("\n\r" + text,string.Empty);
		}else if (ua_temp.Contains ("\n\r" +"@"+ text)) {
			ua_temp=ua_temp.Replace("\n\r" +"@"+ text,string.Empty);
		}
		update_ua = true;		
	}

	public void ToNewUserArea(string[] text){
		for (int i = 0; i < text.Length - 1; i++) {
			if(i==0)
				ua_temp = string.Empty;	
			ToUserArea (text [i]);
		}
	}

	public void WriteMSG(string message){
		if(!message.Equals(string.Empty))
			chatSystem.Talk(this,message);
	}
}
