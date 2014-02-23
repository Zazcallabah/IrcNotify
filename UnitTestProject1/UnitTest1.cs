using System;
using System.Collections.Generic;
using IrcNotify;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
	[TestClass]
	public class UnitTest1
	{
		string regex =  ":(?'NICK'(?!PPGuest[0-9]*)[^!]+)![^ ]+ PRIVMSG (?'CHANNEL'[^ ]+) :(?'MSG'.*)$";
		[TestMethod]
		public void GivenAController_WhenParsingNormalMessage_ThenAlertIsSent()
		{
			var c = Controller(regex, "" );
			c.MessageReceived( null, new MessageEventArgs( ":Zaz!~zaz@c80-216-222-64.bredband.comhem.se PRIVMSG #testinglol234 :aou" ) );

			Assert.AreEqual( 1, Hits.Count );
			Assert.AreEqual( "#testinglol234: Zaz|aou", Hits[0] );
		}
		
		[TestMethod]
		public void GivenAController_WhenParsingIgnoredMessage_ThenAlertIsNotSent()
		{
			var c = Controller(regex, "" );
			c.MessageReceived( null, new MessageEventArgs( ":PPGuest234!~zaz@c80-216-222-64.bredband.comhem.se PRIVMSG #testinglol234 :aou" ) );

			Assert.AreEqual( 0, Hits.Count );
		}
		List<string> Hits;

		IrcController Controller( string message, string command )
		{
			Hits = new List<string>();
			return new IrcController( ( s, s2 ) => Hits.Add( s + "|" + s2 ), ( s ) => { }, "", new[] { "" }, null, message, command );
		}
	}

}
