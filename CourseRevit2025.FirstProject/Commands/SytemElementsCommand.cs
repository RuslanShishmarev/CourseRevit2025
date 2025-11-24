using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;

using CourseRevit2025.FirstProject.Services;
using CourseRevit2025.FirstProject.Services.Extensions;

namespace CourseRevit2025.FirstProject.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal class SytemElementsCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
        Document doc = uidoc.Document;

        ElementService elementService = new(doc);

        var ductLine = Line.CreateBound(XYZ.Zero, new XYZ(0, 3000 / Constants.UNITS_CONVERT, 0));

        if (!elementService.TryInvokeInnerTransaction(
            action: () => CreateDuct(
                doc: doc,
                level: doc.ActiveView.GenLevel,
                line: ductLine),
            trName: nameof(CreateDuct),
            out var ex1))
        {
            TaskDialog.Show("Error", ex1!.Message);
        }

        var pipeLine = ductLine.Offset(1000 / Constants.UNITS_CONVERT, XYZ.BasisX);
        if (!elementService.TryInvokeInnerTransaction(
            action: () => CreatePipe(
                doc: doc,
                level: doc.ActiveView.GenLevel,
                line: pipeLine),
            trName: nameof(CreatePipe),
            out var ex2))
        {
            TaskDialog.Show("Error", ex2!.Message);
        }

        return Result.Succeeded;
    }

    private Duct CreateDuct(
        Document doc,
        Level level,
        Line line)
    {
        var systemType = new FilteredElementCollector(doc)
            .OfClass(typeof(MechanicalSystemType))
            .FirstElementId();

        var ductType = new FilteredElementCollector(doc)
            .OfClass(typeof(DuctType))
            .FirstElementId();

        var duct = Duct.Create(
            document: doc,
            systemTypeId: systemType,
            ductTypeId: ductType,
            levelId: level.Id,
            startPoint: line.GetEndPoint(0),
            endPoint: line.GetEndPoint(1));

        return duct;
    }

    public Pipe CreatePipe(
        Document doc,
        Level level,
        Line line)
    {
        var systemType = new FilteredElementCollector(doc)
            .OfClass(typeof(PipingSystemType))
            .FirstElementId();

        var pipeType = new FilteredElementCollector(doc)
            .OfClass(typeof(PipeType))
            .FirstElementId();

        var pipe = Pipe.Create(
            document: doc,
            systemTypeId: systemType,
            pipeTypeId: pipeType,
            levelId: level.Id,
            startPoint: line.GetEndPoint(0),
            endPoint: line.GetEndPoint(1));

        return pipe;
    }
}
