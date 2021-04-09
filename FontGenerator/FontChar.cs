using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FontGenerator
{
	class EmptyBitmapSource : BitmapSource
	{
		public EmptyBitmapSource(int height)
		{
			PixelHeight = height;
		}

		public override int PixelWidth => 0;
		public override int PixelHeight { get; }
		protected override Freezable CreateInstanceCore()
		{
			throw new NotImplementedException();
		}
	}

	public class FontChar : TextBlock
	{
		public FontChar()
		{
			Foreground = new SolidColorBrush(Colors.White);
			Background = new SolidColorBrush(Colors.Black);
		}

		public BitmapSource GetBitmap(char ch, double pixelOffset)
		{
			RenderTargetBitmap bmp;
			int height;

			Text = ch.ToString();
			Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			Arrange(new Rect(pixelOffset, 0, DesiredSize.Width, DesiredSize.Height));
			height = (int)(DesiredSize.Height + 0.9);
			if (DesiredSize.Width == 0)
				return new EmptyBitmapSource(height);

			bmp = new RenderTargetBitmap(
				(int)(DesiredSize.Width + pixelOffset + 0.9), 
				height, 
				96, 96, PixelFormats.Pbgra32);
			bmp.Render(this);
			return bmp;
		}
	}
}