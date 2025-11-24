using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using CourseRevit2025.FirstProject.Models.SelectFilters;

namespace CourseRevit2025.FirstProject.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal class AreaReinforcementCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
        Document doc = uidoc.Document;

        var wallRef = uidoc.Selection.PickObject(ObjectType.Element, new SelectWallsFilter());
        var wall = doc.GetElement(wallRef);

        if (wall is null) return Result.Failed;

        var aRType = new FilteredElementCollector(doc)
            .OfClass(typeof(AreaReinforcementType))
            .First();

        var rBType = new FilteredElementCollector(doc)
            .OfClass(typeof(RebarBarType))
            .First();

        using var tr = new Transaction(doc, nameof(AreaReinforcementCommand));
        tr.Start();

        AreaReinforcement.Create(
            document: doc,
            hostElement: wall,
            majorDirection: XYZ.BasisZ,
            areaReinforcementTypeId: aRType.Id,
            rebarBarTypeId: rBType.Id,
            rebarHookTypeId: ElementId.InvalidElementId);

        tr.Commit();

        return Result.Succeeded;
    }
}
