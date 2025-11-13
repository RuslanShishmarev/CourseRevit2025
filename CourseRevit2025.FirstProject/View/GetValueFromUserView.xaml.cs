using System.Windows;

namespace CourseRevit2025.FirstProject.View
{
    /// <summary>
    /// Interaction logic for GetValueFromUserView.xaml
    /// </summary>
    public partial class GetValueFromUserView : Window
    {
        public string Value { get; private set; }
        public bool IsValid { get; private set; }

        public GetValueFromUserView(
            string title,
            string text,
            string? defaultValue = null)
        {
            InitializeComponent();
            this.Title = title;
            this.textView.Text = text;
            this.valueTB.Text = defaultValue;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            IsValid = true;
            Value = this.valueTB.Text;
            this.Close();
        }
    }
}
