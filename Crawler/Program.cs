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
        private static StringBuilder _sb;
        private static HashSet<Uri> _links;

        static void Main(string[] args)
        {
            Console.WriteLine("Please enter an URL to scan : (please include http/https)");

            var startString = Console.ReadLine();
            var startUri = new Uri(startString);

            _sb = new StringBuilder();
            _links = new HashSet<Uri>();

            AnalyseHtmlPage(startUri);

            string final = _sb.ToString();

            Console.Write(final);
            Console.ReadKey();
        }
        private static void AnalyseHtmlPage(Uri page)
        {
            using (var web = new WebClient())
            {
                var content = web.DownloadString(page);

                var document = new HtmlDocument();

                document.LoadHtml(content);

                // Read all links :
                // ================
                foreach (var node in document.DocumentNode.SelectNodes("//a[@href]"))
                {
                    if (node.Attributes.Any(e => e.Name.ToLower() == "href"))
                    {
                        var linkUrl = node.Attributes["href"].Value;

                        // Filter API requests :
                        // =====================
                        if (linkUrl.Contains("?"))
                        {
                            continue;
                        }

                        // Filter anchors :
                        // ================
                        if (linkUrl.Contains("#"))
                        {
                            linkUrl = linkUrl.Split('#').First();
                        }

                        // Trim / at the end of url :
                        // ==========================
                        linkUrl = linkUrl.TrimEnd('/');

                        if (linkUrl.Length > 0)
                        {
                            var uri = new Uri(linkUrl);

                             if (!_links.Contains(uri))
                            {
                                _links.Add(uri);
                                _sb.AppendLine($"LINK : {linkUrl}");
                            }
                        }
                    }
                }
            }
        }
    }
}
