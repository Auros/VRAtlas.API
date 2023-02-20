using Quartz;

namespace VRAtlas.Jobs;

public class EventEndingJob : IJob
{
    public static readonly JobKey Key = new("event-ending", nameof(VRAtlas));

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