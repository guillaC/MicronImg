using SkiaSharp;
using System.Text;

public static class MicronConverter
{
    public static string ConvertImageToMicron(SKBitmap image)
    {
        int width = image.Width;
        int height = image.Height;

        int minX = width, maxX = -1;
        int minY = 0, maxY = height - 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor pixel = image.GetPixel(x, y);
                if (pixel.Alpha >= 128 || ColorHelper.GetLuminance(pixel) > 40)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX < minX)
            return "`c\n`c";

        minY = (minY / 2) * 2;
        maxY = ((maxY + 1) / 2) * 2;

        var sb = new StringBuilder();
        sb.AppendLine("`c");

        string? previousFgColor = null;
        string? previousBgColor = null;

        for (int y = minY; y < maxY; y += 2)
        {
            for (int x = minX; x <= maxX; x++)
            {
                SKColor topPixel = image.GetPixel(x, y);
                string topColor = ColorHelper.ColorToMicronHex(topPixel);
                bool topTransparent = topPixel.Alpha < 128;

                SKColor bottomPixel;
                bool bottomTransparent;

                if (y + 1 < height)
                {
                    bottomPixel = image.GetPixel(x, y + 1);
                    bottomTransparent = bottomPixel.Alpha < 128;
                }
                else
                {
                    bottomPixel = new SKColor(0, 0, 0, 0);
                    bottomTransparent = true;
                }

                string bottomColor = ColorHelper.ColorToMicronHex(bottomPixel);

                if (topTransparent && bottomTransparent)
                {
                    sb.Append(' ');
                    previousFgColor = null;
                    previousBgColor = null;
                    continue;
                }

                char block = BlockSelector.SelectBlock(topPixel, bottomPixel, topTransparent, bottomTransparent);

                if (block == ' ')
                {
                    sb.Append(' ');
                    previousFgColor = null;
                    previousBgColor = null;
                    continue;
                }

                if (topColor != previousFgColor && !topTransparent)
                {
                    sb.Append($"`F{topColor}");
                    previousFgColor = topColor;
                }

                if (bottomColor != previousBgColor && !bottomTransparent)
                {
                    sb.Append($"`B{bottomColor}");
                    previousBgColor = bottomColor;
                }

                sb.Append(block);
            }

            sb.AppendLine();
            previousFgColor = null;
            previousBgColor = null;
        }

        sb.AppendLine("`c");
        return sb.ToString();
    }
}

public static class BlockSelector
{
    public static char SelectBlock(SKColor top, SKColor bottom, bool topTransparent, bool bottomTransparent)
    {
        if (topTransparent && bottomTransparent)
            return ' ';

        if (topTransparent)
            return '▄';
        if (bottomTransparent)
            return '▀';

        int colorDist = ColorHelper.ColorDistance(top, bottom);

        if (colorDist < 100)
            return '█';

        int topLum = ColorHelper.GetLuminance(top);
        int bottomLum = ColorHelper.GetLuminance(bottom);

        return topLum > bottomLum ? '▀' : '▄';
    }
}

public static class ColorHelper
{
    public static int ColorDistance(SKColor c1, SKColor c2)
    {
        int dr = c1.Red - c2.Red;
        int dg = c1.Green - c2.Green;
        int db = c1.Blue - c2.Blue;
        return (dr * dr + dg * dg + db * db);
    }

    public static int GetLuminance(SKColor color)
    {
        return (int)(0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);
    }

    public static string ColorToMicronHex(SKColor color)
    {
        if (color.Alpha < 128)
            return "000";

        int r = (color.Red >> 4) & 0xF;
        int g = (color.Green >> 4) & 0xF;
        int b = (color.Blue >> 4) & 0xF;

        return $"{r:X}{g:X}{b:X}";
    }
}
