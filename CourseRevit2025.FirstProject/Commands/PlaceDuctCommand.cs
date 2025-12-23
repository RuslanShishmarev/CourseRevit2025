using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;

using CourseRevit2025.FirstProject.Services;

namespace CourseRevit2025.FirstProject.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal class PlaceDuctCommand : IExternalCommand
{
    private const string PLAN_PARAMETER_NAME = "Plugin_ViewPlan";
    private const string VIEW3D_PARAMETER_NAME = "Plugin_View3D";

    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
        Document doc = uidoc.Document;

        ElementService elementService = new(commandData);
        ViewPlanService viewPlanService = new(doc);
        // get all rooms inner active view (view must be plan)

        var rooms = elementService.GetElements(
            category: BuiltInCategory.OST_Rooms,
            view: doc.ActiveView).Cast<Room>();

        // get symbol duct
        var allDuctSymbols = elementService.GetTypes(BuiltInCategory.OST_DuctCurves).Cast<DuctType>();
        if (!CommonViewService.GetValueFromElements(allDuctSymbols, "Name", out DuctType selectedSymbol))
        {
            return Result.Cancelled;
        }

        var systemTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(MechanicalSystemType))
            .Cast<MechanicalSystemType>()
            .ToArray();

        if (!CommonViewService.GetValueFromElements(systemTypes, "Name", out MechanicalSystemType selectedMEP))
        {
            return Result.Cancelled;
        }

        if (!CommonViewService.TryGetValueFromUser(
            title: "Duct height",
            text: "Set duct offset by Z",
            out double heightOffset,
            defaultV: 2000))
        {
            return Result.Cancelled;
        }

        var ductPointType = elementService.GetTypes(
            category: BuiltInCategory.OST_GenericModel,
            name: "BasePointDuct")
            .First() as FamilySymbol;

        if (ductPointType is null)
        {
            return Result.Cancelled;
        }

        using TransactionGroup transactionGroup = new(
            doc,
            transGroupName: $"Group {nameof(PlaceDuctCommand)}");

        transactionGroup.Start();

        var ductPoints = elementService.PlaceElementByType(ductPointType, 2);
        if (ductPoints.Count() == 0)
        {
            return Result.Cancelled;
        }
        XYZ start = (ductPoints.First().Location as LocationPoint)!.Point + 
            XYZ.BasisZ * heightOffset / Constants.UNITS_CONVERT;
        XYZ end = (ductPoints.Last().Location as LocationPoint)!.Point +
            XYZ.BasisZ * heightOffset / Constants.UNITS_CONVERT;

        var level = elementService.GetLevels().First();
        // place main duct
        using Transaction tr = new(document: doc, name: nameof(PlaceDuctCommand));
        tr.Start();

        var mainDuct = Duct.Create(
            document: doc,
            systemTypeId: selectedMEP!.Id,
            ductTypeId: selectedSymbol!.Id,
            levelId: level.Id,
            startPoint: start,
            endPoint: end);

        var allNewElements = new List<Element>() { mainDuct };

        var mainLine = (mainDuct.Location as LocationCurve)!.Curve;
        var mainCenter = mainLine.Evaluate(0.5, true);

        // place duct for rooms
        foreach (Room room in rooms)
        {
            if (room.IsPointInRoom(mainCenter)) 
                continue;

            // get center of each room and create line from center to main
            var bb = room.get_BoundingBox(doc.ActiveView);
            XYZ roomCenterFact = (bb.Min + bb.Max) / 2;

            XYZ roomCenter = new(roomCenterFact.X, roomCenterFact.Y, mainCenter.Z);
            var connectPoint = mainLine.Project(roomCenter).XYZPoint;

            var newDuct = Duct.Create(
                document: doc,
                systemTypeId: selectedMEP.Id,
                ductTypeId: selectedSymbol.Id,
                levelId: level.Id,
                startPoint: connectPoint,
                endPoint: roomCenter);

            allNewElements.Add(newDuct);

            Connector connector = newDuct.ConnectorManager.Connectors.Cast<Connector>()
                .First(x => x.Origin.IsAlmostEqualTo(connectPoint));

            var newFitting = doc.Create.NewTakeoffFitting(connector, mainDuct);

            allNewElements.Add(newFitting);
        }

        var newPlan = viewPlanService.CreateViewPlan(
            name: $"New plan {Guid.NewGuid()}",
            elements: allNewElements,
            filterParameterName: PLAN_PARAMETER_NAME);

        doc.Regenerate();
        try
        {
            viewPlanService.CreateDimensions(newPlan, allNewElements);
        }
        catch (Exception ex)
        {
        }

        var new3D = viewPlanService.CreateView3D(
            name: $"New 3d {Guid.NewGuid()}",
            elements: allNewElements,
            filterParameterName: VIEW3D_PARAMETER_NAME);

        doc.Delete([.. ductPoints.Select(x => x.Id)]);

        tr.Commit();

        transactionGroup.Assimilate();

        uidoc.ActiveView = newPlan;

        return Result.Succeeded;
    }
}
