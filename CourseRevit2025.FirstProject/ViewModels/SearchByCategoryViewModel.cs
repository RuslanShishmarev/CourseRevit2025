using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using CourseRevit2025.FirstProject.Models;
using CourseRevit2025.FirstProject.Services;

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;

namespace CourseRevit2025.FirstProject.ViewModels;

internal class SearchByCategoryViewModel : ViewModelBase
{
    public List<ModelView<ViewModeType>> ViewListModes { get; }

    private ModelView<ViewModeType> _selectedViewMode;
    public ModelView<ViewModeType> SelectedViewMode
    {
        get => _selectedViewMode;
        set
        {
            if (_selectedViewMode != value)
            {
                _selectedViewMode = value;
                SearchElements();
                OnPropertyChanged(nameof(SelectedViewMode));
            }
        }
    }

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
                SearchElements();
            }
        }
    }

    public List<ModelImageView<Element>> Elements { get; private set; }

    public ObservableCollection<ModelImageView<Element>> SelectedElements { get; } = new();

    private bool _innerView = false;
    public bool InnerView
    {
        get => _innerView;
        set
        {
            if (_innerView == value) return;
            _innerView = value;
            SearchElements();
        }
    }

    public RelayCommand SelectElementCommand { get; }

    public RelayCommand IsolateElementsCommand { get; }


    private ExternalCommandData _data;
    private UIDocument _uidoc;
    private Document _doc;

    public SearchByCategoryViewModel(ExternalCommandData data)
    {
        _data = data;
        _uidoc = _data.Application.ActiveUIDocument;
        _doc = _uidoc.Document;
        SearchCategories = [
            new ModelView<BuiltInCategory>(BuiltInCategory.OST_Walls, "Walls"),
            new ModelView<BuiltInCategory>(BuiltInCategory.OST_Windows, "Windows"),
            new ModelView<BuiltInCategory>(BuiltInCategory.OST_Doors, "Doors"),
            ];

        SelectedCategory = SearchCategories.First();

        ViewListModes = [
            new ModelView<ViewModeType>(ViewModeType.Blocks, "Blocks"),
            new ModelView<ViewModeType>(ViewModeType.Table, "Table"),
            ];

        SelectedViewMode = ViewListModes.First();

        SelectElementCommand = new RelayCommand(SelectElement);
        IsolateElementsCommand = new RelayCommand(IsolateElements);
    }

    private void SearchElements()
    {
        var collector = this.InnerView ?
            new FilteredElementCollector(_doc, _doc.ActiveView.Id) :
            new FilteredElementCollector(_doc);

        var elements = collector
            .OfCategory(this.SelectedCategory.Value)
            .WhereElementIsNotElementType()
            .ToElements();

        this.Elements = [.. elements.Select(
            x => new ModelImageView<Element>(
                value: x,
                image: GetImage(x),
                name: x.Name))];
        OnPropertyChanged(nameof(Elements));
    }

    private Dictionary<ElementId, BitmapImage?> _elementsTypes = new();
    private BitmapImage? GetImage(Element element)
    {
        var typeId = element.GetTypeId();
        if (!_elementsTypes.TryGetValue(typeId, out var elementTypeImage))
        {
            var elementType = _doc.GetElement(element.GetTypeId()) as ElementType;
            elementTypeImage = CommonViewService.GetImageFromSymbol(elementType, 100, 100);
            _elementsTypes.Add(typeId, elementTypeImage);
        }
        return elementTypeImage;
    }

    private void SelectElement()
    {
        if (SelectedElements is null || SelectedElements.Count == 0) return;

        _uidoc.Selection.SetElementIds([..SelectedElements.Select(x => x.Value.Id)]);
    }

    private void IsolateElements()
    {
        if (SelectedElements is null || SelectedElements.Count == 0) return;

        var all3DTypes = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Where(x => (x as ViewFamilyType)!.ViewFamily == ViewFamily.ThreeDimensional);

        if (!CommonViewService.GetValueFromElements(
            elements: all3DTypes,
            memberPath: nameof(ViewFamilyType.Name),
            out var selected3DType)) return;

        var viewName = $"My3d_{DateTime.Now.Ticks}";
        using var tr = new Transaction(_doc, $"Create view {viewName}");
        tr.Start();
        try
        {
            var newView = View3D.CreateIsometric(_doc, selected3DType!.Id);
            newView.Name = viewName;

            newView.IsolateElementsTemporary([.. SelectedElements.Select(x => x.Value.Id)]);
            newView.ConvertTemporaryHideIsolateToPermanent();
            tr.Commit();

            _uidoc.ActiveView = newView;
        }
        catch(Exception ex)
        {
            tr.RollBack();
            TaskDialog.Show("Error", ex.Message);
        }
    }
}
