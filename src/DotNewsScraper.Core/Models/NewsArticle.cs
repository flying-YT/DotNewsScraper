using System;

namespace DotNewsScraper.Core.Models
{
    public class NewsArticle
    {
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public string Url { get; set; } = "";
    }
}