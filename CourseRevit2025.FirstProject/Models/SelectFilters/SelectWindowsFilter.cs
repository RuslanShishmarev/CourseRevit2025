using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CourseRevit2025.FirstProject.Models.SelectFilters;

internal class SelectWindowsFilter : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        return elem?.Category?.BuiltInCategory == BuiltInCategory.OST_Windows;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }
}
