using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace IrcNotify
{
	class IrcController
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
			_listener.MessageReceived += MessageReceived;
			_listener.MessageSent += MessageSent;
			_activeListener = new Thread( Connect );
			_activeListener.Start();
		}

		void Connect()
		{
			if( _listener.CurrentStatus == "Inactive" || _listener.CurrentStatus == "Disconnected" )
			{
				_listener.Connect();
				_listener.Logon();
				if( _listener.CurrentStatus == "Logged in" )
				{
					_listener.Join();
					_listener.Loop();
				}
			}
		}

		readonly IEnumerable<string> _parts = new[] { "JOIN", "PART", "QUIT" };
		readonly Regex _privmsg = new Regex( ConfigurationManager.AppSettings["PRIVMSG_EXTRACT"] );
		readonly Regex _commands = new Regex( ":(?'NICK'[^!]+)![^ ]+ (?'CMD'[^ ]+) " );
		void MessageReceived( object sender, MessageEventArgs e )
		{
			_console( "RECV: " + e.Message + "\r\n" );


			if( e.Message.Contains( "PRIVMSG" ) )
			{
				var match = _privmsg.Match( e.Message );
				_alert( string.Format( "{0}: {1}", match.Groups["CHANNEL"], match.Groups["NICK"] ), match.Groups["MSG"].ToString() );
				//  <add key="PRIVMSG_EXTRACT" value=":(?'NICK'[^!]+)![^ ]+ PRIVMSG (?'CHANNEL'[^ ]+) :(?'MSG')"/>

				//  _alert(line);
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
				_listener.MessageReceived -= MessageReceived;
				_listener.MessageSent -= MessageSent;
				if( _listener.CurrentStatus != "Inactive" && _listener.CurrentStatus != "Disconnected" )
					_listener.Close();
				_listener = null;
			}
		}

		public void ReconnectIfDisconnected()
		{
			if( _listener != null && ( _listener.CurrentStatus == "Disconnected" || _listener.CurrentStatus == "Inactive" ) )
			{
				Close();
			}


			if( _listener == null )
			{
				ConnectAsync();
			}
		}
	}
}
