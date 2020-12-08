using System.Windows;
using System.Windows.Media.Imaging;

namespace FontGenerator
{
	/// <summary>
	/// Interaction logic for FontWindow.xaml
	/// </summary>
	public partial class FontWindow : Window
	{
		public FontWindow(Window owner, BitmapSource bmp, string name)
		{
			Owner = owner;
			InitializeComponent();
			imgFont.Source = bmp;
			imgFont.Width = bmp.Width;
			imgFont.Height = bmp.Height;
			Title = MainWindow.StrTitle + " - " + name;
		}
	}
}
