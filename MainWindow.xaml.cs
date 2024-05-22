using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HttpClient _httpClient = new HttpClient();
        private string _accessToken;
        public MainWindow(string accessToken)
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            _accessToken = accessToken;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            LoadFileNames();
        }
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width;
            Top = desktopWorkingArea.Bottom - Height;
        }
        // Метод для загрузки названий файлов и их типов
        private async void LoadFileNames()
        {
            try
            {
                var fileNames = await _httpClient.GetFromJsonAsync<List<FileMain>>("https://localhost:7229/api/FileMain/user-files");
                if (fileNames != null)
                    UpdateUIWithFileNames(fileNames);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении данных: " + ex.Message);
            }
        }

        // Метод для отображения файлов в соответствующих вкладках
        private void UpdateUIWithFileNames(List<FileMain> files)
        {
            TextFilesPanel.Children.Clear();
            ImageFilesPanel.Children.Clear();
            OtherFilesPanel.Children.Clear();

            foreach (var file in files)
            {
                Grid grid = new Grid { Margin = new Thickness(5) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock fileText = new TextBlock
                {
                    Text = file.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextWrapping = TextWrapping.NoWrap,
                    Margin = new Thickness(5),
                    MaxWidth = 330 // Доступная ширина для текста
                };

                Button firstButton = new Button
                {
                    Style = (Style)Resources["IconButtonStyle"],
                    Margin = new Thickness(5, 0, 5, 0)
                };

                Button deleteButton = new Button
                {
                    Style = (Style)Resources["IconButtonStyle"],
                    Content = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Resources/delete.png"))
                    }
                };
                deleteButton.Click += async (s, e) => { await DeleteFile(file.ID); };

                Grid.SetColumn(fileText, 0);
                Grid.SetColumn(firstButton, 1);
                Grid.SetColumn(deleteButton, 2);

                grid.Children.Add(fileText);
                grid.Children.Add(firstButton);
                grid.Children.Add(deleteButton);

                Border border = new Border
                {
                    Background = new SolidColorBrush(ColorConverter.ConvertFromString("#333333") as Color? ?? Colors.Black),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    Child = grid
                };

                if (file.Name.StartsWith("Text_"))
                {
                    firstButton.Content = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Resources/copy.png"))
                    };
                    firstButton.Click += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(fileText.Text))
                            Clipboard.SetText(fileText.Text);
                    };

                    TextFilesPanel.Children.Add(border);
                    LoadTextContent(file.ID, fileText);
                }
                else if (file.Extension == ".jpg" || file.Extension == ".png" || file.Extension == ".gif")
                {
                    firstButton.Content = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Resources/download.png"))
                    };
                    firstButton.Click += async (s, e) => { DownloadFile(file.ID); };

                    ImageFilesPanel.Children.Add(border);
                }
                else
                {
                    firstButton.Content = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Resources/download.png"))
                    };
                    firstButton.Click += async (s, e) => { DownloadFile(file.ID); };

                    OtherFilesPanel.Children.Add(border);
                }
            }
        }
        private async void DownloadFile(int fileId)
        {
            // Making the HTTP call to download the file
            var response = await _httpClient.GetAsync($"https://localhost:7229/api/FileMain/{fileId}");
            if (response.IsSuccessStatusCode)
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                var contentDisposition = response.Content.Headers.ContentDisposition.FileName.Trim('"');
                var savePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), contentDisposition);

                using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await contentStream.CopyToAsync(fileStream);
                    MessageBox.Show("Файл успешно скачан: " + savePath);
                }
            }
            else
            {
                MessageBox.Show("Не удалось скачать файл. Статус: " + response.StatusCode);
            }
        }

        private async Task DeleteFile(int fileId)
        {
            // Вызов API для удаления файла
            var response = await _httpClient.DeleteAsync($"https://localhost:7229/api/FileDelete/{fileId}");
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Файл удалён");
                LoadFileNames();
            }
            else
            {
                MessageBox.Show("Ошибка при удалении файла");
            }
        }
        // Метод для загрузки текстового содержимого файла
        private async void LoadTextContent(int fileId, TextBlock textBlock)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://localhost:7229/api/FileMain/{fileId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    textBlock.Text = content;
                    textBlock.Tag = content;
                }
                else
                {
                    textBlock.Text = "Ошибка загрузки файла.";
                }
            }
            catch (Exception ex)
            {
                textBlock.Text = "Ошибка: " + ex.Message;
            }
        }
        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            UploadFileButton.Visibility = Visibility.Collapsed; // Скрыть кнопку загрузки файла
            txt.Visibility = Visibility.Visible; //Показать надпись 
            TextInput.Visibility = Visibility.Visible; // Показать текстовое поле
            SendTextButton.Visibility = Visibility.Visible; // Показать кнопку отправки текста

            TextScrollViewer.Visibility = Visibility.Visible;
            ImageScrollViewer.Visibility = Visibility.Collapsed;
            OtherScrollViewer.Visibility = Visibility.Collapsed;
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            ResetControls();

            TextScrollViewer.Visibility = Visibility.Collapsed;
            ImageScrollViewer.Visibility = Visibility.Visible;
            OtherScrollViewer.Visibility = Visibility.Collapsed;
        }

        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            ResetControls();

            TextScrollViewer.Visibility = Visibility.Collapsed;
            ImageScrollViewer.Visibility = Visibility.Collapsed;
            OtherScrollViewer.Visibility = Visibility.Visible;
        }

        private void ResetControls()
        {
            UploadFileButton.Visibility = Visibility.Visible;
            TextInput.Visibility = Visibility.Collapsed;
            SendTextButton.Visibility = Visibility.Collapsed;
            txt.Visibility = Visibility.Collapsed;
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

                // Установка заголовка авторизации с токеном
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

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
            LoadFileNames();
        }
        private async void SendTextButton_Click(object sender, RoutedEventArgs e)
        {
            string textContent = TextInput.Text;
            string fileName = $"Text_{DateTime.Now.Ticks}.txt"; // Уникальное имя файла на основе времени
            byte[] fileBytes = Encoding.UTF8.GetBytes(textContent);

            using (var content = new MultipartFormDataContent())
            {
                content.Add(new ByteArrayContent(fileBytes, 0, fileBytes.Length), "file", fileName);

                var response = await _httpClient.PostAsync("https://localhost:7229/api/FileMain", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Текстовый файл успешно отправлен");
                }
                else
                {
                    MessageBox.Show($"Ошибка при отправке файла: {response.StatusCode}\n{responseContent}");
                }
            }
            LoadFileNames();
        }
        private void TextInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextInput.Text))
                txt.Visibility = Visibility.Visible;
            else
                txt.Visibility = Visibility.Collapsed;
        }
    }
}