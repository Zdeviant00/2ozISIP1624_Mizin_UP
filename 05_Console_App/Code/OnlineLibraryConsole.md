/**
 * @file OnlineLibraryConsole.cpp
 * @brief Консольное приложение "Онлайн библиотека" с демонстрацией работы с буфером экрана.
 *
 * Приложение реализует:
 * - Мменю для работы с каталогом книг
 * - Поиск книги по названию
 * - Вывод информации о книге на второй буфер экрана
 * - Демонстрацию работы с буфером экрана (создание, активация, изменение размеров)
 */

#include <iostream>
#include <string>
#include <vector>
#include <windows.h>
#include <clocale>

using namespace std;

// ============================================================================
// Структура данных для книги
// ============================================================================

/**
 * @struct Book
 * @brief Структура, представляющая книгу в каталоге.
 */
struct Book {
    int id;             ///< Идентификатор книги
    string title;       ///< Название книги
    string author;      ///< Автор книги
    string genre;       ///< Жанр книги
    int year;           ///< Год издания
    double rating;      ///< Рейтинг книги (0.0 - 5.0)
};

// ============================================================================
// Mock-данные: каталог книг
// ============================================================================

/**
 * @brief Инициализация каталога книг тестовыми данными.
 * @return Вектор книг с тестовыми данными.
 */
vector<Book> initializeCatalog() {
    vector<Book> catalog;

    catalog.push_back({ 1, "Звёздный путь", "Дмитрий Орлов", "Фантастика", 2020, 4.7 });
    catalog.push_back({ 2, "Тайна старого замка", "Анна Смирнова", "Детектив", 2019, 4.2 });
    catalog.push_back({ 3, "Магия леса", "Елена Волкова", "Фэнтези", 2021, 4.8 });
    catalog.push_back({ 4, "Путь самурая", "Иван Петров", "Исторический", 2018, 4.5 });
    catalog.push_back({ 5, "Цифровой разум", "Алексей Козлов", "Научпоп", 2022, 4.9 });
    catalog.push_back({ 6, "Голоса тишины", "Мария Иванова", "Поэзия", 2020, 4.3 });
    catalog.push_back({ 7, "Последний герой", "Сергей Николаев", "Фантастика", 2021, 4.6 });
    catalog.push_back({ 8, "Дом у озера", "Ольга Белова", "Роман", 2019, 4.1 });
    catalog.push_back({ 9, "Тени прошлого", "Виктор Сидоров", "Триллер", 2020, 4.4 });
    catalog.push_back({ 10, "Свет в окне", "Наталья Кузнецова", "Драма", 2021, 4.0 });

    return catalog;
}

// ============================================================================
// Функции работы с каталогом
// ============================================================================

/**
 * @brief Поиск книги по названию.
 * @param catalog Каталог книг.
 * @param searchQuery Строка поиска.
 * @return Указатель на найденную книгу или nullptr, если не найдена.
 */
const Book* searchBookByTitle(const vector<Book>& catalog, const string& searchQuery) {
    for (const auto& book : catalog) {
        // Поиск по подстроке (регистронезависимый)
        if (book.title.find(searchQuery) != string::npos) {
            return &book;
        }
    }
    return nullptr;
}

/**
 * @brief Вывод информации о книге в консоль.
 * @param book Указатель на книгу.
 */
void displayBookInfo(const Book* book) {
    if (book == nullptr) {
        cout << "Книга не найдена!" << endl;
        return;
    }

    cout << "========================================" << endl;
    cout << "Информация о книге:" << endl;
    cout << "========================================" << endl;
    cout << "ID: " << book->id << endl;
    cout << "Название: " << book->title << endl;
    cout << "Автор: " << book->author << endl;
    cout << "Жанр: " << book->genre << endl;
    cout << "Год издания: " << book->year << endl;
    cout << "Рейтинг: " << book->rating << " / 5.0" << endl;
    cout << "========================================" << endl;
}

// ============================================================================
// Функции работы с буфером экрана
// ============================================================================

/**
 * @brief Получение информации о текущем буфере экрана.
 * @param hConsoleHandle Дескриптор буфера экрана.
 */
void displayBufferInfo(HANDLE hConsoleHandle) {
    CONSOLE_SCREEN_BUFFER_INFO csbi;

    if (GetConsoleScreenBufferInfo(hConsoleHandle, &csbi)) {
        cout << "Информация о буфере экрана:" << endl;
        cout << "----------------------------------------" << endl;
        cout << "Размер буфера: " << csbi.dwSize.X << " x " << csbi.dwSize.Y << endl;
        cout << "Размер окна: " << csbi.srWindow.Right - csbi.srWindow.Left + 1
            << " x " << csbi.srWindow.Bottom - csbi.srWindow.Top + 1 << endl;
        cout << "Позиция курсора: (" << csbi.dwCursorPosition.X << ", " << csbi.dwCursorPosition.Y << ")" << endl;
        cout << "Атрибуты: " << csbi.wAttributes << endl;
        cout << "----------------------------------------" << endl;
    }
    else {
        cout << "Ошибка получения информации о буфере экрана!" << endl;
    }
}

/**
 * @brief Установка новых размеров буфера экрана.
 * @param hConsoleHandle Дескриптор буфера экрана.
 * @param width Новая ширина буфера.
 * @param height Новая высота буфера.
 * @return true, если размеры успешно изменены; false в противном случае.
 */
bool setBufferSize(HANDLE hConsoleHandle, int width, int height) {
    COORD newSize;
    newSize.X = width;
    newSize.Y = height;

    if (SetConsoleScreenBufferSize(hConsoleHandle, newSize)) {
        cout << "Размеры буфера экрана изменены на: " << width << " x " << height << endl;
        return true;
    }
    else {
        cout << "Ошибка изменения размеров буфера экрана!" << endl;
        return false;
    }
}

/**
 * @brief Вывод информации о книге на второй буфер экрана.
 * @param hBufferHandle Дескриптор второго буфера экрана.
 * @param book Указатель на книгу.
 */
void displayBookOnBuffer(HANDLE hBufferHandle, const Book* book) {
    if (book == nullptr) {
        string errorMsg = "Книга не найдена!\n";
        WriteConsoleA(hBufferHandle, errorMsg.c_str(), errorMsg.length(), nullptr, nullptr);
        return;
    }

    // Формируем строку для вывода
    string output = "========================================\n";
    output += "Информация о книге (вывод на второй буфер):\n";
    output += "========================================\n";
    output += "ID: " + to_string(book->id) + "\n";
    output += "Название: " + book->title + "\n";
    output += "Автор: " + book->author + "\n";
    output += "Жанр: " + book->genre + "\n";
    output += "Год издания: " + to_string(book->year) + "\n";
    output += "Рейтинг: " + to_string(book->rating) + " / 5.0\n";
    output += "========================================\n";
    output += "Нажмите любую клавишу для возврата...\n";

    // Выводим на второй буфер
    WriteConsoleA(hBufferHandle, output.c_str(), output.length(), nullptr, nullptr);
}

// ============================================================================
// Функции меню
// ============================================================================

/**
 * @brief Отображение главного меню приложения.
 */
void displayMainMenu() {
    cout << "\n========================================" << endl;
    cout << "       ОНЛАЙН БИБЛИОТЕКА" << endl;
    cout << "========================================" << endl;
    cout << "1. Поиск книги по названию" << endl;
    cout << "2. Показать весь каталог" << endl;
    cout << "3. Информация о буфере экрана" << endl;
    cout << "4. Изменить размер буфера экрана" << endl;
    cout << "0. Выход" << endl;
    cout << "========================================" << endl;
    cout << "Выберите действие: ";
}

/**
 * @brief Отображение каталога книг.
 * @param catalog Каталог книг.
 */
void displayCatalog(const vector<Book>& catalog) {
    cout << "\n========================================" << endl;
    cout << "           КАТАЛОГ КНИГ" << endl;
    cout << "========================================" << endl;

    for (const auto& book : catalog) {
        cout << book.id << ". " << book.title << " - " << book.author
            << " (" << book.genre << ", " << book.year << ") [Рейтинг: "
            << book.rating << "]" << endl;
    }

    cout << "========================================" << endl;
    cout << "Всего книг: " << catalog.size() << endl;
    cout << "========================================" << endl;
}

// ============================================================================
// Главная функция
// ============================================================================

/**
 * @brief Точка входа в приложение.
 * @return Код завершения программы.
 */
int main() {
    // Установка кодировки консоли на Windows-1251 (кириллица)
    SetConsoleCP(1251);           // Кодировка ввода
    SetConsoleOutputCP(1251);     // Кодировка вывода

    // Установка русской локали
    setlocale(LC_ALL, "Russian");

    // Инициализация каталога книг
    vector<Book> catalog = initializeCatalog();

    // Получение дескриптора стандартного буфера экрана
    HANDLE hStandardBuffer = GetStdHandle(STD_OUTPUT_HANDLE);

    // Создание второго буфера экрана
    HANDLE hSecondBuffer = CreateConsoleScreenBuffer(
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        nullptr,
        CONSOLE_TEXTMODE_BUFFER,
        nullptr
    );

    if (hSecondBuffer == INVALID_HANDLE_VALUE) {
        cout << "Ошибка создания второго буфера экрана!" << endl;
        return 1;
    }

    cout << "Второй буфер экрана успешно создан!" << endl;

    int choice;
    bool running = true;

    while (running) {
        displayMainMenu();
        cin >> choice;
        cin.ignore(); // Очистка буфера ввода

        switch (choice) {
        case 1: {
            // Поиск книги по названию
            cout << "\nВведите название книги для поиска: ";
            string searchQuery;
            getline(cin, searchQuery);

            const Book* foundBook = searchBookByTitle(catalog, searchQuery);

            if (foundBook != nullptr) {
                cout << "\nКнига найдена! Вывод информации на второй буфер экрана..." << endl;

                // Делаем второй буфер активным
                if (SetConsoleActiveScreenBuffer(hSecondBuffer)) {
                    // Выводим информацию о книге на второй буфер
                    displayBookOnBuffer(hSecondBuffer, foundBook);

                    // Ожидаем нажатия клавиши
                    system("pause");

                    // Возвращаем стандартный буфер
                    SetConsoleActiveScreenBuffer(hStandardBuffer);
                }
                else {
                    cout << "Ошибка активации второго буфера экрана!" << endl;
                }
            }
            else {
                cout << "\nКнига \"" << searchQuery << "\" не найдена в каталоге." << endl;
            }
            break;
        }

        case 2: {
            // Показать весь каталог
            displayCatalog(catalog);
            break;
        }

        case 3: {
            // Информация о буфере экрана
            cout << "\n--- Стандартный буфер экрана ---" << endl;
            displayBufferInfo(hStandardBuffer);

            cout << "\n--- Второй буфер экрана ---" << endl;
            displayBufferInfo(hSecondBuffer);
            break;
        }

        case 4: {
            // Изменить размер буфера экрана
            int width, height;
            cout << "\nВведите новую ширину буфера (например, 120): ";
            cin >> width;
            cout << "Введите новую высоту буфера (например, 30): ";
            cin >> height;

            cout << "\nИзменение размеров стандартного буфера:" << endl;
            setBufferSize(hStandardBuffer, width, height);

            cout << "\nИзменение размеров второго буфера:" << endl;
            setBufferSize(hSecondBuffer, width, height);
            break;
        }

        case 0: {
            // Выход
            cout << "\nВыход из приложения..." << endl;
            running = false;
            break;
        }

        default: {
            cout << "\nНеверный выбор! Пожалуйста, выберите действие от 0 до 4." << endl;
            break;
        }
        }
    }

    // Закрытие дескрипторов буферов
    CloseHandle(hSecondBuffer);
    // hStandardBuffer не закрываем, так как это стандартный дескриптор

    cout << "\nПриложение завершено. До свидания!" << endl;

    return 0;
}
