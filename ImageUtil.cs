using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public static class ImageUtil
{
    public static Image<Rgba32>? LoadImageFromFile(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Erreur : fichier '{imagePath}' non trouvé.");
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

    public static async Task<Image<Rgba32>?> DownloadImageAsync(string url)
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
            Console.WriteLine($"Erreur téléchargement : {ex.Message}");
            return null;
        }
    }

    public static Image<Rgba32> ResizeImageForMicron(Image<Rgba32> image)
    {
        const int maxCharWidth = 130;
        const int maxCharHeight = 50;

        int newWidth = image.Width;
        int newHeight = image.Height;

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
}
