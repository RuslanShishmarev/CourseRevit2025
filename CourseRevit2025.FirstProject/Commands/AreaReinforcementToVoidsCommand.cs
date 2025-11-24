using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using CourseRevit2025.FirstProject.Models.SelectFilters;
using CourseRevit2025.FirstProject.Services;

namespace CourseRevit2025.FirstProject.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal class AreaReinforcementToVoidsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
        Document doc = uidoc.Document;
        ElementService elementService = new(doc);

        var windowRef = uidoc.Selection.PickObject(ObjectType.Element, new SelectWindowsFilter());
        var window = doc.GetElement(windowRef) as FamilyInstance;

        if (window is null) return Result.Failed;

        var aRType = new FilteredElementCollector(doc)
            .OfClass(typeof(AreaReinforcementType))
            .First();

        var rBType = new FilteredElementCollector(doc)
            .OfClass(typeof(RebarBarType))
            .First();

        if (!CommonViewService.TryGetValueFromUser("Offset", "Input offset", out double offset))
        {
            return Result.Failed;
        }

        if (!CommonViewService.TryGetValueFromUser("Step", "Input rebar step", out double step))
        {
            return Result.Failed;
        }

        var curves = elementService.GetCurvesFromWindow(
            wnd: window,
            offsetHeight: offset / Constants.UNITS_CONVERT,
            offsetWidth: offset / Constants.UNITS_CONVERT);

        using var tr = new Transaction(doc, nameof(AreaReinforcementToVoidsCommand));
        tr.Start();

        var newAreaR = AreaReinforcement.Create(
            document: doc,
            hostElement: window.Host,
            curveArray: curves,
            majorDirection: XYZ.BasisZ,
            areaReinforcementTypeId: aRType.Id,
            rebarBarTypeId: rBType.Id,
            rebarHookTypeId: ElementId.InvalidElementId);

        newAreaR.get_Parameter(BuiltInParameter.REBAR_SYSTEM_SPACING_TOP_DIR_1_GENERIC).Set(step / Constants.UNITS_CONVERT);
        newAreaR.get_Parameter(BuiltInParameter.REBAR_SYSTEM_SPACING_BOTTOM_DIR_1_GENERIC).Set(step / Constants.UNITS_CONVERT);
        newAreaR.get_Parameter(BuiltInParameter.REBAR_SYSTEM_SPACING_TOP_DIR_2_GENERIC).Set(step / Constants.UNITS_CONVERT);
        newAreaR.get_Parameter(BuiltInParameter.REBAR_SYSTEM_SPACING_BOTTOM_DIR_2_GENERIC).Set(step / Constants.UNITS_CONVERT);

        tr.Commit();

        return Result.Succeeded;
    }
}
