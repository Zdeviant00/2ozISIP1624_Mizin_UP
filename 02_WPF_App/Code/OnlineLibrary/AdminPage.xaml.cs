using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OnlineLibrary
{
    /// <summary>
    /// Страница администрирования системы "Онлайн библиотека".
    /// Доступна только пользователям с ролью "Администратор" (RoleId = 2).
    /// Предоставляет четыре вкладки для управления системой:
    /// 1) "Жалобы" — рассмотрение жалоб на книги и отзывы с возможностью принять или отклонить (FR-7.1 - FR-7.3);
    /// 2) "Заявки на разморозку" — рассмотрение заявок пользователей на снятие заморозки (FR-7.4 - FR-7.6);
    /// 3) "Замороженный контент" — просмотр списков замороженных книг, пользователей и отзывов (FR-7.7 - FR-7.9);
    /// 4) "Пользователи" — управление учётными записями: смена роли и пароля (FR-7.10 - FR-7.12).
    /// </summary>
    public partial class AdminPage : Page
    {
        /// <summary>
        /// Текущий авторизованный пользователь (администратор).
        /// Используется для проверки прав доступа.
        /// </summary>
        private Users _currentUser;

        /// <summary>
        /// Единственный экземпляр контекста базы данных (Singleton).
        /// </summary>
        private OnlineLibraryEntities _context;

        /// <summary>
        /// Имя текущей активной вкладки админки.
        /// Допустимые значения: "Complaints", "FreezeRequests", "FrozenContent", "Users".
        /// По умолчанию установлена вкладка "Жалобы" (Complaints).
        /// </summary>
        private string _currentTab = "Complaints";

        /// <summary>
        /// Конструктор страницы администрирования.
        /// Инициализирует компоненты, получает контекст БД через Singleton,
        /// подсвечивает кнопку "Жалобы" по умолчанию и загружает данные.
        /// </summary>
        /// <param name="user">Текущий авторизованный пользователь (администратор)</param>
        public AdminPage(Users user)
        {
            InitializeComponent();
            _currentUser = user;
            // Получаем единственный экземпляр контекста базы данных (Singleton)
            _context = OnlineLibraryEntities.GetContext();

            // Подсвечиваем кнопку "Жалобы" по умолчанию (FR-7.1)
            BtnComplaints.Style = (Style)FindResource("ActiveTabButton");

            LoadData();
        }

        /// <summary>
        /// Загружает данные для текущей активной вкладки.
        /// В зависимости от значения <see cref="_currentTab"/> вызывает соответствующий метод загрузки:
        /// LoadComplaints, LoadFreezeRequests, LoadFrozenContent или LoadUsers.
        /// </summary>
        private void LoadData()
        {
            switch (_currentTab)
            {
                case "Complaints":
                    LoadComplaints();
                    break;
                case "FreezeRequests":
                    LoadFreezeRequests();
                    break;
                case "FrozenContent":
                    LoadFrozenContent();
                    break;
                case "Users":
                    LoadUsers();
                    break;
            }
        }

        /// <summary>
        /// Загружает список жалоб со статусом "Pending" (ожидающие рассмотрения)
        /// и отображает их в таблице ComplaintsDataGrid (FR-7.1).
        /// Для каждой жалобы отображаются: идентификатор, тип (книга/отзыв),
        /// идентификатор объекта, логин заявителя, причина и статус.
        /// </summary>
        private void LoadComplaints()
        {
            var complaints = _context.Complaints
                .Where(c => c.Status == "Pending")
                .Select(c => new
                {
                    c.Id,
                    c.Type,
                    c.TargetId,
                    UserName = c.Users.Login,
                    c.Reason,
                    c.Status
                }).ToList();

            ComplaintsDataGrid.ItemsSource = complaints;
        }

        /// <summary>
        /// Загружает список заявок на снятие заморозки со статусом "Pending"
        /// и отображает их в таблице FreezeRequestsDataGrid (FR-7.4).
        /// Для каждой заявки отображаются: идентификатор, тип (аккаунт/книга/отзыв),
        /// логин заявителя, причина и статус.
        /// </summary>
        private void LoadFreezeRequests()
        {
            var requests = _context.FreezeRequests
                .Where(r => r.Status == "Pending")
                .Select(r => new
                {
                    r.Id,
                    r.Type,
                    UserName = r.Users.Login,
                    r.Reason,
                    r.Status
                }).ToList();

            FreezeRequestsDataGrid.ItemsSource = requests;
        }

        /// <summary>
        /// Загружает и отображает весь замороженный контент в таблице FrozenContentDataGrid (FR-7.7 - FR-7.9).
        /// Включает три типа объектов:
        /// 1) Замороженные книги (IsFrozen = true) — FR-7.7;
        /// 2) Замороженные пользователи (IsFrozen = true) — FR-7.8;
        /// 3) Замороженные отзывы (IsFrozen = true) — FR-7.9.
        /// Все объекты объединяются в единый список с указанием типа, идентификатора,
        /// названия и причины заморозки.
        /// </summary>
        private void LoadFrozenContent()
        {
            // Загружаем замороженные книги (FR-7.7)
            var frozenBooks = _context.Books
                .Where(b => b.IsFrozen)
                .Select(b => new
                {
                    Type = "Книга",
                    b.Id,
                    Title = b.Title,
                    Reason = "Заморожено"
                }).ToList();

            // Загружаем замороженных пользователей (FR-7.8)
            var frozenUsers = _context.Users
                .Where(u => u.IsFrozen)
                .Select(u => new
                {
                    Type = "Пользователь",
                    u.Id,
                    Title = u.Login,
                    Reason = u.FreezeReason ?? "Не указана"
                }).ToList();

            // Загружаем замороженные отзывы (FR-7.9)
            var frozenReviewsData = _context.Reviews
                .Where(r => r.IsFrozen)
                .ToList();

            var frozenReviews = frozenReviewsData.Select(r => new
            {
                Type = "Отзыв",
                r.Id,
                Title = "Отзыв на книгу " + (r.Books?.Title ?? "Неизвестная книга"),
                Reason = "Заморожено"
            }).ToList();

            // Объединяем все три типа замороженного контента в единый список
            var allFrozen = frozenBooks.Concat(frozenUsers).Concat(frozenReviews).ToList();
            FrozenContentDataGrid.ItemsSource = allFrozen;
        }

        /// <summary>
        /// Загружает список всех пользователей системы и отображает их
        /// в таблице UsersDataGrid (FR-7.10).
        /// Для каждого пользователя отображаются: идентификатор, логин, имя,
        /// email и название роли.
        /// </summary>
        private void LoadUsers()
        {
            var users = _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Login,
                    u.Name,
                    u.Email,
                    RoleName = u.Roles.Name
                }).ToList();

            UsersDataGrid.ItemsSource = users;
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку вкладки админки.
        /// Переключает активную вкладку, сбрасывает стили всех кнопок,
        /// подсвечивает выбранную вкладку, скрывает все Grid'ы и показывает нужный,
        /// после чего загружает данные для новой активной вкладки.
        /// </summary>
        /// <param name="sender">Источник события (кнопка вкладки)</param>
        /// <param name="e">Аргументы события</param>
        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            // Устанавливаем текущую вкладку по Tag кнопки
            _currentTab = button.Tag.ToString();

            // Сбрасываем стиль всех кнопок к стандартному
            BtnComplaints.Style = (Style)FindResource("AccentButton");
            BtnFreezeRequests.Style = (Style)FindResource("AccentButton");
            BtnFrozenContent.Style = (Style)FindResource("AccentButton");
            BtnUsers.Style = (Style)FindResource("AccentButton");

            // Подсвечиваем активную кнопку
            button.Style = (Style)FindResource("ActiveTabButton");

            // Скрываем все Grid'ы с содержимым вкладок
            ComplaintsGrid.Visibility = Visibility.Collapsed;
            FreezeRequestsGrid.Visibility = Visibility.Collapsed;
            FrozenContentGrid.Visibility = Visibility.Collapsed;
            UsersGrid.Visibility = Visibility.Collapsed;

            // Показываем Grid нужной вкладки
            switch (_currentTab)
            {
                case "Complaints":
                    ComplaintsGrid.Visibility = Visibility.Visible;
                    break;
                case "FreezeRequests":
                    FreezeRequestsGrid.Visibility = Visibility.Visible;
                    break;
                case "FrozenContent":
                    FrozenContentGrid.Visibility = Visibility.Visible;
                    break;
                case "Users":
                    UsersGrid.Visibility = Visibility.Visible;
                    break;
            }

            // Загружаем данные для новой активной вкладки
            LoadData();
        }

        /// <summary>
        /// Обработчик кнопки "Принять" для жалобы (FR-7.2).
        /// Изменяет статус жалобы на "Accepted" и замораживает связанный объект:
        /// - для жалобы типа "Book" — замораживает книгу (IsFrozen = true);
        /// - для жалобы типа "Review" — замораживает отзыв (IsFrozen = true).
        /// После сохранения изменений перезагружает список жалоб.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Принять")</param>
        /// <param name="e">Аргументы события</param>
        private void AcceptComplaint_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int complaintId = (int)button.Tag;

            var complaint = _context.Complaints.FirstOrDefault(c => c.Id == complaintId);
            if (complaint == null) return;

            // Устанавливаем статус "Принято"
            complaint.Status = "Accepted";

            // Замораживаем объект, на который подана жалоба
            if (complaint.Type == "Book")
            {
                var book = _context.Books.FirstOrDefault(b => b.Id == complaint.TargetId);
                if (book != null)
                {
                    book.IsFrozen = true;
                }
            }
            else if (complaint.Type == "Review")
            {
                var review = _context.Reviews.FirstOrDefault(r => r.Id == complaint.TargetId);
                if (review != null)
                {
                    review.IsFrozen = true;
                }
            }

            _context.SaveChanges();
            MessageBox.Show("Жалоба принята. Объект заморожен.");
            LoadComplaints();
        }

        /// <summary>
        /// Обработчик кнопки "Отклонить" для жалобы (FR-7.3).
        /// Изменяет статус жалобы на "Rejected" без заморозки связанного объекта.
        /// После сохранения изменений перезагружает список жалоб.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Отклонить")</param>
        /// <param name="e">Аргументы события</param>
        private void RejectComplaint_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int complaintId = (int)button.Tag;

            var complaint = _context.Complaints.FirstOrDefault(c => c.Id == complaintId);
            if (complaint == null) return;

            // Устанавливаем статус "Отклонено"
            complaint.Status = "Rejected";
            _context.SaveChanges();
            MessageBox.Show("Жалоба отклонена.");
            LoadComplaints();
        }

        /// <summary>
        /// Обработчик кнопки "Принять" для заявки на разморозку (FR-7.5).
        /// Изменяет статус заявки на "Accepted" и размораживает связанный объект:
        /// - для заявки типа "Account" — размораживает пользователя (IsFrozen = false, FreezeReason = null);
        /// - для заявки типа "Book" — размораживает книгу (IsFrozen = false);
        /// - для заявки типа "Review" — размораживает отзыв (IsFrozen = false).
        /// После сохранения изменений перезагружает список заявок.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Принять")</param>
        /// <param name="e">Аргументы события</param>
        private void AcceptFreezeRequest_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int requestId = (int)button.Tag;

            var request = _context.FreezeRequests.FirstOrDefault(r => r.Id == requestId);
            if (request == null) return;

            // Устанавливаем статус "Принято"
            request.Status = "Accepted";

            // Размораживаем объект в зависимости от типа заявки
            if (request.Type == "Account")
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == request.TargetId);
                if (user != null)
                {
                    user.IsFrozen = false;
                    user.FreezeReason = null;
                }
            }
            else if (request.Type == "Book")
            {
                var book = _context.Books.FirstOrDefault(b => b.Id == request.TargetId);
                if (book != null)
                {
                    book.IsFrozen = false;
                }
            }
            else if (request.Type == "Review")
            {
                var review = _context.Reviews.FirstOrDefault(r => r.Id == request.TargetId);
                if (review != null)
                {
                    review.IsFrozen = false;
                }
            }

            _context.SaveChanges();
            MessageBox.Show("Заявка принята. Объект разморожен.");
            LoadFreezeRequests();
        }

        /// <summary>
        /// Обработчик кнопки "Отклонить" для заявки на разморозку (FR-7.6).
        /// Изменяет статус заявки на "Rejected" без разморозки связанного объекта.
        /// После сохранения изменений перезагружает список заявок.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Отклонить")</param>
        /// <param name="e">Аргументы события</param>
        private void RejectFreezeRequest_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int requestId = (int)button.Tag;

            var request = _context.FreezeRequests.FirstOrDefault(r => r.Id == requestId);
            if (request == null) return;

            // Устанавливаем статус "Отклонено"
            request.Status = "Rejected";
            _context.SaveChanges();
            MessageBox.Show("Заявка отклонена.");
            LoadFreezeRequests();
        }

        /// <summary>
        /// Обработчик кнопки "Сменить роль" для пользователя (FR-7.11).
        /// Переключает роль пользователя между "Пользователь" (RoleId = 1)
        /// и "Администратор" (RoleId = 2).
        /// Реализует защиту последнего администратора: если пользователь единственный админ,
        /// смена роли запрещается с выводом предупреждающего сообщения.
        /// Требует подтверждения действия через диалоговое окно.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Сменить роль")</param>
        /// <param name="e">Аргументы события</param>
        private void ChangeRole_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int userId = (int)button.Tag;

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return;

            // Защита последнего администратора: нельзя понизить роль единственного админа
            if (user.RoleId == 2)
            {
                // Считаем количество администраторов в системе
                int adminCount = _context.Users.Count(u => u.RoleId == 2);

                if (adminCount <= 1)
                {
                    MessageBox.Show(
                        "Нельзя изменить роль единственного администратора!\nВ системе должен быть хотя бы один админ.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            // Определяем новую роль (переключение между 1 и 2)
            var newRoleId = user.RoleId == 1 ? 2 : 1;
            var roleName = newRoleId == 1 ? "Пользователь" : "Администратор";

            // Запрос подтверждения смены роли
            var result = MessageBox.Show(
                $"Изменить роль пользователя {user.Login} на \"{roleName}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            // Применяем новую роль
            user.RoleId = newRoleId;
            _context.SaveChanges();
            MessageBox.Show($"Роль пользователя изменена на \"{roleName}\"");
            LoadUsers();
        }

        /// <summary>
        /// Обработчик кнопки "Сменить пароль" для пользователя (FR-7.12).
        /// Открывает диалоговое окно для ввода нового пароля,
        /// выполняет валидацию (пароль не может быть пустым)
        /// и сохраняет новый пароль в базе данных.
        /// После успешной смены пароля перезагружает список пользователей.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Сменить пароль")</param>
        /// <param name="e">Аргументы события</param>
        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int userId = (int)button.Tag;

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return;

            // Создаём диалоговое окно для ввода нового пароля
            var passwordWindow = new Window
            {
                Title = "Смена пароля",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var label = new TextBlock
            {
                Text = $"Введите новый пароль для {user.Login}:",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            var passwordBox = new PasswordBox
            {
                Height = 30,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Панель с кнопками "Отмена" и "OK"
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 10, 0)
            };

            var okButton = new Button
            {
                Content = "OK",
                Padding = new Thickness(10, 5, 10, 5)
            };

            bool? dialogResult = false;

            // Обработчик кнопки "Отмена"
            cancelButton.Click += (s, args) =>
            {
                dialogResult = false;
                passwordWindow.Close();
            };

            // Обработчик кнопки "OK" — сохранение нового пароля
            okButton.Click += (s, args) =>
            {
                string newPassword = passwordBox.Password.Trim();

                // Валидация: пароль не может быть пустым
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    MessageBox.Show("Пароль не может быть пустым!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // Сохраняем новый пароль
                    user.PasswordHash = newPassword;
                    _context.SaveChanges();
                    MessageBox.Show("Пароль изменён.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    dialogResult = true;
                    passwordWindow.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при смене пароля: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(passwordBox);
            stackPanel.Children.Add(buttonPanel);

            passwordWindow.Content = stackPanel;
            passwordWindow.ShowDialog();

            // Если пароль был успешно изменён, то перезагружаем список пользователей
            if (dialogResult == true)
            {
                LoadUsers();
            }
        }
    }
}