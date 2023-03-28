using Quartz;
using VRAtlas.Services;

namespace VRAtlas.Jobs;

public class CrosspostSynchronizationJob : IJob
{
    public static readonly JobKey Key = new("crosspost-synchronization", nameof(VRAtlas));

    private readonly ICrosspostingService _crosspostingService;

    public CrosspostSynchronizationJob(ICrosspostingService crosspostingService)
    {
        _crosspostingService = crosspostingService;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _crosspostingService.TriggerCrosspostingSynchronizationRoutine();
        return Task.CompletedTask;
    }
}
