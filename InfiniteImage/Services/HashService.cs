namespace InfiniteImage.Services;

/// <summary>
/// Provides deterministic hashing and seeded random functions.
/// </summary>
public static class HashService
{
    /// <summary>
    /// Generates a deterministic hash from a string.
    /// </summary>
    public static int HashString(string str)
    {
        int hash = 0;
        foreach (char c in str)
        {
            hash = ((hash << 5) - hash) + c;
            hash &= hash; // Convert to 32-bit integer
        }
        return Math.Abs(hash);
    }

    /// <summary>
    /// Generates a seeded random number between 0 and 1.
    /// Uses a simple LCG (Linear Congruential Generator).
    /// </summary>
    public static double SeededRandom(int seed)
    {
        // LCG parameters (Numerical Recipes)
        const long a = 1664525;
        const long c = 1013904223;
        const long m = 0x100000000; // 2^32

        long next = (a * (uint)seed + c) % m;
        return (double)next / m;
    }

    /// <summary>
    /// Gets a random value at a specific offset from a base seed.
    /// </summary>
    public static double RandomAt(int baseSeed, int offset)
    {
        return SeededRandom(baseSeed + offset * 777);
    }
}
