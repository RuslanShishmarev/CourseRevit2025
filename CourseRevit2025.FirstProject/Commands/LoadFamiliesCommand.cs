using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CourseRevit2025.FamilyLoader;

namespace CourseRevit2025.FirstProject.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal class LoadFamiliesCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
        Document doc = uidoc.Document;
        FamilyService familyService = new(doc);

        using var tr = new Transaction(doc, "Load families");
        tr.Start();
        var allFamilies = familyService.LoadFamilies(out var rfaFiles);
        tr.Commit();

        if (allFamilies != null && allFamilies.Count > 0)
        {
            familyService.RemoveFamiliesFromFolder(rfaFiles);
        }

        return Result.Succeeded;
    }
}


