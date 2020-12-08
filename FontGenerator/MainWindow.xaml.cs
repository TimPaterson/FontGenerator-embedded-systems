using FontGenerator.Properties;
using PatersonTech;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FontGenerator
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region Constants

		public const string StrEmptyItem = "+";
		public const string StrTitle = "Font Generator";

		const string StrFontNameLabel = "Font:";
		const string StrCharSetNameLabel = "Character Set:";
		const string StrNamedCharNameLabel = "Character Name:";
		const string StrProjectFileFilter = "Font Files|*.fnt|All Files|*.*";
		const string StrUntitledProject = "Untitled";

		static readonly double[] FontSizes = { 16, 18, 20, 24, 32, 48, 64, 72, 96 };
		static readonly Brush CharBorderBrush = new SolidColorBrush(Colors.Black);
		public static readonly Brush CharTableLabelBrush = new SolidColorBrush(Colors.PaleTurquoise);
		static readonly Brush SelectedCharSequenceBrush = new SolidColorBrush(Colors.GreenYellow);
		static readonly Brush SelectedCharBrush = new SolidColorBrush(Colors.Lime);

		#endregion


		#region Types
		public class Project
		{
			public ObservableCollection<FontInfo> FontInfos { get; set; }
			public ObservableCollection<CharSet> CharSets { get; set; }
			public bool PreSerialize()
			{
				foreach (FontInfo fontInfo in FontInfos)
				{
					if (!fontInfo.PreSerialize())
					{
						MessageBox.Show($"The character set used by font {fontInfo} must be given a name before it can be saved.", 
							MainWindow.StrTitle, MessageBoxButton.OK, MessageBoxImage.Error);
						return false;
					}
				}
				return true;
			}
			public void PostDeserialize()
			{
				foreach (FontInfo fontInfo in FontInfos)
					fontInfo.PostDeserialize(CharSets);
			}
		}

		#endregion


		#region Private Fields

		Project m_project;
		NamedItem m_namedItem;
		ListBox m_curList;
		string m_projectFileName;
		int m_lastRow = 1;

		#endregion


		#region Constructor

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;
		}

		#endregion


		#region Properties

		public static ICollection<FontFamily> FontFamilies { get; } = Fonts.SystemFontFamilies;
		public Brush TableLabelBrush => CharTableLabelBrush;

		string ProjectFileName
		{
			get => m_projectFileName;
			set
			{
				Title = StrTitle + " - " + (string.IsNullOrEmpty(value) ? StrUntitledProject : Path.GetFileName(value));
				m_projectFileName = value;
			}
		}
		FontInfo CurrentFont
		{ 
			get
			{
				FontInfo info = (FontInfo)lstFonts.SelectedItem;
				if (info == null && lstFonts.Items.Count != 0)
				{
					lstFonts.SelectedIndex = 0;
					info = (FontInfo)lstFonts.SelectedItem;
				}
				return info;
			}
		}

		CharSet CurrentCharSet
		{
			get
			{
				CharSet set = (CharSet)lstCharSets.SelectedItem;
				if (set == null && lstCharSets.Items.Count != 0)
				{
					lstCharSets.SelectedIndex = 0;
					set = (CharSet)lstCharSets.SelectedItem;
				}
				return set;
			}
		}

		#endregion

		protected override void OnSourceInitialized(EventArgs e)
		{
			// Initialized event is too early to set window placement.
			// Loaded event has already drawn the window, so if we 
			// wait that long we see it move.
			base.OnSourceInitialized(e);

			// Read in saved per-user settings
			if (!Settings.Default.SettingsUpgraded)
			{
				Settings.Default.Upgrade();
				Settings.Default.SettingsUpgraded = true;
				Settings.Default.Save();
			}
			this.SetPlacement(Settings.Default.MainWindowPlacement);
		}


		#region Private Methods

		void BuildCharGrid(int rowEnd)
		{
			TextBlock textBlock;
			Border border;
			Binding binding;
			Thickness borderMargin;
			Thickness borderThickness;
			Thickness textPadding;
			int row, col;

			if (rowEnd <= m_lastRow)
				return;

			borderMargin = new Thickness(-0.5);
			borderThickness = new Thickness(1);
			textPadding = new Thickness(5);

			binding = new("SelectedItem")
			{
				ElementName = "drpFontFamily"
			};

			for (row = m_lastRow + 1; row <= rowEnd; row++)
			{
				// Set row label
				textBlock = new()
				{
					FontSize = 16,
					Text = $"0x{row:X03}x",
					Padding = textPadding,
					Background = CharTableLabelBrush,
				};

				border = new()
				{
					BorderBrush = CharBorderBrush,
					BorderThickness = borderThickness,
					Margin = borderMargin,
					Child = textBlock
				};

				Grid.SetRow(border, row - 1);
				grdChar.RowDefinitions.Add(new());
				grdChar.Children.Add(border);

				for (col = 0; col < 16; col++)
				{
					textBlock = new()
					{
						FontSize = 16,
						Text = ((char)(row * 16 + col)).ToString(),
						Padding = textPadding,
						TextAlignment = TextAlignment.Center,
					};
					textBlock.SetBinding(TextBlock.FontFamilyProperty, binding);
					textBlock.MouseDown += CharSet_MouseDown;

					border = new()
					{
						BorderBrush = CharBorderBrush,
						BorderThickness = borderThickness,
						Margin = borderMargin,
						Child = textBlock
					};

					Grid.SetColumn(border, col + 1);
					Grid.SetRow(border, row - 1);
					grdChar.Children.Add(border);
				}
			}

			m_lastRow = rowEnd;
		}

		TextBlock GetCharTableElement(int i)
		{
			int row, col;

			row = i / 16 - 1;
			col = i % 16 + 1;
			return (TextBlock)((Border)grdChar.Children[row * 17 + col]).Child;
		}

		void ShowSelectedChar(CharSet charSet, bool isSelected)
		{
			for (int i = charSet.FirstChar; i <= charSet.LastChar; i++)
				GetCharTableElement(i).Background = isSelected ? SelectedCharSequenceBrush : null;

			foreach (CharSet.Glyph glyph in charSet.Glyphs)
				GetCharTableElement(glyph.Char).Background = isSelected ? SelectedCharBrush : null;
		}

		void LoadProject(string fileName)
		{
			int maxRow;

			m_project = JsonSerializer.Deserialize<Project>(File.ReadAllText(fileName));
			m_project.PostDeserialize();
			ProjectFileName = Path.GetFullPath(fileName);
			Settings.Default.LastProjectFileName = ProjectFileName;

			// Find largest character, expand table if necessary
			maxRow = 0;
			foreach (CharSet charSet in m_project.CharSets)
			{
				maxRow = Math.Max(maxRow, charSet.LastChar / 16);
				if (charSet.Glyphs.Count != 0)
				{
					maxRow = Math.Max(maxRow,
						(from glyph in charSet.Glyphs
						 select glyph.Char / 16).Max());
				}
			}
			BuildCharGrid(maxRow);
			ShowProject();
		}

		void SaveProject(string fileName)
		{
			if (m_project.PreSerialize())
			{
				File.WriteAllText(fileName, JsonSerializer.Serialize<Project>(m_project, 
					new JsonSerializerOptions() { WriteIndented = true }));
				Settings.Default.LastProjectFileName = ProjectFileName;
			}
		}

		void NewProject()
		{
			ProjectFileName = null;

			m_project = new Project()
			{
				FontInfos = new(),
				CharSets = new()
			};
			
			// Seed the project with one character set
			m_project.CharSets.Add(new()
			{
				Name = "Full"
			});

			m_project.CharSets.Add(new() { Name = StrEmptyItem });

			m_project.FontInfos.Add(new() { Name = StrEmptyItem });

			ShowProject();
		}

		void ShowProject()
		{
			lstCharSets.ItemsSource = m_project.CharSets;
			lstFonts.ItemsSource = m_project.FontInfos;
			lstFonts.SelectedIndex = 0;
		}

		void FontSelected()
		{
			FontInfo fontInfo;

			fontInfo = CurrentFont;

			// If font info isn't set (new item), use current selections
			if (fontInfo.FontFamily == null)
			{
				fontInfo.FontFamily = (FontFamily)drpFontFamily.SelectedItem;
				fontInfo.Typeface = (FamilyTypeface)drpFontStyle.SelectedItem;
				double.TryParse(drpFontSize.Text, out double size);
				fontInfo.Size = size;	// default to zero if conversion failed
			}
			else
			{
				drpFontFamily.SelectedItem = fontInfo.FontFamily;
				drpFontStyle.SelectedItem = fontInfo.Typeface;
				drpFontSize.SelectedItem = fontInfo.Size;
				drpFontSize.Text = fontInfo.Size.ToString();
			}

			// If it doesn't have a CharSet, use current
			if (fontInfo.CharSet == null)
				fontInfo.CharSet = CurrentCharSet;
			else
				lstCharSets.SelectedItem = fontInfo.CharSet;

			lblItemName.Text = StrFontNameLabel;
			txtItemName.Text = fontInfo.Name == StrEmptyItem ? "" : fontInfo.Name;
			m_namedItem = fontInfo;
			m_curList = lstFonts;
		}

		void CharSetSelected()
		{
			CharSet charSet;

			charSet = CurrentCharSet;
			lblItemName.Text = StrCharSetNameLabel;
			txtItemName.Text = charSet.Name == StrEmptyItem ? "" : charSet.Name;
			m_namedItem = charSet;
			m_curList = lstCharSets;
		}

		void NameCharSelected()
		{
			CharSet.Glyph glyph;


			glyph = (CharSet.Glyph)lstNamedChar.SelectedItem;
			if (glyph == null)
				return;
			lblItemName.Text = StrNamedCharNameLabel;
			txtItemName.Text = glyph.Name;
			m_namedItem = glyph;
			m_curList = lstNamedChar;
		}

		#endregion


		#region Event Handlers

		private void Window_Initialized(object sender, EventArgs e)
		{

			drpFontFamily.ItemsSource = Fonts.SystemFontFamilies;
			drpFontFamily.SelectedIndex = 0;

			drpFontSize.ItemsSource = FontSizes;
			drpFontSize.SelectedItem = 18.0;

			BuildCharGrid(7);

			if (!string.IsNullOrEmpty(Settings.Default.LastProjectFileName))
				LoadProject(Settings.Default.LastProjectFileName);
			else
				NewProject();

			lstFonts.ItemsSource = m_project.FontInfos;
			lstCharSets.ItemsSource = m_project.CharSets;

		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Settings.Default.MainWindowPlacement = this.GetPlacement();
			Settings.Default.Save();
		}

		private void CharSet_MouseDown(object sender, MouseButtonEventArgs e)
		{
			CharSet charSet;
			TextBlock textBlock;
			int charCode;

			charSet = CurrentCharSet;
			textBlock = (TextBlock)sender;
			charCode = textBlock.Text[0];

			if (charSet.FirstChar == charSet.LastChar)
			{
				if (charSet.FirstChar > charCode)
					charSet.FirstChar = charCode;
				else
					charSet.LastChar = charCode;

				ShowSelectedChar(charSet, true);
			}
			else
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					// remove existing range
					for (int i = charSet.FirstChar; i <= charSet.LastChar; i++)
						GetCharTableElement(i).Background = null;

					charSet.FirstChar = charCode;
					charSet.LastChar = charCode;
					textBlock.Background = SelectedCharSequenceBrush;
				}
				else
				{
					// Selecting single character
					// See if already present
					for (int i = 0; i < charSet.Glyphs.Count; i++)
					{
						if (charSet.Glyphs[i].Char == charCode)
						{
							// Remove it
							charSet.Glyphs.RemoveAt(i);
							GetCharTableElement(charCode).Background =
								(charCode >= charSet.FirstChar && charCode <= charSet.LastChar) ? SelectedCharSequenceBrush : null;
							return;
						}
					}
					// Not present
					if (charCode >= charSet.FirstChar && charCode <= charSet.LastChar)
						return;	// already included

					charSet.AddGlyph(charCode, $"CharCode_{charCode:X04}");
					GetCharTableElement(charCode).Background = SelectedCharBrush;
				}
			}
		}

		private void btnShow_Click(object sender, RoutedEventArgs e)
		{
			FontBits fontBits;
			BitmapSource bmp;

			fontBits = CurrentFont.GenerateFont();
			bmp = BitmapSource.Create(fontBits.strideFont * 8, fontBits.height, 96, 96, PixelFormats.BlackWhite, null, fontBits.arPx, fontBits.strideFont);
			new FontWindow(this, bmp, CurrentFont.Name).Show();
		}

		private void lstFonts_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			FontSelected();
		}

		private void lstFonts_GotFocus(object sender, RoutedEventArgs e)
		{
			if (m_curList != lstFonts)
				FontSelected();
		}

		private void lstCharSets_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			CharSet charSet;

			charSet = CurrentCharSet;

			// Clear character table for old selection
			if (e.RemovedItems.Count > 0)
				ShowSelectedChar((CharSet)e.RemovedItems[0], false);

			ShowSelectedChar(charSet, true);
			CurrentFont.CharSet = charSet;
			lstNamedChar.ItemsSource = charSet.Glyphs;
			CharSetSelected();
		}

		private void lstCharSets_GotFocus(object sender, RoutedEventArgs e)
		{
			CharSetSelected();
		}

		private void lstNamedChar_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			NameCharSelected();
		}

		private void lstNamedChar_GotFocus(object sender, RoutedEventArgs e)
		{
			NameCharSelected();
		}

		private void drpFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var fontFamily = (FontFamily)drpFontFamily.SelectedItem;
			drpFontStyle.ItemsSource = fontFamily.FamilyTypefaces;
			drpFontStyle.FontFamily = fontFamily;

			FontInfo fontInfo = CurrentFont;
			if (fontInfo == null)
				return;

			fontInfo.FontFamily = fontFamily;
		}

		private void drpFontStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			FontInfo fontInfo = CurrentFont;
			if (fontInfo == null)
				return;

			fontInfo.Typeface = (FamilyTypeface)drpFontStyle.SelectedItem;
		}

		private void drpFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			FontInfo fontInfo = CurrentFont;
			if (fontInfo == null)
				return;

			if (drpFontSize.SelectedIndex != -1)
				fontInfo.Size = (double)drpFontSize.SelectedItem;
		}

		private void drpFontSize_TextChanged(object sender, TextChangedEventArgs e)
		{
			FontInfo fontInfo = CurrentFont;
			if (fontInfo == null)
				return;

			if (double.TryParse(drpFontSize.Text, out double size) && size > 5 && size < 1000)
				fontInfo.Size = size;
			else
				fontInfo.Size = 0;	// Mark as invalid
		}

		private void btnSaveName_Click(object sender, RoutedEventArgs e)
		{
			NamedItem item;
			IList list;
			int i;

			item = m_namedItem;
			if (string.IsNullOrEmpty(item?.Name) || m_curList == null)
				return;

			if (item.Name == StrEmptyItem)
			{
				switch (item)
				{
					case FontInfo:
						m_project.FontInfos.Add(new() { Name = StrEmptyItem });
						break;

					case CharSet:
						m_project.CharSets.Add(new() { Name = StrEmptyItem });
						break;
				}
			}
			item.Name = txtItemName.Text;

			// To get ListBox to update, remove item then add it back
			list = (IList)m_curList.ItemsSource;
			i = list.IndexOf(item);
			list.RemoveAt(i);
			list.Insert(i, item);
			m_curList.SelectedIndex = i;
		}

		private void txtItemName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
				btnSaveName_Click(sender, null);
			else if (e.Key == Key.Escape)
				btnCancel_Click(sender, null);
		}

		private void btnDelete_Click(object sender, RoutedEventArgs e)
		{
			IList list;

			if (m_namedItem.Name == StrEmptyItem)
				return;

			if (m_namedItem is CharSet)
			{
				int count = (from font in m_project.FontInfos
							 where font.CharSet == m_namedItem && font.Name != StrEmptyItem
							 select font).Count();
				if (count != 0)
				{
					MessageBox.Show($"There are {count} fonts using this character set. Please change the character set of each of these fonts before deleting this charater set.",
						StrTitle, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
			}
			list = (IList)m_curList.ItemsSource;
			list.Remove(m_namedItem);
			m_curList.SelectedIndex = 0;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			txtItemName.Text = m_namedItem.Name == StrEmptyItem ? "" : m_namedItem.Name;
		}

		private void btnNew_Click(object sender, RoutedEventArgs e)
		{
			NewProject();
		}

		private void btnOpen_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog dlg;

			dlg = new();
			dlg.Filter = StrProjectFileFilter;
			if (!string.IsNullOrEmpty(Settings.Default.LastProjectFileName))
				dlg.InitialDirectory = Path.GetDirectoryName(Settings.Default.LastProjectFileName);
			if (dlg.ShowDialog(this) == true)
				LoadProject(dlg.FileName);
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(ProjectFileName))
			{
				Microsoft.Win32.SaveFileDialog dlg;

				dlg = new Microsoft.Win32.SaveFileDialog();
				dlg.Filter = StrProjectFileFilter;
				try
				{
					dlg.InitialDirectory = Path.GetDirectoryName(Settings.Default.LastProjectFileName);
					dlg.FileName = Path.GetFileName(Settings.Default.LastProjectFileName);
				}
				catch { }

				if (dlg.ShowDialog(this) == true)
					ProjectFileName = dlg.FileName;
			}

			SaveProject(ProjectFileName);
		}

		private void btnGenerate_Click(object sender, RoutedEventArgs e)
		{
			FileGenerator output;
			string fileName;

			if (m_project.FontInfos.Count == 0)
				return;
			
			output = new();
			fileName = Path.Combine(Path.GetDirectoryName(m_projectFileName), Path.GetFileNameWithoutExtension(m_projectFileName));
			output.Open(fileName + ".h", fileName + ".bin");

			foreach (FontInfo fontInfo in m_project.FontInfos)
			{
				if (fontInfo.Name != StrEmptyItem)
					output.WriteFont(fontInfo.GenerateFont());
			}

			foreach (CharSet charSet in m_project.CharSets)
				output.WriteCharSet(charSet.Name, charSet.Glyphs);

			output.Close();
		}

		private void btnMoreRows_Click(object sender, RoutedEventArgs e)
		{
			ICollection<Typeface> typefaces;
			int maxRow;

			if (m_lastRow == 7)
				BuildCharGrid(15);
			else
			{
				typefaces = ((FontFamily)drpFontFamily.SelectedItem)?.GetTypefaces();
				foreach (Typeface typeface in typefaces)
				{
					if (typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
					{
						maxRow = glyphTypeface.CharacterToGlyphMap.Keys.Max() / 16;
						if (maxRow > m_lastRow)
							BuildCharGrid(Math.Min(maxRow, (m_lastRow + 1) * 2));
						break;
					}
					else
						Debug.WriteLine("No GlyphTypeface");
				}
			}
		}

		#endregion
	}
}
