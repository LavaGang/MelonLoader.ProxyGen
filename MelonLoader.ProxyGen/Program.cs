using System.Runtime.CompilerServices;
using System.Text;
[assembly: InternalsVisibleTo("MelonLoader.ProxyGen.Mapper")]

namespace MelonLoader.ProxyGen;

internal static class Program
{
    private const string MELONLOADER_SECTION = "MelonLoader";
    private const string SHARED_SECTION = "Shared";

    internal static void Main()
    {
        // Create New Cache
        Dictionary<string, List<string>?> moduleExports = new();

        // Create Directories
        string mapsFolder = "Maps";
        if (!Directory.Exists(mapsFolder))
            Directory.CreateDirectory(mapsFolder);

        string outputPath = "Output";
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        string outputProxyPath = Path.Combine(outputPath, "Proxy");
        if (!Directory.Exists(outputProxyPath))
            Directory.CreateDirectory(outputProxyPath);

        string outputExportPath = Path.Combine(outputProxyPath, "Exports");
        if (!Directory.Exists(outputExportPath))
            Directory.CreateDirectory(outputExportPath);

        // Read All Map Files
        Console.WriteLine("Reading Export Maps...");
        ReadMapFiles(ref moduleExports, mapsFolder);
        Console.WriteLine();
        Console.WriteLine($"{moduleExports.Count} Export Maps:");
        foreach (var modulePair in moduleExports)
        {
            if (modulePair.Value == null)
                continue;
            bool hasExports = (modulePair.Value.Count > 0);
            Console.WriteLine($"    - {modulePair.Key} ({modulePair.Value.Count}){(hasExports ? ":" : string.Empty)}");
            if (hasExports)
                foreach (var export in modulePair.Value)
                    Console.WriteLine($"        - {export}");
        }
        Console.WriteLine();

        // Write Exports.def
        Console.WriteLine("Writing Output/Exports.def");
        WriteDefinitionFile(moduleExports, Path.Combine(outputPath, "Exports.def"));

        // Write ProxyMap.cs
        Console.WriteLine("Writing Output/Proxy/ProxyMap.cs");
        WriteProxyMap(moduleExports, Path.Combine(outputProxyPath, "ProxyMap.cs"));

        // Write .cs Wrappers
        Console.WriteLine();
        WriteWrapperFiles(moduleExports, outputExportPath);

        // Apply New Changes
        Console.WriteLine();
        ApplyNewMapChanges(moduleExports, mapsFolder);
    }

    private static void ApplyNewMapChanges(Dictionary<string, List<string>?> moduleExports, string outputPath)
    {
        foreach (var modulePair in moduleExports)
        {
            // Get Section
            string moduleSection = modulePair.Key;

            // Write Exports
            StringBuilder defBuilder = new StringBuilder();
            List<string>? currentExports = modulePair.Value;
            if (currentExports != null)
                foreach (var export in currentExports)
                    defBuilder.AppendLine(export);

            // Save to File
            string mapFile = $"{moduleSection}.txt";
            Console.WriteLine($"Writing Maps/{mapFile}");
            File.WriteAllText(Path.Combine(outputPath, mapFile), defBuilder.ToString());
        }
    }

    private static void WriteDefinitionFile(Dictionary<string, List<string>?> moduleExports, string outputPath)
    {
        // Create String Builder
        StringBuilder defBuilder = new StringBuilder();

        // Write Header
        defBuilder.AppendLine("EXPORTS");
        defBuilder.AppendLine();

        // Write MelonLoader Section
        if (moduleExports.ContainsKey(MELONLOADER_SECTION))
        {
            defBuilder.AppendLine($"; {MELONLOADER_SECTION}");
            List<string>? currentExports = moduleExports[MELONLOADER_SECTION];
            if (currentExports != null)
                foreach (var export in currentExports)
                    defBuilder.AppendLine($"    {export}");
            defBuilder.AppendLine();
        }

        // Write Shared Section
        if (moduleExports.ContainsKey(SHARED_SECTION))
        {
            defBuilder.AppendLine($"; {SHARED_SECTION}");
            List<string>? currentExports = moduleExports[SHARED_SECTION];
            if (currentExports != null)
                foreach (var export in currentExports)
                    defBuilder.AppendLine($"    {export}=Impl{export}");
            defBuilder.AppendLine();
        }

        // Write Module Sections
        foreach (var modulePair in moduleExports)
        {
            string moduleSection = modulePair.Key;
            if ((moduleSection == MELONLOADER_SECTION)
                || (moduleSection == SHARED_SECTION))
                continue;

            defBuilder.AppendLine($"; {moduleSection}");
            List<string>? currentExports = modulePair.Value;
            if (currentExports != null)
                foreach (var export in currentExports)
                    defBuilder.AppendLine($"    {export}=Impl{export}");
            defBuilder.AppendLine();
        }

        // Save to File
        File.WriteAllText(outputPath, defBuilder.ToString());
    }

    private static void ReadMapFiles(ref Dictionary<string, List<string>?> moduleExports, string mapsFolder)
    {
        // Read All Map Files
        Dictionary<string, string> allExports = new();
        foreach (var mapFile in Directory.GetFiles(mapsFolder))
        {
            // Read Map Info
            string mapModule = Path.GetFileNameWithoutExtension(mapFile);
            string[] mapFileExports = File.ReadAllLines(mapFile);

            // Create Initial Module Listing
            if (!moduleExports.TryGetValue(mapModule, out List<string>? moduleExportList))
                moduleExportList = moduleExports[mapModule] = new();

            // Iterate through all Exports in Map File
            foreach (var mapExport in mapFileExports)
            {
                if (string.IsNullOrEmpty(mapExport)
                    || string.IsNullOrWhiteSpace(mapExport))
                    continue;

                // Check if Export is Already Defined
                if (allExports.ContainsKey(mapExport))
                {
                    // Get Original Section
                    string originalCategory = allExports[mapExport];

                    // Skip if Duplicate of Same Section
                    if (originalCategory == mapModule)
                        continue;

                    // Skip if Duplicate of MelonLoader Section
                    if (originalCategory == MELONLOADER_SECTION)
                        continue;

                    // Remove Original Listing
                    if ((originalCategory != SHARED_SECTION)
                        && moduleExports.TryGetValue(originalCategory, out List<string>? originalExportList)
                        && (originalExportList?.Contains(mapExport) ?? false))
                        originalExportList.Remove(mapExport);

                    // Add Listing to Shared Section
                    if (!moduleExports.TryGetValue(SHARED_SECTION, out List<string>? sharedExportList))
                        sharedExportList = moduleExports[SHARED_SECTION] = new();
                    if ((sharedExportList != null)
                        && !sharedExportList.Contains(mapExport))
                        sharedExportList?.Add(mapExport);
                    allExports[mapExport] = SHARED_SECTION;
                }
                else
                {
                    // Add Listing to Module Section
                    moduleExportList?.Add(mapExport);
                    allExports[mapExport] = mapModule;
                }
            }
        }
    }

    private static void WriteWrapperFiles(Dictionary<string, List<string>?> moduleExports, string outputPath)
    {
        // Write Module Sections
        foreach (var modulePair in moduleExports)
        {
            string moduleSection = modulePair.Key;
            if (moduleSection == MELONLOADER_SECTION)
                continue;

            // Create String Builder
            StringBuilder defBuilder = new StringBuilder();

            // Write Header
            defBuilder.AppendLine("#if WINDOWS");
            defBuilder.AppendLine("using System.Runtime.InteropServices;");
            defBuilder.AppendLine();
            defBuilder.AppendLine("namespace MelonLoader.Bootstrap.Proxy.Exports;");
            defBuilder.AppendLine();
            defBuilder.AppendLine($"internal static class {moduleSection}Exports");
            defBuilder.AppendLine("{");

            // Write Exports
            List<string>? currentExports = modulePair.Value;
            if (currentExports != null)
                foreach (var export in currentExports)
                {
                    defBuilder.AppendLine($"    [UnmanagedCallersOnly(EntryPoint = \"Impl{export}\")]");
                    defBuilder.AppendLine($"    public static void Impl{export}() {"{ }"}");
                }

            // Write Footer
            defBuilder.AppendLine("}");
            defBuilder.AppendLine("#endif");

            // Save to File
            string csFileName = $"{moduleSection}Exports.cs";
            Console.WriteLine($"Writing Output/Proxy/Exports/{csFileName}");
            File.WriteAllText(Path.Combine(outputPath, csFileName), defBuilder.ToString());
        }
    }

    private static void WriteProxyMap(Dictionary<string, List<string>?> moduleExports, string outputPath)
    {
        // Create String Builder
        StringBuilder defBuilder = new StringBuilder();

        // Write Header
        defBuilder.AppendLine("#if WINDOWS");
        defBuilder.AppendLine("using MelonLoader.Bootstrap.Proxy.Exports;");
        defBuilder.AppendLine("using System.Diagnostics.CodeAnalysis;");
        defBuilder.AppendLine();
        defBuilder.AppendLine("namespace MelonLoader.Bootstrap.Proxy;");
        defBuilder.AppendLine();
        defBuilder.AppendLine("internal static class ProxyMap");
        defBuilder.AppendLine("{");
        defBuilder.AppendLine("    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]");
        defBuilder.AppendLine("    internal static readonly Type sharedProxy = typeof(SharedExports);");
        defBuilder.AppendLine();
        defBuilder.AppendLine("    internal class Proxy");
        defBuilder.AppendLine("    {");
        defBuilder.AppendLine("        public required string FileName { get; init; }");
        defBuilder.AppendLine();
        defBuilder.AppendLine("        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]");
        defBuilder.AppendLine("        public required Type ProxyFuncs { get; init; }");
        defBuilder.AppendLine("    }");
        defBuilder.AppendLine();
        defBuilder.AppendLine("    internal static readonly Proxy[] proxies =");
        defBuilder.AppendLine("    [");

        // Write Module Sections
        foreach (var modulePair in moduleExports)
        {
            string moduleSection = modulePair.Key;
            if ((moduleSection == MELONLOADER_SECTION)
                || (moduleSection == SHARED_SECTION))
                continue;

            defBuilder.AppendLine("        new()");
            defBuilder.AppendLine("        {");
            defBuilder.AppendLine($"            FileName = \"{moduleSection.ToLower()}\",");
            defBuilder.AppendLine($"            ProxyFuncs = typeof({moduleSection}Exports),");
            defBuilder.AppendLine("        },");
        }

        // Write Footer
        defBuilder.AppendLine("    ];");
        defBuilder.AppendLine("}");
        defBuilder.AppendLine("#endif");

        // Save to File
        File.WriteAllText(outputPath, defBuilder.ToString());
    }
}
