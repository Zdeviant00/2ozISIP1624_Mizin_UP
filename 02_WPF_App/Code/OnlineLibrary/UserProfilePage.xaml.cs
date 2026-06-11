using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OnlineLibrary
{
    /// <summary>
    /// Страница профиля пользователя.
    /// Отображает личную информацию пользователя (имя, логин, email, роль),
    /// предупреждение о заморозке аккаунта с возможностью оспорить заморозку,
    /// а также список всех отзывов пользователя с пометками о замороженных отзывах.
    /// Для администраторов раздел "Мои отзывы" скрыт, так как они не оставляют отзывов.
    /// Реализует требования FR-6.1 - FR-6.4.
    /// </summary>
    public partial class UserProfilePage : Page
    {
        /// <summary>
        /// Текущий авторизованный пользователь.
        /// Используется для получения персональных данных и проверки прав доступа.
        /// </summary>
        private Users _currentUser;

        /// <summary>
        /// Единственный экземпляр контекста базы данных (Singleton).
        /// </summary>
        private OnlineLibraryEntities _context;

        /// <summary>
        /// Конструктор страницы профиля.
        /// Инициализирует компоненты, получает контекст БД через Singleton и загружает данные пользователя.
        /// </summary>
        /// <param name="user">Текущий авторизованный пользователь</param>
        public UserProfilePage(Users user)
        {
            InitializeComponent();
            _currentUser = user;
            // Получаем единственный экземпляр контекста базы данных (Singleton)
            _context = OnlineLibraryEntities.GetContext();

            LoadUserData();
        }

        /// <summary>
        /// Загружает актуальные данные пользователя из базы данных и отображает их на странице.
        /// Включает отображение имени, логина, email и роли (FR-6.1).
        /// При наличии заморозки аккаунта отображает красное предупреждение с причиной (FR-6.3).
        /// Проверяет наличие активной заявки на разморозку и деактивирует кнопку при её наличии.
        /// Для администраторов скрывает раздел "Мои отзывы", для обычных пользователей — загружает отзывы (FR-6.2).
        /// </summary>
        private void LoadUserData()
        {
            try
            {
                // Загружаем актуальные данные пользователя из БД вместе с ролью и отзывами
                var user = _context.Users
                    .Include("Roles")
                    .Include("Reviews")
                    .FirstOrDefault(u => u.Id == _currentUser.Id);

                if (user == null)
                {
                    MessageBox.Show("Пользователь не найден!");
                    return;
                }

                // Отображаем личную информацию пользователя (FR-6.1)
                NameText.Text = user.Name ?? "Не указано";
                LoginText.Text = user.Login;
                EmailText.Text = user.Email ?? "Не указано";
                RoleText.Text = user.Roles?.Name ?? "Пользователь";

                // Проверяем заморозку аккаунта (FR-6.3)
                if (user.IsFrozen)
                {
                    // Отображаем красное предупреждение с причиной заморозки
                    FreezeWarningBorder.Visibility = Visibility.Visible;
                    FreezeReasonText.Text = $"Причина: {user.FreezeReason ?? "Не указана"}";

                    // Проверяем, есть ли уже активная заявка на разморозку
                    var existingRequest = _context.FreezeRequests
                        .FirstOrDefault(r => r.UserId == user.Id &&
                                           r.Type == "Account" &&
                                           r.TargetId == user.Id &&
                                           r.Status == "Pending");

                    if (existingRequest != null)
                    {
                        // Заявка уже отправлена — деактивируем кнопку
                        var disputeButton = FreezeWarningBorder.ChildOfType<Button>();
                        if (disputeButton != null)
                        {
                            disputeButton.Content = "Заявка уже отправлена (ожидает рассмотрения)";
                            disputeButton.IsEnabled = false;
                            disputeButton.Opacity = 0.6;
                        }
                    }
                }
                else
                {
                    // Аккаунт не заморожен — скрываем предупреждение
                    FreezeWarningBorder.Visibility = Visibility.Collapsed;
                }

                // Скрываем блок отзывов для администраторов (они не оставляют отзывов)
                if (user.RoleId == 2)
                {
                    ReviewsSeparator.Visibility = Visibility.Collapsed;
                    ReviewsHeaderText.Visibility = Visibility.Collapsed;
                    ReviewsItemsControl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Для обычных пользователей отображаем раздел отзывов (FR-6.2)
                    ReviewsSeparator.Visibility = Visibility.Visible;
                    ReviewsHeaderText.Visibility = Visibility.Visible;
                    ReviewsItemsControl.Visibility = Visibility.Visible;

                    LoadUserReviews(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает и отображает все отзывы текущего пользователя (FR-6.2).
        /// Для каждого отзыва загружает связанную книгу.
        /// Замороженные отзывы отображаются с пометкой "[ЗАМОРОЖЕН]" в тексте
        /// и с указанием "(отзыв заморожен)" в рейтинге.
        /// Если у пользователя нет отзывов, отображается соответствующее сообщение.
        /// </summary>
        /// <param name="user">Объект пользователя с загруженными отзывами</param>
        private void LoadUserReviews(Users user)
        {
            try
            {
                // Получаем все отзывы пользователя
                var reviews = user.Reviews.ToList();

                // Для каждого отзыва загружаем связанную книгу
                foreach (var review in reviews)
                {
                    _context.Entry(review).Reference(r => r.Books).Load();
                }

                // Формируем список отзывов для отображения с учётом статуса заморозки
                ReviewsItemsControl.ItemsSource = reviews.Select(r => new
                {
                    BookTitle = r.Books?.Title ?? "Неизвестная книга",
                    // Для замороженных отзывов добавляем пометку в тексте
                    ReviewText = r.IsFrozen
                        ? $"[ЗАМОРОЖЕН] {r.Text}"
                        : r.Text ?? "Без текста",
                    // Для замороженных отзывов добавляем пометку в рейтинге
                    RatingText = r.IsFrozen
                        ? $"⭐ {r.Rating} (отзыв заморожен)"
                        : $"Оценка: {r.Rating} ⭐"
                }).ToList();

                // Если у пользователя нет отзывов, то показываем соответствующее сообщение
                if (reviews.Count == 0)
                {
                    ReviewsItemsControl.ItemsSource = new List<object>
                    {
                        new { BookTitle = "У вас пока нет отзывов", ReviewText = "", RatingText = "" }
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отзывов: {ex.Message}");
                ReviewsItemsControl.ItemsSource = new List<object>();
            }
        }

        /// <summary>
        /// Обработчик кнопки "Оспорить заморозку" (FR-6.4).
        /// Открывает диалоговое окно для ввода причины оспаривания заморозки
        /// и создаёт заявку на снятие заморозки аккаунта в базе данных.
        /// Проверяет наличие уже активной заявки (со статусом "Pending")
        /// и предотвращает создание дубликатов.
        /// После успешной отправки заявки обновляет страницу, деактивируя кнопку.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Оспорить заморозку")</param>
        /// <param name="e">Аргументы события</param>
        private void DisputeFreeze_Click(object sender, RoutedEventArgs e)
        {
            // Создаём диалоговое окно для ввода причины оспаривания
            var inputWindow = new Window
            {
                Title = "Оспаривание заморозки",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var label = new TextBlock
            {
                Text = "Укажите причину оспаривания заморозки:",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            var textBox = new TextBox
            {
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var button = new Button
            {
                Content = "Отправить",
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Обработчик отправки заявки на разморозку
            button.Click += (s, args) =>
            {
                string reason = textBox.Text.Trim();

                // Валидация: причина не может быть пустой
                if (string.IsNullOrWhiteSpace(reason))
                {
                    MessageBox.Show("Необходимо указать причину!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // Проверяем, нет ли уже активной заявки на разморозку
                    var existingRequest = _context.FreezeRequests
                        .FirstOrDefault(r => r.UserId == _currentUser.Id &&
                                           r.Type == "Account" &&
                                           r.TargetId == _currentUser.Id &&
                                           r.Status == "Pending");

                    if (existingRequest != null)
                    {
                        MessageBox.Show(
                            "У вас уже есть активная заявка на снятие заморозки.\n" +
                            "Дождитесь рассмотрения администратором.",
                            "Заявка уже отправлена",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        inputWindow.Close();
                        return;
                    }

                    // Создаём новую заявку на снятие заморозки аккаунта (FR-6.4)
                    var freezeRequest = new FreezeRequests
                    {
                        UserId = _currentUser.Id,
                        Type = "Account",
                        TargetId = _currentUser.Id,
                        Reason = reason,
                        Status = "Pending"
                    };

                    _context.FreezeRequests.Add(freezeRequest);
                    _context.SaveChanges();

                    MessageBox.Show(
                        "Заявка на снятие заморозки отправлена!\n" +
                        "Администратор рассмотрит её в ближайшее время.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    inputWindow.Close();

                    // Обновляем страницу, кнопка станет неактивной
                    LoadUserData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при отправке заявки: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(button);

            inputWindow.Content = stackPanel;
            inputWindow.ShowDialog();
        }
    }

    /// <summary>
    /// Вспомогательный статический класс-расширение для поиска дочернего элемента заданного типа
    /// в визуальном дереве WPF. Используется для поиска элементов управления внутри контейнеров.
    /// </summary>
    public static class VisualTreeHelperExtensions
    {
        /// <summary>
        /// Находит первый дочерний элемент заданного типа в визуальном дереве.
        /// Выполняет рекурсивный обход всех дочерних элементов.
        /// </summary>
        /// <typeparam name="T">Тип искомого элемента (должен наследовать DependencyObject)</typeparam>
        /// <param name="parent">Родительский элемент, в котором выполняется поиск</param>
        /// <returns>Найденный дочерний элемент заданного типа или null, если элемент не найден</returns>
        public static T ChildOfType<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                // Если текущий элемент нужного типа, то возвращаем его
                if (child is T result)
                    return result;

                // Рекурсивно ищем в потомках
                var descendant = ChildOfType<T>(child);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }
    }
}