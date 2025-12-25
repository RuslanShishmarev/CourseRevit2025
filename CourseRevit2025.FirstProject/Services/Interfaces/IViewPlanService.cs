using Autodesk.Revit.DB;
using CourseRevit2025.FirstProject.Models;
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

    ViewSchedule CreateViewSchedule<T>(
        IEnumerable<ScheduleParameterView> fieldNames,
        ScheduleFilterView<T> filter,
        string name);
}
