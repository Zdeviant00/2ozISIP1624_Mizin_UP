using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OnlineLibrary
{
    /// <summary>
    /// Страница авторизации пользователя в системе "Онлайн библиотека".
    /// Предоставляет форму для входа по логину и паролю с валидацией учётных данных.
    /// Поддерживает вход как обычных пользователей, так и замороженных аккаунтов (FR-2.5).
    /// </summary>
    public partial class LoginPage : Page
    {
        /// <summary>
        /// Инициализирует новый экземпляр страницы авторизации.
        /// </summary>
        public LoginPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Войти".
        /// Выполняет валидацию введённых данных, проверку существования пользователя
        /// и корректности пароля. При успешной авторизации открывает главное окно приложения.
        /// Замороженные пользователи также могут войти (с отображением предупреждения в Sidebar).
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Войти")</param>
        /// <param name="e">Аргументы события</param>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем и очищаем введённые данные
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Валидация: проверка заполнения всех полей
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Заполните все поля!");
                return;
            }

            // Получаем единственный экземпляр контекста базы данных (Singleton)
            var context = OnlineLibraryEntities.GetContext();

            // Поиск пользователя по логину
            var user = context.Users.FirstOrDefault(u => u.Login == login);

            // Проверка существования пользователя
            if (user == null)
            {
                ShowError("Пользователь не найден!");
                return;
            }

            // Проверка корректности пароля
            if (user.PasswordHash != password)
            {
                ShowError("Неверный пароль!");
                return;
            }

            // Успешная авторизация — открытие главного окна с передачей данных пользователя
            // ПРИМЕЧАНИЕ: Замороженные пользователи также могут войти в систему.
            // Предупреждение о заморозке отображается в Sidebar (FR-2.5).
            MainWindow mainWindow = new MainWindow(user);
            mainWindow.Show();

            // Закрытие окна авторизации
            Window.GetWindow(this)?.Close();
        }

        /// <summary>
        /// Обработчик события нажатия на ссылку "Зарегистрироваться".
        /// Выполняет переход на страницу регистрации нового пользователя.
        /// </summary>
        /// <param name="sender">Источник события (ссылка "Зарегистрироваться")</param>
        /// <param name="e">Аргументы события</param>
        private void RegisterLink_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegistrationPage());
        }

        /// <summary>
        /// Отображает сообщение об ошибке на странице авторизации.
        /// Делает текстовый блок ошибки видимым и устанавливает текст сообщения.
        /// </summary>
        /// <param name="message">Текст сообщения об ошибке для отображения</param>
        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}