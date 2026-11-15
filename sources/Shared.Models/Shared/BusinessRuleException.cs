namespace Shared.Models.Shared
{
    /// <summary>
    /// Eine Aktion passt nicht zum aktuellen Zustand (z. B. Turnier schon gestartet, Match läuft nicht, Bot am Wurf).
    /// Die API antwortet mit 409 und der Meldung; Programmierfehler bleiben InvalidOperationException (500).
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message)
        {
        }
    }
}
