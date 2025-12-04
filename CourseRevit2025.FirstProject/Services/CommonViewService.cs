using Autodesk.Revit.DB;

using CourseRevit2025.FirstProject.View;

using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace CourseRevit2025.FirstProject.Services;

internal static class CommonViewService
{
    public static bool GetValueFromElements<T>(
        IEnumerable<T> elements,
        string memberPath,
        out T? value) where T : class
    {
        var select3DType = new SelectElementView(elements, memberPath);
        select3DType.ShowDialog();
        value = select3DType.SelectedElement as T;

        if (!select3DType.IsValid || select3DType.SelectedElement is null) return false;

        return true;
    }

    public static BitmapImage? GetImageFromSymbol(ElementType? symbol, int width, int height)
    {
        if (symbol == null) return null;

        Bitmap? imageB = null;

        ElementId typeImageId = symbol.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_IMAGE).AsElementId();

        if (typeImageId != null && typeImageId.Value != -1)
        {
            imageB = (symbol.Document.GetElement(typeImageId) as ImageType)?.GetImage();
        }
        else imageB = symbol.GetPreviewImage(new Size(width, height));

        if (imageB is null) return null;

        using MemoryStream memory = new();

        imageB.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
        memory.Position = 0;

        BitmapImage bitmapimage = new();
        bitmapimage.BeginInit();
        bitmapimage.StreamSource = memory;
        bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapimage.EndInit();

        return bitmapimage;
    }

    public static string GetValueFromUser(string title, string text)
    {
        GetValueFromUserView view = new(title: title, text: text);
        view.ShowDialog();

        if (view.IsValid)
        {
            return view.Value;
        }
        return string.Empty;
    }

    public static bool TryGetValueFromUser(
        string title,
        string text,
        out double value,
        double defaultV = 0)
    {
        GetValueFromUserView view = new(title: title, text: text, defaultValue: defaultV.ToString());
        view.ShowDialog();
        value = 0;

        if (view.IsValid)
        {
            if (double.TryParse(view.Value, out value))
            {
                return true;
            }
        }
        return false;
    }
}
