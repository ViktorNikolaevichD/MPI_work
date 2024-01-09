using Microsoft.EntityFrameworkCore;
using MPI_work.Entities;

namespace MPI_work
{
    internal class AppDbContext : DbContext
    {
        // Таблица пользователей
        public DbSet<User> Users { get; set; }
        // Таблица магазинов
        public DbSet<Shop> Shops { get; set; }
        // Таблица отзывов
        public DbSet<Feedback> Feedbacks { get; set; }

        public AppDbContext()
        {
            // Проверка базы данных на существование
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Строка подключения к базе данных
            optionsBuilder.UseSqlServer("Server=localhost;Database=FeedBacksDb;Trusted_Connection=True;Encrypt=False;");
        }
    }
}
