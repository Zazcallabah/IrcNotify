using System;
using System.Collections.Generic;

namespace IrcNotify
{
	static class ConsoleWriter
	{
		readonly static IList<Action<string>> Output = new List<Action<string>>();

		public static void RegisterWriter( Action<string> writer )
		{
			Output.Add( writer );
		}

		public static void Write( string data, bool verbose = false )
		{
			if( !verbose || ( Settings.DebugMode ) )
				foreach( var o in Output )
				{
					o( data );
				}
		}
	}
}
