using Autodesk.Revit.DB;

namespace CourseRevit2025.FirstProject.Services;

internal class ElementService
{
    private Document _doc;

    public ElementService(Document doc)
    {
        _doc = doc;
    }

    public IList<Level> GetLevels()
    {
        return new FilteredElementCollector(_doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToList();
    }

    public bool TryInvokeInnerTransaction(
        Action action,
        string trName,
        out Exception? innerExeption)
    {
        innerExeption = null;

        using var tr = new Transaction(_doc, trName);
        tr.Start();

        try
        {
            action();
            tr.Commit();
            return true;
        }
        catch (Exception ex)
        {
            tr.RollBack();
            innerExeption = ex;
            return false;
        }
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
            .WhereElementIsNotElementType()
            .WherePasses(filter)
            .ToElements();

        return elements;
    }

    public IList<Element> GetElementsByParameterGreater(
        BuiltInCategory category,
        BuiltInParameter parameter,
        double value,
        Autodesk.Revit.DB.View? view = null)
    {
        double mmTolerance = 10 / Constants.UNITS_CONVERT;

        var provider = new ParameterValueProvider(new ElementId(parameter));
        var rule = new FilterDoubleRule(provider, new FilterNumericGreater(), value, mmTolerance);
        var filter = new ElementParameterFilter(rule, false);

        var elements = (view is null ? new FilteredElementCollector(_doc) : new FilteredElementCollector(_doc, view.Id))
            .OfCategory(category)
            .WhereElementIsNotElementType()
            .WherePasses(filter)
            .ToElements();

        return elements;
    }

    public IList<Curve> GetCurvesFromWindow(
        FamilyInstance wnd,
        double offsetWidth,
        double offsetHeight)
    {
        (double width, double height, XYZ center) = GetWidthHeight(wnd);

        var hostLoc = ((wnd.Host as Wall).Location as LocationCurve).Curve as Line;

        var widthPoint1 = center + hostLoc.Direction * (width / 2 + offsetWidth);
        var pointTop1 = widthPoint1 + XYZ.BasisZ * (height / 2 + offsetHeight);
        var pointBottom1 = widthPoint1 - XYZ.BasisZ * (height / 2 + offsetHeight);

        var widthPoint2 = center - hostLoc.Direction * (width / 2 + offsetWidth);
        var pointTop2 = widthPoint2 + XYZ.BasisZ * (height / 2 + offsetHeight);
        var pointBottom2 = widthPoint2 - XYZ.BasisZ * (height / 2 + offsetHeight);

        var topLine = Line.CreateBound(pointTop1, pointTop2);
        var right = Line.CreateBound(pointTop2, pointBottom2);
        var bottom = Line.CreateBound(pointBottom2, pointBottom1);
        var left = Line.CreateBound(pointBottom1, pointTop1);

        return [topLine, right, bottom, left];
    }

    private (double width, double height, XYZ center) GetWidthHeight(FamilyInstance element)
    {
        var bB = element.get_BoundingBox(null);

        double height = bB.Max.Z - bB.Min.Z;

        var center = (bB.Min + bB.Max) / 2;

        double? width = element.Symbol.get_Parameter(BuiltInParameter.FURNITURE_WIDTH)?.AsDouble();

        if (!width.HasValue)
        {
            width = element.Symbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH)?.AsDouble();
        }

        return (width!.Value, height, center);
    }
}
