using System;

namespace glua_scraper
{
    [Serializable]
    public class Ret
    {
        public string Type { get; set; }

        public Ret(string raw)
        {
            Type = WikiArticle.GetValue(raw, "type");
        }
    }
}