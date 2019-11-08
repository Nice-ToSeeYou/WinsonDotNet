using WinsonDotNet.Main;

namespace WinsonDotNet
{
    internal static class Program
    {
        public static void Main(string[] args)
            => Shards.StartSharding().GetAwaiter().GetResult();
    }
}