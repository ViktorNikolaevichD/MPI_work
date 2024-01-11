using MPI_work.Entities;
using System.Diagnostics;
using System.Text.Json;

namespace MPI_work
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MPI.Environment.Run(ref args, comm =>
            {
                // Создаю объект класса, чтобы он хранил удаленные экземпляры
                DeletedData deletedDb = new DeletedData();
                // Создаю объект класса, чтобы он был доступени из всех контекстов
                LocalDb localDb = new LocalDb();

                // Предзагрузка базы данных
                // Первым загружает базу 0 процесс, чтобы не было проблемы с одновременным созданием базы данных
                if (comm.Rank == 0)
                    // База данных для 0 процесса
                    localDb = Commands.LoadingDb(comm.Rank, comm.Size);
                // После того как 0 процесс загрузит базу, то все процессы выйдут из барьера
                comm.Barrier();
                // Все !0 процессы загрузят базу
                if (comm.Rank != 0)
                    // База данных для остальных(не 0) процессов
                    localDb = Commands.LoadingDb(comm.Rank, comm.Size);

                // Замер времени работы
                Stopwatch stopWatch = new Stopwatch();
                // Команда пользователя
                string? command = null;
                while (command != "quit")
                {
                    // Получение команды от пользователя
                    if (comm.Rank == 0)
                    {
                        Console.Write("Введите команду \ngenerateShops - сгенерировать магазины;" +
                                                      "\ngenerateFeets - сгенерировать отзывы для магазинов;" +
                                                      "\nshops - вывести топ лучших/худших магазинов;" +
                                                      "\ndelShop - удалть магазин по Id; " +
                                                      "\nfeetback - оставить отзыв о магазине;" +
                                                      "\nfeetbacks - вывести список отзывов о магазине;" +
                                                      "\nuserFeetbacks - вывести список отзывов пользователя;" +
                                                      "\ndelFeet - удалить отзыв по Id;" +
                                                      "\nupdate - обновление серверной БД: ");
                        command = Console.ReadLine();
                    }

                    // Рассылка команды по всем процессам
                    comm.Broadcast(ref command, 0);

                    switch (command)
                    {
                        // Перевод игрока в другую команду
                        case "generateShops":
                            // Число строк для генерации
                            int count = 0;
                            // Число строк для генерации каждым процессом
                            int countRank = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Какое количество магазинов хотите сгенерировать: ");
                                count = Convert.ToInt32(Console.ReadLine());
                                stopWatch.Restart();
                                // Если число строк нацело делится на число процессов,
                                // то поделить и присвоить новое значение,
                                // если нет, то к результату деления прибавить 1
                                countRank = count % comm.Size == 0 ? count / comm.Size : count / comm.Size + 1;
                                // Распределить количество строк для генерации по процессам
                                for (int i = 1; i < comm.Size; i++)
                                {
                                    // Уменьшить число строк для генерации
                                    count = count - countRank;  
                                    // Если после уменьшения, число неотрицательноеь
                                    if (count > -1)
                                        // Отправить i-ому процессу число строк для генерации
                                        comm.Send(countRank, i, 0);
                                    // Иначе разослать всем сообщение с 0
                                    else 
                                        comm.Send(0, i, 0);
                                }
                                // Установить остаточное значение строк для генерации
                                countRank = count > -1 ? count : count + countRank;
                                // Генерация магазинов
                                Commands.GenerateData(countRank);
                            }
                            
                            if (comm.Rank != 0)
                            {
                                // Получение значение строк от 0 процесса
                                countRank = comm.Receive<int>(0, 0);
                                // Генерация магазинов
                                Commands.GenerateData(countRank);
                            }
                            // Все процессы ждут окончания генерации
                            comm.Barrier();
                            // Обновление локальной базы данных
                            localDb = Commands.LoadingDb(comm.Rank, comm.Size);

                            // Все процессы ждут окончания обновления локальной БД
                            comm.Barrier();

                            if (comm.Rank == 0)
                            {
                                stopWatch.Stop();
                                Console.WriteLine("Магазины сгенерированы");
                            }
                            break;
                        // Сгенерировать каждому магазину по несколько отзывов
                        case "generateFeets":
                            if (comm.Rank == 0)
                            {
                                Console.WriteLine("Генерируем отзывы");
                                stopWatch.Restart();
                            }
                                
                            // Генерация отзывов
                            Commands.GenerateFeedback(localDb);
                            comm.Barrier();
                            if (comm.Rank == 0)
                            {
                                stopWatch.Stop();
                                Console.WriteLine("Отзывы сгенерированы");
                            }
                            break;
                        // Вывести список магазинов
                        case "shops":
                            int order = 1;
                            if (comm.Rank == 0)
                            {
                                Console.Write("1 - вывести 25 лучших, 0 - 25 худших: ");
                                order = Convert.ToInt32(Console.ReadLine());
                            
                                stopWatch.Restart();
                                // Собрать списки в 0 процессе
                                string[] shops = comm.Gather(JsonSerializer.Serialize(Commands.GetShops(localDb)), 0);
                                // Сборка списка списков в единый список
                                List<Shop> shopList = shops
                                            .Select(x => JsonSerializer.Deserialize<List<Shop>>(x)!)
                                            .Where(p => p != null)
                                            .Aggregate((a, b) => a.Concat(b).ToList());
                                if (shopList.Count() < 0)
                                {
                                    Console.WriteLine("Магазинов нет");
                                    stopWatch.Stop();
                                    break;
                                }
                                
                                // Перечислить 25 магазинов в порядке убывания/возрастания в зависимости от order
                                foreach (var shop in order == 1 ? 
                                shopList.OrderByDescending(p => p.Rating).Take(25) : shopList.OrderBy(p => p.Rating).Take(25))
                                {
                                    Console.WriteLine($"Id {shop.ShopId}, Address: {shop.Address},  NumberReviews: {shop.NumberReviews}, Rating: {shop.Rating}");
                                }    
                                stopWatch.Stop();
                            }
                            else
                            {
                                // Переслать локальные списки БД 0 процессу
                                comm.Gather(JsonSerializer.Serialize(Commands.GetShops(localDb)), 0);
                            }
                            // Разослать всем процессам 
                            comm.Broadcast(ref order, 0);
                            break;
                        // Удалить магазин с отзывами
                        case "delShop":
                            // Айди магазина
                            int shopId = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите Id магазина: ");
                                shopId = Convert.ToInt32(Console.ReadLine());
                                stopWatch.Restart();
                                Console.WriteLine("Удаляем магазин");
                            }    
                            // Переслать всем процессам Id магазина для удаления
                            comm.Broadcast(ref shopId, 0);
                            // Удаление магазина с отзывами
                            Commands.DeleteShop(localDb, deletedDb, shopId);
                            // Ожидание окончания удаления
                            comm.Barrier();
                            if (comm.Rank == 0)
                            {
                                stopWatch.Stop();
                                Console.WriteLine("Магазин удален");
                            }
                            break;
                        // Написать отзыв
                        case "feetback":
                            // Айди магазина
                            shopId = 0;
                            // Рейтинг в отзыве
                            int rating = 5;
                            // Текст в отзыве
                            string? feet = null;
                            if (comm.Rank == 0)
                            {
                                
                                Console.Write("Введите Id магазина: ");
                                shopId = Convert.ToInt32(Console.ReadLine());
                                Console.Write("Введите рейтинг от 1 до 5: ");
                                rating = Convert.ToInt32(Console.ReadLine());
                                Console.Write("Напишите отзыв(необязательно): ");
                                feet = Console.ReadLine();
                                stopWatch.Restart();
                                Console.WriteLine("Добавляем отзыв");
                                // Добавление отзыва
                                Commands.Feetback(shopId, rating, feet);
                            }
                            // Все процессы ждут окончания генерации
                            comm.Barrier();
                            // Обновление локальной базы данных
                            localDb = Commands.LoadingDb(comm.Rank, comm.Size);

                            // Все процессы ждут окончания обновления локальной БД
                            comm.Barrier();
                            if (comm.Rank == 0)
                            {
                                stopWatch.Stop();
                                Console.WriteLine("Отзыв добавлен");
                            }
                            break;
                        // Посмотреть список отзывов магазина
                        case "feetbacks":
                            // Айди магазина
                            shopId = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите Id магазина: ");
                                shopId = Convert.ToInt32(Console.ReadLine());
                                stopWatch.Restart();
                            }
                            // Разослать всем процессам Id магазина
                            comm.Broadcast(ref shopId, 0);
                            if (comm.Rank == 0)
                            {
                                stopWatch.Restart();
                                // Собрать списки в 0 процессе
                                string[] feetbacks = comm.Gather(JsonSerializer.Serialize(Commands.GetFeetbacks(localDb, shopId)), 0);
                                // Сборка списка списков в единый список
                                List<Feedback> feetbackList = feetbacks
                                            .Select(x => JsonSerializer.Deserialize<List<Feedback>>(x)!)
                                            .Where(p => p != null)
                                            .Aggregate((a, b) => a.Concat(b).ToList());
                                if (feetbackList.Count() < 0)
                                {
                                    Console.WriteLine("Отзывов нет");
                                    stopWatch.Stop();
                                    break;
                                }

                                // Вывести отзывы магазина
                                foreach (var feetback in feetbackList)
                                {
                                    string userFeet = feetback.UserFeedback is null ? "No feetback" : feetback.UserFeedback;
                                    Console.WriteLine($"Id {feetback.FeedbackId}, User {feetback.User.FullName}, Rating {feetback.Rating}, Feet {userFeet}");
                                }
                                stopWatch.Stop();
                            }
                            else
                            {
                                // Переслать локальные списки БД 0 процессу
                                comm.Gather(JsonSerializer.Serialize(Commands.GetFeetbacks(localDb, shopId)), 0);
                            }
                            break;
                        // Посмотреть список отзывов пользователя
                        case "userFeetbacks":
                            // Айди магазина
                            shopId = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите Id пользователя: ");
                                shopId = Convert.ToInt32(Console.ReadLine());
                                stopWatch.Restart();
                            }
                            // Разослать всем процессам Id магазина
                            comm.Broadcast(ref shopId, 0);
                            if (comm.Rank == 0)
                            {
                                stopWatch.Restart();
                                // Собрать списки в 0 процессе
                                string[] feetbacks = comm.Gather(JsonSerializer.Serialize(Commands.GetUserFeetbacks(localDb, shopId)), 0);
                                // Сборка списка списков в единый список
                                List<Feedback> feetbackList = feetbacks
                                            .Select(x => JsonSerializer.Deserialize<List<Feedback>>(x)!)
                                            .Where(p => p != null)
                                            .Aggregate((a, b) => a.Concat(b).ToList());
                                if (feetbackList.Count() < 0)
                                {
                                    Console.WriteLine("Отзывов нет");
                                    stopWatch.Stop();
                                    break;
                                }

                                // Вывести отзывы пользователя
                                foreach (var feetback in feetbackList)
                                {
                                    Console.WriteLine($"Id {feetback.FeedbackId}, User {feetback.User.FullName}, Rating {feetback.Rating}, Feet {feetback.UserFeedback}");
                                }
                                stopWatch.Stop();
                            }
                            else
                            {
                                // Переслать локальные списки БД 0 процессу
                                comm.Gather(JsonSerializer.Serialize(Commands.GetUserFeetbacks(localDb, shopId)), 0);
                            }
                            break;
                        // Удалить отзыв по Id
                        case "delFeet":
                            // Айди отзыва
                            int feetbackId = 0;
                            if (comm.Rank == 0)
                            {                                
                                Console.Write("Введите Id отзыва: ");
                                feetbackId = Convert.ToInt32(Console.ReadLine());
                                stopWatch.Restart();
                                Console.WriteLine("Удаляем отзыв");
                                // Удаление отзыва
                                Commands.DeleteFeetback(feetbackId);
                            }
                            // Все процессы дожидаются удаления отзыва
                            comm.Barrier();

                            // Обновлени локальной БД
                            localDb = Commands.LoadingDb(comm.Rank, comm.Size);
                            // Ожидание окончания обновления локальной БД
                            comm.Barrier();
                            if (comm.Rank == 0)
                            {
                                stopWatch.Stop();
                                Console.WriteLine("Отзыв удален");
                            }
                            break;
                        // Обновить БД на сервере
                        case "update":
                            if (comm.Rank == 0)
                            {
                                stopWatch.Restart();
                                Console.WriteLine("Обновляем БД на сервере"); 
                            }
                            // Обновление БД
                            Commands.UpdateDb(localDb, deletedDb);
                            // Ожидание окончания удаления
                            comm.Barrier();
                            if (comm.Rank == 0)
                            {
                                stopWatch.Stop();
                                Console.WriteLine("БД на сервере обновлена");
                            }
                            break;
                        // Неизвестная команда
                        default:
                            if (comm.Rank == 0 && command != "quit")
                                Console.WriteLine("Неизвестная команда");
                            break;
                    }

                    if (comm.Rank == 0)
                    {
                        // Вывод времени выполнения
                        TimeSpan ts = stopWatch.Elapsed;
                        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                        Console.WriteLine($"RunTime {comm.Rank} " + elapsedTime);
                    }
                    // Барьер, чтобы все процессы подождали, пока 0 процесс выведет время работы
                    comm.Barrier();
                }
            });
        }
    }
}