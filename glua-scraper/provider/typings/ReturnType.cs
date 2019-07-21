using System;
using System.Collections.Generic;
using System.Linq;
using glua_scraper.provider.typings;

namespace glua_scraper.provider
{
    public class ReturnType
    {
        private List<Ret> _returnData;
        private string _functionName;
        private bool _canHaveOptionalReturn = false;

        public ReturnType(List<Ret> returnData, string fnName, bool optReturn = false)
        {
            _returnData = returnData;
            _functionName = fnName;
            _canHaveOptionalReturn = optReturn;
        }

        public string Build(string desc = "")
        {
            if (_returnData == null || _returnData.Count == 0)
            {
                return TSTypes.VOID;
            }
            else if (_returnData.Count == 1)
            {
                string type = TypeMapper.MapType(_returnData[0].Type, desc);

                if (hasOptionalReturn())
                {
                    return $"{type} | {TSTypes.VOID}";
                }
                else
                {
                    return type;
                }
            }
            else if (_returnData.Count > 1)
            {
                return $"[{String.Join(", ", _returnData.Select(r => TypeMapper.MapType(r.Type, desc)))}]";
            }
            else
            {
                return TSTypes.UNKNOWN;
            }
        }

        private bool hasOptionalReturn()
        {
            return _canHaveOptionalReturn && !isGetter();
        }

        private bool isGetter()
        {
            return _functionName.StartsWith("Has") || _functionName.StartsWith("Is") || _functionName.StartsWith("Get") || _functionName.StartsWith("Set");
        }
    }
}