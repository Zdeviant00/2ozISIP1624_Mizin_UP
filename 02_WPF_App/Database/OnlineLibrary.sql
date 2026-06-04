-- ============================================
-- Создание базы данных OnlineLibrary
-- ============================================

USE master;
GO

-- Удаляем БД, если она существует
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'OnlineLibrary')
BEGIN
    ALTER DATABASE OnlineLibrary SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE OnlineLibrary;
END
GO

CREATE DATABASE OnlineLibrary;
GO

USE OnlineLibrary;
GO

-- ============================================
-- Таблица: Роли
-- ============================================
CREATE TABLE Roles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- ============================================
-- Таблица: Пользователи
-- ============================================
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Login NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Email NVARCHAR(100),
    Name NVARCHAR(100),
    RoleId INT NOT NULL FOREIGN KEY REFERENCES Roles(Id),
    IsFrozen BIT NOT NULL DEFAULT 0,
    FreezeReason NVARCHAR(500)
);
GO

-- ============================================
-- Таблица: Жанры
-- ============================================
CREATE TABLE Genres (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- ============================================
-- Таблица: Книги
-- ============================================
CREATE TABLE Books (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    CoverPath NVARCHAR(500),
    AuthorName NVARCHAR(100),
    Text NVARCHAR(MAX),
    IsFrozen BIT NOT NULL DEFAULT 0
);
GO

-- ============================================
-- Таблица: Связь книг и жанров (многие-ко-многим)
-- ============================================
CREATE TABLE BookGenres (
    BookId INT NOT NULL FOREIGN KEY REFERENCES Books(Id) ON DELETE CASCADE,
    GenreId INT NOT NULL FOREIGN KEY REFERENCES Genres(Id) ON DELETE CASCADE,
    PRIMARY KEY (BookId, GenreId)
);
GO

-- ============================================
-- Таблица: Отзывы
-- ============================================
CREATE TABLE Reviews (
    Id INT PRIMARY KEY IDENTITY(1,1),
    BookId INT NOT NULL FOREIGN KEY REFERENCES Books(Id) ON DELETE CASCADE,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Text NVARCHAR(MAX),
    Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    IsFrozen BIT NOT NULL DEFAULT 0
);
GO

-- ============================================
-- Таблица: Жалобы
-- ============================================
CREATE TABLE Complaints (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Type NVARCHAR(50) NOT NULL,  -- 'Book', 'Review'
    TargetId INT NOT NULL,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Reason NVARCHAR(500),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'  -- 'Pending', 'Accepted', 'Rejected'
);
GO

-- ============================================
-- Таблица: Списки книг
-- ============================================
CREATE TABLE BookLists (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE,
    Name NVARCHAR(50) NOT NULL  -- 'Заброшено', 'В планах', 'Читаю', 'Прочитано'
);
GO

-- ============================================
-- Таблица: Элементы списков книг
-- ============================================
CREATE TABLE BookListItems (
    BookListId INT NOT NULL FOREIGN KEY REFERENCES BookLists(Id) ON DELETE CASCADE,
    BookId INT NOT NULL FOREIGN KEY REFERENCES Books(Id) ON DELETE CASCADE,
    PRIMARY KEY (BookListId, BookId)
);
GO

-- ============================================
-- Таблица: Заявки на снятие заморозки
-- ============================================
CREATE TABLE FreezeRequests (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Type NVARCHAR(50) NOT NULL,  -- 'Account', 'Book', 'Review'
    TargetId INT,
    Reason NVARCHAR(500),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'
);
GO

-- ============================================
-- ЗАПОЛНЕНИЕ ТЕСТОВЫМИ ДАННЫМИ
-- ============================================

-- Роли
INSERT INTO Roles (Name) VALUES 
('Пользователь'), 
('Администратор');
GO

-- Пользователи
-- Пароль для всех: 123456 (MD5 хэш: e10adc3949ba59abbe56e057f20f883e)
INSERT INTO Users (Login, PasswordHash, Email, Name, RoleId, IsFrozen, FreezeReason) VALUES
('admin', 'e10adc3949ba59abbe56e057f20f883e', 'admin@library.ru', 'Администратор Системы', 2, 0, NULL),
('reader1', 'e10adc3949ba59abbe56e057f20f883e', 'reader1@mail.ru', 'Иван Петров', 1, 0, NULL),
('reader2', 'e10adc3949ba59abbe56e057f20f883e', 'reader2@mail.ru', 'Мария Сидорова', 1, 0, NULL),
('reader3', 'e10adc3949ba59abbe56e057f20f883e', 'reader3@mail.ru', 'Алексей Козлов', 1, 1, 'Нарушение правил сообщества'),
('reader4', 'e10adc3949ba59abbe56e057f20f883e', 'reader4@mail.ru', 'Елена Волкова', 1, 0, NULL);
GO

-- Жанры
INSERT INTO Genres (Name) VALUES 
('Фантастика'), 
('Фэнтези'), 
('Детектив'), 
('Роман'), 
('Научная литература'), 
('Поэзия'), 
('Ужасы'), 
('Приключения'),
('Классика'),
('Современная проза');
GO

-- Книги
INSERT INTO Books (Title, Description, CoverPath, AuthorName, Text, IsFrozen) VALUES
('Звёздный путь', 'Космическая одиссея экипажа корабля "Аврора"', 'covers/star_path.jpg', 'Дмитрий Орлов', 'Глава 1. Пробуждение. Корабль медленно выходил из гиперпространства...', 0),
('Тайна старого замка', 'Детективная история в духе классического английского романа', 'covers/castle.jpg', 'Анна Белова', 'Глава 1. Приглашение. Письмо пришло неожиданно...', 0),
('Магия леса', 'Фэнтезийный мир, где деревья хранят древние тайны', 'covers/forest.jpg', 'Сергей Лесной', 'Глава 1. Пробуждение силы. Эльфийка открыла глаза...', 0),
('Последний герой', 'Приключенческий роман о выживании в постапокалиптическом мире', 'covers/hero.jpg', 'Михаил Сильный', 'Глава 1. Пустошь. Ветер гнал пыль по разбитой дороге...', 0),
('Квантовая физика для всех', 'Научно-популярное издание о квантовой механике', 'covers/quantum.jpg', 'Профессор Иванов', 'Введение. Квантовая физика — это...', 0),
('Тени прошлого', 'Психологический триллер о памяти и идентичности', 'covers/shadows.jpg', 'Ольга Тёмная', 'Глава 1. Воспоминание. Она проснулась в холодной комнате...', 0),
('Сад забытых книг', 'Роман о библиотеке, где хранятся несуществующие произведения', 'covers/garden.jpg', 'Виктор Книжный', 'Глава 1. Дверь. Она появилась внезапно...', 0),
('Голоса тишины', 'Сборник поэзии о внутреннем мире человека', 'covers/voices.jpg', 'Лирика Поэтова', '***. В тишине рождается звук...', 0),
('Путь самурая', 'Исторический роман о Японии эпохи Эдо', 'covers/samurai.jpg', 'Хироши Танака', 'Глава 1. Рассвет. Воин открыл глаза...', 0),
('Цифровой разум', 'Научная фантастика об искусственном интеллекте', 'covers/digital.jpg', 'Андрей Киберов', 'Глава 1. Инициализация. Система запустилась...', 0),
('Дом у озера', 'Современная проза о жизни в маленьком городке', 'covers/lake.jpg', 'Наталья Тихая', 'Глава 1. Возвращение. Поезд медленно подходил...', 0),
('Код да Винчи: продолжение', 'Детектив в стиле Дэна Брауна', 'covers/davinci.jpg', 'Павел Загадочный', 'Глава 1. Шифр. Буквы складывались в узор...', 0),
('Ледяное сердце', 'Роман о любви в условиях вечной мерзлоты', 'covers/ice.jpg', 'Снежана Холодова', 'Глава 1. Мороз. Температура опустилась до...', 0),
('Пираты XXI века', 'Приключенческий роман о современных пиратах', 'covers/pirates.jpg', 'Капитан Крюк', 'Глава 1. Шторм. Волны бились о борт...', 0),
('Философия кода', 'Научная литература о программировании и мышлении', 'covers/philosophy.jpg', 'Профессор Логинов', 'Введение. Код — это язык...', 0);
GO

-- Связи книг с жанрами
INSERT INTO BookGenres (BookId, GenreId) VALUES
(1, 1), (1, 8),    -- Звёздный путь: Фантастика, Приключения
(2, 3), (2, 9),    -- Тайна старого замка: Детектив, Классика
(3, 2), (3, 8),    -- Магия леса: Фэнтези, Приключения
(4, 1), (4, 8),    -- Последний герой: Фантастика, Приключения
(5, 5),            -- Квантовая физика: Научная литература
(6, 7), (6, 4),    -- Тени прошлого: Ужасы, Роман
(7, 4), (7, 10),   -- Сад забытых книг: Роман, Современная проза
(8, 6),            -- Голоса тишины: Поэзия
(9, 9), (9, 8),    -- Путь самурая: Классика, Приключения
(10, 1), (10, 5),  -- Цифровой разум: Фантастика, Научная литература
(11, 10), (11, 4), -- Дом у озера: Современная проза, Роман
(12, 3),           -- Код да Винчи: Детектив
(13, 4),           -- Ледяное сердце: Роман
(14, 8),           -- Пираты XXI века: Приключения
(15, 5), (15, 10); -- Философия кода: Научная литература, Современная проза
GO

-- Отзывы
INSERT INTO Reviews (BookId, UserId, Text, Rating, IsFrozen) VALUES
(1, 2, 'Отличная книга! Читается на одном дыхании.', 5, 0),
(1, 3, 'Неплохо, но затянуто в середине.', 3, 0),
(1, 5, 'Фантастика на высоте, рекомендую!', 5, 0),
(2, 2, 'Захватывающий детектив, разгадка неожиданная.', 4, 0),
(2, 3, 'Слишком предсказуемо.', 2, 0),
(3, 2, 'Прекрасный мир, хочется вернуться.', 5, 0),
(5, 5, 'Наконец-то понятное объяснение квантовой физики!', 5, 0),
(7, 2, 'Оригинальная концепция, но стиль тяжеловат.', 3, 0),
(10, 3, 'Актуальная тема, хорошо раскрыта.', 4, 0),
(12, 5, 'Не дотягивает до оригинала, но интересно.', 3, 0),
(1, 4, 'Спам и реклама в отзыве!', 1, 1);  -- замороженный отзыв
GO

-- Жалобы
INSERT INTO Complaints (Type, TargetId, UserId, Reason, Status) VALUES
('Book', 1, 3, 'Книга содержит плагиат', 'Pending'),
('Review', 11, 2, 'Спам в отзыве', 'Accepted'),
('Book', 6, 5, 'Неприемлемый контент', 'Pending'),
('Review', 5, 2, 'Оскорбления в отзыве', 'Rejected');
GO

-- Списки книг для пользователей
INSERT INTO BookLists (UserId, Name) VALUES
(2, 'Читаю'), (2, 'Прочитано'), (2, 'В планах'), (2, 'Заброшено'),
(3, 'Читаю'), (3, 'Прочитано'), (3, 'В планах'), (3, 'Заброшено'),
(5, 'Читаю'), (5, 'Прочитано'), (5, 'В планах'), (5, 'Заброшено');
GO

-- Элементы списков
-- Иван Петров (UserId=2)
INSERT INTO BookListItems (BookListId, BookId) VALUES
(1, 1),  -- Читаю: Звёздный путь
(1, 3),  -- Читаю: Магия леса
(2, 2),  -- Прочитано: Тайна старого замка
(2, 5),  -- Прочитано: Квантовая физика
(3, 7),  -- В планах: Сад забытых книг
(3, 10); -- В планах: Цифровой разум

-- Мария Сидорова (UserId=3)
INSERT INTO BookListItems (BookListId, BookId) VALUES
(5, 2),  -- Читаю: Тайна старого замка
(6, 1),  -- Прочитано: Звёздный путь
(6, 12), -- Прочитано: Код да Винчи
(7, 6),  -- В планах: Тени прошлого
(8, 14); -- Заброшено: Пираты XXI века

-- Елена Волкова (UserId=5)
INSERT INTO BookListItems (BookListId, BookId) VALUES
(9, 10), -- Читаю: Цифровой разум
(10, 3), -- Прочитано: Магия леса
(11, 11),-- В планах: Дом у озера
(12, 9); -- Заброшено: Путь самурая
GO

-- Заявки на снятие заморозки
INSERT INTO FreezeRequests (UserId, Type, TargetId, Reason, Status) VALUES
(4, 'Account', 4, 'Прошу разблокировать аккаунт, нарушение было случайным', 'Pending'),
(4, 'Book', 1, 'Книга была заморожена по ошибке', 'Pending'),
(2, 'Review', 11, 'Отзыв был заморожен несправедливо', 'Accepted');
GO

-- ============================================
-- ПРОВЕРОЧНЫЕ ЗАПРОСЫ
-- ============================================
SELECT 'База данных OnlineLibrary успешно создана!' AS Результат;
SELECT COUNT(*) AS [Всего книг] FROM Books;
SELECT COUNT(*) AS [Всего пользователей] FROM Users;
SELECT COUNT(*) AS [Всего жанров] FROM Genres;
SELECT COUNT(*) AS [Всего отзывов] FROM Reviews;
GO