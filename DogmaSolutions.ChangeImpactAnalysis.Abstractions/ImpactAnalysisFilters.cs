using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DogmaSolutions.ChangeImpactAnalysis;

public class ImpactAnalysisFilters : INotifyPropertyChanged
{
    private string _gitCommitHashes;
    private string _taskIds;
    private string _forcedNodes;

    public string GitCommitHashes
    {
        get => _gitCommitHashes;
        set => SetField(ref _gitCommitHashes, value);
    }

    public string TaskIds
    {
        get => _taskIds;
        set => SetField(ref _taskIds, value);
    }

    public string ForcedNodes
    {
        get => _forcedNodes;
        set => SetField(ref _forcedNodes, value);
    }


    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}