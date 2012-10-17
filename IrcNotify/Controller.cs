using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hardcodet.Wpf.TaskbarNotification;
using System.ComponentModel;

namespace IrcNotify
{
    class Controller : IDisposable, INotifyPropertyChanged

    {
        bool _running = false;
        TaskbarIcon _icon;
        IrcClient _irc;
        public Controller( TaskbarIcon icon )
        {
            _icon = icon;
            _irc = new IrcClient((s) => { Data += s; },ShowNotification);
            _irc.Connect();
            _irc.Logon();
            _irc.Join();
            
            var t = new System.Threading.Thread( _irc.Loop );
            t.Start();
        }

        string _data;

        public string Data { get { return _data; } set { _data = value; FirePropChanged("Data"); } }

        public void ShowNotification(string msg)
        {
            string title = "IRC";

            _icon.ShowBalloonTip(title, msg, _icon.Icon);
        }

        public void Dispose()
        {
            _irc.Close();
            _running = false;
        }
        void FirePropChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

