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
        private static string _host;
        private static HashSet<Uri> _visited;
        private static HashSet<Uri> _errors;
        private static HashSet<Uri> _internalLinks;
        private static HashSet<Uri> _externalLinks;

        static void Main(string[] args)
        {
            Console.WriteLine("Please enter an URL to scan : (please include http/https)");

            var startString = Console.ReadLine();
            var startUri = new Uri(startString);

            _sb = new StringBuilder();
            _host = startUri.Host;
            _visited = new HashSet<Uri>();
            _errors = new HashSet<Uri>();
            _internalLinks = new HashSet<Uri>();
            _externalLinks = new HashSet<Uri>();

            AnalyseHtmlPage(startUri);

            string final = _sb.ToString();

            Console.Write(final);
            Console.ReadKey();
        }
        private static void AnalyseHtmlPage(Uri page)
        {
            if (!_visited.Contains(page))
            {
                _visited.Add(page);

                Console.WriteLine($"Visiting {page.AbsoluteUri}...");

                using (var web = new WebClient())
                {
                    string content = null;

                    try
                    {
                        content = web.DownloadString(page);
                    }
                    catch(WebException ex)
                    {
                        // Http exception => Discard uri :
                        if (!_errors.Contains(page))
                        {
                            _errors.Add(page);
                        }
                        return;
                    }

                    var document = new HtmlDocument();

                    document.LoadHtml(content);

                    // Read all links :
                    // ================
                    foreach (var node in document.DocumentNode.SelectNodes("//a[@href]"))
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

                            if (uri.Host == _host)
                            {
                                // Internal Link => Analyse :
                                // ==========================
                                if (!_internalLinks.Contains(uri))
                                {
                                    _internalLinks.Add(uri);
                                    _sb.AppendLine($"INTERNAL LINK : {linkUrl}");
                                    AnalyseHtmlPage(uri);
                                }
                            }
                            else
                            {
                                // External Link => Don't Analyse :
                                // ================================
                                if (!_externalLinks.Contains(uri))
                                {
                                    _externalLinks.Add(uri);
                                    _sb.AppendLine($"EXTERNAL LINK : {linkUrl}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
