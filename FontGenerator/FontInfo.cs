using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace FontGenerator
{
	public class NamedItem
	{
		public string Name { get; set; }
		public override string ToString() => Name;
	}

	public class FontInfo : NamedItem
	{
		public string FontFamilyName 
		{ 
			get => FontFamily?.Source;
			set
			{
				if (value != null)
				{
					FontFamily = (from fontFamily in MainWindow.FontFamilies
								  where fontFamily.Source == value
								  select fontFamily).First();
				}
			}
		}
		public string TypefaceName 
		{
			get => Typeface?.AdjustedFaceNames.Values.First();
			set
			{
				if (value != null)
				{
					Typeface = (from typeFace in FontFamily.FamilyTypefaces
								where typeFace.AdjustedFaceNames.Values.First() == value
								select typeFace).First();
				}
			}
		}
		public string CharSetName { get; set; }
		public double Size { get; set; }
		public int Threshold { get; set; }
		public double PixelOffset { get; set; }

		[JsonIgnore]
		public CharSet CharSet { get; set; }
		[JsonIgnore]
		public FontFamily FontFamily { get; set; }
		[JsonIgnore]
		public FamilyTypeface Typeface { get; set; }

		public bool PreSerialize()
		{
			if (CharSetName == MainWindow.StrEmptyItem)
				return false;

			CharSetName = CharSet?.Name;
			return true;
		}

		public void PostDeserialize(ObservableCollection<CharSet> charSets)
		{
			foreach (CharSet set in charSets)
			{
				if (set.Name == CharSetName)
					CharSet = set;
			}
		}

		public FontBits GenerateFont(bool f16bits)
		{
			FontChar fontChar;

			fontChar = new()
			{
				FontFamily = FontFamily,
				FontStyle = Typeface.Style,
				FontWeight = Typeface.Weight,
				FontStretch = Typeface.Stretch,
				FontSize = Size
			};

			return CharSet.GenerateFont(fontChar, Name, Threshold, PixelOffset, f16bits);
		}
	}

	class FontNameConverter : IValueConverter
	{
		// Convert from name dictionary -- choose first name
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((IDictionary<XmlLanguage, String>)value).Values.First();
		}

		// Convert back to FamilyTypeface
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
