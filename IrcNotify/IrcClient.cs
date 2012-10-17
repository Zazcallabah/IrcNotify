using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace IrcNotify
{
    class IrcClient
    {
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
                String server = "irc.quakenet.org";
                var addresses = System.Net.Dns.GetHostAddresses(server);
                if (addresses.Length == 0)
                {
                    throw new ArgumentException(
                        "Unable to retrieve address from specified host name.",
                        "hostName"
                    );
                }
            _client = new TcpClient();
            _client.Connect(new System.Net.IPEndPoint(addresses[0], 6667));
            _comm = _client.GetStream();
            _input = new System.IO.StreamReader(_comm);
            _output = new System.IO.StreamWriter(_comm);
        }
        public bool Logon()
        {
            String nick = "notification_listener45363";
            String login = "notification_listener2345664";

            Send("NICK " + nick + "\r\n");
            Send("USER " + login + " aoeuoaeuaoeu irc.quakenet.org :fulhak2.0\r\n");

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
            String channel = "#dotdash";
            Send("JOIN " + channel + "\r\n");

        }



        public void Loop()
        {
            string line;
            while ((line = BlockingRead()) != null)
            {
                if (line.ToUpperInvariant().Contains("ZAZ"))
                {
                    _alert(line);
                }
            }
            _console("TERM");
        }

    }
}
