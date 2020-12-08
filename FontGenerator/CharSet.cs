using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace FontGenerator
{
	public record FontBits
	(
		string name, 
		byte[] arPx, 
		int[] arWidth, 
		int strideChar, 
		int strideFont, 
		int height, 
		int chFirst, 
		int cntChar
	);

	public class CharSet : NamedItem
	{
		public class Glyph : NamedItem
		{
			public int Char { get; set; }
		}

		class Enumerator : IEnumerable
		{
			public Enumerator(CharSet parent)
			{
				charSet = parent;
			}

			CharSet charSet;

			public IEnumerator GetEnumerator()
			{
				for (int ch = charSet.FirstChar; ch <= charSet.LastChar; ch++)
					yield return ch;

				foreach (Glyph glyph in charSet.Glyphs)
					yield return glyph.Char;
			}
		}

		public ObservableCollection<Glyph> Glyphs { get; set; } = new();
		public int FirstChar { get; set; } = ' ';
		public int LastChar { get; set; } = 0x7E;

		public void AddGlyph(int ch, string name)
		{
			Glyphs.Add(new() { Name = name, Char = ch});
		}

		public FontBits GenerateFont(FontChar font, string name, int GrayLevel = 500)
		{
			int i;
			int cChars;
			int maxWidth;
			int stride;
			int height;
			byte[][] arCharset;
			byte[] arbFont;
			int[] arWidth;
			BitmapSource bmp;

			if (LastChar < FirstChar && Glyphs.Count == 0)
				return null;

			// Let's remove any invidividual characters that are covered by the range
			for (i = Glyphs.Count - 1; i >= 0; i--)
			{
				int ch = Glyphs[i].Char;
				if (ch >= FirstChar && ch <= LastChar)
					Glyphs.RemoveAt(i);
			}

			cChars = LastChar - FirstChar + 1 + Glyphs.Count;
			bmp = null;
			arCharset = new byte[cChars][];
			arWidth = new int[cChars];
			maxWidth = 0;
			i = 0;
			foreach (int ch in new Enumerator(this))
			{
				bmp = font.GetBitmap((char)ch);
				arbFont = ConvertToMonochrome(bmp, GrayLevel);
				arCharset[i] = arbFont;
				arWidth[i] = bmp.PixelWidth;
				if (bmp.PixelWidth > maxWidth)
					maxWidth = bmp.PixelWidth;
				i++;
			}

			// Copy individual characters into one array. All first rows,
			// then all seconds rows, etc.
			height = bmp.PixelHeight;
			stride = (maxWidth + 7) / 8;  // stride of character
			maxWidth = stride * cChars; // stride of full row
			maxWidth = (maxWidth + 3) & ~3;	// align to 4-byte boundary
			arbFont = new byte[height * maxWidth];
			for (i = 0; i < height; i++)
			{
				for (int j = 0; j < cChars; j++)
				{
					int strideChar = (arWidth[j] + 7) / 8;

					for (int k = 0; k < strideChar; k++)
						arbFont[i * maxWidth + j * stride + k] = arCharset[j][i * strideChar + k];
				}
			}

			return new FontBits(name, arbFont, arWidth, stride, maxWidth, height, FirstChar, cChars);
		}

		protected byte[] ConvertToMonochrome(BitmapSource bmp, int GrayLevel)
		{
			byte[] arPx32;
			byte[] arPx;
			byte posBit;
			int i, j, k;
			int byteWidth;

			byteWidth = (bmp.PixelWidth + 7) / 8;
			arPx = new byte[bmp.PixelHeight * byteWidth];
			if (arPx.Length != 0)
			{
				// Convert 32-bit pixel to black & white
				arPx32 = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
				bmp.CopyPixels(arPx32, bmp.PixelWidth * 4, 0);

				posBit = 0x80;

				for (i = 0, j = 0, k = 0; i < arPx32.Length; i += 4)
				{
					int lum = (int)arPx32[i] + (int)arPx32[i + 1] + (int)arPx32[i + 2];
					if (lum >= GrayLevel)
						arPx[j] |= posBit;

					if (++k == bmp.PixelWidth)
					{
						posBit = 0;
						k = 0;
					}
					posBit >>= 1;
					if (posBit == 0)
					{
						posBit = 0x80;
						j++;
					}
				}
			}
			return arPx;
		}
	}
}
