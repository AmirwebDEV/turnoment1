
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;

namespace ExplorerStarter
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<FileItem> files = new();
        private ICollectionView fileView;


        private bool isSelecting;
        private bool additiveSelection;
        private Point selectionStart;

        private readonly HashSet<object> originalSelection = new();

        private static T FindAncestor<T>(DependencyObject element)
            where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T result)
                    return result;

                element = VisualTreeHelper.GetParent(element);
            }

            return null;
        }

        private void FileArea_MouseDown(
            object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            // Let normal item clicks, Ctrl+click, and Shift+click
            // be handled by the ListView itself.
            if (FindAncestor<ListViewItem>(
                    e.OriginalSource as DependencyObject) != null)
                return;

            isSelecting = true;
            additiveSelection =
                Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            selectionStart = e.GetPosition(FileArea);

            originalSelection.Clear();

            foreach (var item in FileList.SelectedItems)
                originalSelection.Add(item);

            if (!additiveSelection)
                FileList.SelectedItems.Clear();

            Canvas.SetLeft(SelectionRect, selectionStart.X);
            Canvas.SetTop(SelectionRect, selectionStart.Y);

            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            SelectionRect.Visibility = Visibility.Visible;

            FileArea.CaptureMouse();
            e.Handled = true;
        }

        private void FileArea_MouseMove(
            object sender, MouseEventArgs e)
        {
            if (!isSelecting ||
                e.LeftButton != MouseButtonState.Pressed)
                return;

            Point current = e.GetPosition(FileArea);

            double left = Math.Min(selectionStart.X, current.X);
            double top = Math.Min(selectionStart.Y, current.Y);
            double width = Math.Abs(current.X - selectionStart.X);
            double height = Math.Abs(current.Y - selectionStart.Y);

            Canvas.SetLeft(SelectionRect, left);
            Canvas.SetTop(SelectionRect, top);
            SelectionRect.Width = width;
            SelectionRect.Height = height;

            Rect selectionBounds = new Rect(left, top, width, height);

            if (!additiveSelection)
                FileList.SelectedItems.Clear();
            else
            {
                FileList.SelectedItems.Clear();

                foreach (var item in originalSelection)
                    FileList.SelectedItems.Add(item);
            }

            foreach (var item in FileList.Items)
            {
                if (FileList.ItemContainerGenerator
                        .ContainerFromItem(item) is not ListViewItem container)
                    continue;

                Rect itemBounds = container
                    .TransformToAncestor(FileArea)
                    .TransformBounds(
                        new Rect(0, 0, container.ActualWidth,
                                       container.ActualHeight));

                if (selectionBounds.IntersectsWith(itemBounds))
                    FileList.SelectedItems.Add(item);
            }
        }

        private void FileArea_MouseUp(
            object sender, MouseButtonEventArgs e)
        {
            if (!isSelecting)
                return;

            isSelecting = false;
            SelectionRect.Visibility = Visibility.Collapsed;

            if (FileArea.IsMouseCaptured)
                FileArea.ReleaseMouseCapture();

            UpdateStatus();
            e.Handled = true;
        }

        private void FileList_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
        {
            UpdateStatus();
        }


        public MainWindow()
        {
            InitializeComponent();
            InitializeTabs();


            files.Add(new FileItem
            {
                Name = "Documents",
                Type = "File folder",
                Modified = DateTime.Today.ToShortDateString(),
                Size = "",
                IsFolder = true
            });

            files.Add(new FileItem
            {
                Name = "Projects",
                Type = "File folder",
                Modified = DateTime.Today.ToShortDateString(),
                Size = "",
                IsFolder = true
            });

            files.Add(new FileItem
            {
                Name = "Notes.txt",
                Type = "Text document",
                Modified = DateTime.Today.ToShortDateString(),
                Size = "4 KB"
            });

            files.Add(new FileItem
            {
                Name = "Settings.json",
                Type = "JSON file",
                Modified = DateTime.Today.ToShortDateString(),
                Size = "2 KB"
            });

            files.Add(new FileItem
            {
                Name = "Report.pdf",
                Type = "PDF document",
                Modified = DateTime.Today.ToShortDateString(),
                Size = "120 KB"
            });

            fileView = CollectionViewSource.GetDefaultView(files);
            fileView.Filter = FilterFiles;

            FileList.ItemsSource = fileView;

            UpdateStatus();
        }

        private bool FilterFiles(object item)
        {
            if (item is not FileItem file)
                return false;

            string query = SearchBox?.Text?.Trim() ?? "";

            return file.Name.Contains(
                query,
                StringComparison.OrdinalIgnoreCase);
        }

        private void SearchBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            fileView?.Refresh();
            UpdateStatus();
        }


        private readonly List<GroupTab> tabs = new();
        private int nextTabId = 1;

        public class GroupTab
        {
            public string Title { get; set; } = "New Tab";
            public string Location { get; set; } = "Home";
            public Brush Background { get; set; } = Brushes.White;
        }

        private void InitializeTabs()
        {
            tabs.Clear();

            tabs.Add(new GroupTab
            {
                Title = "All",
                Location = "Home",
                Background = Brushes.White
            });

            TabBar.ItemsSource = tabs;
        }

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            var tab = new GroupTab
            {
                Title = $"New Tab {nextTabId++}",
                Location = "Home",
                Background = Brushes.White
            };

            tabs.Add(tab);
            TabBar.Items.Refresh();
            ActivateTab(tab);
        }

        private void SelectTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Tag is GroupTab tab)
            {
                ActivateTab(tab);
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not GroupTab tab)
                return;

            if (tabs.Count == 1)
            {
                MessageBox.Show("At least one tab must remain open.");
                return;
            }

            int index = tabs.IndexOf(tab);
            tabs.Remove(tab);
            TabBar.Items.Refresh();

            int newIndex = Math.Min(index, tabs.Count - 1);
            ActivateTab(tabs[newIndex]);
        }

        private void ActivateTab(GroupTab tab)
        {
            foreach (var item in tabs)
            {
                item.Background = ReferenceEquals(item, tab)
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(222, 227, 234));
            }

            TabBar.Items.Refresh();

            FolderTitle.Text = tab.Location;
            AddressBox.Text = tab.Location;
            SearchBox.Clear();

            StatusText.Text = $"Location: {tab.Location}";
        }


        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            string location = button.Tag?.ToString() ?? "Home";

            FolderTitle.Text = location;
            AddressBox.Text = location;

            SearchBox.Clear();
            FileList.SelectedItem = null;

            StatusText.Text = $"Location: {location}";
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("Home");
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("This PC");
        }

        private void NavigateTo(string location)
        {
            FolderTitle.Text = location;
            AddressBox.Text = location;
            SearchBox.Clear();
            FileList.SelectedItem = null;
            UpdateStatus();
        }

        private void NewFolder_Click(
            object sender, RoutedEventArgs e)
        {
            string name = "New Folder";
            int count = 1;

            while (files.Any(f => f.Name == name))
            {
                count++;
                name = $"New Folder ({count})";
            }

            files.Add(new FileItem
            {
                Name = name,
                Type = "File folder",
                Modified = DateTime.Today.ToShortDateString(),
                Size = "",
                IsFolder = true
            });

            fileView.Refresh();
            UpdateStatus();
        }

        private void Refresh_Click(
            object sender, RoutedEventArgs e)
        {
            fileView.Refresh();
            UpdateStatus();
        }

        private void Delete_Click(
            object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is not FileItem selected)
            {
                MessageBox.Show(
                    "Select an item first.",
                    "Delete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Remove '{selected.Name}' from this demo?",
                "Confirm deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                files.Remove(selected);
                UpdateStatus();
            }
        }

        private void FileList_DoubleClick(
            object sender, MouseButtonEventArgs e)
        {
            if (FileList.SelectedItem is not FileItem selected)
                return;

            if (selected.IsFolder)
            {
                string location =
                    $"{FolderTitle.Text} > {selected.Name}";

                FolderTitle.Text = selected.Name;
                AddressBox.Text = location;

                StatusText.Text =
                    $"Demo folder: {selected.Name}";
            }
            else
            {
                MessageBox.Show(
                    $"Selected file: {selected.Name}",
                    "File information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void UpdateStatus()
        {
            if (FileList == null || StatusText == null)
                return;

            int selected = FileList.SelectedItems.Count;
            int total = FileList.Items.Count;

            StatusText.Text = selected > 0
                ? $"{selected} selected | {total} items"
                : $"{total} items";
        }
    }

    public class FileItem
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Modified { get; set; } = "";
        public string Size { get; set; } = "";
        public bool IsFolder { get; set; }
    }
}
