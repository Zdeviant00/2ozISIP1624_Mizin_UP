using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OnlineLibrary
{
    /// <summary>
    /// Страница каталога книг.
    /// Предоставляет пользователю возможность просматривать все доступные книги,
    /// выполнять поиск по названию и автору, сортировать по названию и рейтингу,
    /// фильтровать по жанрам, а также добавлять книги в личные списки.
    /// Реализует требования FR-3.1 - FR-3.8.
    /// </summary>
    public partial class CatalogPage : Page
    {
        /// <summary>
        /// Текущий авторизованный пользователь.
        /// </summary>
        private Users _currentUser;

        /// <summary>
        /// Единственный экземпляр контекста базы данных (Singleton).
        /// </summary>
        private OnlineLibraryEntities _context;

        /// <summary>
        /// Полный список всех книг, загруженных из базы данных.
        /// Используется как источник для фильтрации и сортировки без повторных запросов к БД.
        /// </summary>
        private List<Books> _allBooks;

        /// <summary>
        /// Полный список всех жанров, используемых для заполнения фильтра.
        /// </summary>
        private List<Genres> _allGenres;

        /// <summary>
        /// Текущий тип сортировки: "Title" (по названию) или "Rating" (по рейтингу).
        /// </summary>
        private string _currentSortType = "Title";

        /// <summary>
        /// Идентификатор выбранного жанра для фильтрации.
        /// Значение null означает, что фильтр по жанру не применён.
        /// </summary>
        private int? _currentGenreFilter = null;

        /// <summary>
        /// Текущая строка поиска по названию книги.
        /// </summary>
        private string _currentSearchTitle = "";

        /// <summary>
        /// Текущая строка поиска по автору книги.
        /// </summary>
        private string _currentSearchAuthor = "";

        /// <summary>
        /// Конструктор страницы каталога.
        /// Инициализирует компоненты, получает контекст БД через Singleton и загружает данные.
        /// </summary>
        /// <param name="user">Текущий авторизованный пользователь</param>
        public CatalogPage(Users user)
        {
            InitializeComponent();
            _currentUser = user;
            // Получаем единственный экземпляр контекста базы данных (Singleton)
            _context = OnlineLibraryEntities.GetContext();

            LoadData();
        }

        /// <summary>
        /// Загружает все книги, жанры и отзывы из базы данных.
        /// Заполняет ComboBox жанров для фильтрации и отображает книги по умолчанию.
        /// </summary>
        private void LoadData()
        {
            // Загружаем все книги из базы данных
            _allBooks = _context.Books.ToList();

            // Загружаем жанры и отзывы для каждой книги (ленивая загрузка)
            foreach (var book in _allBooks)
            {
                _context.Entry(book).Collection(b => b.Genres).Load();
                _context.Entry(book).Collection(b => b.Reviews).Load();
            }

            // Загружаем все жанры для фильтра
            _allGenres = _context.Genres.ToList();

            // Добавляем жанры в ComboBox фильтра
            foreach (var genre in _allGenres)
            {
                GenreFilterComboBox.Items.Add(new ComboBoxItem
                {
                    Content = genre.Name,
                    Tag = genre.Id
                });
            }

            // Показываем плейсхолдеры по умолчанию
            SortPlaceholder.Visibility = Visibility.Visible;
            GenrePlaceholder.Visibility = Visibility.Visible;

            // Отображаем все книги
            DisplayBooks(_allBooks);
        }

        /// <summary>
        /// Отображает список книг в интерфейсе с помощью ItemsControl.
        /// Формирует анонимный объект с данными для каждой книги.
        /// </summary>
        /// <param name="books">Список книг для отображения</param>
        private void DisplayBooks(List<Books> books)
        {
            BooksItemsControl.ItemsSource = books.Select(b => new
            {
                b.Id,
                b.Title,
                b.AuthorName,
                Genres = b.Genres != null && b.Genres.Any()
                    ? string.Join(", ", b.Genres.Select(g => g.Name))
                    : "Без жанра",
                RatingText = GetRatingText(b),
                AverageRating = GetAverageRating(b)
            }).ToList();
        }

        /// <summary>
        /// Формирует текстовое представление рейтинга книги для отображения в интерфейсе.
        /// </summary>
        /// <param name="book">Книга, для которой вычисляется рейтинг</param>
        /// <returns>Строка вида "⭐ 4.5 (10 оценок)" или "Нет оценок"</returns>
        private string GetRatingText(Books book)
        {
            if (book.Reviews == null || !book.Reviews.Any())
                return "Нет оценок";

            double avgRating = book.Reviews.Average(r => r.Rating);
            return $"⭐ {avgRating:F1} ({book.Reviews.Count} оценок)";
        }

        /// <summary>
        /// Вычисляет средний рейтинг книги на основе всех её отзывов.
        /// Используется для сортировки книг по оценке.
        /// </summary>
        /// <param name="book">Книга, для которой вычисляется средний рейтинг</param>
        /// <returns>Средний рейтинг (0, если отзывов нет)</returns>
        private double GetAverageRating(Books book)
        {
            if (book.Reviews == null || !book.Reviews.Any())
                return 0;

            return book.Reviews.Average(r => r.Rating);
        }

        /// <summary>
        /// Применяет все активные фильтры (поиск по названию, поиск по автору, фильтр по жанру)
        /// и сортировку к списку книг, после чего обновляет отображение.
        /// </summary>
        private void ApplyFiltersAndSort()
        {
            var filteredBooks = _allBooks.AsEnumerable();

            // Применяем поиск по названию
            if (!string.IsNullOrWhiteSpace(_currentSearchTitle))
            {
                filteredBooks = filteredBooks.Where(b =>
                    b.Title.ToLower().Contains(_currentSearchTitle.ToLower()));
            }

            // Применяем поиск по автору
            if (!string.IsNullOrWhiteSpace(_currentSearchAuthor))
            {
                filteredBooks = filteredBooks.Where(b =>
                    b.AuthorName.ToLower().Contains(_currentSearchAuthor.ToLower()));
            }

            // Применяем фильтр по жанру
            if (_currentGenreFilter.HasValue)
            {
                filteredBooks = filteredBooks.Where(b =>
                    b.Genres != null && b.Genres.Any(g => g.Id == _currentGenreFilter.Value));
            }

            var resultList = filteredBooks.ToList();

            // Применяем сортировку: по рейтингу или по названию
            if (_currentSortType == "Rating")
            {
                resultList = resultList.OrderByDescending(b => GetAverageRating(b)).ToList();
            }
            else
            {
                resultList = resultList.OrderBy(b => b.Title).ToList();
            }

            DisplayBooks(resultList);
        }

        /// <summary>
        /// Обработчик события нажатия клавиши в поле поиска.
        /// При нажатии Enter выполняет поиск.
        /// </summary>
        /// <param name="sender">Источник события (поле поиска)</param>
        /// <param name="e">Аргументы события, содержащие информацию о нажатой клавише</param>
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
            }
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Найти".
        /// Выполняет поиск книг по введённым критериям.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Найти")</param>
        /// <param name="e">Аргументы события</param>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        /// <summary>
        /// Выполняет поиск книг по текущим значениям полей ввода названия и автора.
        /// Обновляет текущие параметры поиска и применяет фильтры.
        /// </summary>
        private void PerformSearch()
        {
            _currentSearchTitle = SearchTitleTextBox.Text.Trim();
            _currentSearchAuthor = SearchAuthorTextBox.Text.Trim();
            ApplyFiltersAndSort();
        }

        /// <summary>
        /// Обработчик события выбора элемента в ComboBox сортировки.
        /// Устанавливает текущий тип сортировки и обновляет отображение книг.
        /// </summary>
        /// <param name="sender">Источник события (ComboBox сортировки)</param>
        /// <param name="e">Аргументы события</param>
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allBooks == null) return;

            var selectedItem = SortComboBox.SelectedItem as ComboBoxItem;

            if (selectedItem == null)
            {
                SortPlaceholder.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                SortPlaceholder.Visibility = Visibility.Collapsed;
            }

            string sortType = selectedItem.Tag?.ToString();
            if (string.IsNullOrEmpty(sortType)) return;

            _currentSortType = sortType;
            ApplyFiltersAndSort();
        }

        /// <summary>
        /// Обработчик события выбора элемента в ComboBox фильтра по жанрам.
        /// Устанавливает текущий фильтр по жанру и обновляет отображение книг.
        /// </summary>
        /// <param name="sender">Источник события (ComboBox фильтра жанров)</param>
        /// <param name="e">Аргументы события</param>
        private void GenreFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allBooks == null) return;

            var selectedItem = GenreFilterComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            GenrePlaceholder.Visibility = Visibility.Collapsed;

            string tagValue = selectedItem.Tag?.ToString();
            if (tagValue == "All")
            {
                // Сбрасываем фильтр по жанру
                _currentGenreFilter = null;
            }
            else
            {
                // Устанавливаем фильтр по выбранному жанру
                _currentGenreFilter = (int?)selectedItem.Tag;
            }

            ApplyFiltersAndSort();
        }

        /// <summary>
        /// Обработчик события выбора списка в ComboBox добавления книги в список.
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
            if (selectedItem?.Tag == null || selectedItem.Content.ToString() == "➕ В список...")
                return;

            string listName = selectedItem.Tag.ToString();
            int bookId = (int)comboBox.Tag;

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

                // Находим книгу
                var book = _context.Books.FirstOrDefault(b => b.Id == bookId);
                if (book == null)
                {
                    MessageBox.Show("Книга не найдена!");
                    comboBox.SelectedIndex = 0;
                    return;
                }

                // Проверяем, есть ли книга уже в целевом списке
                var targetList = currentUserFromDb.BookLists.FirstOrDefault(bl => bl.Name == listName);
                if (targetList != null && targetList.Books.Any(b => b.Id == bookId))
                {
                    MessageBox.Show($"Книга уже есть в списке \"{listName}\"!");
                    comboBox.SelectedIndex = 0;
                    return;
                }

                // Удаляем книгу из всех списков пользователя
                foreach (var list in currentUserFromDb.BookLists)
                {
                    var bookInList = list.Books.FirstOrDefault(b => b.Id == bookId);
                    if (bookInList != null)
                    {
                        list.Books.Remove(bookInList);
                    }
                }

                // Находим или создаём целевой список
                if (targetList == null)
                {
                    targetList = new BookLists
                    {
                        Name = listName,
                        UserId = _currentUser.Id
                    };
                    _context.BookLists.Add(targetList);
                    _context.SaveChanges();

                    // Перезагружаем данные
                    currentUserFromDb = _context.Users
                        .Where(u => u.Id == _currentUser.Id)
                        .Include("BookLists")
                        .FirstOrDefault();
                    targetList = currentUserFromDb.BookLists.FirstOrDefault(bl => bl.Name == listName);
                    _context.Entry(targetList).Collection(bl => bl.Books).Load();
                }

                // Добавляем книгу в целевой список
                targetList.Books.Add(book);
                _context.SaveChanges();

                MessageBox.Show($"Книга \"{book.Title}\" перемещена в список \"{listName}\"!");
                comboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}");
                comboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Обработчик события нажатия на карточку книги.
        /// Выполняет переход на страницу с подробной информацией о книге.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Подробнее")</param>
        /// <param name="e">Аргументы события</param>
        private void BookCard_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext == null) return;

            dynamic bookData = button.DataContext;
            int bookId = bookData.Id;

            // Переходим на страницу книги
            NavigationService.Navigate(new BookDetailPage(_currentUser, bookId));
        }
    }
}