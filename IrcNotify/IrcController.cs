using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

namespace IrcNotify
{
	class IrcController : INotifyPropertyChanged
	{
		readonly Action<string, string> _alert;
		readonly Action<string> _console;
		Thread _activeListener;
		IrcListener _listener;

		public IrcController( Action<string, string> alert, Action<string> console, string server, IEnumerable<string> channels, User user )
		{
			User = user;
			SetServerAndPort( server );
			Channels = channels;
			_alert = alert;
			_console = console;
		}

		void SetServerAndPort( string server )
		{
			var split = server.Split( new[] { ':' }, StringSplitOptions.RemoveEmptyEntries );
			if( split.Length == 2 )
			{
				Server = split[0];
				Port = split[1];
			}
			else
			{
				Server = server;
				Port = "6667";
			}
		}

		public void ConnectAsync()
		{
			_listener = new IrcListener();
			_listener.PropertyChanged += BubbleStatusChange;
			_listener.MessageReceived += MessageReceived;
			_listener.MessageSent += MessageSent;
			ConsoleWriter.Write( string.Format( "****: Registering listener events.\n" ), true );
			_activeListener = new Thread( Connect );
			_activeListener.Start();
		}

		void BubbleStatusChange( object sender, PropertyChangedEventArgs e )
		{
			if( e.PropertyName == "CurrentStatus" )
			{
				FirePropertyChanged( "Status" );
			}
		}

		void Connect()
		{
			try
			{
				if( _listener.CurrentStatus == "Inactive" || _listener.CurrentStatus == "Disconnected" || _listener.CurrentStatus == "Closed" )
				{
					ConsoleWriter.Write( string.Format( "****: Connecting async with listener status {0}.\n", _listener.CurrentStatus ), true );
					_listener.Connect( Server, Port );
					ConsoleWriter.Write( string.Format( "****: Connected. Status: {0}.\n", _listener.CurrentStatus ), true );
					_listener.Logon( User, Server );
					ConsoleWriter.Write( string.Format( "****: Logged on. Status: {0}.\n", _listener.CurrentStatus ), true );
					if( _listener.CurrentStatus == "Logged in" )
					{
						foreach( var channel in Channels )
						{
							_listener.Join( channel );
							ConsoleWriter.Write( string.Format( "****: Joined channel {1}, Status: {0}.\n", _listener.CurrentStatus, channel ), true );
						}
						_listener.Loop();
					}
				}
				else
					ConsoleWriter.Write( string.Format( "****: Didn't reconnect, listener status {0}.\n", _listener.CurrentStatus ), true );
			}
			catch( SocketException )
			{
				ConsoleWriter.Write( "ERR: Not connected to internet" );
				Close();
			}
			catch( NullReferenceException e )
			{
				ConsoleWriter.Write( string.Format( "ERR: Connection dropped (NullRef from {0})", e.StackTrace ) );
				Close();
			}
		}

		public User User { get; private set; }

		public IEnumerable<string> Channels
		{
			get { return _channels; }
			private set
			{
				var newch = new List<string>();
				foreach( var datastring in value )
				{
					var parts = datastring.Split( new[] { ':' }, StringSplitOptions.RemoveEmptyEntries );
					newch.Add( parts.Length == 2 ? parts[1] : datastring );
				}
				_channels = newch;
			}
		}

		public string Server { get; private set; }
		public string Port { get; private set; }

		readonly IEnumerable<string> _parts = new[] { "JOIN", "PART", "QUIT" };

		readonly Regex _privmsg = new Regex( ConfigurationManager.AppSettings["PRIVMSG_EXTRACT"] );

		readonly Regex _commands = new Regex( ConfigurationManager.AppSettings["JOINPART_EXTRACT"] );
		IEnumerable<string> _channels;

		void MessageReceived( object sender, MessageEventArgs e )
		{
			_console( "RECV: " + e.Message + "\r\n" );

			if( e.Message.Contains( "PRIVMSG" ) )
			{
				var match = _privmsg.Match( e.Message );
				_alert( string.Format( "{0}: {1}", match.Groups["CHANNEL"], match.Groups["NICK"] ), match.Groups["MSG"].ToString() );
			}
			else if( _parts.Any( p => e.Message.Contains( p ) ) )
			{
				var match = _commands.Match( e.Message );
				_alert( string.Format( "{0}: {1}", match.Groups["CMD"], match.Groups["NICK"] ), "..." );
			}
		}

		void MessageSent( object sender, MessageEventArgs e )
		{
			_console( "SENT: " + e.Message );
		}

		public void Close()
		{
			if( _listener != null )
			{
				var l = _listener;
				_listener = null;
				ConsoleWriter.Write( string.Format( "****: Deregistering listener events.\n" ), true );
				l.MessageReceived -= MessageReceived;
				l.MessageSent -= MessageSent;
				l.PropertyChanged -= BubbleStatusChange;
				if( l.CurrentStatus != "Inactive" && l.CurrentStatus != "Disconnected" && l.CurrentStatus != "Closed" )
					l.Close();
				FirePropertyChanged( "Status" );
			}
		}

		public void ReconnectIfDisconnected()
		{
			if( _listener != null && ( _listener.CurrentStatus == "Disconnected" || _listener.CurrentStatus == "Inactive" || _listener.CurrentStatus == "Nick taken" ) )
			{
				ConsoleWriter.Write( string.Format( "****: Closing existing connection. Status:{0}\n", _listener.CurrentStatus ), true );
				Close();
			}

			if( _listener == null )
			{
				ConsoleWriter.Write( "****: Connecting async\n", true );
				ConnectAsync();
			}
		}

		public string Status { get { return _listener == null ? "Disconnected" : _listener.CurrentStatus; } }

		void FirePropertyChanged( string property )
		{
			if( PropertyChanged != null )
				PropertyChanged( this, new PropertyChangedEventArgs( property ) );
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
