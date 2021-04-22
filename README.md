# FontGenerator: Convert Windows fonts to bitmaps
FontGenerator is a Windows app for creating font bitmaps from any
Windows fonts. For each font you choose you also select what characters
you want to include.

FontGenerator produces these output files:

- A binary file (.bin) with all chosen fonts, 1 bit per pixel.
- A header file (.h) with macros describing the fonts, including tables of 
character widths.
- An optional XML file with a snippet describing the fonts in the format used 
by [ScreenDesigner](https://github.com/TimPaterson/ScreenDesigner-for-touchscreens).

![Example](https://raw.githubusercontent.com/TimPaterson/FontGenerator-embedded-systems/master/Example/ScreenShot.png)

### Character Sets
As you can see on the right side of the screenshot, characters sets
have names. You select the characters in two ways:

- Dragging the mouse across the character grid will select a range.
- Clicking on individual characters will add (or remove) single characters. 
They can also be added in hex using the `Hex char code` field (above the 
character grid).

Only one character range is permitted. The idea is that the target system
will be using ASCII/Unicode characters, and subtracting the code for the
first character will give an index into the character range.

When single characters are added outside the range, they are given names.
There is a default name assigned that you can change. These characters are
added sequentially to the font bitmap, so they won't be in correct position
to be referenced with their normal ASCII/Unicode value. The header file
contains entries with the names and the correct index in the font bitmap.

Character sets can be shared by multiple fonts. This makes named characters
valid for every font using the same character set.

When creating a new project, there is an initial character set called
"Full" that contains all the characters from 0x20 - 0x7E (i.e., the printable
ASCII character set).

### Preview
The `Show` button above the character grid will open a window displaying the
exact bitmap that will be generated for the font. For example:

![FontPreview](https://raw.githubusercontent.com/TimPaterson/FontGenerator-embedded-systems/master/Example/PreviewScreenshot.png)

You can make adjustments to the `Threshold` or `Pixel Offset` controls
and preview again to get a side-by-side comparison. `Threshold` is the luminosity
level that triggers the difference between 0 and 1 for a pixel; it seems to
work best left at 50%. `Pixel Offset` affects the width of vertical lines in
the font glyph; it should be in the range 0 - 0.5. These settings are adjustable
for each font.

### Output Files

The binary output is **not** a series of one-character bitmaps. As shown in the
preview above, it is one long bitmap per font. The length of each font bitmap 
is padded to a multiple of 4 bytes as required by some systems. Each font 
immediately follows the previous one in the file.

The `Project` group near the top has a selection called `Bitmap multiple`.
This selects whether the rows of the bitmap for are made up of 8-bit or 16-bit
chunks. The 16-bit chunks are little-endian, while the font data is used
MSB first, so this effectively swaps the bytes in addition to rounding up
the stride of each character. The RAIO RA8876 graphics controller chip
works best with a 16-bit multiple. The `CHARSET_WIDTH` and `CHAR_STRIDE`
values discussed below are in units of this multiple.

Here is a portion of the header file created for the example (Example/Fonts.h):

	START_FONT(DigitDisplay)
		FONT_START_OFFSET(0)
		CHARSET_WIDTH(156)
		CHAR_HEIGHT(111)
		FONT_SIZE(96)
		FIRST_CHAR(32)
		LAST_CHAR(25)
		CHAR_STRIDE(6)
		START_CHAR_WIDTHS(DigitDisplay)
			CHAR_WIDTH(27)
			CHAR_WIDTH(27)
			CHAR_WIDTH(34)
			CHAR_WIDTH(54)
			CHAR_WIDTH(54)
			CHAR_WIDTH(86)
			CHAR_WIDTH(64)
			CHAR_WIDTH(19)
			CHAR_WIDTH(32)
			CHAR_WIDTH(32)
			CHAR_WIDTH(38)
			CHAR_WIDTH(56)
			CHAR_WIDTH(27)
			CHAR_WIDTH(32)
			CHAR_WIDTH(27)
			CHAR_WIDTH(27)
			CHAR_WIDTH(54)
			CHAR_WIDTH(54)
			CHAR_WIDTH(54)
			CHAR_WIDTH(54)
			CHAR_WIDTH(54)
			CHAR_WIDTH(54)
			CHAR_WIDTH(54)
			CHAR_WIDTH(54)
			CHAR_WIDTH(54)
			CHAR_WIDTH(54)
		END_CHAR_WIDTHS(DigitDisplay)
	END_FONT(DigitDisplay)

The macros above define the following data:

| Macro             | Description |
| -----             | ----------- |
| FONT_START_OFFSET | Byte offset in binary file of this font |
| CHARSET_WIDTH     | Stride of the font bitmap |
| CHAR_HEIGHT       | Number of rows in each character and the bitmap |
| FIRST_CHAR        | ASCII/Unicode value of the first chararcter |
| LAST_CHAR         | Index of the last character (number of characters minus 1) |
| CHAR_STRIDE       | Stride of each character within the bitmap |

Here is an example of code in C that creates a data structure for each font:

	typedef struct
	{
		ulong	FontStart;
		ushort	CharsetWidth;
		ushort	Height;
		byte	FirstChar;
		byte	LastChar;
		byte	CharStride;
		byte	arWidths[];
	} FontInfo;

	#define START_FONT(name)	const FontInfo FONT_##name = {
	#define END_FONT(name)		};

	#define FONT_START_OFFSET(val)	.FontStart = val + RamFontStart,
	#define CHARSET_WIDTH(val)	.CharsetWidth = val,
	#define CHAR_HEIGHT(val)	.Height = val,
	#define FIRST_CHAR(val)		.FirstChar = val,
	#define LAST_CHAR(val)		.LastChar = val,
	#define CHAR_STRIDE(val)	.CharStride = val,
	#define START_CHAR_WIDTHS(name)	{
	#define CHAR_WIDTH(val)		val,
	#define END_CHAR_WIDTHS(name)	}

	// Run the macros
	#include "Fonts/Fonts.h"

This sample uses an extension in the GCC C compiler that allows initialization
of flexible arrays (this is not available in GCC C++). The work-around if this
is not available is to define a struct for each font with the character-width
array length set to the value in the `LAST_CHAR` macro plus 1.
