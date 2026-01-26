namespace BattleShipGame.BattleShip.Core.Strategy
{
    public interface IShotStrategy
    {
        Coordinate GetNextShot(BoardState boardState);
        void RegisterShotResult(Coordinate coord, CellState result);
    }
}
