using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace IrcNotify
{
    class IrcClient
    {
        private IEnumerable<string> _listen = new[] {"ZAZ", "FLILY", "ALMIRA", "DASH"};
        Action<string> _alert;
        Action<string> _console;
        public IrcClient(Action<string> console, Action<string> alert)
        {
            _console = console;
            _alert = alert;
        }

        TcpClient _client;
        System.IO.Stream _comm;
        System.IO.StreamReader _input;
        System.IO.StreamWriter _output;
        public void Send( string command )
        {
            _console("SEND: " + command);
            _output.Write(command);
            _output.Flush();
        }

        public string BlockingRead()
        {
            try
            {
                var inline = _input.ReadLine();
                _console("RECV: " + inline+"\r\n");
                if (inline.ToUpperInvariant().StartsWith("PING "))
                {
                    Send("PONG " + inline.Substring(5) + "\r\n");
                }
                return inline;
            }
            catch (System.IO.IOException)
            {
                return null;
            }
        }

        public void Close()
        {
            _client.Close();
        }

        public void Connect()
        {
            String server = ConfigurationManager.AppSettings["SERVER"];
                var addresses = System.Net.Dns.GetHostAddresses(server);
                if (addresses.Length == 0)
                {
                    throw new ArgumentException(
                        "Unable to retrieve address from specified host name.",
                        "hostName"
                    );
                }
            _client = new TcpClient();
            _client.Connect(new System.Net.IPEndPoint(addresses[0], Int32.Parse(ConfigurationManager.AppSettings["PORT"])));
            _comm = _client.GetStream();
            _input = new System.IO.StreamReader(_comm);
            _output = new System.IO.StreamWriter(_comm);
        }
        public bool Logon()
        {
            String nick = ConfigurationManager.AppSettings["NICK"];
            String login = ConfigurationManager.AppSettings["LOGIN"];

            Send("NICK " + nick + "\r\n");
            Send(string.Format("USER {0} {1} {2} :fulhak2.0\r\n", login, ConfigurationManager.AppSettings["ALT_NICK"],
                               ConfigurationManager.AppSettings["SERVER"]));
 
            String line = null;
            while ((line = BlockingRead()) != null)
            {
                if (line.IndexOf("004") >= 0)
                {
                    return true;
                }
                else if (line.IndexOf("433") >= 0)
                {
                    return false;
                }
            }
            return false;
        }
        public void Join()
        {
            Send("JOIN " + ConfigurationManager.AppSettings["CHANNEL"] + "\r\n");
        }

        IEnumerable<string> _parts = new []{"JOIN","PART","QUIT"};
        Regex _privmsg = new Regex( ConfigurationManager.AppSettings["PRIVMSG_EXTRACT"] );
        Regex _commands = new Regex( ":(?'NICK'[^!]+)![^ ]+ (?'CMD'[^ ]+) " );
        public void Loop()
        {
            string line;
            while ((line = BlockingRead()) != null)
            {
                if (_listen.Any( l => line.ToUpperInvariant().Contains(l)))
                {
                    if (line.Contains("PRIVMSG"))
                    {
                       var match= _privmsg.Match(line);
                       _alert(string.Format("{0} {1}: {2}", match.Groups["CHANNEL"],match.Groups["NICK"],match.Groups["MSG"]));
                            //  <add key="PRIVMSG_EXTRACT" value=":(?'NICK'[^!]+)![^ ]+ PRIVMSG (?'CHANNEL'[^ ]+) :(?'MSG')"/>

                      //  _alert(line);
                    }
                    else if( _parts.Any(p=>line.Contains(p)))
                    {
                      var match =  _commands.Match(line);
                        _alert(string.Format("{0}: {1}", match.Groups["CMD"], match.Groups["NICK"]));
                    }
                }
            }
            _console("TERM");
        }

    }
}
