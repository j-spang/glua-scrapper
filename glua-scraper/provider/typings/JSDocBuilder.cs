using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace glua_scraper.provider.typings
{
    public class JSDocBuilder
    {
        private string _description;
        private List<Arg> _args = new List<Arg>();
        private bool _indent = true;

        public void SetDescription(string description)
        {
            _description = description;
        }

        public void AddArguments(List<Arg> args)
        {
            _args = args;
        }

        public void SetIndent(bool indent)
        {
            _indent = indent;
        }

        public string Build()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string prefix = _indent ? "\t" : "";

            if (_description != null && _description.Trim().Length > 0)
            {
                stringBuilder.AppendLine($"{prefix} * {_description}");
            }

            if (_args != null && _args.Count() > 0)
            {
                _args.ForEach(a =>
                {
                    if (a.Name != null && a.Name.Trim().Length > 0)
                    {
                        stringBuilder.AppendLine($"{prefix} * @param {{{TypeMapper.MapType(a.Type)}}} {{{a.Name}}}");
                    }
                });
            }

            if (stringBuilder.Length > 0)
            {
                stringBuilder.Insert(0, $"{prefix}/**\n");
                stringBuilder.Append($"{prefix} */");
                return stringBuilder.ToString();
            }
            else
            {
                return "";
            }
        }

        public void Reset()
        {
            _description = null;
            _args = new List<Arg>();
        }
    }
}