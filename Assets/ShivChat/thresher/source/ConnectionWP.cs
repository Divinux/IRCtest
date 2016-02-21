/*
 * Thresher IRC client library
 * Copyright (C) 2002 Aaron Hunter <thresher@sharkbite.org>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * 
 * See the gpl.txt file located in the top-level-directory of
 * the archive of this library for complete text of license.
 *
 * Modified for Windows Store Apps *
*/
#if NETFX_CORE
using System.Runtime.InteropServices;
using System;
using System.Collections;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.System.Threading;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;


[assembly:CLSCompliant(true)]
[assembly:ComVisible(false)]
namespace Sharkbite.Irc
{
	/// <summary>
	/// This class manages the connection to the IRC server and provides
	/// access to all the objects needed to send and receive messages.
	/// </summary>
	public sealed class Connection 
	{
		/// <summary>
		/// Receive all the messages, unparsed, sent by the IRC server. This is not
		/// normally needed but provided for those who are interested.
		/// </summary>
		public event RawMessageReceivedEventHandler OnRawMessageReceived;
		/// <summary>
		/// Receive all the raw messages sent to the IRC from this connection
		/// </summary>
		public event RawMessageSentEventHandler OnRawMessageSent;

		private StreamSocket client;
		private readonly Regex propertiesRegex;
		private Listener listener;
		private Sender sender;
		private bool ctcpEnabled;
		private bool dccEnabled;
        private DataReader reader;
		private DateTime timeLastSent;
		//Connected and registered with IRC server
		private bool registered;
		//TCP/IP connection established with IRC server
		private bool connected;
		private bool handleNickFailure;
		private ArrayList parsers;
		private ServerProperties properties;
		private Encoding encoding;

        private DataWriter writer;
		internal ConnectionArgs connectionArgs;

		/// <summary>
		/// Used for internal test purposes only.
		/// </summary>
		internal Connection( ConnectionArgs args ) 
		{
			connectionArgs = args;
			sender = new Sender( this );
			listener = new Listener();
			timeLastSent = DateTime.Now;
			TextEncoding = Encoding.UTF8;
		}
		/// <summary>
		/// Prepare a connection to an IRC server but do not open it. This sets the text Encoding to Default.
		/// </summary>
		/// <param name="args">The set of information need to connect to an IRC server</param>
		/// <param name="enableCtcp">True if this Connection should support CTCP.</param>
		/// <param name="enableDcc">True if this Connection should support DCC.</param>
		public Connection( ConnectionArgs args, bool enableCtcp, bool enableDcc ) 
		{
			propertiesRegex = new Regex("([A-Z]+)=([^\\s]+)",  RegexOptions.Singleline);
			registered = false;
			connected = false;
			handleNickFailure = true;
			connectionArgs = args;
			parsers = new ArrayList();
			sender = new Sender( this );
			listener = new Listener( );
			RegisterDelegates();
			timeLastSent = DateTime.Now;
			TextEncoding = Encoding.UTF8;
		}


		/// <summary>
		/// Prepare a connection to an IRC server but do not open it.
		/// </summary>
		/// <param name="args">The set of information need to connect to an IRC server</param>
		/// <param name="enableCtcp">True if this Connection should support CTCP.</param>
		/// <param name="enableDcc">True if this Connection should support DCC.</param>
		/// <param name="textEncoding">The text encoding for the incoming stream.</param>
		public Connection( Encoding textEncoding, ConnectionArgs args, bool enableCtcp, bool enableDcc ) : this( args,enableCtcp, enableDcc)
		{
			TextEncoding = textEncoding;
		}


		/// <summary>
		/// Sets the text encoding used by the read and write streams.
		/// Must be set before Connect() is called and should not be changed
		/// while the connection is processing messages.
		/// </summary>
		/// <value>An Encoding constant.</value>
		public Encoding TextEncoding 
		{
			get 
			{
				return encoding;
			}
			set 
			{
				encoding = value;
			}
		}

		/// <summary>
		/// A read-only property indicating whether the connection 
		/// has been opened with the IRC server and the 
		/// client has been successfully registered.
		/// </summary>
		/// <value>True if the client is connected and registered.</value>
		public bool Registered 
		{
			get
			{
				return registered;
			}
		}
		/// <summary>
		/// A read-only property indicating whether a connection 
		/// has been opened with the IRC server (but not whether 
		/// registration has succeeded).
		/// </summary>
		/// <value>True if the client is connected.</value>
		public bool Connected 
		{
			get
			{
				return connected;
			}
		}
		/// <summary>
		/// By default the connection itself will handle the case
		/// where, while attempting to register the client's nick
		/// is already in use. It does this by simply appending
		/// 2 random numbers to the end of the nick. 
		/// </summary>
		/// <remarks>
		/// The NickError event is shows that the nick collision has happened
		/// and it is fixed by calling Sender's Register() method passing
		/// in the replacement nickname.
		/// </remarks>
		/// <value>True if the connection should handle this case and
		/// false if the client will handle it itself.</value>
		public bool HandleNickTaken 
		{
			get 
			{
				return handleNickFailure;
			}
			set 
			{
				handleNickFailure = value;
			}
		}
		/// <summary>
		/// A user friendly name of this Connection in the form 'nick@host'
		/// </summary>
		/// <value>Read only string</value>
		public string Name
		{
			get
			{
				return connectionArgs.Nick + "@" + connectionArgs.Hostname;
			}
		}

		/// <summary>
		/// The amount of time that has passed since the client
		/// sent a command to the IRC server.
		/// </summary>
		/// <value>Read only TimeSpan</value>
		public TimeSpan IdleTime
		{
			get
			{
				return DateTime.Now - timeLastSent;
			}
		}
		/// <summary>
		/// The object used to send commands to the IRC server.
		/// </summary>
		/// <value>Read-only Sender.</value>
		public Sender Sender
		{
			get
			{
				return sender;
			}
		}
		/// <summary>
		/// The object that parses messages and notifies the appropriate delegate.
		/// </summary>
		/// <value>Read only Listener.</value>
		public Listener Listener
		{
			get
			{
				return listener;
			}
		}
		/// <summary>
		/// The collection of data used to establish this connection.
		/// </summary>
		/// <value>Read only ConnectionArgs.</value>
		public ConnectionArgs ConnectionData
		{
			get
			{
				return connectionArgs;
			}
		}

		/// <summary>
		/// A read-only collection of string key/value pairs
		/// representing IRC server proprties.
		/// </summary>
		/// <value>This connection's ServerProperties obejct or null if it 
		/// has not been created.</value>
		public ServerProperties ServerProperties 
		{
			get 
			{
				return properties;
			}
		}

		private bool CustomParse( string line ) 
		{
			foreach( IParser parser in parsers ) 
			{
				if( parser.CanParse( line ) ) 
				{
					parser.Parse( line );
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Respond to IRC keep-alives.
		/// </summary>
		/// <param name="message">The message that should be echoed back</param>
		private void KeepAlive(string message) 
		{
			sender.Pong( message );
		}
		/// <summary>
		/// Update the ConnectionArgs object when the user
		/// changes his nick.
		/// </summary>
		/// <param name="user">Who changed their nick</param>
		/// <param name="newNick">The new nick name</param>
		private void MyNickChanged(UserInfo user, string newNick) 
		{
			if ( connectionArgs.Nick == user.Nick ) 
			{
				connectionArgs.Nick = newNick;
			}
		}
		private void OnRegistered() 
		{
			registered = true;
			listener.OnRegistered -= new RegisteredEventHandler( OnRegistered );
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="badNick"></param>
		/// <param name="reason"></param>
		private void OnNickError( string badNick, string reason ) 
		{
			//If this is our initial connection attempt
			if( !registered && handleNickFailure ) 
			{
				NameGenerator generator = new NameGenerator();
				string nick;
				do 
				{
					nick = generator.MakeName();
				}
				while( !Rfc2812Util.IsValidNick( nick) || nick.Length == 1 ); 
				//Try to reconnect
				Sender.Register( nick );
			}
		}
		/// <summary>
		/// Listen for the 005 info messages sent during registration so that the maximum lengths
		/// of certain items (Nick, Away, Topic) can be determined dynamically.
		/// </summary>
		/// <param name="code">Reply code enum</param>
		/// <param name="info">An info line</param>
		private void OnReply( ReplyCode code, string info) 
		{
			if( code == ReplyCode.RPL_BOUNCE ) //Code 005
			{
				//Lazy instantiation
				if( properties == null ) 
				{
					properties = new ServerProperties();
				}
				//Populate properties from name/value matches
				MatchCollection matches = propertiesRegex.Matches( info );
				if( matches.Count > 0 ) 
				{
					foreach( Match match in matches ) 
					{
						properties.SetProperty(match.Groups[1].ToString(), match.Groups[2].ToString() );
					}
				}
				//Extract ones we are interested in
				ExtractProperties();
			}
		}
		private void ExtractProperties() 
		{
			//For the moment the only one we care about is NickLen
			//In fact we don't cae about any but keep here as an example
			/*
			if( properties.ContainsKey("NICKLEN") ) 
			{
				try 
				{
					maxNickLength = int.Parse( properties[ "NICKLEN" ] );
				}
				catch( Exception e )
				{
				}
			}
			*/
		}
		private void RegisterDelegates() 
		{
			listener.OnPing += new PingEventHandler( KeepAlive );
			listener.OnNick += new NickEventHandler( MyNickChanged );
			listener.OnNickError += new NickErrorEventHandler( OnNickError );
			listener.OnReply += new ReplyEventHandler( OnReply );
			listener.OnRegistered += new RegisteredEventHandler( OnRegistered );
		}

		/// <summary>
		/// Read in message lines from the IRC server
		/// and send them to a parser for processing.
		/// Discard CTCP and DCC messages if these protocols
		/// are not enabled.
		/// </summary>
		private async void NewListener(){
			string line;
			try 
			{
		         uint s = await reader.LoadAsync(2048);
				 line = reader.ReadString(s);
                 if(line!=string.Empty)
                 {
				     if( !CustomParse( line ) ) 
			              listener.Parse( line );

				     if( OnRawMessageReceived != null ) 
				     {
					     OnRawMessageReceived( line );
				     }
                 }
			}
			catch (Exception ) { 
			    //Trap a connection failure
			    listener.Error( ReplyCode.ConnectionFailed, "Connection to server unexpectedly failed.");
			}	

			if( connected ) 
                NewListener();
		}

		private async void NewSender(string msg){
            try
			{
                writer.WriteString(msg + "\r\n");
                await writer.StoreAsync();
                await writer.FlushAsync();
            }
            catch( Exception  ) 
			{

			}
		}

		/// <summary>
		/// Send a message to the IRC server and clear the command buffer.
		/// </summary>
		internal void SendCommand( StringBuilder command) 
		{	
			NewSender(command.ToString());
			timeLastSent = DateTime.Now;
			if( OnRawMessageSent != null ) 
			{
				OnRawMessageSent( command.ToString() );
			}
			command.Remove(0, command.Length );
		}
		/// <summary>//
		/// Send a message to the IRC server which does
		/// not affect the client's idle time. Used for automatic replies
		/// such as PONG or Ctcp repsones.
		/// </summary>
		internal void SendAutomaticReply( StringBuilder command) 
		{	
			try
			{
				NewSender(command.ToString());
			}
			catch( Exception  ) 
			{
			}
			command.Remove(0, command.Length );
		}

		/// <summary>
		/// Connect to the IRC server and start listening for messages
		/// on a new thread.
		/// </summary>
		/// <exception cref="SocketException">If a connection cannot be established with the IRC server</exception>
		public async Task Connect() 
		{
			if( connected ) 
			{
				throw new Exception("Connection with IRC server already opened.");
			}
			try 
			{
		    	client = new StreamSocket();
		    	await client.ConnectAsync(new HostName(connectionArgs.Hostname),connectionArgs.Port.ToString());
			    reader = new DataReader( client.InputStream ){InputStreamOptions = InputStreamOptions.Partial, UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8 };
			    writer = new DataWriter(client.OutputStream);
                connected = true;
			    sender.RegisterConnection( connectionArgs );
                NewListener();
            }
            catch( Exception  ) 
			{
                throw new Exception("Error while connecting to the Server.");
			}
			return;
		}
		//
		/// <summary>
		/// Sends a 'Quit' message to the server, closes the connection,
		/// and stops the listening thread. 
		/// </summary>
		/// <remarks>The state of the connection will remain the same even after a disconnect,
		/// so the connection can be reopened. All the event handlers will remain registered.</remarks>
		/// <param name="reason">A message displayed to IRC users upon disconnect.</param>
		public void Disconnect( string reason ) 
		{	
			lock ( this ) 
			{
				if( !connected ) 
				{
					throw new Exception("Not connected to IRC server.");
				}
				listener.Disconnecting();
				sender.Quit( reason );
				listener.Disconnected();
				client.Dispose();
				registered = false;
				connected = false;
			}
		}
		/// <summary>
		/// A friendly name for this connection.
		/// </summary>
		/// <returns>The Name property</returns>
		public override string ToString() 
		{
			return this.Name;
		}

		/// <summary>
		/// Adds a parser class to a list of custom parsers. 
		/// Any number can be added. The custom parsers
		/// will be tested using <c>CanParse()</c> before
		/// the default parsers. The last parser to be added
		/// will be the first to process a message.
		/// </summary>
		/// <param name="parser">Any class that implements IParser.</param>
		public void AddParser( IParser parser ) 
		{
			parsers.Insert(0, parser );
		}
		/// <summary>
		/// Remove a custom parser class.
		/// </summary>
		/// <param name="parser">Any class that implements IParser.</param>
		public void RemoveParser( IParser parser ) 
		{
			parsers.Remove( parser );
		}

	}
}
#endif