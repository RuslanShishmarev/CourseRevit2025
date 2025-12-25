namespace CourseRevit2025.FirstProject.Models;

internal class ScheduleParameterView(
    string name,
    bool isGroup,
    bool isVisible = true)
{
    public string Name { get; } = name;

    public bool IsVisible { get; } = isVisible;

    public bool IsGroup { get; } = isGroup;
}
