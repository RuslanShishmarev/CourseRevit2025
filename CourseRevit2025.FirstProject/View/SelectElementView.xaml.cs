using System.Windows;

namespace CourseRevit2025.FirstProject.View
{
    /// <summary>
    /// Interaction logic for SelectElementView.xaml
    /// </summary>
    public partial class SelectElementView : Window
    {
        public object? SelectedElement { get; private set; }

        public bool IsValid { get; private set; }

        public SelectElementView(IEnumerable<object> elements, string memberPath)
        {
            InitializeComponent();

            var items = elements.ToList();
            this.elementsViewBox.ItemsSource = items;
            this.elementsViewBox.DisplayMemberPath = memberPath;

            SelectedElement = items.FirstOrDefault();
            this.elementsViewBox.SelectedItem = SelectedElement;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedElement = this.elementsViewBox.SelectedItem;
            IsValid = true;
            this.Close();
        }
    }
}
