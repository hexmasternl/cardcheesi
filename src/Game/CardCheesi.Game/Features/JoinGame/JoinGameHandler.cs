using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.Features.JoinGame;

public sealed class JoinGameHandler : ICommandHandler<JoinGameCommand, JoinGameResult>
{
    private readonly IGameRepository _repo;

    public JoinGameHandler(IGameRepository repo) => _repo = repo;

    public async Task<JoinGameResult> Handle(JoinGameCommand command, CancellationToken ct)
    {
        var game = await _repo.GetByCodeAsync(command.GameCode, ct)
            ?? throw new NotFoundException($"Game with code '{command.GameCode}' not found.");

        if (game.Status != GameStatus.Waiting)
            throw new DomainException("Game is not accepting new players.");

        if (game.Players.Count >= 4)
            throw new DomainException("Game is already full.");

        if (game.Players.Any(p => p.Id == command.PlayerId))
            throw new DomainException("You have already joined this game.");

        var newPlayer = GameFactory.CreatePlayer(command.PlayerId, command.PlayerName);
        var updatedGame = game.AddPlayer(newPlayer);

        await _repo.SaveAsync(updatedGame, ct);

        return new JoinGameResult(updatedGame.Id, command.PlayerId, updatedGame.GameCode);
    }
}
