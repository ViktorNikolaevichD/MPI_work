namespace MPI_work.Entities
{
    public class Shop
    {
        // PK, Id магазина
        public int ShopId { get; set; }
        // Адрес магазина
        public string Address { get; set; }
        // Количество отзывов
        public int NumberReviews { get; set; } = 0;
        // Средняя
        public decimal Rating { get; set; } = 0;
    }
}
