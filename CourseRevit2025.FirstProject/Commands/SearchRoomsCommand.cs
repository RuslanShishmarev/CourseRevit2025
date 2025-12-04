using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using CourseRevit2025.FirstProject.Services;
using CourseRevit2025.FirstProject.View;
using CourseRevit2025.FirstProject.ViewModels;

namespace CourseRevit2025.FirstProject.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal class SearchRoomsCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        Document doc = commandData.Application.ActiveUIDocument.Document;
        ElementService elementService = new(commandData);
        string name = CommonViewService.GetValueFromUser("ROOMS", "Input search name");
        var searchFunc = () =>
        {
            var rooms = elementService.GetElementsByParameter(
                BuiltInCategory.OST_Rooms,
                BuiltInParameter.ROOM_NAME,
                name);
            return rooms;
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
