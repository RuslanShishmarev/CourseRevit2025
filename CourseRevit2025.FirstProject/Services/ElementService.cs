using Autodesk.Revit.DB;

namespace CourseRevit2025.FirstProject.Services;

internal class ElementService
{
    private Document _doc;

    public ElementService(Document doc)
    {
        _doc = doc;
    }

    public IList<Element> GetElementsByParameter(
        BuiltInCategory category,
        BuiltInParameter parameter,
        string value,
        Autodesk.Revit.DB.View? view = null)
    {
        var provider = new ParameterValueProvider(new ElementId(parameter));
        var rule = new FilterStringRule(valueProvider: provider, new FilterStringEquals(), value);
        var filter = new ElementParameterFilter(rule, false);

        var elements = (view is null ? new FilteredElementCollector(_doc) : new FilteredElementCollector(_doc, view.Id))
            .OfCategory(category)
            .WherePasses(filter)
            .ToElements();

        return elements;
    }
}
