using System.Collections.Generic;
using System.Linq;
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
		readonly IList<IrcController> _ircControllers;
		bool _listensForPowerMode;

		void PowerMode( object sender, PowerModeChangedEventArgs e )
		{
			ConsoleWriter.Write( string.Format( "****: Power mode changed event fired! {0}\n", e.Mode ), true );
			if( e.Mode == PowerModes.Suspend )
				foreach( var irc in _ircControllers )
					irc.Close();
			else
				foreach( var irc in _ircControllers.Where( r => r.ExceptionalState ) )
				{
					ConsoleWriter.Write( "****: Bad state detected." );
					irc.Close();
				}
		}

		public void ClosePowerModeListening()
		{
#if !DEBUG
			if( _listensForPowerMode )
			{
				ConsoleWriter.Write( "****: Deregistering power mode changed event\n", true );
				Microsoft.Win32.SystemEvents.PowerModeChanged -= PowerMode;
				_listensForPowerMode = false;
			}
#endif
		}

		public void RegisterController( IrcController controller )
		{
			_ircControllers.Add( controller );
		}

		public SystemPowerStateListener()
		{
			_ircControllers = new List<IrcController>();

			// visual studio + static api events = badness?
#if !DEBUG
			ConsoleWriter.Write( "****: Registering power mode changed event\n", true );
			Microsoft.Win32.SystemEvents.PowerModeChanged += PowerMode;
			_listensForPowerMode = true;
			Application.Current.Exit += ( a, e ) => ClosePowerModeListening();
			Application.Current.SessionEnding += ( a, e ) => ClosePowerModeListening();
#endif
		}
	}
}
