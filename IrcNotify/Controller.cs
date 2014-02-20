using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;

namespace IrcNotify
{
	class Controller : IDisposable, INotifyPropertyChanged
	{
		readonly TaskbarIcon _icon;
		readonly IList<IrcController> _ircControllers;
		string _data;
		Visibility _visibileconsole;
		readonly int _maxBacklog;

		bool _hasAlert;
		bool HasAlert
		{
			get { return _hasAlert; }
			set { if( _hasAlert != value ) { _hasAlert = value; FirePropChanged( "CurrentIconState" ); } }
		}

		public Controller( TaskbarIcon icon )
		{
			_icon = icon;
			_powerstate = new SystemPowerStateListener();
			_ircControllers = new List<IrcController>();
			Data = "";
			HasAlert = false;
			ConsoleWriter.RegisterWriter( ( s ) => Data += s );
			ConsoleVisibility = Visibility.Hidden;
			icon.TrayBalloonTipClicked += ( o, e ) => Acknowledge();
			_maxBacklog = Int32.Parse( ConfigurationManager.AppSettings["BACKLOG_SIZE"] );
			var servers = ConfigurationManager.AppSettings["SERVERS"].Split( ',' );
			var channels = ConfigurationManager.AppSettings["CHANNELS"].Split( ',' );
			var globalchannels = channels.Where( c => !c.Contains( ":" ) ).ToList();
			var user = new User( ConfigurationManager.AppSettings["NICKS"].Split( ',' ) );
			for( int i = 0; i < servers.Length; i++ )
			{
				var ch = channels.Where( c => c.StartsWith( i + ":" ) );
				var irc = new IrcController( ShowNotification, ( s ) => { Data += s; }, servers[i], globalchannels.Concat( ch ), user );
				irc.PropertyChanged += ( o, e ) => FirePropChanged( "CurrentIconState" );
				irc.ConnectAsync();
				_powerstate.RegisterController( irc );
				_ircControllers.Add( irc );
			}
		}

		public void Reconnect()
		{
			foreach( var irc in _ircControllers )
				irc.ReconnectIfDisconnected();
		}

		public void ShowNotification( string title, string msg )
		{
			HasAlert = true;
			_icon.ShowBalloonTip( title, msg, _icon.Icon );
		}

		public string Data
		{
			get
			{
				return _data;
			}

			set
			{
				if( value.Length > _maxBacklog )
					_data = value.Substring( value.Length - _maxBacklog );
				else
					_data = value;
				FirePropChanged( "Data" );
			}
		}

		public Visibility ConsoleVisibility
		{
			get { return _visibileconsole; }
			set { _visibileconsole = value; FirePropChanged( "ConsoleVisibility" ); }
		}


		public void Dispose()
		{
			_powerstate.ClosePowerModeListening();
			foreach( var irc in _ircControllers )
				irc.Close();
		}

		void FirePropChanged( string property )
		{
			if( PropertyChanged != null )
				PropertyChanged( this, new PropertyChangedEventArgs( property ) );
		}

		public event PropertyChangedEventHandler PropertyChanged;


		readonly IDictionary<string, string> _uriLookup = new Dictionary<string, string> {
		{ "Disconnected", "offline" },
		{ "Closed", "offline" },
		{ "Nick taken", "offline" },
		{ "Inactive", "offline" },
		{ "Connecting", "connecting" } };

		readonly SystemPowerStateListener _powerstate;

		public ImageSource CurrentIconState
		{
			get
			{
				var iconname = HasAlert ? "notify" : "chat";
				foreach( var irc in _ircControllers )
				{
					if( _uriLookup.ContainsKey( irc.Status ) )
						iconname = _uriLookup[irc.Status];
				}
				return new BitmapImage( new Uri( @"pack://application:,,,/IrcNotify;component/Resources/" + iconname + ".ico" ) );
			}
		}

		public void Acknowledge()
		{
			HasAlert = false;
		}
	}

}

