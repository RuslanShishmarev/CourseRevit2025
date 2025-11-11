using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CourseRevit2025.FirstProject.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal class PlaceElementPromtCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;

        string testName = "36\" Diameter";
        FamilySymbol? testSymbol = new FilteredElementCollector(uidoc.Document)
            .OfCategory(BuiltInCategory.OST_Furniture)
            .WhereElementIsElementType()
            .FirstOrDefault(x =>  x.Name == testName) as FamilySymbol;

        if (testSymbol == null)
        {
            TaskDialog.Show("NotFoundError", $"Symblol \'{testName}\' is not found");
            return Result.Failed;
        }

        List<Element> newElements = [];

        commandData.Application.Application.DocumentChanged += (s, e) =>
        {
            var newElementsIds = e.GetAddedElementIds();
            foreach (var elId in newElementsIds)
            {
                Element element = uidoc.Document.GetElement(elId);
                if (element.Name == testName)
                {
                    newElements.Add(element);
                }
            }
        };

        try
        {
            uidoc.PromptForFamilyInstancePlacement(testSymbol);
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
        {
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", ex.Message);
            return Result.Failed;
        }

        using var tr = new Transaction(uidoc.Document, "Set tag");
        tr.Start();
        foreach (var newEl in newElements)
        {
            newEl.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set("My tag!");
        }
        tr.Commit();
        return Result.Succeeded;
    }
}
