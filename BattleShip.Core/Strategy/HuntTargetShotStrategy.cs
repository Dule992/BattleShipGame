namespace BattleShipGame.BattleShip.Core.Strategy
{
    internal class HuntTargetShotStrategy : IShotStrategy
    {
        private readonly BoardState _board;
        private readonly Queue<Coordinate> _targetQueue = new();
        private readonly List<Coordinate> _hitCluster = new(); // current ship hits

        public HuntTargetShotStrategy(BoardState board)
        {
            _board = board;
        }

        public Coordinate GetNextShot(BoardState nextShoot)
        {
            // If we have target cells queued, use them first.
            if (_targetQueue.Count > 0)
            {
                var next = _targetQueue.Dequeue();
                if (_board[next] == CellState.Unknown)
                    return next;
                return GetNextShot(nextShoot);
            }

            // HUNT MODE: parity-based search.
            foreach (var coord in GetHuntPattern())
            {
                if (_board[coord] == CellState.Unknown)
                    return coord;
            }

            // Fallback: any unknown cell (should not happen often)
            var unknown = _board.AllCoordinates()
                .FirstOrDefault(c => _board[c] == CellState.Unknown);

            return unknown;
        }

        public void RegisterShotResult(Coordinate coord, CellState result)
        {
            _board[coord] = result;

            if (result == CellState.Hit)
            {
                _hitCluster.Add(coord);
                EnqueueAdjacentUnknowns(coord);
            }
            else if (result == CellState.Sunk)
            {
                _hitCluster.Clear();
                _targetQueue.Clear();
            }
        }

        private IEnumerable<Coordinate> GetHuntPattern()
        {
            // Simple checkerboard: (row + col) % 2 == 0
            for (int r = 0; r < _board.Size; r++)
            {
                for (int c = 0; c < _board.Size; c++)
                {
                    if ((r + c) % 2 == 0)
                        yield return new Coordinate(r, c);
                }
            }
        }

        private void EnqueueAdjacentUnknowns(Coordinate hit)
        {
            var neighbours = new[]
            {
            new Coordinate(hit.Row - 1, hit.Col),
            new Coordinate(hit.Row + 1, hit.Col),
            new Coordinate(hit.Row, hit.Col - 1),
            new Coordinate(hit.Row, hit.Col + 1),
        };

            foreach (var n in neighbours)
            {
                if (_board.IsInside(n) && _board[n] == CellState.Unknown)
                {
                    _targetQueue.Enqueue(n);
                }
            }
        }
    }
}
