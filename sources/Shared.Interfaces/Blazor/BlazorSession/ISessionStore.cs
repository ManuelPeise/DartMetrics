namespace Shared.Interfaces.Blazor.BlazorSession
{
    public interface ISessionStore
    {
        Task<string> CreateAsync(
            Models.Blazor.BlazorSession session,
            CancellationToken cancellationToken = default);

        Task<Models.Blazor.BlazorSession?> GetAsync(
            string sessionId,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            string sessionId,
            Models.Blazor.BlazorSession session,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string sessionId,
            CancellationToken cancellationToken = default);
    }
}
