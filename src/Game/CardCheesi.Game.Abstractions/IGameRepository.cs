namespace CardCheesi.Game.Abstractions;

public interface IGameRepository
{
    Task SaveAsync(GameState gameState, CancellationToken cancellationToken = default);
    Task<GameState?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GameState?> GetByCodeAsync(string gameCode, CancellationToken cancellationToken = default);
}
