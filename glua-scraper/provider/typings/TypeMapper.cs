using System;
using System.Collections.Generic;

namespace glua_scraper.provider.typings
{
    public class TypeMapper
    {
        private static Dictionary<string, string> _classes = new Dictionary<string, string>();

        public static string MapType(string type, string description = "")
        {
            if (_classes.ContainsKey(type))
            {
                return _classes[type];
            }

            switch (type.Trim().ToLower())
            {
                case "string":
                    return TSTypes.STRING;

                case "table":
                    return TSTypes.OBJECT;

                case "number":
                    return TSTypes.NUMBER;

                case "boolean":
                    return TSTypes.BOOLEAN;

                default:
                    //Console.WriteLine($"Unknown type {type.Trim().ToLower()} encountered! ({description})");
                    return TSTypes.ANY;
            }
        }

        public static void registerType(string luaType, string tsType)
        {
            if (!_classes.ContainsKey(luaType))
            {
                _classes[luaType] = tsType;
            }
        }
    }
}