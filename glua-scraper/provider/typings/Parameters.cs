using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using glua_scraper.provider.typings;

namespace glua_scraper.provider
{
    public class Parameters
    {
        private List<Arg> _args;

        public Parameters(List<Arg> args)
        {
            _args = args;
        }

        public string Build()
        {
            if (_args == null || _args.Count == 0)
            {
                return "";
            }
            else
            {
                return String.Join(", ", _args.Select((a, index) => new Parameter(a, index).Build()));
            }

        }

        private class Parameter
        {
            private Arg _arg;
            private int _index;
            private static Dictionary<string, string> reservedWords = new Dictionary<string, string>() {
                { "class", "_class" },
                { "default", "def" },
                { "new", "_new" },
                { "interface", "_interface" },
                { "function", "func" },
                { "var", "variable" },
                { "let", "_let" },
                {"argn...", "...args"},
                {"...", "...args"}
            };

            public Parameter(Arg arg, int index)
            {
                _arg = arg;
                _index = index;
            }

            public string Build()
            {
                string arg = Sanitize(_arg.Name);

                // Sometimes we receive an argument without a name, just a type. So lets suggest something
                string tsType = TypeMapper.MapType(_arg.Type);
                if (arg.Trim().Length == 0)
                {
                    arg = GetSuggestedArgumentName(tsType);
                }

                // If we receive a default value, we can assume this is an optional parameter
                if (_arg.Default != null && !isRestParam(arg))
                {
                    arg += '?';
                }

                arg += $": {tsType}";

                return arg;
            }

            private bool isRestParam(string arg)
            {
                return arg == reservedWords["..."] || arg == reservedWords["argn..."];
            }

            private string Sanitize(string arg)
            {
                // Make sure our parameter name does not clash with a reserved word in JS
                if (Parameter.reservedWords.ContainsKey(_arg.Name))
                {
                    arg = Parameter.reservedWords[_arg.Name];
                }

                // Make sure there is no whitespace in the parameter
                arg = arg.Replace(' ', '_');

                // Also replace slashes
                arg = arg.Replace('/', '_');

                return arg;
            }

            private string GetSuggestedArgumentName(string tsType)
            {
                string baseName = "";

                switch (tsType)
                {
                    case TSTypes.ARRAY:
                        baseName = "arr";
                        break;

                    case TSTypes.BOOLEAN:
                        baseName = "bool";
                        break;

                    case TSTypes.FUNCTION:
                        baseName = "fn";
                        break;

                    case TSTypes.NUMBER:
                        baseName = "num";
                        break;

                    case TSTypes.OBJECT:
                        baseName = "obj";
                        break;

                    case TSTypes.STRING:
                        baseName = "str";
                        break;

                    case TSTypes.ANY:
                    case TSTypes.UNKNOWN:
                    default:
                        baseName = "arg";
                        break;
                }

                return $"{baseName}{_index}";
            }
        }
    }
}