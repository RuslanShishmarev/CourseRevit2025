using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using CourseRevit2025.FirstProject.View;
using CourseRevit2025.FirstProject.ViewModels;

namespace CourseRevit2025.FirstProject.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal class SearchByCategoryCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        SearchByCategoryView wnd = new();
        wnd.DataContext = new SearchByCategoryViewModel(commandData);
        wnd.ShowDialog();
        return Result.Succeeded;
    }
}
