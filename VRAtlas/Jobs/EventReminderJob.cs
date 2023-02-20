using Quartz;

namespace VRAtlas.Jobs;

public class EventReminderJob : IJob
{
    public static readonly JobKey Key = new("event-reminder", nameof(VRAtlas));

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