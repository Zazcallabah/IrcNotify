using System;
using System.ComponentModel;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace IrcNotify
{
	class Controller : IDisposable, INotifyPropertyChanged
	{
		readonly TaskbarIcon _icon;
		readonly IrcController _irc;
		string _data;
		Visibility _visibileconsole;

		public Controller( TaskbarIcon icon )
		{
			_icon = icon;
			Data = "";
			ConsoleVisibility = Visibility.Hidden;

			_irc = new IrcController( ShowNotification, ( s ) => { Data += s; } );
			_irc.ConnectAsync();
		}

		public void Reconnect()
		{
			_irc.ReconnectIfDisconnected();
		}
		public void ShowNotification( string title, string msg )
		{
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


	}
}

