using System;
using System.ComponentModel;
using System.Configuration;
using System.Net.Sockets;

namespace IrcNotify
{
	public class MessageEventArgs : EventArgs
	{
		public MessageEventArgs( string message )
		{
			Message = message;
		}
		public string Message { get; private set; }
	}

	public class IrcListener : INotifyPropertyChanged
	{
		TcpClient _client;
		System.IO.Stream _comm;
		System.IO.StreamReader _input;
		System.IO.StreamWriter _output;
		string _currentstatus;

		public IrcListener()
		{
			CurrentStatus = "Inactive";
		}

		public string CurrentStatus
		{
			get { return _currentstatus; }
			private set { _currentstatus = value; FirePropertyChanged( "CurrentStatus" ); }
		}

		public event EventHandler<MessageEventArgs> MessageReceived;
		public event EventHandler<MessageEventArgs> MessageSent;

		void FireMessageReceived( string message )
		{
			if( MessageReceived != null )
				MessageReceived( this, new MessageEventArgs( message ) );
		}

		void FireMessageSent( string message )
		{
			if( MessageSent != null )
				MessageSent( this, new MessageEventArgs( message ) );
		}


		void Send( string command )
		{
			_output.Write( command );
			_output.Flush();
			FireMessageSent( command );
		}

		string BlockingRead()
		{
			try
			{
				var inline = _input.ReadLine();
				if( inline != null && inline.ToUpperInvariant().StartsWith( "PING " ) )
				{
					Send( "PONG " + inline.Substring( 5 ) + "\r\n" );
				}
				return inline;
			}
			catch( System.IO.IOException )
			{
				ConsoleWriter.Write( string.Format( "*IOE: Remote disconnected?\n" ), true );

				return null;
			}
		}

		public void Close()
		{
			if( CurrentStatus == "Listening" )
			{
				ConsoleWriter.Write( string.Format( "****: Leaving channel. Status: {0}.\n", CurrentStatus ), true );
				Send( "PART #dotdash\r\n" );
			}
			if( CurrentStatus == "Logged in" || CurrentStatus == "Connected" )
			{
				ConsoleWriter.Write( string.Format( "****: Sending Quit message to server. Status: {0}.\n", CurrentStatus ), true );
				Send( "QUIT\r\n" );
			}
			ConsoleWriter.Write( string.Format( "****: Closing client. Status: {0}.\n", CurrentStatus ), true );
			_client.Close();
			CurrentStatus = "Closed";
		}

		public void Connect( string server, string port )
		{
			CurrentStatus = "Connecting";
			var addresses = System.Net.Dns.GetHostAddresses( server );
			if( addresses.Length == 0 )
			{
				FireMessageSent( "DNS lookup failed for " + server );
				CurrentStatus = "Disconnected";
				return;
			}
			_client = new TcpClient();
			try
			{
				_client.Connect( new System.Net.IPEndPoint( addresses[0], Int32.Parse( port ) ) );
			}
			catch( SocketException )
			{
				FireMessageSent( string.Format( "Couldnt connect to {0} port {1}", addresses[0], port ) );
				CurrentStatus = "Disconnected";
				return;
			}
			_comm = _client.GetStream();
			_input = new System.IO.StreamReader( _comm );
			_output = new System.IO.StreamWriter( _comm );
			CurrentStatus = "Connected";
		}
		public void Logon()
		{
			String nick = ConfigurationManager.AppSettings["NICK"];
			String login = ConfigurationManager.AppSettings["LOGIN"];

			Send( "NICK " + nick + "\r\n" );
			Send( string.Format( "USER {0} {1} {2} :fulhak2.0\r\n", login, ConfigurationManager.AppSettings["ALT_NICK"],
							   ConfigurationManager.AppSettings["SERVER"] ) );

			String line;
			while( ( line = BlockingRead() ) != null )
			{
				if( line.IndexOf( "004" ) >= 0 )
				{
					CurrentStatus = "Logged in";
					return;
				}
				if( line.IndexOf( "433" ) >= 0 )
				{
					FireMessageSent( "Nick taken" );
					CurrentStatus = "Nick taken";
					ConsoleWriter.Write( string.Format( "****: Error. Status: {0}.\n", CurrentStatus ), true );
					return;
				}
			}
			CurrentStatus = "Disconnected";
			ConsoleWriter.Write( string.Format( "****: Got null from server during logon. Status: {0}.\n", CurrentStatus ), true );
		}

		public void Join( string channel )
		{
			Send( "JOIN " + channel + "\r\n" );
			CurrentStatus = "Listening";
		}

		public void Loop()
		{
			string line;
			while( ( line = BlockingRead() ) != null )
			{
				FireMessageReceived( line );
			}
			CurrentStatus = "Disconnected";
			ConsoleWriter.Write( string.Format( "****: Got null from read. Status: {0}.\n", CurrentStatus ), true );
		}

		void FirePropertyChanged( string property )
		{
			if( PropertyChanged != null )
			{
				PropertyChanged( this, new PropertyChangedEventArgs( property ) );
			}
		}
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
