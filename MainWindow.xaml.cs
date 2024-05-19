using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HttpClient _httpClient = new HttpClient();
        public MainWindow()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            _httpClient = new HttpClient();
            // Настройка базового адреса, если требуется:
            _httpClient.BaseAddress = new Uri("https://localhost:7229");
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width;
            Top = desktopWorkingArea.Bottom - Height;
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)this.Resources["TextContentTemplate"];
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)this.Resources["ImageContentTemplate"];
        }

        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)this.Resources["FileContentTemplate"];
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            
            this.Close();
        }
        private async void UploadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await UploadFile(openFileDialog.FileName);
            }
        }

        private async Task UploadFile(string filePath)
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(fileStream), "file", System.IO.Path.GetFileName(filePath));

                var response = await _httpClient.PostAsync("https://localhost:7229/api/FileMain", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Файл успешно загружен");
                }
                else
                {
                    MessageBox.Show($"Ошибка при загрузке файла: {response.StatusCode}\n{responseContent}");
                }
            }
        }
    }
}