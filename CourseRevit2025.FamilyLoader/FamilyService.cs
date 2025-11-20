using Autodesk.Revit.DB;

using CourseRevit2025.FamilyLoader.Models;

using System.Reflection;

namespace CourseRevit2025.FamilyLoader;

public class FamilyService
{
    private Document _doc;
    private Assembly _currentAssembly;
    private IEnumerable<string> _resourcesRfas;
    public FamilyService(Document doc)
    {
        _doc = doc;
        _currentAssembly = Assembly.GetExecutingAssembly();
        var resourcesNames = _currentAssembly.GetManifestResourceNames();
        _resourcesRfas = resourcesNames.Where(IsRfa);
    }

    public List<Family> LoadFamilies(out List<string> rfaFiles)
    {
        var allFamilies = new List<Family>();
        rfaFiles = GetFamilies();
        foreach (var rfaPath in rfaFiles)
        {
            _doc.LoadFamily(rfaPath, new FamilyOption(), out var family);
            allFamilies.Add(family);
        }

        return allFamilies;
    }

    public void RemoveFamiliesFromFolder(List<string> rfaFiles)
    {
        foreach (var file in rfaFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch { }
        }
    }

    private List<string> GetFamilies()
    {
        var tempFolder = Path.GetTempPath();

        var result = new List<string>();
        foreach (var rfaRes in _resourcesRfas)
        {
            string familyName = GetRfaName(rfaRes, _currentAssembly.GetName().Name!);
            string familyFileName = Path.Combine(tempFolder, familyName);

            using Stream input = _currentAssembly.GetManifestResourceStream(rfaRes);
            using var output = File.Create(familyFileName);
            try
            {
                CopyStream(input, output);
                result.Add(familyFileName);
            }
            finally { output.Close(); }
            input.Close();
        }

        return result;
    }

    private void CopyStream(Stream source, Stream destination)
    {
        var buffer = new byte[8192];

        int bytesRead;
        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            destination.Write(buffer, 0, bytesRead);
        }
    }

    private bool IsRfa(string resource)
    {
        return resource.EndsWith(".rfa");
    }

    private string GetRfaName(string resource, string assemlyName)
    {
        return resource.Replace($"{assemlyName}.Families.", string.Empty);
    }
}
