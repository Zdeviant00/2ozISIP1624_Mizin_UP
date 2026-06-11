namespace OnlineLibrary
{
    using System.Data.Entity;

    /// <summary>
    /// Частичный класс контекста базы данных OnlineLibrary.
    /// Реализует паттерн проектирования Singleton для обеспечения
    /// единственной точки доступа к базе данных во всём приложении.
    /// </summary>
    public partial class OnlineLibraryEntities : DbContext
    {
        /// <summary>
        /// Приватная статическая переменная, хранящая единственный экземпляр контекста.
        /// Реализация паттерна Singleton.
        /// </summary>
        private static OnlineLibraryEntities _context;

        /// <summary>
        /// Публичный статический метод для получения единственного экземпляра контекста базы данных.
        /// Если экземпляр ещё не создан, создаёт новый.
        /// </summary>
        /// <returns>Единственный экземпляр контекста базы данных OnlineLibraryEntities</returns>
        public static OnlineLibraryEntities GetContext()
        {
            if (_context == null)
            {
                _context = new OnlineLibraryEntities();
            }
            return _context;
        }
    }
}