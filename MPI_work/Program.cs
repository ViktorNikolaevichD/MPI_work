namespace MPI_work
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MPI.Environment.Run(ref args, comm =>
            {

            });
        }
    }
}