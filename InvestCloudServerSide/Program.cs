namespace InvestCloudServerSide
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            int size = 1000; 
            var service = new MatrixMultiplicationService();
            await service.Run(size);
        }
    }
}
