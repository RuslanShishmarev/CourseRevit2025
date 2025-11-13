using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using CourseRevit2025.FirstProject.Services;
using CourseRevit2025.FirstProject.View;
using CourseRevit2025.FirstProject.ViewModels;

namespace CourseRevit2025.FirstProject.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal class SearchWallsCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        Document doc = commandData.Application.ActiveUIDocument.Document;
        ElementService elementService = new(doc);
        string heightStr = CommonViewService.GetValueFromUser("WALLS", "Input height");

        if(!double.TryParse(heightStr, out var height))
        {
            TaskDialog.Show("Error", "Height should be double");
            return Result.Cancelled;
        }

        var searchFunc = () =>
        {
            var walls = elementService.GetElementsByParameterGreater(
                BuiltInCategory.OST_Walls,
                BuiltInParameter.WALL_USER_HEIGHT_PARAM,
                height / Constants.UNITS_CONVERT);
            return walls;
        };
        var searchElementsViewModel = new SearchElementsViewModel(commandData, searchFunc);

        var view = new SearchElementsWindow
        {
            DataContext = searchElementsViewModel
        };

        view.ShowDialog();

        return Result.Succeeded;
    }
}
