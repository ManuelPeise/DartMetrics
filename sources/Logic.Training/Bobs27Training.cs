using Data.DartContext.Configurations.Training;
using Data.DartContext.Entities.Training;
using Logic.Shared.Rules;
using Shared.Enums.Training;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Training;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Shared;
using Shared.Models.Training;

namespace Logic.Training
{
    /// <summary>
    /// Bob's 27: Start mit 27 Punkten, 21 Runden auf D1 … D20 und Bull, je Zug die Anzahl Treffer (0–3).
    /// Regeln und Ablauf (Reihenfolge, Ausscheiden, Sieger) in Logic.Shared.Rules.Bobs27Rules/Bobs27Game;
    /// gespeichert werden nur die Züge, der Stand wird daraus jedes Mal neu berechnet.
    /// </summary>
    public class Bobs27Training : TrainingBase, IBobs27Training
    {
        private const int RecentTurnCount = 10;

        public Bobs27Training(
            IUnitOfWork unitOfWork,
            IMessageLog messageLog,
            IClaimsHelper claimsHelper) : base(unitOfWork, messageLog, claimsHelper)
        {
        }

        public async Task<Bobs27TrainingState?> GetRunningTrainingAsync()
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var sessionId = await FindRunningSessionIdAsync(currentUser.UserId, TrainingTypeEnum.Bobs27Training);

                return sessionId is null
                    ? null
                    : ToState(await LoadBobs27Training(sessionId.Value, currentUser.UserId));
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading running Bob's 27 training.", nameof(Bobs27Training), exception);
                throw;
            }
        }

        public async Task<Bobs27TrainingState> StartTrainingAsync(StartBobs27TrainingModel model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var names = (model.PlayerNames ?? new List<string>()).Select(n => n?.Trim() ?? "").ToList();

                if (names.Count is < 1 or > Bobs27TrainingPlayerConfiguration.MaxPlayers)
                {
                    throw new ArgumentException($"Bob's 27 needs 1 to {Bobs27TrainingPlayerConfiguration.MaxPlayers} players.");
                }

                if (names.Any(n => n.Length is 0 or > Bobs27TrainingPlayerConfiguration.NameMaxLength))
                {
                    throw new ArgumentException($"Player names must have 1 to {Bobs27TrainingPlayerConfiguration.NameMaxLength} characters.");
                }

                var training = new Bobs27TrainingEntity
                {
                    IsBeginnerMode = model.IsBeginnerMode,
                    Players = names.Select((name, index) => new Bobs27TrainingPlayerEntity
                    {
                        PlayerNumber = index + 1,
                        Name = name
                    }).ToList()
                };

                var session = new TrainingSessionEntity
                {
                    UserId = currentUser.UserId,
                    StartedAt = DateTime.UtcNow,
                    TrainingType = TrainingTypeEnum.Bobs27Training,
                    Bobs27Training = training
                };

                await FinishOpenSessionsAsync(currentUser.UserId, TrainingTypeEnum.Bobs27Training);
                await UnitOfWork.DartTrainingUnitOfWork.TrainingSessionRepository.AddAsync(session);
                await UnitOfWork.SaveChangesAsync();

                training.TrainingSession = session;
                return ToState(training);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error starting Bob's 27 training.", nameof(Bobs27Training), exception);
                throw;
            }
        }

        public async Task<Bobs27TrainingState> AddTurnAsync(Bobs27TrainingTurnModel model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                if (!Bobs27Rules.IsValidHits(model.Hits))
                {
                    throw new ArgumentException("Hits must be between 0 and 3.");
                }

                var training = await LoadBobs27Training(model.TrainingId, currentUser.UserId);

                if (training.TrainingSession.FinishedAt.HasValue)
                {
                    throw new BusinessRuleException("Training is already finished.");
                }

                var game = Replay(training);

                if (game.IsFinished)
                {
                    throw new BusinessRuleException("The game is already over.");
                }

                // Zweites Gerät/Tab oder doppelt gesendet: nur der erwartete nächste Zug wird angenommen.
                if (model.TurnNumber != game.Turns.Count + 1)
                {
                    throw new BusinessRuleException($"Expected turn {game.Turns.Count + 1}.");
                }

                var turn = game.Play(model.Hits);

                training.Turns.Add(new Bobs27TrainingTurnEntity
                {
                    TurnNumber = turn.TurnNumber,
                    PlayerNumber = turn.PlayerNumber,
                    Round = turn.Round,
                    Hits = turn.Hits,
                    ScoreBefore = turn.ScoreBefore,
                    ScoreAfter = turn.ScoreAfter,
                    IsBust = turn.IsBust
                });

                // Spiel vorbei (alle raus oder Runde 21 gespielt): Training ist fertig.
                if (game.IsFinished)
                {
                    training.TrainingSession.FinishedAt = DateTime.UtcNow;
                }

                await UnitOfWork.SaveChangesAsync();

                return ToState(training, game);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error adding Bob's 27 turn.", nameof(Bobs27Training), exception);
                throw;
            }
        }

        public async Task<Bobs27TrainingState> UndoLastTurnAsync(Bobs27TrainingUndoModel model)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await LoadBobs27Training(model.TrainingId, currentUser.UserId);
                var game = Replay(training);

                // Nach „Beenden“ (Spiel noch nicht vorbei) gibt es kein Zurück; direkt nach dem Spielende schon.
                if (training.TrainingSession.FinishedAt.HasValue && !game.IsFinished)
                {
                    throw new BusinessRuleException("Training is already finished.");
                }

                var last = training.Turns.OrderBy(t => t.TurnNumber).LastOrDefault()
                    ?? throw new BusinessRuleException("There is no turn to undo.");

                if (model.TurnNumber != last.TurnNumber)
                {
                    throw new BusinessRuleException($"Only the last turn ({last.TurnNumber}) can be undone.");
                }

                training.Turns.Remove(last);
                UnitOfWork.DartTrainingUnitOfWork.Bobs27TrainingTurnRepository.Delete(last);

                // Das Spielende wird mit dem letzten Zug zurückgenommen.
                training.TrainingSession.FinishedAt = null;

                await UnitOfWork.SaveChangesAsync();

                return ToState(training);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error undoing Bob's 27 turn.", nameof(Bobs27Training), exception);
                throw;
            }
        }

        public async Task FinishTrainingAsync(int trainingId)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                var training = await LoadBobs27Training(trainingId, currentUser.UserId);

                if (training.TrainingSession.FinishedAt.HasValue)
                {
                    throw new BusinessRuleException("Training is already finished.");
                }

                training.TrainingSession.FinishedAt = DateTime.UtcNow;

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error finishing Bob's 27 training.", nameof(Bobs27Training), exception);
                throw;
            }
        }

        public async Task<Bobs27TrainingState> GetTrainingAsync(int trainingId)
        {
            try
            {
                EnsureCurrentUserIsValid(out var currentUser);

                return ToState(await LoadBobs27Training(trainingId, currentUser.UserId));
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading Bob's 27 training.", nameof(Bobs27Training), exception);
                throw;
            }
        }

        private static Bobs27Game Replay(Bobs27TrainingEntity training) =>
            Bobs27Game.Replay(
                training.Players.Count,
                training.IsBeginnerMode,
                training.Turns.OrderBy(t => t.TurnNumber).Select(t => t.Hits));

        private static Bobs27TrainingState ToState(Bobs27TrainingEntity training, Bobs27Game? game = null)
        {
            game ??= Replay(training);

            var names = training.Players.ToDictionary(p => p.PlayerNumber, p => p.Name);
            var isFinished = game.IsFinished || training.TrainingSession.FinishedAt.HasValue;

            return new Bobs27TrainingState
            {
                TrainingId = training.TrainingSessionId,
                StartedAt = training.TrainingSession.StartedAt,
                IsBeginnerMode = training.IsBeginnerMode,
                IsFinished = isFinished,
                Round = game.Round,
                TargetLabel = game.TargetLabel,
                TargetValue = game.TargetValue,
                CurrentPlayerNumber = isFinished ? null : game.CurrentPlayer,
                NextTurnNumber = game.Turns.Count + 1,
                // Nach „Beenden“ mitten im Spiel nicht mehr; nach dem regulären Spielende schon (Vertipper beim letzten Zug).
                CanUndo = game.Turns.Count > 0 && (!training.TrainingSession.FinishedAt.HasValue || game.IsFinished),
                MaxScore = Bobs27Rules.MaxScore,
                Winners = game.IsFinished ? game.Winners.ToList() : [],
                Players = game.Players.Select(p => new Bobs27PlayerResult
                {
                    Number = p.Number,
                    Name = names.GetValueOrDefault(p.Number, $"Spieler {p.Number}"),
                    Score = p.Score,
                    IsEliminated = p.IsEliminated,
                    EliminatedInRound = p.EliminatedInRound,
                    Hits = p.Hits,
                    RoundsPlayed = p.RoundsPlayed,
                    RoundsWithHit = p.RoundsWithHit,
                    HitRate = p.RoundsPlayed == 0
                        ? 0
                        : Math.Round(p.Hits * 100m / (p.RoundsPlayed * Bobs27Rules.DartsPerRound), 1)
                }).ToList(),
                RecentTurns = game.Turns
                    .Reverse()
                    .Take(RecentTurnCount)
                    .Select(t => new Bobs27TurnItem
                    {
                        TurnNumber = t.TurnNumber,
                        PlayerNumber = t.PlayerNumber,
                        Round = t.Round,
                        TargetLabel = Bobs27Rules.TargetLabel(t.Round),
                        Hits = t.Hits,
                        ScoreBefore = t.ScoreBefore,
                        ScoreAfter = t.ScoreAfter,
                        IsBust = t.IsBust
                    })
                    .ToList()
            };
        }
    }
}
