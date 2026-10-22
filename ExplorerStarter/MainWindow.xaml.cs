using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

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


        private Process? pythonProcess;



        private async Task TestLoadJsonAsync()
        {
            if (backend == null)
                return;

            try
            {
                using JsonDocument response = await backend.SendAsync(
                    new { action = "load", flag = "all" });

                string json = JsonSerializer.Serialize(
                    response.RootElement,
                    new JsonSerializerOptions { WriteIndented = true });

                MessageBox.Show(json, "Python JSON Response");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "JSON Load Error");
            }
        }



        //private void StartPythonBackend()
        //{
        //    string scriptPath = Path.GetFullPath("C:\\Users\\Bahalou\\Desktop\\Campus\\UI\\ExplorerStarter\\ExplorerStarter\\PythonBackend");

        //    var startInfo = new ProcessStartInfo
        //    {
        //        FileName = "python",
        //        Arguments = $"\"{scriptPath}\"",
        //        UseShellExecute = false,
        //        RedirectStandardInput = true,
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        CreateNoWindow = true
        //    };

        //    pythonProcess = new Process
        //    {
        //        StartInfo = startInfo
        //    };

        //    pythonProcess.Start();
        //}
        //private async Task<JsonDocument> SendRequestAsync(
        //    object request)
        //{
        //    if (pythonProcess == null || pythonProcess.HasExited)
        //        throw new InvalidOperationException(
        //            "Python backend is not running.");

        //    string json = JsonSerializer.Serialize(request);

        //    await pythonProcess.StandardInput.WriteLineAsync(json);
        //    await pythonProcess.StandardInput.FlushAsync();

        //    string? response =
        //        await pythonProcess.StandardOutput.ReadLineAsync();

        //    if (string.IsNullOrWhiteSpace(response))
        //        throw new InvalidOperationException(
        //            "Python backend returned no response.");

        //    return JsonDocument.Parse(response);
        //}




        //private async Task LoadBackendItemsAsync()
        //{
        //    using JsonDocument response = await SendRequestAsync(
        //        new { action = "get_items" });

        //    JsonElement root = response.RootElement;

        //    if (!root.GetProperty("success").GetBoolean())
        //    {
        //        MessageBox.Show("Could not load backend data.");
        //        return;
        //    }

        //    var items = root.GetProperty("data").GetProperty("items");

        //    // Replace the sample entries with backend data.
        //    files.Clear();

        //    foreach (JsonElement item in items.EnumerateArray())
        //    {
        //        files.Add(new FileItem
        //        {
        //            Name = item.GetProperty("name").GetString() ?? "",
        //            Type = "Application item",
        //            Modified = DateTime.Now.ToShortDateString(),
        //            Size = ""
        //        });
        //    }

        //    fileView.Refresh();
        //    UpdateStatus();
        //}



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


        private PythonBackend? backend;


        public MainWindow()
        {

            InitializeComponent();
            fileView = CollectionViewSource.GetDefaultView(files);
            fileView.Filter = FilterFiles;
            FileList.ItemsSource = fileView;


            string scriptPath = Path.Combine(
                AppContext.BaseDirectory,
                "PythonBackend",
                "main.py");

            if (!File.Exists(scriptPath))
            {
                MessageBox.Show(
                    $"Python backend not found:\n{scriptPath}",
                    "Backend startup error");

                return;
            }

            backend = new PythonBackend("python", scriptPath);


            // Set this to your actual Python executable path if needed.
            backend = new PythonBackend("python", scriptPath);
            Loaded += async (_, _) => await TestLoadJsonAsync();



            /*files.Add(new FileItem
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
            });*/

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
