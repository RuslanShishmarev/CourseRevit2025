using Autodesk.Revit.DB;

using ViewRevit = Autodesk.Revit.DB.View;

namespace CourseRevit2025.FirstProject.Services.Interfaces;

internal interface IViewPlanService
{
    ViewPlan CreateViewPlan(
        string name,
        IEnumerable<Element> elements,
        string filterParameterName);

    View3D CreateView3D(
        string name,
        IEnumerable<Element> elements,
        string filterParameterName);

    void CreateDimensions(ViewRevit view, IEnumerable<Element> elements);
}
