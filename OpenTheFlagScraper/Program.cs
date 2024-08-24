using HtmlAgilityPack;

using Newtonsoft.Json;

using OpenTheFlagScraper;

using System.Text.RegularExpressions;

string projectDirectory = Directory.GetCurrentDirectory();
string imagesFolder = Path.Combine(projectDirectory, "images");
Directory.CreateDirectory(imagesFolder);

List<Card> cards = new();

await GetCardsLinksFromEveryPageAsync();

string json = JsonConvert.SerializeObject(cards, Formatting.Indented);
File.WriteAllText(Path.Combine(projectDirectory, "cards.json"), json);

Console.WriteLine("Cards information has been successfully written to the JSON file.");

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
                await GetCardDataAsync(hrefValue);
                Console.WriteLine();
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
        }
    }
}

async Task GetCardDataAsync(string url)
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

        // Extract the card name
        HtmlNode imgNode = document.DocumentNode.SelectSingleNode("//div[@class='card-image']//img");
        string imgSrc = imgNode.GetAttributeValue("src", string.Empty);
        await DownloadImageAsync(imgSrc.Replace("/thumbs", string.Empty), Path.Combine(imagesFolder, $"{id}.png"));

        Console.WriteLine("Gathering stats...");

        HtmlNode? worldNode = document.DocumentNode.SelectSingleNode("//div[@class='card-worlds']//a[@title]");
        string? cardName = GetInfo(document, "//h1");
        string? attributes = GetAttribtes(document, "//div[@class='card-attributes']//a");
        string? power = GetInfo(document, "//li[@class='card-power']/a");
        string? critical = GetInfo(document, "//li[@class='card-critical']/a");
        string? defence = GetInfo(document, "//li[@class='card-defense']/a");
        string? size = GetInfo(document, "//li[@class='card-size']/a");
        string? type = GetInfo(document, "//li[@class='card-type']/a");

        Card card = new()
        {
            Name = cardName,
            ImageName = $"{id}.png",
            World = worldNode?.GetAttributeValue("title", string.Empty),
            Attributes = attributes,
            Power = int.TryParse(power, out int powerValue) ? powerValue : null,
            Critical = int.TryParse(critical, out int criticalValue) ? criticalValue : null,
            Defence = int.TryParse(defence, out int defenceValue) ? defenceValue : null,
            Size = size,
            Type = type,
        };

        cards.Add(card);
        Console.WriteLine("Stats extracted and saved");
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine($"Request error: {e.Message}");
    }
}

string? GetInfo(HtmlDocument doc, string xpath)
{
    HtmlNode node = doc.DocumentNode.SelectSingleNode(xpath);
    return node?.InnerText.Trim();
}

string? GetAttribtes(HtmlDocument doc, string xpath)
{
    HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(xpath);
    if (nodes is null)
    {
        return null;
    }

    List<string> attributes = new();
    foreach (HtmlNode node in nodes)
    {
        attributes.Add(node.InnerText.Trim());
    }

    return string.Join(" / ", attributes);
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
