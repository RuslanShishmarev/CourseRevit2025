using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace CourseRevit2025.FirstProject.View;

internal static class ListViewHelper
{
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            name: "SelectedItems",
            propertyType: typeof(IList),
            ownerType: typeof(ListViewHelper),
            defaultMetadata: new UIPropertyMetadata(null, OnSelectedItemsChanged));

    public static IList GetSelectedItems(DependencyObject obj) =>
        (IList)obj.GetValue(SelectedItemsProperty);

    public static void SetSelectedItems(DependencyObject obj, IList value) =>
        obj.SetValue(SelectedItemsProperty, value);

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listView) return;

        listView.SelectionChanged -= ListView_SelectionChanged;
        listView.SelectionChanged += ListView_SelectionChanged;

        var boundList = GetSelectedItems(listView);
        if (boundList is null) return;

        listView.SelectedItems.Clear();
        foreach (var item in boundList)
        {
            listView.SelectedItems.Add(item);
        }
    }

    private static void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listView) return;

        var boundList = GetSelectedItems(listView);
        if (boundList is null) return;

        foreach (var removed in e.RemovedItems)
        {
            boundList.Remove(removed);
        }

        foreach (var added in e.AddedItems)
        {
            if (!boundList.Contains(added))
                boundList.Add(added);
        }
    }
}
