using HtmlAgilityPack;

using System.Text.RegularExpressions;

string projectDirectory = Directory.GetCurrentDirectory();
string imagesFolder = Path.Combine(projectDirectory, "images");
Directory.CreateDirectory(imagesFolder);

await GetCardsLinksFromEveryPageAsync();

async Task GetCardsLinksFromEveryPageAsync()
{
    using HttpClient client = new();
    for (int i = 1; i <= 1; i++) // Hardcoded end page number 221
    {
        Console.WriteLine($"Scraping page {i:000}/221 for card links");
        try
        {
            string url = $"https://opentheflag.com/card/search?page={i}";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string html = await response.Content.ReadAsStringAsync();

            HtmlDocument document = new();
            document.LoadHtml(html);

            HtmlNode parentDiv = document.DocumentNode.SelectSingleNode("//div[@class='cards-view-mode-gallery']");
            HtmlNodeCollection anchorTags = parentDiv.SelectNodes("//div[@class='card-view-mode-gallery']//a");
            foreach (HtmlNode anchor in anchorTags)
            {
                string hrefValue = anchor.GetAttributeValue("href", string.Empty);
                await GetImagesTODO(hrefValue);
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
        }
    }
}

async Task GetImagesTODO(string url)
{
    string id = ExtractIdFromUrl(url);

    using HttpClient client = new();

    Console.WriteLine($"Getting data from {url}");
    try
    {
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string html = await response.Content.ReadAsStringAsync();

        HtmlDocument document = new();
        document.LoadHtml(html);

        HtmlNode imgNode = document.DocumentNode.SelectSingleNode("//div[@class='card-image']//img");
        string imgSrc = imgNode.GetAttributeValue("src", string.Empty);

        await DownloadImageAsync(imgSrc.Replace("/thumbs", string.Empty), Path.Combine(imagesFolder, $"{id}.png"));

        HtmlNode? titleNode = document.DocumentNode.SelectSingleNode("//div[@class='card-worlds']//a[@title]");
        if (titleNode is not null)
        {
            Console.WriteLine("World: " + titleNode.GetAttributeValue("title", string.Empty));
        }
        
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine($"Request error: {e.Message}");
    }
}

string ExtractIdFromUrl(string url)
{
    Match match = Regex.Match(url, @"\/card\/(\d+)\/");
    return match.Groups[1].Value;
}

async Task DownloadImageAsync(string imageUrl, string outputPath)
{
    using HttpClient client = new();
    Console.WriteLine("Downloading image...");
    try
    {
        byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);
        await File.WriteAllBytesAsync(outputPath, imageBytes);
        Console.WriteLine($"Image downloaded to {outputPath}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Failed to download image: {e.Message}");
    }
}
