using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using CourseRevit2025.FirstProject.Models;
using CourseRevit2025.FirstProject.Services;

using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace CourseRevit2025.FirstProject.ViewModels;

internal class SearchElementsViewModel : ViewModelBase
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

    protected Func<ICollection<Element>> GetElementsFunc { get; set; }

    protected ExternalCommandData Data;
    protected UIDocument UIdoc;
    protected Document Doc;

    public SearchElementsViewModel(ExternalCommandData data)
    {
        Data = data;
        UIdoc = Data.Application.ActiveUIDocument;
        Doc = UIdoc.Document;

        ViewListModes = [
            new ModelView<ViewModeType>(ViewModeType.Blocks, "Blocks"),
            new ModelView<ViewModeType>(ViewModeType.Table, "Table"),
            ];

        SelectedViewMode = ViewListModes.First();

        SelectElementCommand = new RelayCommand(SelectElement);
        IsolateElementsCommand = new RelayCommand(IsolateElements);
    }

    public SearchElementsViewModel(
        ExternalCommandData data,
        Func<ICollection<Element>> getElementsFunc) : this(data)
    {
        GetElementsFunc = getElementsFunc;
        ViewElements();
    }

    protected void ViewElements()
    {
        SearchElements();
    }

    private void SearchElements()
    {
        if (GetElementsFunc is null) return;

        var elements = GetElementsFunc.Invoke();

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
            var elementType = Doc.GetElement(element.GetTypeId()) as ElementType;
            elementTypeImage = CommonViewService.GetImageFromSymbol(elementType, 100, 100);
            _elementsTypes.Add(typeId, elementTypeImage);
        }
        return elementTypeImage;
    }

    private void SelectElement()
    {
        if (SelectedElements is null || SelectedElements.Count == 0) return;

        UIdoc.Selection.SetElementIds([..SelectedElements.Select(x => x.Value.Id)]);
    }

    private void IsolateElements()
    {
        if (SelectedElements is null || SelectedElements.Count == 0) return;

        var all3DTypes = new FilteredElementCollector(Doc)
            .OfClass(typeof(ViewFamilyType))
            .Where(x => (x as ViewFamilyType)!.ViewFamily == ViewFamily.ThreeDimensional);

        if (!CommonViewService.GetValueFromElements(
            elements: all3DTypes,
            memberPath: nameof(ViewFamilyType.Name),
            out var selected3DType)) return;

        var viewName = $"My3d_{DateTime.Now.Ticks}";
        using var tr = new Transaction(Doc, $"Create view {viewName}");
        tr.Start();
        try
        {
            var newView = View3D.CreateIsometric(Doc, selected3DType!.Id);
            newView.Name = viewName;

            newView.IsolateElementsTemporary([.. SelectedElements.Select(x => x.Value.Id)]);
            newView.ConvertTemporaryHideIsolateToPermanent();
            tr.Commit();

            UIdoc.ActiveView = newView;
        }
        catch(Exception ex)
        {
            tr.RollBack();
            TaskDialog.Show("Error", ex.Message);
        }
    }
}
