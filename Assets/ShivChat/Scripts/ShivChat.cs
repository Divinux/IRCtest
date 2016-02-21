using UnityEngine;
using System;
using System.Collections.Generic;
using Sharkbite.Irc;
#if NETFX_CORE
using System.Threading.Tasks;
#endif

[CLSCompliant(false)]
public class ShivChat : MonoBehaviour {

	public string server = "open.ircnet.net";
	public int port = 6667;
	public string serverPassword = "";
	public string nick = "ChatBotTest";
	public bool useIdentd = true;
	public string generalColor;
	public string privateColor;
	public string systemColor;
	public string userListColor;
	public ShivChatTab tab1;
	public ShivChatTab tab2;
	public ShivChatTab tab3;
	public ShivChatTab tab4;
	private Connection connection;
	private List<string> blockedNicks = new List<string>();
	private bool _tab1 = false;
	private bool _tab2 = false; 
	private bool _tab3 = false; 
	private bool _tab4 = false; 


	void Start() 
	{
		//Thread Safe		
		if (tab1)
			_tab1 = true;
		if (tab2)
			_tab2 = true;
		if (tab3)
			_tab3 = true;
		if (tab4)
			_tab4 = true;
	}

	public void ButtonConnect() 
	{
        #if NETFX_CORE
	    var _ = Connect();
        #else
        Connect();
        #endif
    }
//connect to server
#if NETFX_CORE
	private async Task Connect () {
#else
    private void Connect () {
	#endif
		#if !NETFX_CORE
		if(useIdentd) 
			Identd.Start(nick);
		#endif
	    ConnectionArgs cargs = new ConnectionArgs(nick, server);
		if (port != 6667)
			cargs.Port = port; 
		if (!serverPassword.Equals(string.Empty))
			cargs.ServerPassword = serverPassword;
		connection = new Connection( cargs, false, false );		

		try
		{			
			ToAllTabs("<color="+systemColor+">System: Connecting...</color>");			
			#if NETFX_CORE
			await connection.Connect();
			#else
			connection.Connect();	
			#endif
			connection.Listener.OnRegistered += new RegisteredEventHandler( OnRegistered );
			connection.Listener.OnNames += new NamesEventHandler ( OnNames );
			connection.Listener.OnPublic += new PublicMessageEventHandler( OnPublic );
			connection.Listener.OnPrivate += new PrivateMessageEventHandler( OnPrivate );
			connection.Listener.OnError += new ErrorMessageEventHandler( OnError );
			connection.Listener.OnDisconnected += new DisconnectedEventHandler( OnDisconnected );
			connection.Listener.OnPart += new PartEventHandler(OnPart);
			connection.Listener.OnQuit += new QuitEventHandler(OnQuit);
			connection.Listener.OnKick += new KickEventHandler(OnKick);
			connection.Listener.OnJoin += new JoinEventHandler(OnJoin);
			ToAllTabs("<color="+systemColor+">System: Connected! Loading Channel...</color>");
		}
		catch( Exception e ) 
		{
			//Debug.LogError("Error during connection process."+e);
			ToAllTabs("<color="+systemColor+">System: Error during connection process.</color>");	
			Debug.Log( e );
			#if !NETFX_CORE
			if(useIdentd) 
				Identd.Stop();
			#endif
		}
		return;
	}
	//write message to chat
	public void Talk (ShivChatTab tab, string message) {

				if (message.Length >= 3 && message.StartsWith("/")) {
						switch (message.Substring (0, 3)) {
						case "/hp":
								tab.ToTextArea ("<color="+systemColor+">Chat commands:</color>");
								tab.ToTextArea ("<color="+systemColor+">/pm nick message = Sends a private message to this user.</color>");
								tab.ToTextArea ("<color="+systemColor+">/bk nick = Add this user to your blocked list.</color>");
								tab.ToTextArea ("<color="+systemColor+">/rb nick = Remove this user from your blocked list.</color>");
								tab.ToTextArea ("<color="+systemColor+">/vb = Shows users in your blocked list.</color>");
								break;
						case "/pm":
							string[] temp = message.Split (' ');
							if (temp.Length >= 3) {
								try {
									connection.Sender.PrivateMessage (temp [1], message.Replace ("/pm " + temp [1]+" ", string.Empty));
									tab.ToTextArea ("<color="+privateColor+">To " + temp [1] + " : " + message.Replace ("/pm " + temp [1]+" ", string.Empty)+"</color>");
								} catch (Exception e) {
									tab.ToTextArea ("<color="+systemColor+">System: /pm command error</color>");
									Debug.LogError (e);
								}
							} else {
								tab.ToTextArea ("<color="+systemColor+">System: /pm command error</color>");
							}
								break;
						case "/bk":
								string[] tempbk = message.Split (' ');
								if (tempbk.Length >= 2) {
										if (!blockedNicks.Contains (tempbk [1])) {
												blockedNicks.Add (tempbk [1]);
												tab.ToTextArea ("<color="+systemColor+">System: User " + tempbk [1] + " added to your blocked list</color>");
										}
										else
											tab.ToTextArea ("<color="+systemColor+">System: User " + tempbk [1] + " was already in your blocked list</color>");
								} else
									tab.ToTextArea ("<color="+systemColor+">System: /bk command error</color>");
								break;
						case "/rb":
								string[] temprb = message.Split (' ');
								if (temprb.Length >= 2) {
										if (blockedNicks.Contains (temprb [1])) {
												blockedNicks.Remove (temprb [1]);
												tab.ToTextArea ("<color="+systemColor+">System: User " + temprb [1] + " was removed from your blocked list</color>");
										}
										else
											tab.ToTextArea ("<color="+systemColor+">System: User " + temprb [1] + " is not in your blocked list</color>");
								} else
									tab.ToTextArea ("<color="+systemColor+">System: /rb command error</color>");
								break;
						case "/vb":
								tab.ToTextArea ("<color="+systemColor+">Users in Blocked list: " + blockedNicks.Count.ToString ()+"</color>");
								for (int i = 0; i < blockedNicks.Count; i++) {
									tab.ToTextArea ("<color="+systemColor+">"+blockedNicks [i]+"</color>");
								}
								break;
						}
				} else {
						try {
								connection.Sender.PublicMessage (tab.channel, message);
								tab.ToTextArea ("<color="+generalColor+">"+nick + " : " + message +"</color>");
						} catch (Exception e) {					
                            if(connection!=null && connection.Connected)
							    tab.ToTextArea ("<color="+systemColor+">System: Error</color>");
                            else
                                tab.ToTextArea("<color=" + systemColor + ">System: Not connected.</color>");
                        }
				}
	}
	//join a channel
	private void JoinChannel (ShivChatTab tab){
		try{	
			if(tab.channelPassword!="")
				connection.Sender.Join(tab.channel,tab.channelPassword);
			else
				connection.Sender.Join(tab.channel);
			tab.ToTextArea("<color="+systemColor+">System: Joined #"+tab.tabName + "</color>");
			connection.Sender.Names(tab.channel);
		}
		catch( Exception e ) 
		{
			//Debug.Log("Error while joining a channel: " + e ) ;
			tab.ToTextArea("<color="+systemColor+">System: Error while joining "+tab.tabName+": " + e + "</color>");
		}
	}


	public void ExtConnect( string tempNick )
	{
		nick=tempNick;
		ButtonConnect();
	}
	
	public void SetNick( string tempNick )
	{
		nick = tempNick;
	}

	public void Disconnect()
	{
		connection.Disconnect("" + nick + " disconnected.");
	}
				
	public void ToAllTabs( string message)
	{
		if (_tab1) {
			tab1.ToTextArea (message);
		}
		if (_tab2) {
			tab2.ToTextArea (message);
		}
		if (_tab3) {
			tab3.ToTextArea (message);
		}
		if (_tab4) {
			tab4.ToTextArea (message);
		}
	}

	void OnApplicationQuit()
	{
		if(connection!=null && connection.Connected)
			connection.Disconnect("" + nick + " disconnected.");			
	}

	/****************************** Event Handlers from now on ********************************/

	public void OnRegistered() 
	{
		if (_tab1)
				JoinChannel(tab1);
		if (_tab2)
				JoinChannel(tab2);
		if (_tab3)
				JoinChannel(tab3);
		if (_tab4)
				JoinChannel(tab4);
		#if !NETFX_CORE
		if(useIdentd) 
				Identd.Stop();
		#endif
	}

	public void OnPublic( UserInfo user, string channel, string message )
	{
				if (!blockedNicks.Contains (user.Nick)) {
						
					if (_tab1 && tab1.channel == channel) {
						tab1.ToTextArea ("<color="+generalColor+">"+user.Nick + " : " + message+"</color>");
					}
						
					if (_tab2 && tab2.channel == channel) {
						tab2.ToTextArea ("<color="+generalColor+">"+user.Nick + " : " + message+"</color>");
					}
						
					if (_tab3 && tab3.channel == channel) {
						tab3.ToTextArea ("<color="+generalColor+">"+user.Nick + " : " + message+"</color>");
					}
					
					if (_tab4 && tab4.channel == channel) {
						tab4.ToTextArea ("<color="+generalColor+">"+user.Nick + " : " + message+"</color>");
					}
				}
	}

	public void OnPrivate( UserInfo user,  string message )
	{
				if (!blockedNicks.Contains (user.Nick)) {
						
					if (_tab1) {
						tab1.ToTextArea ("<color="+privateColor+">"+user.Nick + " : " + message+"</color>");
					}

					if (_tab2) {
						tab2.ToTextArea ("<color="+privateColor+">"+user.Nick + " : " + message+"</color>");
					}

					if (_tab3) {
						tab3.ToTextArea ("<color="+privateColor+">"+user.Nick + " : " + message+"</color>");
					}

					if (_tab4) {
						tab4.ToTextArea ("<color="+privateColor+">"+user.Nick + " : " + message+"</color>");
					}
				}
	}

	public void OnPart( UserInfo user,string channel, string text )
	{
		if (_tab1 && tab1.channel == channel)
			tab1.RemoveFromUserArea(user.Nick);

		if (_tab2 && tab2.channel == channel)
			tab2.RemoveFromUserArea(user.Nick);

 		if (_tab3 && tab3.channel == channel)
			tab3.RemoveFromUserArea(user.Nick);

		if (_tab4 && tab4.channel == channel)
			tab4.RemoveFromUserArea(user.Nick);
	}

	public void OnKick( UserInfo user,string channel, string a, string b  )
	{
		if (_tab1 && tab1.channel == channel)
			tab1.RemoveFromUserArea(user.Nick);
			
		if (_tab2 && tab2.channel == channel)
			tab2.RemoveFromUserArea(user.Nick);

		if (_tab3 && tab3.channel == channel)
			tab3.RemoveFromUserArea(user.Nick);

		if (_tab4 && tab4.channel == channel)
			tab4.RemoveFromUserArea(user.Nick);
	}

	public void OnQuit( UserInfo user,string channel  )
	{
		if (_tab1 && tab1.channel == channel)
			tab1.RemoveFromUserArea(user.Nick);

		if (_tab2 && tab2.channel == channel)
			tab2.RemoveFromUserArea(user.Nick);

		if (_tab3 && tab3.channel == channel)
			tab3.RemoveFromUserArea(user.Nick);

		if (_tab4 && tab4.channel == channel)
			tab4.RemoveFromUserArea(user.Nick);
	}

	public void OnJoin( UserInfo user,string channel  )
	{
		if (_tab1 && tab1.channel == channel)
			tab1.ToUserArea(user.Nick);

		if (_tab2 && tab2.channel == channel)
			tab2.ToUserArea(user.Nick);

		if (_tab3 && tab3.channel == channel)
			tab3.ToUserArea(user.Nick);

		if (_tab4 && tab4.channel == channel)
			tab4.ToUserArea(user.Nick);
	}
				
	public void OnNames(string channel, string[] users, bool a){
		if (_tab1 && tab1.channel == channel)
			tab1.ToNewUserArea(users);

		if (_tab2 && tab2.channel == channel)
			tab2.ToNewUserArea(users);
					
		if (_tab3 && tab3.channel == channel)
			tab3.ToNewUserArea(users);

		if (_tab4 && tab4.channel == channel)
			tab4.ToNewUserArea(users);
	}

	public void OnError( ReplyCode code, string message) 
	{
        //Disabled for Windows Store Apps because of common errors at startup with some servers
        #if !NETFX_CORE
        ToAllTabs("<color="+systemColor+">System: A error has occurred:</color>" + message);
        #endif
    }

    public void OnDisconnected() 
	{
		ToAllTabs("<color="+systemColor+">System: Connection to the server has been closed.</color>");
	}
}
