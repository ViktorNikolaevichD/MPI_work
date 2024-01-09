namespace MPI_work.Entities
{
    public class User
    {
        // PK, Id пользователя
        public int UserId {  get; set; }
        // ФИО пользователя
        public string FullName {  get; set; }
        // Возраст пользователя, может принимаеть значение null
        public int Age {  get; set; }
    }
}
