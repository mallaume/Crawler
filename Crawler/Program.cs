using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Crawler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter an URL to scan : (please include http/https)");

            var startString = Console.ReadLine();
            var startUri = new Uri(startString);

            AnalyseHtmlPage(startUri);

            Console.ReadKey();
        }
        private static void AnalyseHtmlPage(Uri page)
        {
            using (var web = new WebClient())
            {
                var content = web.DownloadString(page);

                var html = new HtmlDocument();

                html.LoadHtml(content);
                StringBuilder sb = new StringBuilder();

                foreach (var node in html.DocumentNode.SelectNodes("//a[@href]"))
                {
                    if (node.Attributes.Any(e => e.Name.ToLower() == "href"))
                    {
                        var linkUrl = node.Attributes["href"].Value;
                        sb.AppendLine($"LINK : {linkUrl}");
                    }
                }

                string final = sb.ToString();

                Console.Write(final);
            }
        }
    }
}
