namespace Asteroids.Shared.Options
{
    public record VersionedValue<T>(long Version, T Value);

    public class CompareAndSwapRequest
    {
        public string Key { get; set; } = null!;
        public string? Unmodified { get; set; }
        public string Modified { get; set; } = null!;
    }

    public interface IStorageService
    {
        Task<VersionedValue<string>> StrongGet(string key);
        Task<VersionedValue<string>> EventualGet(string key);
        Task CompareAndSwap(string key, string oldValue, string newValue);
    }
}
