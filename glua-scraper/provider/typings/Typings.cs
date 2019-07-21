using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using glua_scraper.provider.typings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace glua_scraper.provider
{

    public class TypingsProvider : IProvider
    {


        public string GetName()
        {
            return "Typings";
        }

        public void OnStart()
        {
            Directory.CreateDirectory(GetName());

            if (!Directory.Exists($"{GetName()}/gmod-typings"))
            {
                Directory.CreateDirectory($"{GetName()}/gmod-typings");
            }
        }

        public void OnFinish()
        {

        }


        private string BuildLibDefintion(Function func)
        {
            StringBuilder sb = new StringBuilder();
            JSDocBuilder jsDoc = new JSDocBuilder();

            jsDoc.AddArguments(func.Args);
            jsDoc.SetDescription(func.Description);

            string doc = jsDoc.Build();

            if (doc.Length > 0)
            {
                sb.AppendLine($"{doc}");
            }

            sb.AppendLine($"\tfunction {func.Name}({new Parameters(func.Args).Build()}): {new ReturnType(func.ReturnValues, true).Build(func.Description)};");

            return sb.ToString();
        }

        private string BuildClassDefinition(Function func)
        {
            StringBuilder sb = new StringBuilder();
            JSDocBuilder jsDoc = new JSDocBuilder();

            jsDoc.AddArguments(func.Args);
            jsDoc.SetDescription(func.Description);

            string doc = jsDoc.Build();

            if (doc.Length > 0)
            {
                sb.AppendLine($"{doc}");
            }

            sb.AppendLine($"\tfunction {func.Name}({new Parameters(func.Args).Build()}): {new ReturnType(func.ReturnValues, true).Build(func.Description)};");

            return sb.ToString();
        }

        private string BuildGlobalFunctionDefinition(Function func, bool indent = true, bool global = false)
        {
            StringBuilder sb = new StringBuilder();
            JSDocBuilder jsDoc = new JSDocBuilder();

            jsDoc.AddArguments(func.Args);
            jsDoc.SetDescription(func.Description);
            jsDoc.SetIndent(indent);

            string doc = jsDoc.Build();

            if (doc.Length > 0)
            {
                sb.AppendLine($"{doc}");
            }

            string prefix = indent ? "\t" : "";
            prefix += global ? "declare " : "";

            sb.AppendLine($"{prefix}function {func.Name}({new Parameters(func.Args).Build()}): {new ReturnType(func.ReturnValues).Build(func.Description)};");


            return sb.ToString();
        }

        private string BuildHookDefinition(Hook hook)
        {
            StringBuilder sb = new StringBuilder();
            JSDocBuilder jsDoc = new JSDocBuilder();

            jsDoc.AddArguments(hook.Args);
            jsDoc.SetDescription(hook.Description);

            string doc = jsDoc.Build();

            if (doc.Length > 0)
            {
                sb.AppendLine($"{doc}");
            }

            sb.AppendLine($"\tfunction {hook.Name}({new Parameters(hook.Args).Build()}): {new ReturnType(hook.ReturnValues, true).Build(hook.Description)};");

            return sb.ToString();
        }

        public void SaveHooks(Dictionary<string, List<Hook>> hooks)
        {
            Dictionary<string, List<StringBuilder>> hookGroups = new Dictionary<string, List<StringBuilder>>();

            foreach (string nameSpace in hooks.Keys)
            {
                hookGroups[nameSpace] = new List<StringBuilder>();

                foreach (Hook hook in hooks[nameSpace])
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(BuildHookDefinition(hook));

                    if (hook.Name != null && hook.Name.Length > 0)
                    {
                        hookGroups[nameSpace].Add(sb);
                    }
                }
            }

            foreach (string group in hookGroups.Keys)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"declare namespace {group} {{");

                foreach (StringBuilder hookDefinition in hookGroups[group])
                {
                    sb.AppendLine(hookDefinition.ToString());
                }

                sb.AppendLine("}");

                createTypingsFile(sb, group, "hooks");
            }
        }

        public void SaveLibFuncs(Dictionary<string, List<Function>> funcs)
        {
            Dictionary<string, List<StringBuilder>> libGroups = new Dictionary<string, List<StringBuilder>>();

            foreach (string nameSpace in funcs.Keys)
            {
                libGroups[nameSpace] = new List<StringBuilder>();

                foreach (Function func in funcs[nameSpace])
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(BuildLibDefintion(func));

                    if (func.Name != null && func.Name.Length > 0)
                    {
                        libGroups[nameSpace].Add(sb);
                    }
                }
            }

            foreach (string group in libGroups.Keys)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"declare namespace {group} {{");

                foreach (StringBuilder libDefinition in libGroups[group])
                {
                    sb.AppendLine(libDefinition.ToString());
                }

                sb.AppendLine("}");

                createTypingsFile(sb, group, "libs");
            }

        }

        public void SavePanelFuncs(Dictionary<string, List<Function>> panelFuncs)
        {
            SaveGenericClassFuncs(panelFuncs, "panels");
        }

        public void SaveClassFuncs(Dictionary<string, List<Function>> classFuncs)
        {
            SaveGenericClassFuncs(classFuncs, "classes");
        }

        public void SaveGenericClassFuncs(Dictionary<string, List<Function>> funcs, string fileName)
        {
            Dictionary<string, List<StringBuilder>> funcGroups = new Dictionary<string, List<StringBuilder>>();

            foreach (string nameSpace in funcs.Keys)
            {
                funcGroups[nameSpace] = new List<StringBuilder>();

                foreach (Function func in funcs[nameSpace])
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(BuildClassDefinition(func));

                    if (func.Name != null && func.Name.Length > 0)
                    {
                        funcGroups[nameSpace].Add(sb);
                    }
                }
            }

            foreach (string group in funcGroups.Keys)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"declare namespace {group} {{");

                foreach (StringBuilder classDefinition in funcGroups[group])
                {
                    sb.AppendLine(classDefinition.ToString());
                }

                sb.AppendLine("}");

                createTypingsFile(sb, group, fileName);
            }
        }
        public void SaveGlobals(Dictionary<string, List<Function>> globals)
        {

            StringBuilder sbGlobal = new StringBuilder();
            StringBuilder sb = new StringBuilder();

            foreach (string nameSpace in globals.Keys)
            {
                sb.Clear();
                sb.AppendLine("/** @noSelfInFile **/");
                sb.AppendLine("");
                sb.AppendLine($"declare namespace _G {{");

                sbGlobal.Clear();
                sbGlobal.AppendLine("/** @noSelfInFile **/");
                sbGlobal.AppendLine("");

                StringBuilder funcSb = new StringBuilder();

                foreach (Function func in globals[nameSpace])
                {
                    sb.AppendLine(BuildGlobalFunctionDefinition(func));
                    sb.AppendLine("");

                    sbGlobal.AppendLine(BuildGlobalFunctionDefinition(func, false, true));
                    sbGlobal.AppendLine("");
                }

                sb.AppendLine("}");

                // Save it as a namespace
                createTypingsFile(sb, "_g", "globals");

                // We do not create the global.d.ts file because some functions collide with lib.d.ts (Error)
                // createTypingsFile(sbGlobal, "global", "globals");
            }
        }

        private void createTypingsFile(StringBuilder sb, string fileName, string folderName)
        {
            if (!Directory.Exists($"{GetName()}/gmod-typings/{folderName}"))
            {
                Directory.CreateDirectory($"{GetName()}/gmod-typings/{folderName}");
            }

            File.WriteAllText($"{GetName()}/gmod-typings/{folderName}/{fileName.ToLower()}.d.ts", sb.ToString());
        }

        private class ReturnType
        {
            private List<Ret> _data;
            private bool _canHaveOptionalReturn = false;

            public ReturnType(List<Ret> data, bool optReturn = false)
            {
                _data = data;
                _canHaveOptionalReturn = optReturn;
            }

            public string Build(string desc = "")
            {
                if (_data == null || _data.Count == 0)
                {
                    return TSTypes.VOID;
                }
                else if (_data.Count == 1)
                {
                    string type = TypeMapper.MapType(_data[0].Type, desc);

                    if (hasOptionalReturn())
                    {
                        return $"{type} | {TSTypes.VOID}";
                    }
                    else
                    {
                        return type;
                    }
                }
                else if (_data.Count > 1)
                {
                    return $"[{String.Join(", ", _data.Select(r => TypeMapper.MapType(r.Type, desc)))}]";
                }
                else
                {
                    return TSTypes.UNKNOWN;
                }
            }

            private bool hasOptionalReturn()
            {
                return _canHaveOptionalReturn;
            }
        }

        private class Parameters
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
                    return String.Join(", ", _args.Select(a => new Parameter(a).Build()));
                }

            }

            private class Parameter
            {
                private Arg _arg;
                private static Dictionary<string, string> reservedWords = new Dictionary<string, string>() { { "class", "_class" }, { "default", "def" }, { "new", "_new" }, { "interface", "_interface" }, { "function", "func" }, { "var", "variable" }, { "let", "_let" },
                };

                public Parameter(Arg arg)
                {
                    _arg = arg;
                }

                public string Build()
                {
                    string arg = _arg.Name;

                    if (Parameter.reservedWords.ContainsKey(_arg.Name))
                    {
                        arg = Parameter.reservedWords[_arg.Name];
                    }

                    if (_arg.Default != null)
                    {
                        arg += '?';
                    }

                    string tsType = TypeMapper.MapType(_arg.Type);
                    arg += $": {tsType}";

                    return arg;
                }
            }
        }
    }
}