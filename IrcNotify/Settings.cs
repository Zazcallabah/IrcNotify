using System;
using System.Configuration;

namespace IrcNotify
{
	class Settings
	{
		public static bool DebugMode
		{
			get
			{
				bool b;
				return Boolean.TryParse( ConfigurationManager.AppSettings["VERBOSE"], out b ) && b;
			}
		}
	}
}
