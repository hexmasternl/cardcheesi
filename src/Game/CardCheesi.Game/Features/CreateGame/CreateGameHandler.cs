using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace CardCheesi.Game.Features.CreateGame;

public sealed class CreateGameHandler : ICommandHandler<CreateGameCommand, CreateGameResult>
{
    private readonly IGameRepository _repo;

    public CreateGameHandler(IGameRepository repo) => _repo = repo;

    public async Task<CreateGameResult> Handle(CreateGameCommand command, CancellationToken ct)
    {
        const int maxAttempts = 5;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var code = GenerateGameCode();
            var existing = await _repo.GetByCodeAsync(code, ct);
            if (existing is not null) continue;

            var game = GameFactory.CreateWaiting(command.PlayerName, code, command.PlayerId);
            try
            {
                await _repo.SaveAsync(game, ct);
                return new CreateGameResult(game.Id, game.GameCode);
            }
            catch (DbUpdateException)
            {
                // unique constraint race — retry
            }
        }

        throw new DomainException("Could not generate a unique game code. Please try again.");
    }

    private static string GenerateGameCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
}
