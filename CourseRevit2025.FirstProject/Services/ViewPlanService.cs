using Autodesk.Revit.DB;
using CourseRevit2025.FirstProject.Models;
using CourseRevit2025.FirstProject.Services.Interfaces;

using ViewRevit = Autodesk.Revit.DB.View;

namespace CourseRevit2025.FirstProject.Services;

internal class ViewPlanService : IViewPlanService
{
    private Document _doc;
    private ViewFamilyType _typeForPlan;
    private ViewFamilyType _typeFor3D;
    private IEnumerable<ParameterFilterElement> _parameterFilterElements;

    public ViewPlanService(Document doc)
    {
        _doc = doc;

        var viewTypes = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>();

        _parameterFilterElements = new FilteredElementCollector(_doc)
            .OfClass(typeof(ParameterFilterElement))
            .Cast<ParameterFilterElement>();

        _typeForPlan = viewTypes.First(x => x.ViewFamily == ViewFamily.FloorPlan);
        _typeFor3D = viewTypes.First(x => x.ViewFamily == ViewFamily.ThreeDimensional);
    }

    public ViewPlan CreateViewPlan(
        string name,
        IEnumerable<Element> elements,
        string filterParameterName)
    {
        return CreateView(
            name: name,
            elements: elements,
            filterParameterName: filterParameterName,
            detailLevel: ViewDetailLevel.Fine,
            scale: 50,
            createViewFunc: () =>
            {
                var firstEl = elements.First();
                return ViewPlan.Create(_doc, _typeForPlan.Id, firstEl.LevelId);
            });
    }

    public View3D CreateView3D(
        string name,
        IEnumerable<Element> elements,
        string filterParameterName)
    {
        return CreateView(
            name: name,
            elements: elements,
            filterParameterName: filterParameterName,
            detailLevel: ViewDetailLevel.Fine,
            scale: 50,
            createViewFunc: () =>
            {
                var newView = View3D.CreateIsometric(
                    document: _doc,
                    viewFamilyTypeId: _typeFor3D.Id);
                newView.SaveOrientationAndLock();

                return newView;
            });
    }

    public void CreateDimensions(
        ViewRevit view,
        IEnumerable<Element> elements)
    {
        double offset = 1000 / Constants.UNITS_CONVERT;
        double maxX = -1;
        double maxY = -1;

        double minX = double.MaxValue;
        double minY = double.MaxValue;

        Dictionary<DimensionDir,ReferenceArray> lineRefByDir = new();

        void addToDir(DimensionDir dir, Reference reference)
        {
            if (lineRefByDir.TryGetValue(dir, out var references))
            {
                references.Append(reference);
            }
            else
            {
                var newRefs = new ReferenceArray();
                newRefs.Append(reference);
                lineRefByDir.Add(dir, newRefs);
            }
        }

        var mepCurves = new List<MEPCurve>();

        foreach (Element el in elements)
        {
            var bb = el.get_BoundingBox(null);

            if (maxX < bb.Max.X) maxX = bb.Max.X;

            if (maxY < bb.Max.Y) maxY = bb.Max.Y;

            if (minX > bb.Min.X) minX = bb.Min.X;

            if (minY > bb.Min.Y) minY = bb.Min.Y;

            if (el is MEPCurve mepCurve)
                mepCurves.Add(mepCurve);
        }

        var baseLineForVert = Line.CreateBound(
            new XYZ(maxX, maxY, 0) + offset * XYZ.BasisX,
            new XYZ(maxX, minY, 0) + offset * XYZ.BasisX);
        var projectForVert = new Dictionary<double, List<(double len, Reference refer)> >();

        var baseLineForHor = Line.CreateBound(
            new XYZ(minX, minY, 0) - offset * XYZ.BasisY,
            new XYZ(maxX, minY, 0) - offset * XYZ.BasisY);
        var projectForHor = new Dictionary<double, List<(double len, Reference refer)>>();

        void addToDirPoint(DimensionDir dir, XYZ point, Reference reference)
        {
            if (dir == DimensionDir.Hor)
            {
                var checkPosX = Math.Round(point.X, 2);

                if (projectForHor.TryGetValue(checkPosX, out var points))
                {
                    points.Add((point.Y, reference));
                }
                else
                {
                    projectForHor.Add(checkPosX, [(point.Y, reference)]);
                }
            }
            else
            {
                var checkPosY = Math.Round(point.Y, 2);
                if (projectForVert.TryGetValue(checkPosY, out var points))
                {
                    points.Add((point.X, reference));
                }
                else
                {
                    projectForVert.Add(checkPosY, [(point.X, reference)]);
                }
            }
        }

        foreach (var mepCurve in mepCurves)
        {
            var line = GetCurve(mepCurve, view);

            if (line == null) continue;

            var checkAngle = Math.Round(line.Direction.AngleTo(XYZ.BasisX), 2);
            if (checkAngle == 0 || checkAngle == Math.Round(Math.PI, 2))
            {
                addToDir(DimensionDir.Vert, line.Reference);

                addToDirPoint(DimensionDir.Hor, line.GetEndPoint(0), line.GetEndPointReference(0));
                addToDirPoint(DimensionDir.Hor, line.GetEndPoint(1), line.GetEndPointReference(1));
            }
            else if (checkAngle == Math.Round(Math.PI / 2, 2) || checkAngle == Math.Round(1.5 * Math.PI, 2))
            {
                addToDir(DimensionDir.Hor, line.Reference);

                addToDirPoint(DimensionDir.Vert, line.GetEndPoint(0), line.GetEndPointReference(0));
                addToDirPoint(DimensionDir.Vert, line.GetEndPoint(1), line.GetEndPointReference(1));
            }
        }


        if (lineRefByDir.TryGetValue(DimensionDir.Vert, out var refForVert) &&
            refForVert.Size > 1)
        {
            foreach (var addV in projectForVert)
            {
                var nearest = addV.Value.OrderBy(x => x.len).Last().refer;
                refForVert.Append(nearest);
            }

            if (refForVert.Size > 1)
                _doc.Create.NewDimension(
                    view: view,
                    line: baseLineForVert,
                    references: refForVert);
        }

        if (lineRefByDir.TryGetValue(DimensionDir.Hor, out var refForHor))
        {
            foreach (var addV in projectForHor)
            {
                var nearest = addV.Value.OrderBy(x => x.len).First().refer;
                refForHor.Append(nearest);
            }

            if (refForHor.Size > 1)
                _doc.Create.NewDimension(
                view: view,
                line: baseLineForHor,
                references: refForHor);
        }
    }

    private Line? GetCurve(MEPCurve el, ViewRevit view)
    {
        var geometry = el.get_Geometry(new Options
        {
            View = view,
            ComputeReferences = true,
            IncludeNonVisibleObjects = true,
        });

        return geometry.OfType<Line>().FirstOrDefault(IsDuctCurve);
    }


    private bool IsDuctCurve(Curve curve)
    {
        var grStyle = (GraphicsStyle)_doc.GetElement(curve.GraphicsStyleId);
        var categoryDuct = new ElementId(BuiltInCategory.OST_DuctCurves);

        return grStyle.GraphicsStyleCategory.Id == categoryDuct;
    }

    private T CreateView<T>(
        string name,
        IEnumerable<Element> elements,
        string filterParameterName,
        ViewDetailLevel detailLevel,
        int scale,
        Func<T> createViewFunc) where T : Autodesk.Revit.DB.View
    {
        var firstEl = elements.First();
        var parameterForFilter = firstEl.LookupParameter(filterParameterName);

        if (parameterForFilter is null)
        {
            throw new ArgumentException($"Parameter {filterParameterName} is not exist");
        }

        var view = createViewFunc.Invoke();
        view.Name = name;

        foreach (var el in elements)
        {
            el.LookupParameter(filterParameterName).Set(name);
        }

        view.IsolateElementsTemporary([.. elements.Select(x => x.Id)]);

        view.ConvertTemporaryHideIsolateToPermanent();

        view.DetailLevel = detailLevel;
        view.Scale = scale;

        SetFilterToView(
            view: view,
            parameterId: parameterForFilter.Id,
            parameterName: filterParameterName,
            parameterValue: name);

        return view;
    }

    private string _filterNamePattern = "NOT_{0}_{1}";
    private ParameterFilterElement SetFilterToView(
        ViewRevit view,
        string parameterName,
        ElementId parameterId,
        string parameterValue)
    {
        var filterName = string.Format(_filterNamePattern, parameterName, parameterValue);

        var existedFilter = _parameterFilterElements.FirstOrDefault(x => x.Name == filterName);

        if (existedFilter != null)
        {
            var filterIds = view.GetFilters();
            if (filterIds.Contains(existedFilter.Id))
            {
                return existedFilter;
            }

            view.AddFilter(existedFilter.Id);

            view.SetFilterVisibility(existedFilter.Id, false);

            return existedFilter;
        }

        var provider = new ParameterValueProvider(parameterId);

        var stringEquals = new FilterStringEquals();

        var rule = new FilterStringRule(provider, stringEquals, parameterValue);

        var ruleInverse = new FilterInverseRule(rule);

        var elementFilter = new ElementParameterFilter(ruleInverse);

        var filter = ParameterFilterElement.Create(
            aDocument: _doc,
            name: filterName,
            categories: [
                new ElementId(BuiltInCategory.OST_DuctCurves),
                new ElementId(BuiltInCategory.OST_DuctFitting),
                new ElementId(BuiltInCategory.OST_DuctAccessory),
                ],
            elementFilter: elementFilter);

        view.AddFilter(filter.Id);
        view.SetFilterVisibility(filter.Id, false);

        return filter;
    }
}
