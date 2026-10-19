using SkiaSharp;

public static class ImageUtil
{
    public static SKBitmap? LoadImageFromFile(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Erreur : fichier '{imagePath}' non trouvé.");
            return null;
        }

        try
        {
            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            return SKBitmap.Decode(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur chargement : {ex.Message}");
            return null;
        }
    }

    public static async Task<SKBitmap?> DownloadImageAsync(string url)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            return SKBitmap.Decode(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur téléchargement : {ex.Message}");
            return null;
        }
    }

    public static SKBitmap ResizeImageForMicron(SKBitmap image)
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

            var resizedBitmap = image.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
            return resizedBitmap ?? image;
        }

        return image;
    }
}
