using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using JOP_PodcastDownloader.Properties;

namespace JOP_PodcastDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            var downloadFolderPath = Settings.Default["DownloadFolderPath"].ToString();
            if (!Directory.Exists(downloadFolderPath))
            {
                Console.WriteLine("Download folder doesn't exist: " + downloadFolderPath);

            }
            else
            {
                var newOnly = args.SingleOrDefault(x => x.ToLower() == "new-only") != null;
                //var downloadedFiles = Directory.GetFiles(downloadFolderPath);
                
var numberOfAdded = 0;
                var podcastsUrls = new List<string>();
                var client = new WebClient();
                client.Encoding = Encoding.UTF8;
                var pageUrl = "https://jakoszczedzacpieniadze.pl/podcast/";
                var pageNo = 0;
                var maxPageNo = 0;
                var podcastsListRegex =
                    new Regex("<h2 class=\"entry-title\"><a href=\"(?<url>.+?)\".+?>(?<title>.+?)</a>");
                var podcastLinkRegex = new Regex("<a href=\"(?<url>.+?mp3)\".+?>Download</a>");

                if (!newOnly)
                {
                    var paginationRegex =
                        new Regex("<a href='https://jakoszczedzacpieniadze.pl/podcast/page/\\d+'>(?<pageNo>\\d+)</a>");

                    var task = client.DownloadStringTaskAsync(pageUrl);
                    Console.Write("Download information about number of pages ");
                    while (!task.IsCompleted)
                    {
                        Console.Write(".");
                        Thread.Sleep(500);
                    }
                    Console.WriteLine();
                    var content1 = task.Result;
                    maxPageNo = int.Parse(paginationRegex.Matches(content1).Cast<Match>().Last().Groups["pageNo"]
                        .Value);
                    Console.WriteLine("There is " + maxPageNo + "pages found");
                }

                pageUrl += "page/";
                Console.WriteLine("To download next podcasts press 'N'");
                do
                {

                    var task1 = client.DownloadStringTaskAsync(pageUrl + ++pageNo);
                    Console.Write("Download page " + pageNo);
                    while (!task1.IsCompleted)
                    {
                        Console.Write(".");
                        Thread.Sleep(500);
                    }
                    Console.WriteLine();
                    var content = task1.Result;

                    foreach (Match match in podcastsListRegex.Matches(content))
                    {
                        Console.WriteLine(match.Groups["title"].Value);
                        //Console.WriteLine("\t" + match.Groups["url"].Value);
                        podcastsUrls.Add(match.Groups["url"].Value);
                    }


                } while (pageNo < maxPageNo);
                //while (Console.ReadKey(true).Key == ConsoleKey.N);
                Console.WriteLine();
                foreach (var podcastsUrl in podcastsUrls)
                {

                    var task2 = client.DownloadStringTaskAsync(podcastsUrl);
                    //Console.Write("Download podcast page ");
                    //while (!task2.IsCompleted)
                    //{
                    //    Console.Write(".");
                    //    Thread.Sleep(500);
                    //}
                    //Console.WriteLine();
                    var url = podcastLinkRegex.Match(task2.Result).Groups["url"].Value;
                    var name = url.Substring(url.IndexOf("WNOP", StringComparison.Ordinal));
                    var filePath = Path.Combine(downloadFolderPath, name);
                    //Console.WriteLine(url);

                    if (File.Exists(filePath))
                    {
                        Console.WriteLine(name + " already exists");
                        if (newOnly)
                        {
                            break;
                        }
                    }
                    else
                    {
                        var task3 = client.DownloadFileTaskAsync(url, Path.ChangeExtension(filePath, ".temp"));
                        Console.Write(name + " is being downloaded");
                        while (!task3.IsCompleted)
                        {
                            Console.Write(".");
                            Thread.Sleep(2000);
                        }
                        File.Move(Path.ChangeExtension(filePath, ".temp"), filePath);
                        numberOfAdded++;
                        Console.WriteLine();
                    }
                }
                Console.WriteLine();
                Console.WriteLine(numberOfAdded + " podcast(s) added");
            }
            
            Console.WriteLine("Done!");

            Task.Run(() =>
            {
                Task.Delay(15000).Wait();
                Environment.Exit(0);
            });

            Console.ReadLine();
        }
    }

    static class StringExt
    {
        public static string RemovePlChars(this string value)
        {
            return value.Replace('ą', 'a')
                .Replace('ę', 'e')
                .Replace('ś', 's')
                .Replace('ć', 'c')
                .Replace('ó', 'ó')
                .Replace('ł', 'l')
                .Replace('ń', 'n');
        }
    }
}
