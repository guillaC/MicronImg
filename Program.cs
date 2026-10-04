using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Net.Http;
using System.Text;

var parsedArgs = CommandLineParser.Parse(args);

if (parsedArgs is null)
{
    DisplayHelp();
    Environment.Exit(0);
}

Image<Rgba32>? image = parsedArgs.IsUrl
    ? await DownloadImageAsync(parsedArgs.Path)
    : LoadImageFromFile(parsedArgs.Path);

if (image is null)
{
    Console.WriteLine("Erreur : impossible de charger l'image.");
    Environment.Exit(1);
}

try
{
    Image<Rgba32> resized = ResizeImageForMicron(image);
    string micronContent = ConvertImageToMicron(resized);
    string outputPath = string.IsNullOrEmpty(parsedArgs.OutputPath)
        ? DetermineOutputPath(parsedArgs.Path, parsedArgs.IsUrl)
        : parsedArgs.OutputPath;

    await File.WriteAllTextAsync(outputPath, micronContent, Encoding.UTF8);
    Console.WriteLine($"Fichier genere : {outputPath}");
    resized.Dispose();
}
finally
{
    image?.Dispose();
}

Environment.Exit(0);

// ====== FUNCTIONS ======

void DisplayHelp()
{
    Console.WriteLine(@"MicronImg - Convertisseur d'images en format Micron 1:1

Usage:
  MicronImg -p <chemin> [-o <fichier_sortie>]
  MicronImg -u <url> [-o <fichier_sortie>]

Exemples:
  MicronImg -p image.png
  MicronImg -u https://example.com/image.png -o resultat.mu

Arguments:
  -p <chemin>    Fichier image local
  -u <url>       URL de l'image
  -o <fichier>   Fichier de sortie (optionnel)

Formats: PNG, JPG, JPEG, BMP, GIF");
}

Image<Rgba32>? LoadImageFromFile(string imagePath)
{
    if (!File.Exists(imagePath))
    {
        Console.WriteLine($"Erreur : fichier '{imagePath}' non trouve.");
        return null;
    }

    try
    {
        return Image.Load<Rgba32>(imagePath);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur chargement : {ex.Message}");
        return null;
    }
}

async Task<Image<Rgba32>?> DownloadImageAsync(string url)
{
    try
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        return await Image.LoadAsync<Rgba32>(stream);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur telechargement : {ex.Message}");
        return null;
    }
}

Image<Rgba32> ResizeImageForMicron(Image<Rgba32> image)
{
    // Grande résolution pour du vrai pixel art
    const int maxCharWidth = 130;
    const int maxCharHeight = 50;

    int newWidth = image.Width;
    int newHeight = image.Height;

    // Redimensionne seulement si l'image dépasse les limites
    if (newWidth > maxCharWidth || newHeight > maxCharHeight * 2)
    {
        float ratioWidth = (float)maxCharWidth / newWidth;
        float ratioHeight = (float)(maxCharHeight * 2) / newHeight;

        float ratio = Math.Min(ratioWidth, ratioHeight);

        newWidth = (int)(newWidth * ratio);
        newHeight = (int)(newHeight * ratio);

        image.Mutate(x => x.Resize(newWidth, newHeight));
    }

    return image;
}

string ConvertImageToMicron(Image<Rgba32> image)
{
    int width = image.Width;
    int height = image.Height;

    // Trouve les limites du contenu non-vide
    int minX = width, maxX = -1;
    int minY = 0, maxY = height - 1;

    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            Rgba32 pixel = image[x, y];
            if (pixel.A >= 128 || GetLuminance(pixel) > 40)
            {
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }
    }

    // Si l'image est vide, retourne juste le conteneur
    if (maxX < minX)
        return "`c\n`c";

    // Arrondit minY et maxY au niveau des paires de pixels
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
            Rgba32 topPixel = image[x, y];
            string topColor = ColorToMicronHex(topPixel);
            bool topTransparent = topPixel.A < 128;

            Rgba32 bottomPixel;
            bool bottomTransparent;

            if (y + 1 < height)
            {
                bottomPixel = image[x, y + 1];
                bottomTransparent = bottomPixel.A < 128;
            }
            else
            {
                bottomPixel = new Rgba32(0, 0, 0, 0);
                bottomTransparent = true;
            }

            string bottomColor = ColorToMicronHex(bottomPixel);

            if (topTransparent && bottomTransparent)
            {
                sb.Append(' ');
                previousFgColor = null;
                previousBgColor = null;
                continue;
            }

            char block = SelectBlock(topPixel, bottomPixel, topTransparent, bottomTransparent);

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

char SelectBlock(Rgba32 top, Rgba32 bottom, bool topTransparent, bool bottomTransparent)
{
    // Pixel art pur : demi-carrés avec couleurs
    if (topTransparent && bottomTransparent)
        return ' ';

    // Transparent + opaque : full block de la couleur opaque
    if (topTransparent)
        return '▄';  // Bottom visible
    if (bottomTransparent)
        return '▀';  // Top visible

    // Les deux visibles : utilise un demi-bloc simple basé sur la similarité
    int colorDist = ColorDistance(top, bottom);

    // Si couleurs presque identiques : full block (ou one side)
    if (colorDist < 100)
        return '█';  // Full block couleur du plus lumineux

    // Sinon : half block pour montrer les deux couleurs
    int topLum = GetLuminance(top);
    int bottomLum = GetLuminance(bottom);

    return topLum > bottomLum ? '▀' : '▄';
}

int ColorDistance(Rgba32 c1, Rgba32 c2)
{
    int dr = c1.R - c2.R;
    int dg = c1.G - c2.G;
    int db = c1.B - c2.B;
    return (dr * dr + dg * dg + db * db);
}

int GetLuminance(Rgba32 color)
{
    return (int)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
}

string ColorToMicronHex(Rgba32 color)
{
    if (color.A < 128)
        return "000";

    int r = (color.R >> 4) & 0xF;
    int g = (color.G >> 4) & 0xF;
    int b = (color.B >> 4) & 0xF;

    return $"{r:X}{g:X}{b:X}";
}

string DetermineOutputPath(string imagePath, bool isUrl)
{
    if (isUrl)
    {
        string filename = Path.GetFileNameWithoutExtension(new Uri(imagePath).LocalPath);
        return Path.Combine(Directory.GetCurrentDirectory(), $"{filename}.mu");
    }

    string directory = Path.GetDirectoryName(imagePath) ?? Directory.GetCurrentDirectory();
    string name = Path.GetFileNameWithoutExtension(imagePath);
    return Path.Combine(directory, $"{name}.mu");
}

// ====== COMMAND LINE PARSER ======

record ParsedArgs(string Path, bool IsUrl, string? OutputPath);

static class CommandLineParser
{
    public static ParsedArgs? Parse(string[] args)
    {
        if (args.Length == 0)
            return null;

        string? path = null;
        string? outputPath = null;
        bool isUrl = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "-p":
                    if (i + 1 < args.Length)
                    {
                        path = NormalizePath(args[++i]);
                        isUrl = false;
                    }
                    break;

                case "-u":
                    if (i + 1 < args.Length)
                    {
                        path = args[++i];
                        isUrl = true;
                    }
                    break;

                case "-o":
                    if (i + 1 < args.Length)
                    {
                        outputPath = NormalizePath(args[++i]);
                    }
                    break;

                default:
                    // Autodetection : si c'est un argument sans flag, traite-le comme un chemin
                    if (!args[i].StartsWith("-"))
                    {
                        // Détecte si c'est une URL ou un fichier
                        if (args[i].StartsWith("http://") || args[i].StartsWith("https://"))
                        {
                            path = args[i];
                            isUrl = true;
                        }
                        else
                        {
                            path = NormalizePath(args[i]);
                            isUrl = false;
                        }
                    }
                    break;
            }
        }

        return string.IsNullOrEmpty(path) ? null : new ParsedArgs(path, isUrl, outputPath);
    }

    private static string NormalizePath(string input)
    {
        string normalized = input.Trim('"', '\'');
        return Path.IsPathRooted(normalized) ? normalized : Path.GetFullPath(normalized);
    }
}
