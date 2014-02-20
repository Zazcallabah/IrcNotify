using System;
using System.Linq;

namespace IrcNotify
{
	public class User
	{
		static readonly Random R = new Random();
		readonly string[] _nicks;
		public User( string[] nicks )
		{
			Nick = nicks.First();
			Login = nicks.Last();
			_nicks = nicks;
		}
		public string AltNick
		{
			get
			{
				return _nicks[R.Next( _nicks.Length )] + R.Next( 100 );
			}
		}
		public string Login { get; set; }
		public string Nick { get; set; }
	}
}