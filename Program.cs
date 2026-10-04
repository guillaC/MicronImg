using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

var parsedArgs = CommandLineParser.Parse(args);

if (parsedArgs is null)
{
    DisplayHelp();
    Environment.Exit(0);
}

Image<Rgba32>? image = parsedArgs.IsUrl
    ? await ImageUtil.DownloadImageAsync(parsedArgs.Path)
    : ImageUtil.LoadImageFromFile(parsedArgs.Path);

if (image is null)
{
    Console.WriteLine("Erreur : impossible de charger l'image.");
    Environment.Exit(1);
}

try
{
    Image<Rgba32> resized = ImageUtil.ResizeImageForMicron(image);
    string micronContent = MicronConverter.ConvertImageToMicron(resized);
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
                    if (!args[i].StartsWith("-"))
                    {
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
