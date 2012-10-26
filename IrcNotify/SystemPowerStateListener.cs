using System.Windows;
using Microsoft.Win32;

namespace IrcNotify
{
	/// <summary>
	/// Handle listening for power mode changed event.
	/// -> When computer goes to sleep, make sure we leave channel and disconnect proper.
	/// Also unsubscribe from static system event when app closes.
	/// </summary>
	class SystemPowerStateListener
	{
		readonly IrcController _ircController;
		bool _listensForPowerMode;

		void PowerMode( object sender, PowerModeChangedEventArgs e )
		{
			_ircController.Close();
		}

		public void ClosePowerModeListening()
		{
			if( _listensForPowerMode )
			{
				if( Settings.DebugMode )

					Microsoft.Win32.SystemEvents.PowerModeChanged -= PowerMode;
				_listensForPowerMode = false;
			}
		}

		public SystemPowerStateListener( IrcController ircController )
		{
			_ircController = ircController;

			// visual studio + static api events = badness?
#if !DEBUG
			Microsoft.Win32.SystemEvents.PowerModeChanged += PowerMode;
			_listensForPowerMode = true;
#endif
			Application.Current.Exit += ( a, e ) => ClosePowerModeListening();
			Application.Current.SessionEnding += ( a, e ) => ClosePowerModeListening();
		}
	}
}
