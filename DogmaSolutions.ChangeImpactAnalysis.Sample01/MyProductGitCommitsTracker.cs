namespace DogmaSolutions.ChangeImpactAnalysis.Sample01;

public sealed class MyProductGitCommitsTracker : GitCommitsTracker
{
    public MyProductGitCommitsTracker(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override Task<IEnumerable<string>> GetImpactedComponents(
        IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        CancellationToken cancellationToken = default)
    {
        var retList = new List<string>();
        retList.Add("CommonUtils.C");
        return Task.FromResult<IEnumerable<string>>(retList);
    }
}