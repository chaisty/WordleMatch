using Microsoft.Playwright;
using System.Globalization;

namespace WordleFetcher;

class Program
{
    private static readonly DateTime WordleStartDate = new DateTime(2021, 6, 19); // Game 0

    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Wordle Solution Fetcher");
        Console.WriteLine("======================\n");

        // Parse command line arguments
        DateTime targetDate;
        int gameNumber;

        if (args.Length == 0)
        {
            // Default: find next unused game number and date
            var csvPath = Path.Combine("..", "..", "wwwroot", "used-words.csv");
            var (nextGameNumber, nextDate) = await GetNextUnusedGameNumberAndDateAsync(csvPath);

            gameNumber = nextGameNumber;
            targetDate = nextDate;
            Console.WriteLine($"No date specified. Using next unused game number and date:");
        }
        else if (args.Length == 1)
        {
            // Try parsing as date first, then as game number
            if (DateTime.TryParse(args[0], out targetDate))
            {
                gameNumber = (targetDate - WordleStartDate).Days;
            }
            else if (int.TryParse(args[0], out gameNumber))
            {
                targetDate = WordleStartDate.AddDays(gameNumber);
            }
            else
            {
                Console.WriteLine("Error: Invalid date or game number format");
                Console.WriteLine("Usage: WordleFetcher [date|gameNumber]");
                Console.WriteLine("Examples:");
                Console.WriteLine("  WordleFetcher                    (fetch tomorrow's word)");
                Console.WriteLine("  WordleFetcher 2025-12-16         (fetch by date)");
                Console.WriteLine("  WordleFetcher 1641               (fetch by game number)");
                return 1;
            }
        }
        else
        {
            Console.WriteLine("Error: Too many arguments");
            Console.WriteLine("Usage: WordleFetcher [date|gameNumber]");
            return 1;
        }

        Console.WriteLine($"  Date: {targetDate:yyyy-MM-dd} ({targetDate:MMMM d, yyyy})");
        Console.WriteLine($"  Game: #{gameNumber}\n");

        // Construct NYT URL
        var url = $"https://www.nytimes.com/{targetDate:yyyy/MM/dd}/crosswords/wordle-review-{gameNumber}.html";
        Console.WriteLine($"Fetching from: {url}\n");

        // Fetch and parse the word
        string word;
        try
        {
            word = await FetchWordleAnswerAsync(url);
            if (string.IsNullOrEmpty(word))
            {
                Console.WriteLine("Error: Could not extract word from page");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching word: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"✓ Found word: {word.ToUpper()}\n");

        // Validate word
        if (word.Length != 5)
        {
            Console.WriteLine($"Error: Word '{word}' is not 5 letters");
            return 1;
        }

        // Update CSV
        try
        {
            await UpdateUsedWordsCsvAsync(word.ToUpper(), gameNumber, targetDate);
            Console.WriteLine($"✓ Updated used-words.csv\n");
            Console.WriteLine("Success!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating CSV: {ex.Message}");
            return 1;
        }

        return 0;
    }

    static async Task<(int gameNumber, DateTime date)> GetNextUnusedGameNumberAndDateAsync(string csvPath)
    {
        var usedGameNumbers = new HashSet<int>();
        var usedDates = new HashSet<DateTime>();

        if (File.Exists(csvPath))
        {
            var lines = await File.ReadAllLinesAsync(csvPath);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    // Parse game number
                    if (int.TryParse(parts[1].Trim(), out int gameNum))
                    {
                        usedGameNumbers.Add(gameNum);
                    }

                    // Parse date
                    if (DateTime.TryParse(parts[2].Trim(), out DateTime date))
                    {
                        usedDates.Add(date.Date);
                    }
                }
            }
        }

        // Find next unused game number (start from 0)
        int nextGameNumber = 0;
        while (usedGameNumbers.Contains(nextGameNumber))
        {
            nextGameNumber++;
        }

        // Find next unused date (start from Wordle start date)
        DateTime nextDate = WordleStartDate;
        while (usedDates.Contains(nextDate.Date))
        {
            nextDate = nextDate.AddDays(1);
        }

        // Make sure the date is at least today or later
        if (nextDate.Date < DateTime.Today)
        {
            nextDate = DateTime.Today;
            while (usedDates.Contains(nextDate.Date))
            {
                nextDate = nextDate.AddDays(1);
            }
        }

        return (nextGameNumber, nextDate);
    }

    static async Task<string> FetchWordleAnswerAsync(string url)
    {
        // Install Playwright browsers if needed
        Console.WriteLine("Initializing Playwright...");

        // Create playwright instance
        var playwright = await Playwright.CreateAsync();

        // Launch browser (headless mode)
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });

        // Create page
        var page = await browser.NewPageAsync();

        // Navigate to URL
        Console.WriteLine("Loading page...");
        var response = await page.GotoAsync(url, new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        if (response?.Status != 200)
        {
            throw new Exception($"HTTP {response?.Status}: Failed to load page");
        }

        // Wait for content to load
        await Task.Delay(2000);

        // Try multiple selectors to find the answer
        var selectors = new[]
        {
            "text=/The answer to Wordle \\d+ is/i",
            "text=/answer.*is/i",
            "h2:has-text('answer')",
            "p:has-text('answer')",
            ".wordle-answer",
            "[data-testid='wordle-answer']"
        };

        Console.WriteLine("Searching for answer...");

        // Get page text content
        var pageText = await page.ContentAsync();

        // Look for patterns like "The answer to Wordle 1641 is TRUCK"
        var patterns = new[]
        {
            @"answer\s+to\s+Wordle\s+\d+\s+is\s+([A-Z]{5})",
            @"answer\s+is\s+([A-Z]{5})",
            @"solution\s+is\s+([A-Z]{5})",
            @"today'?s?\s+word\s+is\s+([A-Z]{5})"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                pageText,
                pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (match.Success)
            {
                return match.Groups[1].Value.ToLower();
            }
        }

        // If we can't find it with regex, try looking at the DOM structure
        var bodyText = await page.Locator("body").TextContentAsync() ?? "";

        // Look for 5-letter words in all caps (common pattern)
        var wordMatch = System.Text.RegularExpressions.Regex.Match(
            bodyText,
            @"\b([A-Z]{5})\b"
        );

        if (wordMatch.Success)
        {
            var candidate = wordMatch.Groups[1].Value.ToLower();
            Console.WriteLine($"Found potential answer: {candidate.ToUpper()}");
            return candidate;
        }

        throw new Exception("Could not find answer on page");
    }

    static async Task UpdateUsedWordsCsvAsync(string word, int gameNumber, DateTime date)
    {
        var csvPath = Path.Combine("..", "..", "wwwroot", "used-words.csv");

        // Read existing entries
        var entries = new List<(string word, int gameNumber, string date)>();

        if (File.Exists(csvPath))
        {
            var lines = await File.ReadAllLinesAsync(csvPath);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    var existingWord = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int existingGameNumber))
                    {
                        var existingDate = parts[2].Trim();
                        entries.Add((existingWord, existingGameNumber, existingDate));
                    }
                }
            }
        }

        // Check for duplicates
        var existingEntry = entries.FirstOrDefault(e =>
            e.gameNumber == gameNumber ||
            e.word.Equals(word, StringComparison.OrdinalIgnoreCase)
        );

        if (existingEntry != default)
        {
            if (existingEntry.gameNumber == gameNumber &&
                existingEntry.word.Equals(word, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Note: Entry already exists (no changes made)");
                return;
            }
            else if (existingEntry.gameNumber == gameNumber)
            {
                Console.WriteLine($"Warning: Game #{gameNumber} already exists with word '{existingEntry.word}'");
                Console.WriteLine($"Replacing with '{word}'");
                entries.RemoveAll(e => e.gameNumber == gameNumber);
            }
            else if (existingEntry.word.Equals(word, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Warning: Word '{word}' already used in game #{existingEntry.gameNumber}");
                Console.WriteLine($"Adding duplicate entry for game #{gameNumber}");
            }
        }

        // Add new entry
        entries.Add((word, gameNumber, date.ToString("M/d/yyyy")));

        // Sort by game number
        entries = entries.OrderBy(e => e.gameNumber).ToList();

        // Write back to file
        await File.WriteAllLinesAsync(csvPath, entries.Select(e =>
            $"{e.word},{e.gameNumber},{e.date}"
        ));
    }
}
