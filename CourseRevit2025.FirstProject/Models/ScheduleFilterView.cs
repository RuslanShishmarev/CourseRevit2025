namespace CourseRevit2025.FirstProject.Models;

internal class ScheduleFilterView<T>(string name, T value)
{
    public string Name { get; } = name;

    public T Value { get; } = value;
}
