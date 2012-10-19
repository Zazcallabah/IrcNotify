using System.Windows;

namespace IrcNotify
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		readonly Controller _controller;

		public MainWindow()
		{
			InitializeComponent();
			DataContext = _controller = new Controller( MyNotifyIcon );
		}


		protected override void OnClosing( System.ComponentModel.CancelEventArgs e )
		{
			//clean up notifyicon (would otherwise stay open until application finishes)
			MyNotifyIcon.Dispose();
			_controller.Dispose();
			base.OnClosing( e );
		}

		void OpenConsole( object sender, RoutedEventArgs e )
		{
			if( Application.Current.Windows.Count == 1 )
			{
				var newWindow = new ConsoleWindow { DataContext = _controller };
				newWindow.Show();
			}
		}

		void Exit( object sender, RoutedEventArgs e )
		{
			Application.Current.Shutdown();
		}

		void Reconnect( object sender, RoutedEventArgs e )
		{
			_controller.Reconnect();
		}

		private void MyNotifyIcon_TrayLeftMouseUp( object sender, RoutedEventArgs e )
		{
			_controller.Acknowledge();
		}
	}
}
