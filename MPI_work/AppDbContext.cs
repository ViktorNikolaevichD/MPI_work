using Microsoft.EntityFrameworkCore;
using MPI;
//using MPI_work.Entities;
using static System.Net.Mime.MediaTypeNames;

namespace MPI_work
{
    internal class AppDbContext : DbContext
    {
        // Таблицы для работы

        // Таблица пользователей ПРИМЕР
        //public DbSet<User> Users { get; set; }

        public AppDbContext()
        {
            // Проверка базы данных на существование
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Строка подключения к базе данных
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=DbCourseWork;Trusted_Connection=True;");
        }
    }
}
