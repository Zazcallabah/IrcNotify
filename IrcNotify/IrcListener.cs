using System;
using System.Configuration;
using System.Net.Sockets;

namespace IrcNotify
{
	public class MessageEventArgs : EventArgs
	{
		public MessageEventArgs(string message)
		{
			Message=message;
		}
		public string Message{get;private set;}
	}

    public class IrcListener
    {
        TcpClient _client;
        System.IO.Stream _comm;
        System.IO.StreamReader _input;
        System.IO.StreamWriter _output;

		public IrcListener()
		{
			CurrentStatus = "Inactive";
		}

		public string CurrentStatus { get; private set; }
		
		public event EventHandler<MessageEventArgs> MessageReceived;
		public event EventHandler<MessageEventArgs> MessageSent;

		void FireMessageReceived( string message )
		{
			if( MessageReceived != null )
				MessageReceived( this, new MessageEventArgs( message ) );
		}

		void FireMessageSent( string message )
		{
			if( MessageSent != null )
			 MessageSent(this,new MessageEventArgs(message));
		}


        void Send( string command )
        {
            _output.Write(command);
            _output.Flush();
            FireMessageSent(command);
        }

        string BlockingRead()
        {
            try
            {
                var inline = _input.ReadLine();
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
			CurrentStatus = "Closed";
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
			CurrentStatus = "Connected";
        }
        public void Logon()
        {
            String nick = ConfigurationManager.AppSettings["NICK"];
            String login = ConfigurationManager.AppSettings["LOGIN"];

            Send("NICK " + nick + "\r\n");
            Send(string.Format("USER {0} {1} {2} :fulhak2.0\r\n", login, ConfigurationManager.AppSettings["ALT_NICK"],
                               ConfigurationManager.AppSettings["SERVER"]));
 
            String line;
            while ((line = BlockingRead()) != null)
            {
                if (line.IndexOf("004") >= 0)
                {
                    CurrentStatus = "Logged in";
					return;
                }
                if (line.IndexOf("433") >= 0)
                {
					CurrentStatus = "Nick taken";
                    return;
                }
            }
			CurrentStatus = "Disconnected";
        }

        public void Join()
        {
            Send("JOIN " + ConfigurationManager.AppSettings["CHANNEL"] + "\r\n");
			CurrentStatus = "Listening";
        }

		public void Loop()
        {
            string line;
            while ((line = BlockingRead()) != null)
            {
				FireMessageReceived( line );
            }
			CurrentStatus = "Disconnected";
        }
    }
}
