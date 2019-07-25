using System.Collections.Generic;

namespace glua_scraper
{
    public interface IMember
    {
        string Name { get; set; }
        string Description { get; set; }
        string DescriptionUrl { get; set; }
        string Realm { get; set; }
        List<Arg> Args { get; set; }
        List<Ret> ReturnValues { get; set; }
    }
}