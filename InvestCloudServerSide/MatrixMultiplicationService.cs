using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InvestCloudServerSide
{
    public class ApiResponse
    {
        public int[] Value { get; set; }
        public string Cause { get; set; }
        public bool Success { get; set; }
    }

    public class MatrixMultiplicationService
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly Stopwatch stopwatch = new Stopwatch();

        public void StartTimer()
        {
            stopwatch.Restart();
        }

        public void StopAndPrintTimer(string taskName)
        {
            stopwatch.Stop();
            Console.WriteLine($"{taskName} took {stopwatch.ElapsedMilliseconds} ms");
        }
        public async Task InitializeDatasets(int size)
        {
            await client.GetAsync($"https://recruitment-test.investcloud.com/api/numbers/init/{size}");
        }

        public async Task<int[]> GetRoworCoulmnAsync(string dataset, string rowOrcolumn, int idx)
        {
            var response = await client.GetStringAsync($"https://recruitment-test.investcloud.com/api/numbers/{dataset}/{rowOrcolumn}/{idx}");
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(response);
            return apiResponse.Value;
        }

        public async Task<int[,]> GetMatrixAsync(string dataset, int size)
        {
            var tasks = new Task<int[]>[size];

            for (int i = 0; i < size; i++)
            {
                tasks[i] = GetRoworCoulmnAsync(dataset, "row", i);
            }

            var rows = await Task.WhenAll(tasks);

            int[,] matrix = new int[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    matrix[i, j] = rows[i][j];
                }
            }

            return matrix;
        }

        public double[,] MultiplyMatricesParallel(int[,] A, int[,] B)
        {
            int n = A.GetLength(0);
            double[,] result = new double[n, n];

            Parallel.For(0, n, i =>
            {
                for (int j = 0; j < n; j++)
                {
                    result[i, j] = 0;
                    for (int k = 0; k < n; k++)
                    {
                        result[i, j] += A[i, k] * B[k, j];
                    }
                }
            });

            return result;
        }

        public string ConvertMatrixToString(double[,] matrix)
        {
            StringBuilder sb = new StringBuilder();
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    sb.Append((int)matrix[row, col]);
                }
            }

            return sb.ToString();
        }

        public string ComputeMd5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                string result = Regex.Replace(input, @"\D", "");
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(result));
                return Encoding.UTF8.GetString(hashBytes);//DO NOT use Hex
                // return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public async Task SubmitHashAsync(string hash)
        {
            var content = new StringContent($"\"{hash}\"", Encoding.UTF8, "application/json");
            Console.WriteLine(content.ReadAsStringAsync().Result);
            var response = await client.PostAsync("https://recruitment-test.investcloud.com/api/numbers/validate", content);
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        public async Task Run(int size)
        {
            // Step 1: Initialize datasets
            await InitializeDatasets(size);
            StartTimer();

            // Step 2: Fetch matrices A and B in parallel
            var taskA = GetMatrixAsync("A", size);
            var taskB = GetMatrixAsync("B", size);
            await Task.WhenAll(taskA, taskB);
            var A = await taskA;
            var B = await taskB;

            // Step 3: Perform matrix multiplication
            var result = MultiplyMatricesParallel(A, B);

            // Step 4: Convert the result matrix to a string and compute MD5 hash
            string resultString = ConvertMatrixToString(result);
            string resultHash = ComputeMd5Hash(resultString);

            // Step 5: Submit the hash
            await SubmitHashAsync(resultHash);
            StopAndPrintTimer("Hash Submission");
        }

    }
}


