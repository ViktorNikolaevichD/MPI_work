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
                                                      "\ntransfer - перевести игрока в другую команду;" +
                                                      "\nsalary - изменить зарплату игроку;" +
                                                      "\nplayer - вывести информацию об игроке;" +
                                                      "\nteam - вывести состав команды;" +
                                                      "\ntransfers - вывести список трансферов игрока: ");
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
                                countRank = count > 0 ? count : 0;
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
                            // Обнвление локальной базы данных
                            localDb = Commands.LoadingDb(comm.Rank, comm.Size);

                            // Все процессы ждут окончания обновления локальной БД
                            comm.Barrier();

                            if (comm.Rank == 0)
                                Console.WriteLine("Магазины сгенерированы");
                            break;
                        // Сгенерировать каждому магазину по несколько отзывов
                        case "generateFeets":
                            if (comm.Rank == 0)
                                Console.WriteLine("Генерируем отзывы");
                            // Генерация отзывов
                            Commands.GenerateFeedback(localDb);
                            comm.Barrier();
                            if (comm.Rank == 0)
                                Console.WriteLine("Отзывы сгенерированы");
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