# Crawler
Simple Web Crawler written in C#

# How to build and run the solution
Simply open the solution in Visual Studio and run it ! It has only one Nuget dependency that will be automatically
restored during first build...

# About the project
This simple web crawler relies on Nuget package HtmlAgilityPack which is very helpful to read and parse HTML document,
especially to extract urls and dig further recursively into a http domain. The analysis of a domain is then processed
page by page... The crawler does not support assets served by APIs and thus is not recommended to crawl an SPA...

# Improvements
There could be many improvements to bring to this crawler. Here are a few of them :
-Process multiple pages at a time using concurrency
-Handle static assets or html document served by an API ? Difficult without knwowing the API first...
-Improve output and draw a graph that show relationships between the pages

# Final note
Thanks ! That was very fun to code !
