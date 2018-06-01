using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Crawler
{
    class Program
    {
        private static string _host;
        private static string _scheme;
        private static HashSet<Uri> _visited;
        private static HashSet<Uri> _errors;
        private static HashSet<Uri> _internalLinks;
        private static HashSet<Uri> _externalLinks;
        private static HashSet<Uri> _images;
        private static HashSet<Uri> _scripts;
        private static HashSet<Uri> _styleSheets;

        static void Main(string[] args)
        {
            Console.WriteLine("Please enter an URL to scan : (please include http/https)");

            var startString = Console.ReadLine();
            var startUri = new Uri(startString);

            _host = startUri.Host;
            _scheme = startUri.Scheme;
            _visited = new HashSet<Uri>();
            _errors = new HashSet<Uri>();
            _internalLinks = new HashSet<Uri>();
            _externalLinks = new HashSet<Uri>();
            _images = new HashSet<Uri>();
            _scripts = new HashSet<Uri>();
            _styleSheets = new HashSet<Uri>();

            AnalyseHtmlPage(startUri);

            // Write result to text file :
            // ===========================
            var sb = new StringBuilder();

            foreach (var uri in _internalLinks)
            {
                sb.AppendLine($"-> INTERNAL LINK : {uri.ToString()}");
            }

            foreach (var uri in _externalLinks)
            {
                sb.AppendLine($"-> EXTERNAL LINK : {uri.ToString()}");
            }

            foreach (var uri in _images)
            {
                sb.AppendLine($"-> IMAGE : {uri.ToString()}");
            }

            foreach (var uri in _scripts)
            {
                sb.AppendLine($"-> SCRIPT : {uri.ToString()}");
            }

            foreach (var uri in _styleSheets)
            {
                sb.AppendLine($"-> STYLE SHEET : {uri.ToString()}");
            }

            foreach (var uri in _errors)
            {
                sb.AppendLine($"-> ERROR : {uri.ToString()}");
            }

            var filename = $"{System.IO.Path.GetTempPath()}crawlerOutput.txt";

            System.IO.File.WriteAllText(filename, sb.ToString());

            Console.WriteLine();
            Console.WriteLine($"Crawling finished ! Output written to {filename}");
            Console.WriteLine("Do you want to open the file ? (Y/N)");

            var result = Console.ReadLine();

            if (result == "Y")
            {
                Process.Start(filename);
            }

            Console.WriteLine("Press a key to exit...");
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

                        var headers = web.ResponseHeaders;

                        // Filter Content-Type = text/html only :
                        // ======================================
                        var contentType = headers.Get("Content-Type");

                        if (contentType == null || contentType != "text/html")
                        {
                            return;
                        }
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
                        var linkUrl = CleanUri(node.Attributes["href"].Value);

                        if (linkUrl != null && linkUrl.Length > 0)
                        {
                            var uri = new Uri(linkUrl);

                            if (uri.Host == _host)
                            {
                                // Internal Link => Analyse :
                                // ==========================
                                if (!_internalLinks.Contains(uri))
                                {
                                    _internalLinks.Add(uri);
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
                                }
                            }
                        }
                    }

                    // Read static assets :
                    // ====================
                    ReadStaticAssets(document, "//img[@src]", "src", _images, "IMAGE");
                    ReadStaticAssets(document, "//script[@src]", "src", _scripts, "SCRIPT");
                    ReadStaticAssets(document, "//link[@rel='stylesheet' and @href]", "href", _styleSheets, "STYLESHEET");
                }
            }
        }
        private static void ReadStaticAssets(HtmlDocument document, string nodeQuery, string uriAttributeName, HashSet<Uri> targetCollection, string staticAssetLabel)
        {
            foreach (var node in document.DocumentNode.SelectNodes(nodeQuery))
            {
                var attributeStringUri = CleanUri(node.Attributes[uriAttributeName].Value);

                if (attributeStringUri != null && attributeStringUri.Length > 0)
                {
                    var uri = new Uri(attributeStringUri);

                    if (!targetCollection.Contains(uri))
                    {
                        targetCollection.Add(uri);
                    }
                }
            }
        }
        private static string CleanUri(string input)
        {
            // Filter API requests :
            // =====================
            if (input.Contains("?"))
            {
                return null;
            }

            // Clean anchors :
            // ===============
            if (input.Contains("#"))
            {
                input = input.Split('#').First();
            }

            // If Uri is relative, add _host :
            // ===============================
            Uri relativeUri = null; 

            bool isRelative = Uri.TryCreate(input, UriKind.Relative, out relativeUri);

            if (isRelative)
            {
                input = $"{_scheme}://{_host}/{input.TrimStart('/')}";
            }

            // Trim / at the end of url :
            // ==========================
            input = input.TrimEnd('/');

            return input;
        }
    }
}
