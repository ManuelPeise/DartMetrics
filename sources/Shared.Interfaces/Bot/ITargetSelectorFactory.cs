using Data.DartContext.Entities.Bot;

namespace Shared.Interfaces.Bot
{
    /// <summary>
    /// Liefert die Zielwahl für einen Bot-Spieler: mit dem beim Spielstart gemerkten Modell (Hybrid), sonst das Regelwerk.
    /// Fehlt die Datei, passt die SHA-256 oder das Schema nicht, wird regelbasiert gespielt (und protokolliert).
    /// </summary>
    public interface ITargetSelectorFactory
    {
        ITargetSelector For(DartBotModelEntity? targetModel);
    }
}
