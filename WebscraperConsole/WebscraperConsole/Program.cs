using System;
using System.Collections.Generic;
using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using Newtonsoft.Json;
using System.IO;
using System.Formats.Asn1;
using CsvHelper;
using CsvHelper.Configuration;

class Program
{
    static List<Dictionary<string, string>> wishlist = new List<Dictionary<string, string>>();

    static void Main()
    {
        bool exit = false;

        while (!exit)
        {
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1. YouTube");
            Console.WriteLine("2. ICTJob");
            Console.WriteLine("3. Bol.com");
            Console.WriteLine("4. Show wishlist");
            Console.WriteLine("5. Exit");
            Console.WriteLine("----------------------------\n");
            Console.Write("Your choice: ");
            string userInput = Console.ReadLine();

            if (int.TryParse(userInput, out int choice))
            {
                if (choice >= 1 && choice <= 5)
                {
                    // The input is a valid integer within the range
                    Console.WriteLine($"Your choice is: {choice}");
                }
                else
                {
                    // The input is a valid integer, but not within the range
                    Console.WriteLine("Your choice must be between 1 and 5.");
                }
            }
            else
            {
                // The conversion failed, and 'choice' is not a valid integer
                Console.WriteLine("This is not a valid number.");
            }

            Console.WriteLine();

            switch (choice)
            {
                case 1:
                    Console.Write("Enter a search term for YouTube: ");
                    string youtubeSearchTerm = Console.ReadLine();
                    ScrapeYouTube(youtubeSearchTerm);
                    break;
                case 2:
                    Console.Write("Enter a search term for ICTJob: ");
                    string ictjobSearchTerm = Console.ReadLine();
                    ScrapeICTJob(ictjobSearchTerm);
                    break;
                case 3:
                    Console.Write("Enter a search term for Bol.com: ");
                    string bolSearchTerm = Console.ReadLine();
                    ScrapeBolCom(bolSearchTerm);
                    break;
                case 4:
                    ShowWishlist();
                    break;
                case 5:
                    Console.WriteLine("Your CSV and JSON files can be found in your Downloads folder.");
                    exit = true;
                    break;
            }
        }
    }

    static void ScrapeYouTube(string searchTerm)
    {
        IWebDriver driver = new EdgeDriver(); // Use EdgeDriver for Microsoft Edge

        // Navigate to the YouTube search page
        driver.Navigate().GoToUrl($"https://www.youtube.com/results?search_query={searchTerm}");

        // Wait for the page to be fully loaded
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

        // Get the video elements
        var videoElements = driver.FindElements(By.CssSelector("div#contents ytd-video-renderer"));

        // Extract data from the first 5 video elements
        List<Dictionary<string, string>> videos = new List<Dictionary<string, string>>();

        for (int i = 0; i < Math.Min(5, videoElements.Count); i++)
        {
            var video = new Dictionary<string, string>
            {
                { "Title", GetTextSafely(videoElements[i], "#video-title") },
                { "Link", videoElements[i].FindElement(By.CssSelector("#video-title")).GetAttribute("href") },
                { "Uploader", GetTextSafely(videoElements[i], "ytd-video-renderer #channel-info #text-container yt-formatted-string a") },
                { "Views", GetTextSafely(videoElements[i], "#metadata-line span:nth-child(3)") }
            };
            videos.Add(video);
            // Print data to console
            Console.WriteLine($"Video {i + 1}:");
            Console.WriteLine($"Title: {video["Title"]}");
            Console.WriteLine($"Link: {video["Link"]}");
            Console.WriteLine($"Uploader: {video["Uploader"]}");
            Console.WriteLine($"Views: {video["Views"]}\n");
        }

        // Close the browser
        driver.Quit();

        // Write data to JSON and CSV
        WriteToJSON(videos, "youtube_data.json");
        WriteToCSV(videos, "youtube_data.csv");
    }

    // Helper method to safely get text from an element or return an empty string if not found
    static string GetTextSafely(IWebElement element, string selector)
    {
        try
        {
            return element.FindElement(By.CssSelector(selector)).Text;
        }
        catch (NoSuchElementException)
        {
            return string.Empty;
        }
    }

    static void ScrapeICTJob(string searchTerm)
    {
        IWebDriver driver = new EdgeDriver(); // Use EdgeDriver for Microsoft Edge

        // Navigate to the ICTJob search page
        driver.Navigate().GoToUrl($"https://www.ictjob.be/nl/it-vacatures-zoeken?keywords={searchTerm}");

        // Wait for the page to be fully loaded
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

        // Get the job elements
        var jobElements = driver.FindElements(By.CssSelector(".search-item"));

        // Extract data from the first 5 job elements
        List<Dictionary<string, string>> jobs = new List<Dictionary<string, string>>();

        int displayedJobNumber = 0;

        for (int i = 0; i < jobElements.Count && displayedJobNumber < 5; i++)
        {
            // Skip elements with the class 'create-job-alert-search-item'
            if (jobElements[i].GetAttribute("class").Contains("create-job-alert-search-item"))
            {
                continue;
            }

            displayedJobNumber++;

            var job = new Dictionary<string, string>
            {
                { "Title", GetTextSafely(jobElements[i], ".job-title h2") },
                { "Company", GetTextSafely(jobElements[i], ".job-company") },
                { "Location", GetTextSafely(jobElements[i], ".job-location span[itemprop='addressLocality']") },
                { "Keywords", GetTextSafely(jobElements[i], ".job-keywords") },
                { "Link", jobElements[i].FindElement(By.CssSelector("a.job-title.search-item-link")).GetAttribute("href") }
            };

            // Print data to console
            Console.WriteLine($"Job {displayedJobNumber}:");
            Console.WriteLine($"Title: {job["Title"]}");
            Console.WriteLine($"Company: {job["Company"]}");
            Console.WriteLine($"Location: {job["Location"]}");
            Console.WriteLine($"Keywords: {job["Keywords"]}");
            Console.WriteLine($"Link: {job["Link"]}\n");
            jobs.Add( job );
        }

        // Close the browser
        driver.Quit();

        // Write data to JSON and CSV
        WriteToJSON(jobs, "ictjob_data.json");
        WriteToCSV(jobs, "ictjob_data.csv");
    }

    static void ScrapeBolCom(string searchTerm)
    {
        IWebDriver driver = new EdgeDriver(); // Use EdgeDriver for Microsoft Edge

        // Navigate to the Bol.com search page
        driver.Navigate().GoToUrl($"https://www.bol.com/nl/nl/s/?searchtext={searchTerm}/");

        // Wait for the page to be fully loaded
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

        // Get data for the 5 best products
        List<Dictionary<string, string>> products = new List<Dictionary<string, string>>();

        var productElements = driver.FindElements(By.CssSelector("li.product-item--row"));
        for (int i = 0; i < Math.Min(5, productElements.Count); i++)
        {
            var priceElements = productElements[i].FindElements(By.CssSelector(".promo-price, .promo-price__fraction"));
            var priceText = string.Join("", priceElements.Select(element => element.Text.Trim()));

            // Extract only digits and a dot
            var regex = new System.Text.RegularExpressions.Regex(@"[0-9\.]+");
            var match = regex.Match(priceText);
            var extractedPrice = match.Success ? match.Value : "N/A";

            var product = new Dictionary<string, string>
            {
                { "Title", productElements[i].FindElement(By.CssSelector(".product-title")).Text },
                { "Price", extractedPrice },
                { "Link", productElements[i].FindElement(By.CssSelector(".product-title")).GetAttribute("href") }
            };
            products.Add(product);

            Console.WriteLine($"Product {i + 1}:");
            Console.WriteLine($"Title: {product["Title"]}");
            Console.WriteLine($"Price: {product["Price"]}");
            Console.WriteLine($"Link: {product["Link"]}\n");

            // Ask the user if they want to add the product to the wishlist
            Console.Write("Do you want to add this product to the wishlist? (y/n): ");
            string addToWishlist = Console.ReadLine().ToLower();

            if (addToWishlist == "y")
            {
                wishlist.Add(product);
                Console.WriteLine("Product added to the wishlist!");
            }

            Console.WriteLine();
        }

        // Close the browser
        driver.Quit();

        // Write data to JSON and CSV
        WriteToJSON(products, "bolcom_data.json");
        WriteToCSV(products, "bolcom_data.csv");
    }

    static void ShowWishlist()
    {
        if (wishlist.Count == 0)
        {
            Console.WriteLine("Your wishlist is empty.");
        }
        else
        {
            Console.WriteLine("Your wishlist contains the following products:");

            for (int i = 0; i < wishlist.Count; i++)
            {
                Console.WriteLine($"Product {i + 1}:");
                Console.WriteLine($"Title: {wishlist[i]["Title"]}");
                Console.WriteLine($"Price: {wishlist[i]["Price"]}");
                Console.WriteLine($"Link: {wishlist[i]["Link"]}\n");
            }
        }
    }

    static void WriteToCSV(List<Dictionary<string, string>> data, string filename)
    {
        string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string fullPath = Path.Combine(downloadsPath, "Downloads", filename);

        using (var writer = new StreamWriter(fullPath))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, Delimiter = "," }))
        {
            foreach (var record in data)
            {
                csv.WriteRecords(record);
            }
        }
    }

    static void WriteToJSON(List<Dictionary<string, string>> data, string filename)
    {
        string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string fullPath = Path.Combine(downloadsPath, "Downloads", filename);

        File.WriteAllText(fullPath, JsonConvert.SerializeObject(data, Formatting.Indented));
    }
}
