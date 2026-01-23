using InfiniteImage.Models;

namespace InfiniteImage.Services;

/// <summary>
/// Manages chunk generation and caching with efficient LRU.
/// </summary>
public class ChunkService
{
    private readonly Dictionary<string, Chunk> _cache = new();
    private readonly LinkedList<string> _lruOrder = new();
    private readonly Dictionary<string, LinkedListNode<string>> _lruNodes = new();
    private readonly object _lock = new();

    private static readonly string[] ArtistNames =
    [
        "Luna Nova", "Azure Storm", "Cosmic Ray", "Stellar Dream",
        "Nova Bright", "Eclipse Moon", "Nebula Star", "Solar Wind",
        "Galaxy Core", "Photon Light", "Quantum Flux", "Void Walker"
    ];

    private static readonly string[] ArtworkPrefixes =
    [
        "Ethereal", "Cosmic", "Infinite", "Abstract", "Luminous",
        "Mystic", "Celestial", "Astral", "Primal", "Temporal"
    ];

    private static readonly string[] ArtworkSuffixes =
    [
        "Dreams", "Echoes", "Visions", "Reflections", "Horizons",
        "Passages", "Fragments", "Memories", "Journeys", "Whispers"
    ];

    /// <summary>
    /// Gets or generates a chunk at the specified coordinates with O(1) LRU.
    /// </summary>
    public Chunk GetChunk(int cx, int cy, int cz)
    {
        var key = $"{cx},{cy},{cz}";

        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var chunk))
            {
                // Move to end of LRU using O(1) node lookup
                if (_lruNodes.TryGetValue(key, out var node))
                {
                    _lruOrder.Remove(node);
                    var newNode = _lruOrder.AddLast(key);
                    _lruNodes[key] = newNode;
                }
                return chunk;
            }

            // Generate new chunk
            chunk = GenerateChunk(cx, cy, cz);
            _cache[key] = chunk;
            var addedNode = _lruOrder.AddLast(key);
            _lruNodes[key] = addedNode;

            // Evict oldest if over limit
            while (_cache.Count > CanvasConfig.MaxCacheSize && _lruOrder.First != null)
            {
                var oldest = _lruOrder.First.Value;
                _lruOrder.RemoveFirst();
                _lruNodes.Remove(oldest);
                _cache.Remove(oldest);
            }

            return chunk;
        }
    }

    /// <summary>
    /// Gets all active chunks around a camera position.
    /// </summary>
    public List<Chunk> GetActiveChunks(int cameraCX, int cameraCY, int cameraCZ)
    {
        var chunks = new List<Chunk>();

        for (int dx = -CanvasConfig.RenderRadiusXY; dx <= CanvasConfig.RenderRadiusXY; dx++)
        {
            for (int dy = -CanvasConfig.RenderRadiusXY; dy <= CanvasConfig.RenderRadiusXY; dy++)
            {
                for (int dz = -CanvasConfig.RenderRadiusZ; dz <= CanvasConfig.RenderRadiusZ; dz++)
                {
                    chunks.Add(GetChunk(cameraCX + dx, cameraCY + dy, cameraCZ + dz));
                }
            }
        }

        return chunks;
    }

    private Chunk GenerateChunk(int cx, int cy, int cz)
    {
        var seed = HashService.HashString($"chunk3d_{cx}_{cy}_{cz}");
        var planes = new List<ImagePlane>();

        for (int i = 0; i < CanvasConfig.PlanesPerChunk; i++)
        {
            var s = seed + i * 777;

            var width = (float)(CanvasConfig.PlaneMinSize +
                HashService.RandomAt(s, 3) * (CanvasConfig.PlaneMaxSize - CanvasConfig.PlaneMinSize));

            var aspectRatio = (float)(0.7 + HashService.RandomAt(s, 4) * 0.6);

            var plane = new ImagePlane
            {
                Id = $"{cx}_{cy}_{cz}_{i}",
                LocalX = (float)(HashService.RandomAt(s, 0) * CanvasConfig.ChunkSize - CanvasConfig.ChunkSize / 2.0),
                LocalY = (float)(HashService.RandomAt(s, 1) * CanvasConfig.ChunkSize - CanvasConfig.ChunkSize / 2.0),
                LocalZ = (float)(HashService.RandomAt(s, 2) * CanvasConfig.ChunkSize),
                Width = width,
                Height = width * aspectRatio,
                RotationY = (float)((HashService.RandomAt(s, 5) - 0.5) * 20),
                RotationX = (float)((HashService.RandomAt(s, 6) - 0.5) * 10),
                ImageIndex = (int)(HashService.RandomAt(s, 7) * 1_000_000),
                Hue = (int)(HashService.RandomAt(s, 8) * 360),
                Title = GenerateTitle(s),
                Artist = ArtistNames[(int)(HashService.RandomAt(s, 10) * ArtistNames.Length)],
                Year = 2020 + (int)(HashService.RandomAt(s, 11) * 6)
            };

            // Pre-compute trigonometric values
            plane.CacheTrigValues();

            planes.Add(plane);
        }

        return new Chunk(cx, cy, cz, planes);
    }

    private string GenerateTitle(int seed)
    {
        var prefixIdx = (int)(HashService.RandomAt(seed, 9) * ArtworkPrefixes.Length);
        var suffixIdx = (int)(HashService.RandomAt(seed, 12) * ArtworkSuffixes.Length);
        return $"{ArtworkPrefixes[prefixIdx]} {ArtworkSuffixes[suffixIdx]}";
    }

    public int CachedChunkCount => _cache.Count;
}
