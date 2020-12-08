using System;
using System.Collections.ObjectModel;
using System.IO;

namespace FontGenerator
{
	class FileGenerator
	{
		const string StrMacroUndef = "#undef {0}";
		const string StrMacroPredfine1 = 
@"#ifndef {0}
#define {0}(a)
#endif";
		const string StrMacroPredfine3 =
@"#ifndef {0}
#define {0}(a,b,c)
#endif";

		const string StrStartFont		= "START_FONT";
		const string StrFontAddres		= "FONT_START_OFFSET";
		const string StrCharSetWidth	= "CHARSET_WIDTH";
		const string StrCharHeight		= "CHAR_HEIGHT";
		const string StrFirstChar		= "FIRST_CHAR";
		const string StrLastChar		= "LAST_CHAR";
		const string StrCharStride		= "CHAR_STRIDE";
		const string StrCharWidths		= "START_CHAR_WIDTHS";
		const string StrCharWidth		= "CHAR_WIDTH";
		const string StrEndWidths		= "END_CHAR_WIDTHS";
		const string StrEndFont			= "END_FONT";
		const string StrFontLength		= "FONT_FILE_LENGTH";
		const string StrStartCharSet	= "START_CHARSET";
		const string StrEndCharSet		= "END_CHARSET";
		const string StrDefineChar		= "DEFINE_CHAR";

		StreamWriter Writer;
		FileStream Stream;
		int Offset;

		public void Open(string fileNameHeader, string fileNameBinary)
		{
			if (fileNameHeader != null)
			{
				Writer = new StreamWriter(fileNameHeader);
				Offset = 0;

				Predefine1(StrStartFont);
				Predefine1(StrFontAddres);
				Predefine1(StrCharSetWidth);
				Predefine1(StrCharHeight);
				Predefine1(StrFirstChar);
				Predefine1(StrLastChar);
				Predefine1(StrCharStride);
				Predefine1(StrCharWidths);
				Predefine1(StrCharWidth);
				Predefine1(StrEndWidths);
				Predefine1(StrEndFont);
				Predefine1(StrFontLength);
				Predefine1(StrStartCharSet);
				Predefine1(StrEndCharSet);
				Predefine3(StrDefineChar);
			}

			if (fileNameBinary != null)
			{
				Stream = new FileStream(fileNameBinary, FileMode.Create);
			}
		}

		public void Close()
		{
			if (Writer != null)
			{
				Writer.WriteLine();
				DefineHead(StrFontLength, Offset);

				Writer.WriteLine();
				Undefine(StrStartFont);
				Undefine(StrFontAddres);
				Undefine(StrCharSetWidth);
				Undefine(StrCharHeight);
				Undefine(StrFirstChar);
				Undefine(StrLastChar);
				Undefine(StrCharStride);
				Undefine(StrCharWidths);
				Undefine(StrCharWidth);
				Undefine(StrEndWidths);
				Undefine(StrEndFont);
				Undefine(StrFontLength);
				Undefine(StrStartCharSet);
				Undefine(StrEndCharSet);
				Undefine(StrDefineChar);

				Writer.Close();
				Writer = null;
			}

			if (Stream != null)
			{
				Stream.Close();
				Stream = null;
			}
		}

		public void WriteFont(FontBits fontBits)
		{
			if (fontBits.strideFont * fontBits.height != fontBits.arPx.Length)
				throw new Exception("Bitmap size does not match dimensions");

			if (Writer != null)
			{
				Writer.WriteLine();
				DefineHead(StrStartFont, fontBits.name);
				DefineValue(StrFontAddres, Offset);
				DefineValue(StrCharSetWidth, fontBits.strideFont);
				DefineValue(StrCharHeight, fontBits.height);
				DefineValue(StrFirstChar, fontBits.chFirst);
				DefineValue(StrLastChar, fontBits.cntChar - 1);
				DefineValue(StrCharStride, fontBits.strideChar);

				DefineValue(StrCharWidths, fontBits.name);
				for (int i = 0; i < fontBits.cntChar; i++)
					DefineValue("\t" + StrCharWidth, fontBits.arWidth[i]);

				DefineValue(StrEndWidths, fontBits.name);
				DefineHead(StrEndFont, fontBits.name);

				Offset += fontBits.arPx.Length;
			}

			if (Stream != null)
			{
				Stream.Write(fontBits.arPx, 0, fontBits.arPx.Length);
			}
		}

		public void WriteCharSet(string name, Collection<CharSet.Glyph> glyphs)
		{
			if (glyphs.Count == 0)
				return;

			Writer.WriteLine();
			DefineHead(StrStartCharSet, name);

			foreach (CharSet.Glyph glyph in glyphs)
				Writer.WriteLine("\t" + StrDefineChar + "({0}, {1}, {2})", name, glyph.Name, glyph.Char);

			DefineHead(StrEndCharSet, name);
		}

		void Predefine1(string macro)
		{
			Writer.WriteLine(StrMacroPredfine1, macro);
		}

		void Predefine3(string macro)
		{
			Writer.WriteLine(StrMacroPredfine3, macro);
		}

		void DefineHead(string macro, object value)
		{
			Writer.WriteLine(macro + "({0})", value);
		}

		void DefineValue(string macro, object value)
		{
			Writer.WriteLine("\t" + macro + "({0})", value);
		}

		void Undefine(string macro)
		{
			Writer.WriteLine(StrMacroUndef, macro);
		}
	}
}
