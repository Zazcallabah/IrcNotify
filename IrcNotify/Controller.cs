using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;

namespace IrcNotify
{
	class Controller : IDisposable, INotifyPropertyChanged
	{
		readonly TaskbarIcon _icon;
		readonly IrcController _irc;
		string _data;
		Visibility _visibileconsole;
		bool _hasAlert;
		bool HasAlert
		{
			get { return _hasAlert; }
			set { if( _hasAlert != value ) { _hasAlert = value; FirePropChanged( "CurrentIconState" ); } }
		}

		public Controller( TaskbarIcon icon )
		{
			_icon = icon;
			Data = "";
			HasAlert = false;
			ConsoleVisibility = Visibility.Hidden;
			icon.TrayBalloonTipClicked += ( o, e ) => Acknowledge();

			_irc = new IrcController( ShowNotification, ( s ) => { Data += s; } );
			_irc.PropertyChanged += ( o, e ) => FirePropChanged( "CurrentIconState" );
			_irc.ConnectAsync();

		}

		public void Reconnect()
		{
			_irc.ReconnectIfDisconnected();
		}

		public void ShowNotification( string title, string msg )
		{
			HasAlert = true;
			_icon.ShowBalloonTip( title, msg, _icon.Icon );
		}

		public string Data { get { return _data; } set { _data = value; FirePropChanged( "Data" ); } }

		public Visibility ConsoleVisibility
		{
			get { return _visibileconsole; }
			set { _visibileconsole = value; FirePropChanged( "ConsoleVisibility" ); }
		}

		public void Dispose()
		{
			_irc.Close();
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
		public ImageSource CurrentIconState
		{
			get
			{
				var iconname = HasAlert ? "notify" : "chat";
				if( _uriLookup.ContainsKey( _irc.Status ) )
					iconname = _uriLookup[_irc.Status];
				return new BitmapImage( new Uri( @"pack://application:,,,/IrcNotify;component/Resources/" + iconname + ".ico" ) );
			}
		}

		public void Acknowledge()
		{
			HasAlert = false;
		}
	}

}

