using System.Windows.Media.Imaging;

namespace CourseRevit2025.FirstProject.Models;

internal record ModelImageView<T> : ModelView<T>
{
    public BitmapImage? Image { get; }

    public ModelImageView(T value, BitmapImage? image, string name) : base(value, name)
    {
        Image = image;
    }
}
