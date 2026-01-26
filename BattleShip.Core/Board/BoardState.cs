public class BoardState
{
    public int Size { get; }
    private readonly CellState[,] _cells;

    public BoardState(int size = 10)
    {
        Size = size;
        _cells = new CellState[size, size];
    }

    public CellState this[int row, int col]
    {
        get => _cells[row, col];
        set => _cells[row, col] = value;
    }

    public CellState this[Coordinate c]
    {
        get => _cells[c.Row, c.Col];
        set => _cells[c.Row, c.Col] = value;
    }

    public bool IsInside(Coordinate c) =>
        c.Row >= 0 && c.Row < Size && c.Col >= 0 && c.Col < Size;

    public IEnumerable<Coordinate> AllCoordinates()
    {
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                yield return new Coordinate(r, c);
    }
}
