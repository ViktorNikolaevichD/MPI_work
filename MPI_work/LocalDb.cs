using MPI_work.Entities;

namespace MPI_work
{
    // Класс для хранение списков таблиц
    public class LocalDb
    {
        public List<Shop> ShopList {  get; set; }
        public List<User> UserList { get; set; }
        public List<Feedback> FeedbackList { get; set; }
    }
}
