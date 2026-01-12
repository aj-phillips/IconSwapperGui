using System;

namespace IconSwapperGui.Core.PixelArt;

public sealed class PixelArtDocument
{
    private uint[] _pixelsArgb = Array.Empty<uint>();

    public int Rows { get; private set; }
    public int Columns { get; private set; }

    public ReadOnlySpan<uint> PixelsArgb => _pixelsArgb;

    public PixelArtDocument(int rows, int columns)
    {
        Resize(rows, columns);
    }

    public void Resize(int rows, int columns)
    {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));

        Rows = rows;
        Columns = columns;
        _pixelsArgb = new uint[checked(rows * columns)];
    }

    public void Clear(uint argb)
    {
        if (_pixelsArgb.Length == 0) return;
        Array.Fill(_pixelsArgb, argb);
    }

    public void SetCell(int index, uint argb)
    {
        if ((uint)index >= (uint)_pixelsArgb.Length) return;
        _pixelsArgb[index] = argb;
    }

    public uint GetCell(int index)
    {
        if ((uint)index >= (uint)_pixelsArgb.Length) throw new ArgumentOutOfRangeException(nameof(index));
        return _pixelsArgb[index];
    }

    public void FloodFill(int startIndex, uint replacementArgb, Action<int>? onChanged = null)
    {
        if ((uint)startIndex >= (uint)_pixelsArgb.Length) return;

        var target = _pixelsArgb[startIndex];
        if (target == replacementArgb) return;

        var visited = new bool[_pixelsArgb.Length];
        var queue = new Queue<int>();
        queue.Enqueue(startIndex);
        visited[startIndex] = true;

        while (queue.Count > 0)
        {
            var index = queue.Dequeue();
            if (_pixelsArgb[index] != target) continue;

            _pixelsArgb[index] = replacementArgb;
            onChanged?.Invoke(index);

            var row = index / Columns;
            var col = index % Columns;

            EnqueueIfUnvisited(index - 1, col > 0);
            EnqueueIfUnvisited(index + 1, col < Columns - 1);
            EnqueueIfUnvisited(index - Columns, row > 0);
            EnqueueIfUnvisited(index + Columns, row < Rows - 1);

            void EnqueueIfUnvisited(int neighborIndex, bool condition)
            {
                if (!condition) return;
                if (visited[neighborIndex]) return;
                visited[neighborIndex] = true;
                queue.Enqueue(neighborIndex);
            }
        }
    }
}
