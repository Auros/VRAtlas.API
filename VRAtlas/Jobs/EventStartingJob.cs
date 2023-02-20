using Quartz;

namespace VRAtlas.Jobs;

public class EventStartingJob : IJob
{
    public static readonly JobKey Key = new("event-starting", nameof(VRAtlas));

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Task.Yield();
        }
        catch
        {

        }
    }
}