namespace MPI_work.Entities
{
    public class Feedback
    {
        // PK, Id отзыва
        public int FeedbackId { get; set; }

        // FK, Id мазагина
        public int ShopId { get; set; }
        // FK, навигационное свойство на таблицу Shop
        public Shop Shop { get; set; }

        // FK, Id пользователя
        public int UserId { get; set; }
        // FK, навигационное свойство на таблицу User
        public User User { get; set; }

        // Оценка от пользователя
        public int Rating { get; set; }
        // Отзыв пользователя, может принимать значение null
        public string? UserFeedback {  get; set; }
    }
}
