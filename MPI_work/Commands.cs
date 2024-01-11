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

        // Функция для просмотра 25 лучших и 25 худших магазинов
        public static List<Shop> GetShops(LocalDb localDb)
        {
            // Возвращаем список магазинов 
            return localDb.ShopList.ToList();
        }

        // Функция для написания отзыва магазину по Id
        public static void Feetback(int shopId, int rating, string? feet)
        {
            using (var db = new AppDbContext())
            {
                // Генерация экземпляра случайного пользователя
                User user = new User
                {
                    FullName = Faker.Name.FullName(),
                    Age = Faker.RandomNumber.Next(1, 65)
                };
                // Добавить сгенерированного пользователя в базу данных
                db.Users.Add(user);
                // Зафиксировать изменения в БД
                db.SaveChanges();
                
                // Экземпляр магазина, которому выставлен отзыв
                Shop? shop = db.Shops.Where(p => p.ShopId == shopId).FirstOrDefault();
                // Выйти из функции, если такого магазина нет
                if (shop == null) return;
                // Добавить в базу данных новый отзыв от случайного пользователя
                db.Feedbacks.Add(new Feedback
                {
                    UserId = user.UserId,
                    ShopId = shopId,
                    Rating = rating,
                    UserFeedback = feet
                });
                // Добавить число отзывов у магазина
                shop.NumberReviews = shop.NumberReviews + 1;
                // Обновить рейтинг магазина
                shop.Rating = (decimal)(db.Feedbacks.Where(p => p.ShopId == shop.ShopId).Sum(p => p.Rating) + rating) / shop.NumberReviews;
                // Зафиксировать изменения в БД
                db.SaveChanges();
            }
        }

        // Функция для вывода отзывов о магазине по Id
        public static List<Feedback> GetFeetbacks(LocalDb localDb, int shopId)
        {
            // Вернуть список отзывов
            return localDb.FeedbackList.Where(p => p.ShopId == shopId).ToList();
        }

        // Функция для вывода всех отзывов пользователя по Id
        public static List<Feedback> GetUserFeetbacks(LocalDb localDb, int userId)
        {
            // Вернуть список отзывов
            return localDb.FeedbackList.Where(p => p.UserId == userId).ToList();
        }


        // Удалить магазин из БД со всеми отзывами
        public static void DeleteShop(LocalDb localDb, DeletedData deletedDb, int shopId)
        {
            // Удаление из локальной БД отзывов
            foreach (var feetback in localDb.FeedbackList.Where(p => p.ShopId == shopId).ToList())
                localDb.FeedbackList.Remove(feetback);
            // Получить экземпляр магазина
            Shop? shop = localDb.ShopList.Where(p => p.ShopId == shopId).FirstOrDefault();
            // Если магазина не нашлось, выйти
            if (shop == null) return;
            // Удаление из локальной БД магазина
            localDb.ShopList.Remove(shop);
            // Добавить в список удаленных
            deletedDb.DeletedShopList.Add(shop);
        }

        // Удалить отзыв по Id
        public static void DeleteFeetback(int feetbackId)
        {
            using (var db = new AppDbContext())
            {
                // Получить экземпляр отзыва
                Feedback? feetback = db.Feedbacks.Include(p => p.Shop).Where(p => p.FeedbackId == feetbackId).FirstOrDefault();
                // Если отзыва не нашлось, выйти
                if (feetback == null) return;

                // Удалить из базы отзыв
                db.Feedbacks.Remove(feetback);
                db.SaveChanges();
                // Добавить число отзывов у магазина
                feetback.Shop.NumberReviews = feetback.Shop.NumberReviews - 1;
                if (feetback.Shop.NumberReviews == 0)
                    feetback.Shop.Rating = 0;
                else
                    // Обновить рейтинг магазина
                    feetback.Shop.Rating = (decimal)(db.Feedbacks.Where(p => p.ShopId == feetback.Shop.ShopId).Sum(p => p.Rating)) / feetback.Shop.NumberReviews;
                // Зафиксировать изменения в БД
                db.SaveChanges();
            }
        }

        // Обновить базу данных на сервере
        public static void UpdateDb(LocalDb localDb, DeletedData deletedDb)
        {
            using (var db = new AppDbContext())
            {
                // Удаление в БД локально удаленных магазинов 
                foreach (var shop in deletedDb.DeletedShopList)
                    db.Shops.Remove(shop);
                // Удаление в БД локально удаленных пользователей
                foreach (var user in deletedDb.DeletedUserList)
                    db.Users.Remove(user);
                // Удаление в БД локально удаленных отзывов
                foreach (var feetback in deletedDb.DeletedFeedbackList)
                    db.Feedbacks.Remove(feetback);

                // Обновление в БД данных магазинов
                db.Shops.UpdateRange(localDb.ShopList);
                // Обновление в БД данных пользователей
                db.Users.UpdateRange(localDb.UserList);
                // Обновление в БД данных отзывов
                db.Feedbacks.UpdateRange(localDb.FeedbackList);

                // Зафиксировать изменения в БД
                db.SaveChanges();

                // Очистить базу удаленных объектов
                deletedDb = new DeletedData();
            }
        }
    }
}
