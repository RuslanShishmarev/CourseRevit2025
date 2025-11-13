using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using CourseRevit2025.FirstProject.Models;

namespace CourseRevit2025.FirstProject.ViewModels;

internal class SearchByCategoryViewModel : SearchElementsViewModel
{
    public List<ModelView<BuiltInCategory>> SearchCategories { get; }

    private ModelView<BuiltInCategory> _selectedCategory;
    public ModelView<BuiltInCategory> SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory != value)
            {
                _selectedCategory = value;
                this.ViewElements();
            }
        }
    }

    public SearchByCategoryViewModel(ExternalCommandData data) : base(data)
    {
        SearchCategories = [
            new ModelView<BuiltInCategory>(BuiltInCategory.OST_Walls, "Walls"),
            new ModelView<BuiltInCategory>(BuiltInCategory.OST_Windows, "Windows"),
            new ModelView<BuiltInCategory>(BuiltInCategory.OST_Doors, "Doors"),
            ];

        SelectedCategory = SearchCategories.First();

        this.GetElementsFunc += () =>
        {
            var collector = this.InnerView ?
            new FilteredElementCollector(Doc, Doc.ActiveView.Id) :
            new FilteredElementCollector(Doc);

            return collector
                .OfCategory(this.SelectedCategory.Value)
                .WhereElementIsNotElementType()
                .ToElements();
        };
        ViewElements();
    }
}
