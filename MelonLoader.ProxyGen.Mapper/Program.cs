using PeNet;

namespace MelonLoader.ProxyGen.Mapper;

internal static class Program
{
    internal static void Main(string[] args)
    {
        if (args.Length <= 0)
        {
            Console.WriteLine("No File Path Given!");
            return;
        }

        // Check File Existance
        string filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"No File Exists at {filePath}");
            return;
        }

        // Read Module
        Console.WriteLine("Reading PE Image...");
        PeFile? peFile = null;
        try { peFile = new PeFile(filePath); } catch { peFile = null; }
        if (peFile == null)
        {
            Console.WriteLine($"Unable to Read PE Image of {filePath}");
            return;
        }

        // Check Exported Functions
        Console.WriteLine($"Reading Exported Functions from PE Image...");
        if (peFile.ExportedFunctions == null)
        {
            Console.WriteLine($"Unable to Read Exported Functions from PE Image of {filePath}");
            return;
        }
        if (peFile.ExportedFunctions.Length <= 0)
        {
            Console.WriteLine($"No Exported Functions found in PE Image of {filePath}");
            return;
        }

        // Get Valid Exported Functions
        List<string> foundExports = new List<string>();
        foreach (var export in peFile.ExportedFunctions)
            if ((export != null)
                && !string.IsNullOrEmpty(export.Name)
                && !string.IsNullOrWhiteSpace(export.Name))
                foundExports.Add(export.Name);
        if (foundExports.Count <= 0)
        {
            Console.WriteLine($"No Exported Functions found in PE Image of {filePath}");
            return;
        }

        // Create Directory
        string mapsFolder = "Maps";
        if (!Directory.Exists(mapsFolder))
            Directory.CreateDirectory(mapsFolder);

        // Find Existing Map
        string fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
        string mapFilePath = Path.Combine(mapsFolder, $"{fileNameNoExt}.txt");
        foreach (var foundFile in Directory.GetFiles(mapsFolder))
        {
            var foundFileNameNoExt = Path.GetFileNameWithoutExtension(foundFile);
            if (foundFileNameNoExt.Equals(fileNameNoExt, StringComparison.OrdinalIgnoreCase))
            {
                mapFilePath = foundFile;
                fileNameNoExt = foundFileNameNoExt;
                break;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"FileName: {fileNameNoExt}");
        Console.WriteLine($"FilePath: {filePath}");
        Console.WriteLine($"Map: {mapFilePath}");
        Console.WriteLine();
        Console.WriteLine($"{foundExports.Count} Exported Functions:");
        foreach (var export in foundExports)
            Console.WriteLine($"    - {export}");
        Console.WriteLine();

        // Write Exports to Map
        Console.WriteLine("Writing Exported Functions to Map...");
        File.AppendAllLines(mapFilePath, foundExports);

        // Regenerate Proxy
        Console.WriteLine("Processing Map Changes through ProxyGen...");
        ProxyGen.Program.Main();
    }
}
