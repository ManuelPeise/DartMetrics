namespace Shared.Interfaces.Bot
{
    public interface IRandomSource
    {
        /// <summary>Gleichverteilt in [0, 1).</summary>
        double NextDouble();
    }
}
