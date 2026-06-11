using System.Windows;

namespace OnlineLibrary
{
    /// <summary>
    /// Главное окно приложения "Онлайн библиотека" с боковой панелью навигации (Sidebar).
    /// Содержит кнопки для перехода между основными разделами приложения:
    /// каталог книг, списки книг, профиль пользователя и администрирование.
    /// Для замороженных пользователей отображается дополнительное предупреждение в Sidebar (FR-2.5).
    /// Кнопка "Администрирование" видна только пользователям с ролью "Администратор" (FR-2.4).
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Текущий авторизованный пользователь.
        /// Передаётся во все открываемые страницы для проверки прав доступа
        /// и определения роли пользователя.
        /// </summary>
        private Users _currentUser;

        /// <summary>
        /// Конструктор главного окна приложения.
        /// Инициализирует компоненты, сохраняет данные текущего пользователя,
        /// настраивает видимость кнопок Sidebar в зависимости от роли и статуса аккаунта,
        /// и открывает каталог книг по умолчанию (FR-2.2).
        /// </summary>
        /// <param name="user">Текущий авторизованный пользователь, полученный со страницы входа</param>
        public MainWindow(Users user)
        {
            InitializeComponent();
            _currentUser = user;

            // Показываем кнопку администрирования только для администраторов (FR-2.4)
            if (user.RoleId == 2) // Роль "Администратор"
            {
                AdminButton.Visibility = Visibility.Visible;
            }

            // Показываем предупреждение о заморозке, если аккаунт заморожен (FR-2.5)
            if (user.IsFrozen)
            {
                FrozenWarningButton.Visibility = Visibility.Visible;
                FrozenWarningButton.ToolTip = $"Ваш аккаунт заморожен. Причина: {user.FreezeReason ?? "Не указана"}";
            }

            // Открываем каталог книг по умолчанию (FR-2.2)
            MainFrame.Navigate(new CatalogPage(_currentUser));
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Каталог книг" в Sidebar.
        /// Выполняет навигацию на страницу каталога книг с передачей данных текущего пользователя.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Каталог книг")</param>
        /// <param name="e">Аргументы события</param>
        private void CatalogButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CatalogPage(_currentUser));
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Списки книг" в Sidebar.
        /// Выполняет навигацию на страницу управления личными списками книг пользователя.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Списки книг")</param>
        /// <param name="e">Аргументы события</param>
        private void ListsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new BookListsPage(_currentUser));
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Профиль" в Sidebar.
        /// Выполняет навигацию на страницу профиля пользователя с отображением
        /// личной информации, списка отзывов и предупреждения о заморозке (если аккаунт заморожен).
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Профиль")</param>
        /// <param name="e">Аргументы события</param>
        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new UserProfilePage(_currentUser));
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Администрирование" в Sidebar.
        /// Выполняет навигацию на страницу администрирования, доступную только администраторам (FR-2.4).
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Администрирование")</param>
        /// <param name="e">Аргументы события</param>
        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AdminPage(_currentUser));
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку предупреждения о заморозке в Sidebar (FR-2.5).
        /// Выполняет переход на страницу профиля, где отображается причина заморозки
        /// и возможность оспорить заморозку аккаунта (FR-6.4).
        /// </summary>
        /// <param name="sender">Источник события (кнопка предупреждения о заморозке)</param>
        /// <param name="e">Аргументы события</param>
        private void FrozenWarningButton_Click(object sender, RoutedEventArgs e)
        {
            // Переходим на страницу профиля, где будет видна причина заморозки
            MainFrame.Navigate(new UserProfilePage(_currentUser));
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Выйти" в Sidebar.
        /// Запрашивает подтверждение выхода у пользователя. При подтверждении
        /// создаёт новое окно для страницы входа и закрывает текущее главное окно.
        /// Используется создание нового окна вместо навигации, чтобы полностью
        /// очистить контекст авторизованной сессии.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Выйти")</param>
        /// <param name="e">Аргументы события</param>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Запрашиваем подтверждение выхода
            var result = MessageBox.Show(
                "Вы действительно хотите выйти?",
                "Выход из системы",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Создаём новое окно для страницы входа
                var loginWindow = new Window
                {
                    Title = "Онлайн библиотека - Вход",
                    Width = 500,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Style = (Style)FindResource("MainWindowStyle")
                };

                var frame = new System.Windows.Controls.Frame
                {
                    NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden
                };

                frame.Navigate(new LoginPage());
                loginWindow.Content = frame;
                loginWindow.Show();

                // Закрываем текущее главное окно
                this.Close();
            }
        }
    }
}