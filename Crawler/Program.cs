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

            _sb = new StringBuilder();
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
                        _sb.AppendLine($"{staticAssetLabel} : {attributeStringUri}");
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
