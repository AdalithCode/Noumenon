using MentorRouletteTracker.DutyTracking;

namespace MentorRouletteTracker;

internal sealed class PlayerAnonymizer
{
    private readonly Dictionary<(string Name, string World), int> numbers = new();
    private readonly Dictionary<(string Name, string World), string> jobs = new();

    internal void Clear()
    {
        numbers.Clear();
        jobs.Clear();
    }

    private int Number(string name, string world)
    {
        var key = (name, world);
        if (!numbers.TryGetValue(key, out var number)) numbers.Add(key, number = numbers.Count + 1);
        return number;
    }

    internal void Refresh(IEnumerable<DutyRecord> records)
    {
        jobs.Clear();
        var observations = new List<(string Name, string World, string Job, DateTime Time)>();
        foreach (var record in records)
        {
            foreach (var player in record.Party)
            {
                if (string.IsNullOrWhiteSpace(player.Name)) continue;
                Number(player.Name, player.World);
                if (IsJobAbbreviation(player.Job))
                    observations.Add((player.Name, player.World, player.Job, record.Timestamp));
            }
            if (!string.IsNullOrWhiteSpace(record.RecorderName)) Number(record.RecorderName, record.RecorderWorld);
            foreach (var death in record.DeathTimestamps)
                if (!string.IsNullOrWhiteSpace(death.Name)) Number(death.Name, death.World);
        }
        foreach (var player in observations.GroupBy(value => (value.Name, value.World)))
            jobs[player.Key] = player.GroupBy(value => value.Job).OrderByDescending(group => group.Count())
                .ThenByDescending(group => group.Max(value => value.Time)).First().Key;
    }

    private static bool IsJobAbbreviation(string? job) => job?.Length == 3 && job.All(char.IsAsciiLetterUpper);

    internal string Name(string name, string world, bool useJobs, string? job = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unknown player";
        var number = Number(name, world);
        if (useJobs)
        {
            job ??= jobs.GetValueOrDefault((name, world));
            if (IsJobAbbreviation(job)) return $"{job} {number}";
        }
        return $"Player {number}";
    }
}
