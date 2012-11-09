using System.Windows;

namespace IrcNotify
{
	/// <summary>
	/// Interaction logic for ConsoleWindow.xaml
	/// </summary>
	public partial class ConsoleWindow : Window
	{
		public ConsoleWindow()
		{
			InitializeComponent();
		}

		private void Window_GotFocus( object sender, RoutedEventArgs e )
		{
			Scroll.ScrollToBottom();
		}

	}
}
