namespace Data.DartContext.Entities
{
    /// <summary>
    /// Optimistische Nebenläufigkeit: Die UnitOfWork vergibt bei jeder Änderung einen neuen Stempel.
    /// Speichern zwei Vorgänge denselben Datensatz gleichzeitig, schlägt der zweite mit DbUpdateConcurrencyException fehl
    /// statt die Änderung des ersten still zu überschreiben.
    /// </summary>
    public interface IConcurrencyStamped
    {
        Guid ConcurrencyStamp { get; set; }
    }
}
