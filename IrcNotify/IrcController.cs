using System;
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

		public IrcController( Action<string, string> alert, Action<string> console )
		{
			_alert = alert;
			_console = console;
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
					string server = ConfigurationManager.AppSettings["SERVER"];
					ConsoleWriter.Write( string.Format( "****: Connecting async with listener status {0}.\n", _listener.CurrentStatus ), true );
					_listener.Connect( server, ConfigurationManager.AppSettings["PORT"] );
					ConsoleWriter.Write( string.Format( "****: Connected. Status: {0}.\n", _listener.CurrentStatus ), true );
					_listener.Logon( ConfigurationManager.AppSettings["NICK"], ConfigurationManager.AppSettings["LOGIN"], ConfigurationManager.AppSettings["ALT_NICK"], server );
					ConsoleWriter.Write( string.Format( "****: Logged on. Status: {0}.\n", _listener.CurrentStatus ), true );
					if( _listener.CurrentStatus == "Logged in" )
					{
						_listener.Join( ConfigurationManager.AppSettings["CHANNEL"] );
						ConsoleWriter.Write( string.Format( "****: Joined channel. Status: {0}.\n", _listener.CurrentStatus ), true );
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
				ConsoleWriter.Write( string.Format("ERR: Connection dropped (NullRef from {0})",e.StackTrace) );
				Close();
			}
		}

		readonly IEnumerable<string> _parts = new[] { "JOIN", "PART", "QUIT" };

		readonly Regex _privmsg = new Regex( ConfigurationManager.AppSettings["PRIVMSG_EXTRACT"] );

		readonly Regex _commands = new Regex( ConfigurationManager.AppSettings["JOINPART_EXTRACT"] );

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
				ConsoleWriter.Write( string.Format( "****: Deregistering listener events.\n" ), true );
				_listener.MessageReceived -= MessageReceived;
				_listener.MessageSent -= MessageSent;
				_listener.PropertyChanged -= BubbleStatusChange;
				if( _listener.CurrentStatus != "Inactive" && _listener.CurrentStatus != "Disconnected" && _listener.CurrentStatus != "Closed" )
					_listener.Close();
				_listener = null;
				FirePropertyChanged( "Status" );
			}
		}

		public void ReconnectIfDisconnected()
		{
			if( _listener != null && ( _listener.CurrentStatus == "Disconnected" || _listener.CurrentStatus == "Inactive" ) )
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
