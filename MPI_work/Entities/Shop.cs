namespace MPI_work.Entities
{
    public class Shop
    {
        // PK, Id магазина
        public int ShopId { get; set; }
        // Адрес магазина
        public string Address { get; set; }
        // Количество отзывов
        public int NumberReviews { get; set; }
        // Средняя
        public float Rating { get; set; }
    }
}
