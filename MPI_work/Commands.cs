using Microsoft.EntityFrameworkCore;
using MPI_work.Entities;
namespace MPI_work
{
    public class Commands
    {
        // Функция генерации отзывов
        public static void GenerateFeedback(LocalDb localDb)
        {
            // Список магазинов из локальной БД
            List<Shop> shops = localDb.ShopList.ToList();
            // Пустой список пользователей для хранения новых пользователей
            List<User> users = new List<User> { };
            // Пустой список отзывов для будущего хранения новых отзывов
            List<Feedback> feedbacks = new List<Feedback> { };
            // Перебор каждого магазина для добавления отзыва
            foreach (var shop in shops)
            {
                // Случайное число от 1 до 4 включительно
                var countFeedback = Faker.RandomNumber.Next(1, 4);
                // Случайные пользователя добавляют отзывы
                for (int i = 0; i < countFeedback; i++)
                {
                    // Генерация экземпляра случайного пользователя
                    User user = new User
                    {
                        FullName = Faker.Name.FullName(),
                        Age = Faker.RandomNumber.Next(1, 65)
                    };
                    // Добавить сгенерированного пользователя в локальный список
                    users.Add(user);
                    // Добавить сгенерированного пользователя в локальную БД
                    localDb.UserList.Add(user);

                    // Сгенерировать случайный отзыв
                    Feedback feedback = new Feedback
                    {
                        // Айди магазина в отзыве
                        ShopId = shop.ShopId,
                        Shop = shop,
                        // Айди пользователя в отзыве
                        UserId = user.UserId,
                        User = user,
                        // Оценка от пользователя
                        Rating = Faker.RandomNumber.Next(1, 5)
                    };
                    // Добавить отзыв в список отзывов
                    feedbacks.Add(feedback);
                    // Добавить отзыв в локальную БД
                    localDb.FeedbackList.Add(feedback);
                }
                // Число отзывов для каждого магазина
                shop.NumberReviews = shop.NumberReviews + feedbacks.Where(p => p.ShopId == shop.ShopId).Count();
                // Вычислить средний рейтинг у магазина перед выходом(сумма оценок / количество оценок)
                shop.Rating = (decimal)feedbacks.Where(p => p.ShopId == shop.ShopId).Sum(p => p.Rating) / shop.NumberReviews;
                        
            }
            // Подключение к БД
            using (var db = new AppDbContext())
            {
                // Добавить список пользователей в серверную БД
                db.Users.AddRange(users);
                // Обновить данные о магазина в БД
                db.Shops.UpdateRange(shops);
                // Добавить отзывы в БД
                db.Feedbacks.AddRange(feedbacks);

                // Зафиксировать изменения в БД
                db.SaveChanges();
            }
        }
        // Генерация данных в БД
        public static void GenerateData(int count)
        {
            using (var db = new AppDbContext())
            {
                // Генерация магазинов
                for (int i = 0; i < count; i++)
                {
                    // Генерация экземпляра случайного магазина
                    Shop shop = new Shop
                    {
                        Address = Faker.Address.StreetAddress()
                    };
                    // Добавить в таблицу магазин
                    db.Shops.Add(shop);
                }
                // Зафиксировать изменения в БД
                db.SaveChanges();
            }
        }
        // Функция загрузки базы данных
        public static LocalDb LoadingDb(int rank, int size)
        {
            using (var db = new AppDbContext())
            {
                // Количество строк в каждой таблице
                int countShop = db.Shops.Count();
                int countUser = db.Users.Count();
                int countFeedback = db.Feedbacks.Count();
                // Размер части для каждой таблицы
                int partShop = (countShop / size + 1);
                int partUser = (countUser / size + 1);
                int partFeedback = (countFeedback / size + 1);
                // Смещение по каждой таблице
                int offsetShop = rank * partShop;
                int offsetUser = rank * partUser;
                int offsetFeedback = rank * partFeedback;

                // Вернуть локальную базу данных
                return new LocalDb
                {
                    // Список магазинов
                    ShopList = db.Shops
                            .Skip(offsetShop)
                            .Take(partShop)
                            .ToList(),
                    // Список пользователей
                    UserList = db.Users
                            .Skip(offsetUser)
                            .Take(partUser)
                            .ToList(),
                    // Список отзывов
                    FeedbackList = db.Feedbacks
                            .Include(p => p.User)
                            .Include(p => p.Shop)
                            .Skip(offsetFeedback)
                            .Take(partFeedback)
                            .ToList()
                };
            }
        }
    }
}
