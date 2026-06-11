using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OnlineLibrary
{
    /// <summary>
    /// Страница регистрации нового пользователя в системе.
    /// Предоставляет форму для создания учётной записи с валидацией введённых данных.
    /// </summary>
    public partial class RegistrationPage : Page
    {
        /// <summary>
        /// Инициализирует новый экземпляр страницы регистрации.
        /// </summary>
        public RegistrationPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Зарегистрироваться".
        /// Выполняет валидацию введённых данных, проверяет уникальность логина
        /// и создаёт нового пользователя с ролью "Пользователь".
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Зарегистрироваться")</param>
        /// <param name="e">Аргументы события</param>
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем и очищаем введённые данные
            string name = NameTextBox.Text.Trim();
            string login = LoginTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Валидация: проверка заполнения всех полей
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(login) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Заполните все поля!");
                return;
            }

            // Получаем единственный экземпляр контекста базы данных (Singleton)
            var context = OnlineLibraryEntities.GetContext();

            // Проверка уникальности логина
            if (context.Users.Any(u => u.Login == login))
            {
                ShowError("Логин уже занят!");
                return;
            }

            // Создание нового пользователя
            var newUser = new Users
            {
                Name = name,
                Login = login,
                Email = email,
                PasswordHash = password,
                RoleId = 1 // Роль "Пользователь"
            };

            // Сохранение пользователя в базе данных
            context.Users.Add(newUser);
            context.SaveChanges();

            // Уведомление об успешной регистрации и переход на страницу входа
            MessageBox.Show("Регистрация успешна! Теперь вы можете войти.");
            NavigationService.Navigate(new LoginPage());
        }

        /// <summary>
        /// Обработчик события нажатия на ссылку "Войти".
        /// Выполняет переход на страницу авторизации.
        /// </summary>
        /// <param name="sender">Источник события (ссылка "Войти")</param>
        /// <param name="e">Аргументы события</param>
        private void LoginLink_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LoginPage());
        }

        /// <summary>
        /// Отображает сообщение об ошибке на странице регистрации.
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