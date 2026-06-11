using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OnlineLibrary
{
    /// <summary>
    /// Страница с подробной информацией о книге, отзывами и функциями модерации.
    /// Предоставляет пользователю возможность просматривать полную информацию о книге,
    /// оставлять и редактировать отзывы, подавать жалобы на книги и отзывы.
    /// Для администраторов доступны функции заморозки книг и отзывов (FR-4.1 - FR-4.7).
    /// </summary>
    public partial class BookDetailPage : Page
    {
        /// <summary>
        /// Текущий авторизованный пользователь.
        /// Используется для проверки прав доступа и определения роли.
        /// </summary>
        private Users _currentUser;

        /// <summary>
        /// Единственный экземпляр контекста базы данных (Singleton).
        /// </summary>
        private OnlineLibraryEntities _context;

        /// <summary>
        /// Идентификатор текущей книги, отображаемой на странице.
        /// </summary>
        private int _bookId;

        /// <summary>
        /// Объект текущей книги, загруженный из базы данных.
        /// Содержит полную информацию о книге, включая жанры и отзывы.
        /// </summary>
        private Books _currentBook;

        /// <summary>
        /// Максимально допустимая длина текста отзыва в символах.
        /// Используется для валидации введённого текста отзыва.
        /// </summary>
        private const int MaxReviewLength = 1000;

        /// <summary>
        /// Конструктор страницы книги.
        /// Инициализирует компоненты, получает контекст БД через Singleton и загружает данные книги.
        /// </summary>
        /// <param name="user">Текущий авторизованный пользователь</param>
        /// <param name="bookId">Идентификатор книги для отображения</param>
        public BookDetailPage(Users user, int bookId)
        {
            InitializeComponent();
            _currentUser = user;
            // Получаем единственный экземпляр контекста базы данных (Singleton)
            _context = OnlineLibraryEntities.GetContext();
            _bookId = bookId;

            LoadBookData();
        }

        /// <summary>
        /// Отображает предупреждение для замороженного пользователя.
        /// Замороженные пользователи не могут оставлять отзывы, жалобы и выполнять другие активные действия.
        /// Предлагает оспорить заморозку в разделе "Профиль".
        /// </summary>
        private void ShowFrozenWarning()
        {
            MessageBox.Show(
                "Ваш аккаунт заморожен. Вы не можете выполнять это действие.\n" +
                "Оспорить заморозку можно в разделе «Профиль».",
                "Аккаунт заморожен",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        /// <summary>
        /// Загружает данные книги из базы данных и отображает их на странице.
        /// Включает название, автора, жанры, рейтинг, описание и список отзывов.
        /// Также настраивает видимость кнопок модерации для администратора.
        /// </summary>
        private void LoadBookData()
        {
            try
            {
                // Загружаем книгу с жанрами, отзывами и авторами отзывов
                _currentBook = _context.Books
                    .Include("Genres")
                    .Include("Reviews")
                    .Include("Reviews.Users")
                    .FirstOrDefault(b => b.Id == _bookId);

                if (_currentBook == null)
                {
                    MessageBox.Show("Книга не найдена!");
                    NavigationService.GoBack();
                    return;
                }

                // Заполняем основную информацию о книге
                TitleText.Text = _currentBook.Title;
                AuthorText.Text = _currentBook.AuthorName ?? "Неизвестен";
                GenresText.Text = _currentBook.Genres != null && _currentBook.Genres.Any()
                    ? string.Join(", ", _currentBook.Genres.Select(g => g.Name))
                    : "Без жанра";

                // Вычисляем и отображаем рейтинг книги (только по активным, не замороженным отзывам)
                if (_currentBook.Reviews != null && _currentBook.Reviews.Any(r => !r.IsFrozen))
                {
                    var activeReviews = _currentBook.Reviews.Where(r => !r.IsFrozen).ToList();
                    double avgRating = activeReviews.Average(r => r.Rating);
                    RatingText.Text = $"⭐ {avgRating:F1} ({activeReviews.Count} оценок)";
                }
                else
                {
                    RatingText.Text = "Нет оценок";
                }

                // Отображаем описание книги
                DescriptionText.Text = string.IsNullOrWhiteSpace(_currentBook.Description)
                    ? "Описание отсутствует"
                    : _currentBook.Description;

                // Предупреждение о заморозке книги (видно всем пользователям)
                if (_currentBook.IsFrozen)
                {
                    FrozenWarning.Visibility = Visibility.Visible;
                }

                // Настройка кнопки "Заморозить/Разморозить книгу" только для администратора (FR-4.6)
                if (_currentUser.RoleId == 2)
                {
                    FreezeBookButton.Visibility = Visibility.Visible;
                    FreezeBookButton.Content = _currentBook.IsFrozen
                        ? "✅ Разморозить книгу"
                        : "❄️ Заморозить книгу";
                }
                else
                {
                    FreezeBookButton.Visibility = Visibility.Collapsed;
                }

                // Загружаем отзывы с учётом роли пользователя
                LoadReviews();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки книги: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает и отображает список отзывов к текущей книге.
        /// Обычные пользователи видят только активные (не замороженные) отзывы.
        /// Администраторы видят все отзывы с индикатором заморозки и кнопками управления.
        /// Настраивает видимость кнопок действий в зависимости от роли пользователя и статуса отзыва.
        /// </summary>
        private void LoadReviews()
        {
            var allReviews = _currentBook.Reviews?.OrderByDescending(r => r.Id).ToList()
                ?? new List<Reviews>();

            bool isAdmin = _currentUser.RoleId == 2;

            // Фильтрация отзывов: админ видит все, обычные пользователи только активные
            var visibleReviews = isAdmin
                ? allReviews
                : allReviews.Where(r => !r.IsFrozen).ToList();

            var reviewData = visibleReviews.Select(r => new
            {
                Id = r.Id,
                UserName = r.Users?.Name ?? r.Users?.Login ?? "Неизвестный",
                Text = r.Text ?? "Без текста",
                RatingText = $"⭐ {r.Rating}",
                UserId = r.UserId,
                IsReviewFrozen = r.IsFrozen,

                // Индикатор заморозки отзыва (виден всем, если отзыв заморожен)
                FrozenIndicatorVisibility = r.IsFrozen ? Visibility.Visible : Visibility.Collapsed,

                // === ЛОГИКА ОТОБРАЖЕНИЯ КНОПОК ===
                // Кнопка "Пожаловаться" видна обычным пользователям на чужие активные отзывы
                ComplainButtonVisibility = (!isAdmin && r.UserId != _currentUser.Id && !r.IsFrozen)
                    ? Visibility.Visible : Visibility.Collapsed,
                // Кнопка "Редактировать" видна обычным пользователям на свои активные отзывы
                EditButtonVisibility = (!isAdmin && r.UserId == _currentUser.Id && !r.IsFrozen)
                    ? Visibility.Visible : Visibility.Collapsed,
                // Кнопка "Удалить" видна обычным пользователям на свои активные отзывы
                DeleteButtonVisibility = (!isAdmin && r.UserId == _currentUser.Id && !r.IsFrozen)
                    ? Visibility.Visible : Visibility.Collapsed,

                // Кнопка "Заморозить/Разморозить отзыв" видна только администратору (FR-4.7)
                AdminFreezeButtonVisibility = isAdmin ? Visibility.Visible : Visibility.Collapsed,
                FreezeButtonText = r.IsFrozen ? "✅ Разморозить" : "❄️ Заморозить"
            }).ToList();

            ReviewsItemsControl.ItemsSource = reviewData;

            // Показываем сообщение, если отзывов нет
            NoReviewsText.Visibility = reviewData.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Обработчик кнопки "Назад к каталогу".
        /// Возвращает пользователя на предыдущую страницу (каталог книг).
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Назад")</param>
        /// <param name="e">Аргументы события</param>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        /// <summary>
        /// Обработчик кнопки "Опубликовать отзыв".
        /// Создаёт новый отзыв или обновляет существующий.
        /// Выполняет комплексную валидацию: проверка заморозки аккаунта, роли пользователя,
        /// заморозки книги, длины текста, выбора оценки и заморозки существующего отзыва.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Опубликовать отзыв")</param>
        /// <param name="e">Аргументы события</param>
        private void SubmitReview_Click(object sender, RoutedEventArgs e)
        {
            // === Проверка 1: замороженный аккаунт ===
            if (_currentUser.IsFrozen)
            {
                ShowFrozenWarning();
                return;
            }

            // === Проверка 2: администраторы не могут оставлять отзывы ===
            if (_currentUser.RoleId == 2)
            {
                MessageBox.Show(
                    "Администраторы не могут оставлять отзывы.\n" +
                    "Ваша роль — модерация контента.",
                    "Доступ запрещён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // === Проверка 3: нельзя оставить отзыв на замороженную книгу ===
            if (_currentBook.IsFrozen)
            {
                MessageBox.Show(
                    "Нельзя оставить отзыв на замороженную книгу.\n" +
                    "Книга временно недоступна для комментирования.",
                    "Книга заморожена",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string reviewText = ReviewTextBox.Text.Trim();
            var ratingItem = RatingComboBox.SelectedItem as ComboBoxItem;

            // === Проверка 4: пустой текст отзыва ===
            if (string.IsNullOrWhiteSpace(reviewText))
            {
                MessageBox.Show("Введите текст отзыва!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // === Проверка 5: длина отзыва (валидация MaxReviewLength) ===
            if (reviewText.Length > MaxReviewLength)
            {
                MessageBox.Show(
                    $"Текст отзыва слишком длинный!\n\n" +
                    $"Максимальная длина: {MaxReviewLength} символов.\n" +
                    $"Ваш текст: {reviewText.Length} символов.\n\n" +
                    "Пожалуйста, сократите отзыв.",
                    "Превышена максимальная длина",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // === Проверка 6: не выбрана оценка ===
            if (ratingItem?.Tag == null)
            {
                MessageBox.Show("Выберите оценку!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int rating = int.Parse(ratingItem.Tag.ToString());

            try
            {
                // Проверяем, не оставлял ли пользователь уже отзыв на эту книгу
                var existingReview = _context.Reviews
                    .FirstOrDefault(r => r.BookId == _bookId && r.UserId == _currentUser.Id);

                if (existingReview != null)
                {
                    // === Проверка 7: существующий отзыв заморожен ===
                    if (existingReview.IsFrozen)
                    {
                        MessageBox.Show(
                            "Ваш отзыв заморожен модератором. Вы не можете его изменить.\n" +
                            "Для уточнения информации обратитесь к администратору.",
                            "Отзыв заморожен",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Предлагаем обновить существующий отзыв
                    var result = MessageBox.Show(
                        "Вы уже оставляли отзыв на эту книгу. Обновить его?",
                        "Отзыв уже существует",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return;

                    existingReview.Text = reviewText;
                    existingReview.Rating = rating;
                }
                else
                {
                    // Создаём новый отзыв
                    var newReview = new Reviews
                    {
                        BookId = _bookId,
                        UserId = _currentUser.Id,
                        Text = reviewText,
                        Rating = rating,
                        IsFrozen = false
                    };
                    _context.Reviews.Add(newReview);
                }

                _context.SaveChanges();
                MessageBox.Show("Отзыв опубликован!");

                // Очищаем форму
                ReviewTextBox.Text = "";
                RatingComboBox.SelectedIndex = 0;

                // Перезагружаем данные страницы
                LoadBookData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при публикации отзыва: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик кнопки "Пожаловаться на отзыв" (FR-4.5).
        /// Открывает диалоговое окно для ввода причины жалобы и создаёт запись в таблице Complaints.
        /// Запрещено для замороженных пользователей. Проверяет наличие предыдущей жалобы.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Пожаловаться")</param>
        /// <param name="e">Аргументы события</param>
        private void ComplainReview_Click(object sender, RoutedEventArgs e)
        {
            // Проверка заморозки аккаунта
            if (_currentUser.IsFrozen)
            {
                ShowFrozenWarning();
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;

            int reviewId = (int)button.Tag;

            // Создаём диалоговое окно для ввода причины жалобы
            var reasonWindow = new Window
            {
                Title = "Пожаловаться на отзыв",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            var label = new TextBlock
            {
                Text = "Укажите причину жалобы:",
                Margin = new Thickness(0, 0, 0, 10)
            };
            var reasonBox = new TextBox
            {
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var submitBtn = new Button
            {
                Content = "Отправить жалобу",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Обработчик отправки жалобы
            submitBtn.Click += (s, args) =>
            {
                string reason = reasonBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(reason))
                {
                    MessageBox.Show("Укажите причину жалобы!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // Проверяем, не подавал ли пользователь уже жалобу на этот отзыв
                    var existingComplaint = _context.Complaints
                        .FirstOrDefault(c => c.Type == "Review" &&
                                           c.TargetId == reviewId &&
                                           c.UserId == _currentUser.Id &&
                                           c.Status == "Pending");

                    if (existingComplaint != null)
                    {
                        MessageBox.Show("Вы уже подавали жалобу на этот отзыв!", "Внимание",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        reasonWindow.Close();
                        return;
                    }

                    // Создаём новую жалобу на отзыв
                    var complaint = new Complaints
                    {
                        Type = "Review",
                        TargetId = reviewId,
                        UserId = _currentUser.Id,
                        Reason = reason,
                        Status = "Pending"
                    };
                    _context.Complaints.Add(complaint);
                    _context.SaveChanges();

                    MessageBox.Show("Жалоба отправлена на рассмотрение!");
                    reasonWindow.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            };

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(reasonBox);
            stackPanel.Children.Add(submitBtn);
            reasonWindow.Content = stackPanel;
            reasonWindow.ShowDialog();
        }

        /// <summary>
        /// Обработчик кнопки "Пожаловаться на книгу" (FR-4.3).
        /// Открывает диалоговое окно для ввода причины жалобы и создаёт запись в таблице Complaints.
        /// Запрещено для замороженных пользователей и на уже замороженные книги.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Пожаловаться на книгу")</param>
        /// <param name="e">Аргументы события</param>
        private void ComplainBook_Click(object sender, RoutedEventArgs e)
        {
            // Проверка заморозки аккаунта
            if (_currentUser.IsFrozen)
            {
                ShowFrozenWarning();
                return;
            }

            // Проверка: нельзя пожаловаться на уже замороженную книгу
            if (_currentBook.IsFrozen)
            {
                MessageBox.Show(
                    "Эта книга уже заморожена модератором.",
                    "Книга заморожена",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Создаём диалоговое окно для ввода причины жалобы
            var reasonWindow = new Window
            {
                Title = "Пожаловаться на книгу",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            var label = new TextBlock
            {
                Text = "Укажите причину жалобы на книгу:",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            var reasonBox = new TextBox
            {
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var submitBtn = new Button
            {
                Content = "Отправить жалобу",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Обработчик отправки жалобы
            submitBtn.Click += (s, args) =>
            {
                string reason = reasonBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(reason))
                {
                    MessageBox.Show("Укажите причину жалобы!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // Проверяем, не подавал ли пользователь уже жалобу на эту книгу
                    var existingComplaint = _context.Complaints
                        .FirstOrDefault(c => c.Type == "Book" &&
                                           c.TargetId == _bookId &&
                                           c.UserId == _currentUser.Id &&
                                           c.Status == "Pending");

                    if (existingComplaint != null)
                    {
                        MessageBox.Show(
                            "Вы уже подавали жалобу на эту книгу!\nДождитесь рассмотрения администратором.",
                            "Внимание",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        reasonWindow.Close();
                        return;
                    }

                    // Создаём новую жалобу на книгу
                    var complaint = new Complaints
                    {
                        Type = "Book",
                        TargetId = _bookId,
                        UserId = _currentUser.Id,
                        Reason = reason,
                        Status = "Pending"
                    };
                    _context.Complaints.Add(complaint);
                    _context.SaveChanges();

                    MessageBox.Show("Жалоба на книгу отправлена на рассмотрение!");
                    reasonWindow.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            };

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(reasonBox);
            stackPanel.Children.Add(submitBtn);
            reasonWindow.Content = stackPanel;
            reasonWindow.ShowDialog();
        }

        /// <summary>
        /// Обработчик кнопки "Заморозить/Разморозить книгу" (FR-4.6).
        /// Доступна только администраторам. Переключает статус IsFrozen у книги.
        /// Требует подтверждения действия через диалоговое окно.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Заморозить/Разморозить книгу")</param>
        /// <param name="e">Аргументы события</param>
        private void FreezeBook_Click(object sender, RoutedEventArgs e)
        {
            // Проверка прав администратора
            if (_currentUser.RoleId != 2)
            {
                MessageBox.Show(
                    "Только администратор может замораживать книги!",
                    "Доступ запрещён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string action = _currentBook.IsFrozen ? "разморозить" : "заморозить";
            var result = MessageBox.Show(
                $"Вы действительно хотите {action} книгу \"{_currentBook.Title}\"?",
                "Подтверждение действия",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Переключаем статус заморозки книги
                _currentBook.IsFrozen = !_currentBook.IsFrozen;
                _context.SaveChanges();

                string message = _currentBook.IsFrozen
                    ? "Книга успешно заморожена."
                    : "Книга успешно разморожена.";

                MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Обновляем видимость предупреждения о заморозке
                FrozenWarning.Visibility = _currentBook.IsFrozen ? Visibility.Visible : Visibility.Collapsed;

                // Перезагружаем данные страницы
                LoadBookData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении статуса книги: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик кнопки "Заморозить/Разморозить отзыв" (FR-4.7).
        /// Доступна только администраторам. Переключает статус IsFrozen у отзыва.
        /// Требует подтверждения действия через диалоговое окно.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Заморозить/Разморозить отзыв")</param>
        /// <param name="e">Аргументы события</param>
        private void FreezeReview_Click(object sender, RoutedEventArgs e)
        {
            // Проверка прав администратора
            if (_currentUser.RoleId != 2)
            {
                MessageBox.Show(
                    "Только администратор может замораживать отзывы!",
                    "Доступ запрещён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;

            int reviewId = (int)button.Tag;

            try
            {
                var review = _context.Reviews.FirstOrDefault(r => r.Id == reviewId);
                if (review == null)
                {
                    MessageBox.Show("Отзыв не найден!");
                    return;
                }

                string action = review.IsFrozen ? "разморозить" : "заморозить";
                var result = MessageBox.Show(
                    $"Вы действительно хотите {action} этот отзыв?",
                    "Подтверждение действия",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                // Переключаем статус заморозки отзыва
                review.IsFrozen = !review.IsFrozen;
                _context.SaveChanges();

                string message = review.IsFrozen
                    ? "Отзыв успешно заморожен."
                    : "Отзыв успешно разморожен.";

                MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Перезагружаем данные страницы
                LoadBookData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении статуса отзыва: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик кнопки "Редактировать отзыв".
        /// Заполняет форму данными отзыва для последующего редактирования.
        /// Запрещено для замороженных пользователей и замороженных отзывов.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Редактировать")</param>
        /// <param name="e">Аргументы события</param>
        private void EditReview_Click(object sender, RoutedEventArgs e)
        {
            // Проверка заморозки аккаунта
            if (_currentUser.IsFrozen)
            {
                ShowFrozenWarning();
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;

            int reviewId = (int)button.Tag;

            var review = _context.Reviews.FirstOrDefault(r => r.Id == reviewId);
            if (review == null) return;

            // Проверка: нельзя редактировать замороженный отзыв
            if (review.IsFrozen)
            {
                MessageBox.Show(
                    "Этот отзыв заморожен модератором. Вы не можете его редактировать.",
                    "Отзыв заморожен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Заполняем форму данными отзыва
            ReviewTextBox.Text = review.Text;
            RatingComboBox.SelectedIndex = 5 - review.Rating;

            ReviewTextBox.Focus();

            MessageBox.Show("Отредактируйте отзыв в форме выше и нажмите 'Опубликовать отзыв'");
        }

        /// <summary>
        /// Обработчик кнопки "Удалить отзыв".
        /// Удаляет отзыв текущего пользователя после подтверждения.
        /// Запрещено для замороженных пользователей.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Удалить")</param>
        /// <param name="e">Аргументы события</param>
        private void DeleteReview_Click(object sender, RoutedEventArgs e)
        {
            // Проверка заморозки аккаунта
            if (_currentUser.IsFrozen)
            {
                ShowFrozenWarning();
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;

            int reviewId = (int)button.Tag;

            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить свой отзыв?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var review = _context.Reviews.FirstOrDefault(r => r.Id == reviewId);
                if (review != null)
                {
                    _context.Reviews.Remove(review);
                    _context.SaveChanges();
                    MessageBox.Show("Отзыв удалён!");
                    LoadBookData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик выбора списка для добавления книги.
        /// Реализует логику "книга только в одном списке":
        /// перед добавлением в новый список книга удаляется из всех остальных.
        /// </summary>
        /// <param name="sender">Источник события (ComboBox добавления в список)</param>
        /// <param name="e">Аргументы события</param>
        private void AddToListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem == null) return;

            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag == null || selectedItem.Content.ToString() == "➕ Добавить в список...")
                return;

            string listName = selectedItem.Tag.ToString();

            try
            {
                // Перезагружаем пользователя с его списками
                var currentUserFromDb = _context.Users
                    .Where(u => u.Id == _currentUser.Id)
                    .Include("BookLists")
                    .FirstOrDefault();

                if (currentUserFromDb == null)
                {
                    MessageBox.Show("Пользователь не найден!");
                    comboBox.SelectedIndex = 0;
                    return;
                }

                // Загружаем книги для каждого списка
                foreach (var list in currentUserFromDb.BookLists)
                {
                    _context.Entry(list).Collection(bl => bl.Books).Load();
                }

                // Удаляем книгу из всех списков пользователя
                foreach (var list in currentUserFromDb.BookLists)
                {
                    var bookInList = list.Books.FirstOrDefault(b => b.Id == _bookId);
                    if (bookInList != null)
                    {
                        list.Books.Remove(bookInList);
                    }
                }

                // Находим или создаём целевой список
                var targetList = currentUserFromDb.BookLists.FirstOrDefault(bl => bl.Name == listName);
                if (targetList == null)
                {
                    targetList = new BookLists
                    {
                        Name = listName,
                        UserId = _currentUser.Id
                    };
                    _context.BookLists.Add(targetList);
                    _context.SaveChanges();

                    targetList = currentUserFromDb.BookLists.FirstOrDefault(bl => bl.Name == listName);
                    _context.Entry(targetList).Collection(bl => bl.Books).Load();
                }

                // Добавляем книгу в целевой список
                targetList.Books.Add(_currentBook);
                _context.SaveChanges();

                MessageBox.Show($"Книга добавлена в список \"{listName}\"!");
                comboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                comboBox.SelectedIndex = 0;
            }
        }
    }
}