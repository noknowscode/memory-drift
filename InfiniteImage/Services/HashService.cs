namespace InfiniteImage.Services;

public static class HashService
{
    public static int HashString(string str)
    {
        int hash = 0;
        foreach (char c in str)
        {
            hash = ((hash << 5) - hash) + c;
        }
        return Math.Abs(hash);
    }

    public static double SeededRandom(int seed)
    {
        const long a = 1664525;
        const long c = 1013904223;
        const long m = 0x100000000;

        long next = (a * (uint)seed + c) % m;
        return (double)next / m;
    }

    public static double RandomAt(int baseSeed, int offset)
    {
        return SeededRandom(baseSeed + offset * 777);
    }
}
