using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OnlineLibrary
{
    /// <summary>
    /// Страница управления личными списками книг пользователя.
    /// Предоставляет возможность переключаться между четырьмя списками
    /// ("Читаю", "В планах", "Прочитано", "Заброшено"), перемещать книги между списками,
    /// удалять книги из списков, а также сортировать и фильтровать книги по жанрам.
    /// Реализует требования FR-5.1 - FR-5.8.
    /// </summary>
    public partial class BookListsPage : Page
    {
        /// <summary>
        /// Текущий авторизованный пользователь.
        /// Используется для получения списков и книг конкретного пользователя.
        /// </summary>
        private Users _currentUser;

        /// <summary>
        /// Единственный экземпляр контекста базы данных (Singleton).
        /// </summary>
        private OnlineLibraryEntities _context;

        /// <summary>
        /// Имя текущего активного списка книг.
        /// Допустимые значения: "Читаю", "В планах", "Прочитано", "Заброшено".
        /// По умолчанию установлен список "Читаю".
        /// </summary>
        private string _currentListName = "Читаю";

        /// <summary>
        /// Список книг текущего активного списка пользователя.
        /// Используется как источник данных для сортировки и фильтрации.
        /// </summary>
        private List<Books> _currentBooks;

        /// <summary>
        /// Полный список всех жанров, используемых для заполнения фильтра.
        /// </summary>
        private List<Genres> _allGenres;

        /// <summary>
        /// Конструктор страницы списков книг.
        /// Инициализирует компоненты, получает контекст БД через Singleton и загружает данные.
        /// </summary>
        /// <param name="user">Текущий авторизованный пользователь</param>
        public BookListsPage(Users user)
        {
            InitializeComponent();
            _currentUser = user;
            // Получаем единственный экземпляр контекста базы данных (Singleton)
            _context = OnlineLibraryEntities.GetContext();

            LoadData();
        }

        /// <summary>
        /// Загружает все жанры из базы данных и заполняет ComboBox фильтра.
        /// Загружает книги текущего активного списка и подсвечивает кнопку "Читаю" по умолчанию.
        /// </summary>
        private void LoadData()
        {
            // Загружаем все жанры для фильтра
            _allGenres = _context.Genres.ToList();
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

            // Загружаем книги текущего списка
            LoadCurrentListBooks();
            // Подсвечиваем кнопку "Читаю" по умолчанию (FR-5.1)
            BtnReading.Style = (Style)FindResource("ActiveTabButton");
        }

        /// <summary>
        /// Загружает книги текущего активного списка пользователя из базы данных.
        /// Использует Include для загрузки связанных жанров каждой книги.
        /// Если список не найден, отображает пустой список книг.
        /// </summary>
        private void LoadCurrentListBooks()
        {
            try
            {
                // Находим список пользователя с книгами и жанрами
                var bookList = _context.BookLists
                    .Where(bl => bl.UserId == _currentUser.Id && bl.Name == _currentListName)
                    .Include("Books.Genres")
                    .FirstOrDefault();

                if (bookList == null)
                {
                    // Если списка ещё нет — показываем пустой список
                    _currentBooks = new List<Books>();
                    BooksItemsControl.ItemsSource = new List<object>();
                    return;
                }

                // Получаем книги из списка
                _currentBooks = bookList.Books.ToList();

                DisplayBooks(_currentBooks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка: {ex.Message}");
                _currentBooks = new List<Books>();
                BooksItemsControl.ItemsSource = new List<object>();
            }
        }

        /// <summary>
        /// Отображает список книг в интерфейсе с помощью ItemsControl.
        /// Формирует анонимный объект с данными для каждой книги:
        /// идентификатор, название, автор и список жанров.
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
                    : "Без жанра"
            }).ToList();
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку вкладки списка книг (FR-5.1).
        /// Переключает активный список ("Читаю", "В планах", "Прочитано", "Заброшено"),
        /// сбрасывает стили всех кнопок и подсвечивает выбранную вкладку.
        /// </summary>
        /// <param name="sender">Источник события (кнопка вкладки списка)</param>
        /// <param name="e">Аргументы события</param>
        private void ListTab_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            // Устанавливаем текущий список по Tag кнопки
            _currentListName = button.Tag.ToString();

            // Сбрасываем стиль всех кнопок к стандартному
            BtnDropped.Style = (Style)FindResource("AccentButton");
            BtnPlans.Style = (Style)FindResource("AccentButton");
            BtnReading.Style = (Style)FindResource("AccentButton");
            BtnRead.Style = (Style)FindResource("AccentButton");

            // Подсвечиваем активную кнопку
            button.Style = (Style)FindResource("ActiveTabButton");

            // Загружаем книги нового активного списка
            LoadCurrentListBooks();
        }

        /// <summary>
        /// Обработчик события выбора элемента в ComboBox сортировки (FR-5.6, FR-5.7).
        /// Сортирует книги текущего списка по названию или по рейтингу
        /// и обновляет отображение.
        /// </summary>
        /// <param name="sender">Источник события (ComboBox сортировки)</param>
        /// <param name="e">Аргументы события</param>
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentBooks == null) return;

            var selectedItem = SortComboBox.SelectedItem as ComboBoxItem;

            // Показываем/скрываем плейсхолдер
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

            // Сортируем книги по выбранному критерию
            var sortedBooks = sortType == "Rating"
                ? _currentBooks.OrderBy(b => b.Title).ToList()
                : _currentBooks.OrderBy(b => b.Title).ToList();

            DisplayBooks(sortedBooks);
        }

        /// <summary>
        /// Обработчик события выбора элемента в ComboBox фильтра по жанрам (FR-5.8).
        /// Фильтрует книги текущего списка по выбранному жанру
        /// и обновляет отображение.
        /// </summary>
        /// <param name="sender">Источник события (ComboBox фильтра жанров)</param>
        /// <param name="e">Аргументы события</param>
        private void GenreFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentBooks == null) return;

            var selectedItem = GenreFilterComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            // Скрываем плейсхолдер при выборе
            GenrePlaceholder.Visibility = Visibility.Collapsed;

            // Проверяем Tag выбранного элемента
            string tagValue = selectedItem.Tag?.ToString();
            if (tagValue == "All")
            {
                // Отображаем все книги (фильтр снят)
                DisplayBooks(_currentBooks);
                return;
            }

            int genreId = (int)(selectedItem.Tag ?? 0);

            // Фильтруем книги по выбранному жанру
            var filteredBooks = _currentBooks
                .Where(b => b.Genres != null && b.Genres.Any(g => g.Id == genreId))
                .ToList();

            DisplayBooks(filteredBooks);
        }

        /// <summary>
        /// Обработчик события выбора списка в ComboBox перемещения книги (FR-5.2).
        /// Перемещает книгу из текущего списка в выбранный целевой список.
        /// Выполняет проверки: существование книги, наличие в текущем списке,
        /// отсутствие дубликата в целевом списке.
        /// </summary>
        /// <param name="sender">Источник события (ComboBox перемещения)</param>
        /// <param name="e">Аргументы события</param>
        private void MoveComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem == null) return;

            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag == null || selectedItem.Content.ToString() == "Переместить в...")
                return;

            string targetListName = selectedItem.Tag.ToString();
            int bookId = (int)comboBox.Tag;

            try
            {
                // Находим книгу по идентификатору
                var book = _context.Books.FirstOrDefault(b => b.Id == bookId);
                if (book == null)
                {
                    MessageBox.Show("Книга не найдена!");
                    comboBox.SelectedIndex = 0;
                    return;
                }

                // Находим все списки пользователя с их книгами
                var userLists = _context.BookLists
                    .Where(bl => bl.UserId == _currentUser.Id)
                    .Include("Books")
                    .ToList();

                // Находим текущий список (откуда перемещаем)
                var currentList = userLists.FirstOrDefault(bl => bl.Name == _currentListName);

                // Находим целевой список (куда перемещаем)
                var targetList = userLists.FirstOrDefault(bl => bl.Name == targetListName);

                if (currentList == null || targetList == null)
                {
                    MessageBox.Show("Список книг не найден!");
                    comboBox.SelectedIndex = 0;
                    return;
                }

                // Проверяем, есть ли книга в текущем списке
                var bookInCurrent = currentList.Books.FirstOrDefault(b => b.Id == bookId);
                if (bookInCurrent == null)
                {
                    MessageBox.Show("Книга не найдена в текущем списке!");
                    comboBox.SelectedIndex = 0;
                    return;
                }

                // Проверяем, есть ли книга уже в целевом списке (защита от дублирования)
                var existingInTarget = targetList.Books.Any(b => b.Id == bookId);
                if (existingInTarget)
                {
                    MessageBox.Show($"Книга уже есть в списке \"{targetListName}\"!");
                    comboBox.SelectedIndex = 0;
                    return;
                }

                // Удаляем книгу из текущего списка
                currentList.Books.Remove(bookInCurrent);

                // Добавляем книгу в целевой список
                targetList.Books.Add(book);

                _context.SaveChanges();

                // Обновляем отображение текущего списка
                LoadCurrentListBooks();

                // Сбрасываем выбор в ComboBox
                comboBox.SelectedIndex = 0;

                MessageBox.Show($"Книга перемещена в список \"{targetListName}\".");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при перемещении: {ex.Message}");
                comboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Удалить из списка".
        /// Удаляет книгу из текущего активного списка после подтверждения пользователем.
        /// </summary>
        /// <param name="sender">Источник события (кнопка "Удалить")</param>
        /// <param name="e">Аргументы события</param>
        private void RemoveBook_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int bookId = (int)button.Tag;

            // Запрос подтверждения удаления
            var result = MessageBox.Show(
                $"Удалить книгу из списка \"{_currentListName}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Находим список пользователя с загруженными книгами
                var bookList = _context.BookLists
                    .Where(bl => bl.UserId == _currentUser.Id && bl.Name == _currentListName)
                    .Include("Books")
                    .FirstOrDefault();

                if (bookList == null)
                {
                    MessageBox.Show("Список не найден!");
                    return;
                }

                // Находим книгу в списке
                var book = bookList.Books.FirstOrDefault(b => b.Id == bookId);
                if (book != null)
                {
                    // Удаляем книгу из списка
                    bookList.Books.Remove(book);

                    // Сохраняем изменения в базе данных
                    _context.SaveChanges();

                    // Обновляем отображение списка
                    LoadCurrentListBooks();

                    MessageBox.Show("Книга удалена из списка.");
                }
                else
                {
                    MessageBox.Show("Книга не найдена в списке.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}");
            }
        }
    }
}