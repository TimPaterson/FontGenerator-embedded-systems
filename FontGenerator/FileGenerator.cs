﻿using System;
using System.IO;

namespace FontGenerator;

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

	const string StrStartHeader		= "START_HEADER";
	const string StrSignature		= "SIGNATURE";
	const string StrVersion			= "FILE_VERSION";
	const string StrEndHeader		= "END_HEADER";

	const string StrStartFont		= "START_FONT";
	const string StrFontAddress		= "FONT_START_OFFSET";
	const string StrCharSetWidth	= "CHARSET_WIDTH";
	const string StrCharHeight		= "CHAR_HEIGHT";
	const string StrFontHeight		= "FONT_SIZE";
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

	const string StrXmlFont = "<Font Name='{0}' FontFamily='{1}' FontStyle='{2}' FontWeight='{3}' FontStretch='{4}' FontSize='{5}'/>";
	const string StrXmlHeight = "<Set>{0}_Height = {1}</Set>";
	const string StrXmlWidth = "<Set>{0}_WidthZero = {1}</Set>";

	StreamWriter Writer;
	FileStream Stream;
	StreamWriter Xml;

	int Offset => (int)Stream.Position;

	public void Open(string fileNameHeader, string fileNameBinary, string fileNameXml)
	{
		if (fileNameHeader != null)
		{
			Writer = new StreamWriter(fileNameHeader);

			Predefine1(StrStartHeader);
			Predefine1(StrSignature);
			Predefine1(StrVersion);
			Predefine1(StrEndHeader);
			Predefine1(StrStartFont);
			Predefine1(StrFontAddress);
			Predefine1(StrCharSetWidth);
			Predefine1(StrCharHeight);
			Predefine1(StrFontHeight);
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
			Stream = new FileStream(fileNameBinary, FileMode.Create);

		if (fileNameXml != null)
			Xml = new StreamWriter(fileNameXml);
	}

	public void Close()
	{
		if (Writer != null)
		{
			Writer.WriteLine();
			DefineHead(StrFontLength, Offset);

			Writer.WriteLine();
			Undefine(StrStartHeader);
			Undefine(StrSignature);
			Undefine(StrVersion);
			Undefine(StrEndHeader);
			Undefine(StrStartFont);
			Undefine(StrFontAddress);
			Undefine(StrCharSetWidth);
			Undefine(StrCharHeight);
			Undefine(StrFontHeight);
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

		if (Xml != null)
		{
			Xml.Close();
			Xml = null;
		}
	}

	public void WriteVersion(uint version, string signature)
	{
		uint sigVal;

		uint StrToUint(string str)
		{
			if (str.Length < 4)
				str = str.PadRight(4);
			return (uint)((str[0] << 0) | (str[1] << 8) | (str[2] << 16) | (str[3] << 24));
		}

		Writer.WriteLine();
		sigVal = StrToUint(signature);
		DefineHead(StrStartHeader, Offset);
		DefineValue(StrSignature, sigVal);
		DefineValue(StrVersion, version);
		DefineHead(StrEndHeader, sizeof(uint) * 2);
		Stream.Write(BitConverter.GetBytes(sigVal), 0, sizeof(uint));
		Stream.Write(BitConverter.GetBytes(version), 0, sizeof(uint));
	}

	public void WriteFont(FontBits fontBits)
	{
		if (fontBits.strideFont * fontBits.height != fontBits.arPx.Length)
			throw new Exception("Bitmap size does not match dimensions");

		if (Writer != null)
		{
			Writer.WriteLine();
			DefineHead(StrStartFont, fontBits.name);
			DefineValue(StrFontAddress, Offset);
			DefineValue(StrCharSetWidth, fontBits.strideFont / fontBits.bytesPerPixel);
			DefineValue(StrCharHeight, fontBits.height);
			DefineValue(StrFontHeight, fontBits.fontHeight);
			DefineValue(StrFirstChar, fontBits.chFirst);
			DefineValue(StrLastChar, fontBits.cntChar - 1);
			DefineValue(StrCharStride, fontBits.strideChar / fontBits.bytesPerPixel);

			DefineValue(StrCharWidths, fontBits.name);
			for (int i = 0; i < fontBits.cntChar; i++)
				DefineValue("\t" + StrCharWidth, fontBits.arWidth[i]);

			DefineValue(StrEndWidths, fontBits.name);
			DefineHead(StrEndFont, fontBits.name);
		}

		Stream?.Write(fontBits.arPx, 0, fontBits.arPx.Length);

		if (Xml != null)
		{
			FontChar font;

			font = fontBits.font;
			Xml.WriteLine(StrXmlFont, fontBits.name, font.FontFamily, font.FontStyle, font.FontWeight, font.FontStretch, font.FontSize);
			Xml.WriteLine(StrXmlHeight, fontBits.name, fontBits.height);
			if (fontBits.chFirst <= '0' && fontBits.chFirst + fontBits.cntChar > '0')
				Xml.WriteLine(StrXmlWidth, fontBits.name, fontBits.arWidth['0' - fontBits.chFirst]);
		}
	}

	public void WriteCharSet(CharSet charSet)
	{
		int charVal;

		if (charSet.Glyphs.Count == 0)
			return;

		Writer.WriteLine();
		DefineHead(StrStartCharSet, charSet.Name);

		charVal = charSet.LastChar;
		foreach (CharSet.Glyph glyph in charSet.Glyphs)
			Writer.WriteLine("\t" + StrDefineChar + "({0}, {1}, {2})", charSet.Name, glyph.Name, ++charVal);

		DefineHead(StrEndCharSet, charSet.Name);
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
