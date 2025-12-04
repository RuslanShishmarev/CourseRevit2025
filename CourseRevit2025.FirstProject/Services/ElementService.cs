using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using System.Linq;

namespace CourseRevit2025.FirstProject.Services;

internal class ElementService
{
    private ExternalCommandData _commandData;
    private Document _doc;
    private UIDocument _uidoc;

    public ElementService(ExternalCommandData commandData)
    {
        _commandData = commandData;
        _uidoc = _commandData.Application.ActiveUIDocument;
        _doc = _uidoc.Document;
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

    public IList<Element> GetTypes(BuiltInCategory category, string name)
    {
        return [.. new FilteredElementCollector(_doc)
            .OfCategory(category)
            .WhereElementIsElementType()
            .Where(x => x.Name == name)];
    }

    public IList<Element> GetTypes(BuiltInCategory category)
    {
        var elements = new FilteredElementCollector(_doc)
            .OfCategory(category)
            .WhereElementIsElementType()
            .ToElements();

        return elements;
    }

    public IList<Element> GetElements(
        BuiltInCategory category,
        Autodesk.Revit.DB.View? view = null)
    {
        var elements = (view is null ? 
            new FilteredElementCollector(_doc) : 
            new FilteredElementCollector(_doc, view.Id))
            .OfCategory(category)
            .WhereElementIsNotElementType()
            .ToElements();

        return elements;
    }

    public List<Element> PlaceElementByType(FamilySymbol symbol, int? count = null)
    {
        _commandData.Application.Application.DocumentChanged += GetElementsFromPromt;

        try
        {
            _uidoc.PromptForFamilyInstancePlacement(symbol);
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException ex) { }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", ex.Message);
        }

        _commandData.Application.Application.DocumentChanged -= GetElementsFromPromt;
        var prevousResult = newElements.Where(x => x.GetTypeId() == symbol.Id);
        List<Element> result = count is null ? [.. prevousResult] : [.. prevousResult.Take(count.Value)];
        newElements.Clear();

        return result;
    }

    private List<Element> newElements = [];
    private void GetElementsFromPromt(object? sender, DocumentChangedEventArgs args)
    {
        var newElementsIds = args.GetAddedElementIds();
        foreach (var elId in newElementsIds)
        {
            Element element = _doc.GetElement(elId);
            newElements.Add(element);
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
