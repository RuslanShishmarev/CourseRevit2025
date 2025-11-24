using Autodesk.Revit.DB;

namespace CourseRevit2025.FirstProject.Services.Extensions;

internal static class LineExtentions
{
    public static Line Offset(this Line line, double offset, XYZ vector)
    {
        var start = line.GetEndPoint(0);
        var end = line.GetEndPoint(1);

        var newStart = start + vector * offset;
        var newEnd = end + vector * offset;

        return Line.CreateBound(newStart, newEnd);
    }
}
