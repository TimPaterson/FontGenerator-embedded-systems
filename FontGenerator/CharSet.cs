using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FontGenerator;

public record FontBits
(
	string name,
	byte[] arPx,
	int[] arWidth,
	int strideChar,
	int strideFont,
	int bytesPerPixel,
	int height,
	int fontHeight,
	int chFirst,
	int cntChar,
	FontChar font
);

public class CharSet : NamedItem
{
	int m_charFirst = ' ';
	int m_charLast = 0x7E;

	public class Glyph : NamedItem
	{
		public int Char { get; set; }
	}

	IEnumerable<int> AllGlyphs
	{
		get
		{
			for (int ch = FirstChar; ch <= LastChar; ch++)
				yield return ch;

			foreach (Glyph glyph in Glyphs)
				yield return glyph.Char;
		}
	}

	public ObservableCollection<Glyph> Glyphs { get; set; } = new();

	public int FirstChar
	{
		get => m_charFirst > m_charLast ? m_charLast : m_charFirst;
		set => m_charFirst = value;
	}

	public int LastChar
	{
		get => m_charFirst > m_charLast ? m_charFirst : m_charLast;
		set => m_charLast = value;
	}

	public void AddGlyph(int ch, string name)
	{
		Glyphs.Add(new() { Name = name, Char = ch });
	}

	public int ContainsGlyphPos(int ch)
	{
		for (int i = 0; i < Glyphs.Count; i++)
		{
			if (Glyphs[i].Char == ch)
				return i;
		}
		return -1;
	}

	public bool GlyphInRange(int ch)
	{
		return ch >= FirstChar && ch <= LastChar;
	}

	public FontBits GenerateFont(FontChar font, string name, int threshold, double pixelOffset, bool f16bits)
	{
		int i;
		int cChars;
		int maxWidth;
		int stride;
		int height;
		int grayLevel;
		byte[][] arCharset;
		byte[] arbFont;
		int[] arWidth;
		BitmapSource bmp;

		if (LastChar < FirstChar && Glyphs.Count == 0)
			return null;

		grayLevel = threshold * (3 * 255) / 100;

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
		foreach (int ch in AllGlyphs)
		{
			bmp = font.GetBitmap((char)ch, pixelOffset);
			arbFont = ConvertToMonochrome(bmp, grayLevel);
			//arbFont = ConvertToBw(bmp);
			arCharset[i] = arbFont;
			arWidth[i] = bmp.PixelWidth;
			if (bmp.PixelWidth > maxWidth)
				maxWidth = bmp.PixelWidth;
			i++;
		}

		// Copy individual characters into one array. All first rows,
		// then all seconds rows, etc.
		//
		// stride of character
		if (f16bits)
			stride = ((maxWidth + 15) / 16);	// 16-bit pixels
		else
			stride = (maxWidth + 7) / 8;

		height = bmp!.PixelHeight;
		maxWidth = stride * cChars; // stride of full row in pixels
		maxWidth = (maxWidth + 3) & ~3; // align to 4-pixel boundary
		if (f16bits)
		{
			// convert pixels to bytes
			maxWidth *= 2;
			stride *= 2;
		}
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

		return new FontBits(name, arbFont, arWidth, stride, maxWidth, f16bits ? 2 : 1, height, (int)font.FontSize, FirstChar, cChars, font);
	}

	//****************************************************************************
	// Manual converion to black & white. Uses a fixed (specified) threshold.
	// Vertical lines come out perfectly straight.

	protected static byte[] ConvertToMonochrome(BitmapSource bmp, int GrayLevel)
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

	//****************************************************************************
	// Built-in conversion to black & white. The bitmap has gray scale due to
	// anti-aliasing, and this converts that to a dithered pattern. The result
	// is vertical lines with extra or missing dots.

	protected static byte[] ConvertToBw(BitmapSource bmp)
	{
		byte[] arPx;
		int byteWidth;

		byteWidth = (bmp.PixelWidth + 7) / 8;
		arPx = new byte[bmp.PixelHeight * byteWidth];
		if (arPx.Length != 0)
		{
			bmp = new FormatConvertedBitmap(bmp, PixelFormats.BlackWhite, null, 50);
			bmp.CopyPixels(arPx, byteWidth, 0);
		}
		return arPx;
	}
}
