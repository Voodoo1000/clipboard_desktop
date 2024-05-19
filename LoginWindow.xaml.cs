using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace test
{
    public partial class LoginWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = login.Text;
            var password = pass.Text; 

            try
            {
                var response = await GetToken(username, password);
                if (!string.IsNullOrEmpty(response.AccessToken))
                {
                    // Успешная аутентификация, открыть главное окно
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверные учетные данные", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к серверу: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<TokenResponse> GetToken(string username, string password)
        {
            var url = "https://localhost:7229/token";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
                    return tokenResponse;
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Ошибка при разборе ответа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            else
            {
                MessageBox.Show($"Ошибка сервера: {response.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new TokenResponse(); // Возвращаем пустой токен, если ошибка
            }
        }



        public class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("username")]
            public string Username { get; set; }
        }
    }
}
