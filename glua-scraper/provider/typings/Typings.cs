using System.Collections.Generic;
using System.IO;
using System.Text;
using glua_scraper.provider.typings;

namespace glua_scraper.provider
{

    public class TypingsProvider : IProvider
    {
        private static Dictionary<string, List<string>> hookAliases = new Dictionary<string, List<string>>(){
            {
                "GM", new List<string>{"GAMEMODE"}
            },
            {
                "WEAPON", new List<string>{"SWEP"}
            },
        };

        private static Dictionary<string, string> classToHookMapper = new Dictionary<string, string>(){
                {"Panel", "PANEL"}
        };

        private static Dictionary<string, List<StringBuilder>> classFuncGroups = new Dictionary<string, List<StringBuilder>>();

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
            if (classFuncGroups.Count > 0)
            {
                SaveGenericClassFuncFiles();
            }
        }


        private string BuildLibDefintion(Function func)
        {
            return BuildDefinition(func);
        }

        private string BuildDefinition(IMember func, bool canHaveOptionalReturn = false, bool isClassDefinition = false)
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

            ReturnType returns = new ReturnType(func.ReturnValues, func.Name, canHaveOptionalReturn);
            if (returns.IsTupleReturn())
            {
                sb.AppendLine("\t/** @tupleReturn */");
            }

            string prefix = !isClassDefinition ? "function " : "";

            sb.AppendLine($"\t{prefix}{func.Name}({new Parameters(func.Args).Build()}): {returns.Build(func.Description)};");

            return sb.ToString();
        }

        private string BuildClassDefinition(Function func)
        {
            return BuildDefinition(func, false, true);
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

            sb.AppendLine($"{prefix}function {func.Name}({new Parameters(func.Args).Build()}): {new ReturnType(func.ReturnValues, func.Name).Build(func.Description)};");

            return sb.ToString();
        }

        private string BuildHookDefinition(Hook hook, bool isModule)
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

            string declaration = isModule ? "function " : "";
            sb.AppendLine($"\t{declaration}{hook.Name}({new Parameters(hook.Args).Build()}): {new ReturnType(hook.ReturnValues, hook.Name, true).Build(hook.Description)};");

            return sb.ToString();
        }

        public void SaveHooks(Dictionary<string, List<Hook>> hooks)
        {
            Dictionary<string, List<StringBuilder>> hookGroups = new Dictionary<string, List<StringBuilder>>();

            foreach (string nameSpace in hooks.Keys)
            {
                bool isModule = !classToHookMapper.ContainsValue(nameSpace);

                hookGroups[nameSpace] = new List<StringBuilder>();

                foreach (Hook hook in hooks[nameSpace])
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(BuildHookDefinition(hook, isModule));

                    if (hook.Name != null && hook.Name.Length > 0)
                    {
                        hookGroups[nameSpace].Add(sb);
                    }
                }
            }

            foreach (string group in hookGroups.Keys)
            {
                StringBuilder sb = new StringBuilder();

                List<string> aliases = GetHookGroupAliases(group);

                foreach (string alias in aliases)
                {
                    bool isModule = !classToHookMapper.ContainsValue(alias);

                    if (isModule)
                    {
                        sb.AppendLine($"declare module {alias} {{");
                    }
                    else
                    {
                        sb.AppendLine("// We declare this hook as a class so another class can 'extend' it");
                        sb.AppendLine("");
                        sb.AppendLine($"declare class {alias} {{");
                    }

                    foreach (StringBuilder hookDefinition in hookGroups[group])
                    {
                        sb.AppendLine(hookDefinition.ToString());
                    }

                    sb.AppendLine("}");
                    sb.AppendLine("");
                }

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

                sb.AppendLine("/** @noSelfInFile **/");
                sb.AppendLine("");
                sb.AppendLine($"declare module {group} {{");

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
            PrepareGenericClassFuncFiles(panelFuncs, "panels");
        }

        public void SaveClassFuncs(Dictionary<string, List<Function>> classFuncs)
        {
            PrepareGenericClassFuncFiles(classFuncs, "classes");
        }

        private void PrepareGenericClassFuncFiles(Dictionary<string, List<Function>> funcs, string fileName)
        {

            // We run through all the classes once to register them as TypeScript class type references
            // This way, a method from class B can have a return type of class B
            foreach (string nameSpace in funcs.Keys)
            {
                if (!classFuncGroups.ContainsKey(nameSpace))
                {
                    classFuncGroups[nameSpace] = new List<StringBuilder>();
                    TypeMapper.registerType(nameSpace, nameSpace);
                }
            }

            foreach (string nameSpace in funcs.Keys)
            {
                foreach (Function func in funcs[nameSpace])
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(BuildClassDefinition(func));

                    if (func.Name != null && func.Name.Length > 0)
                    {
                        classFuncGroups[nameSpace].Add(sb);
                    }
                }
            }
        }

        private void SaveGenericClassFuncFiles()
        {
            foreach (string group in classFuncGroups.Keys)
            {
                StringBuilder sb = new StringBuilder();

                string classDeclaration = $"declare class {group}";

                if (classToHookMapper.ContainsKey(group))
                {
                    classDeclaration += $" extends {classToHookMapper[group]}";
                }

                sb.AppendLine($"{classDeclaration} {{");

                foreach (StringBuilder classDefinition in classFuncGroups[group])
                {
                    sb.AppendLine(classDefinition.ToString());
                }

                sb.AppendLine("}");

                createTypingsFile(sb, group, "classes");
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
                sb.AppendLine($"declare module _G {{");

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

        private static List<string> GetHookGroupAliases(string hookGroup)
        {
            List<string> aliases = new List<string> { hookGroup };

            if (hookAliases.ContainsKey(hookGroup))
            {
                aliases.AddRange(hookAliases[hookGroup]);
            }

            return aliases;
        }
    }
}