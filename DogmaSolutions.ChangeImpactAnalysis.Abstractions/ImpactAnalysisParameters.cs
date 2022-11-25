using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DogmaSolutions.ChangeImpactAnalysis;

public class ImpactAnalysisParameters  :INotifyPropertyChanged
{
    private string _artifactsBaseFolderPath;
    private string _architectureGraphFileName;
    private string _impactedArchitectureGraphFileName;
    private ImpactAnalysisFilters _filters = new ImpactAnalysisFilters();

    public string ArtifactsBaseFolderPath
    {
        get => _artifactsBaseFolderPath;
        set => SetField(ref _artifactsBaseFolderPath, value);
    }

    public string ArchitectureGraphFileName
    {
        get => _architectureGraphFileName;
        set => SetField(ref _architectureGraphFileName, value);
    }

    public string ImpactedArchitectureGraphFileName
    {
        get => _impactedArchitectureGraphFileName;
        set => SetField(ref _impactedArchitectureGraphFileName, value);
    }

    public ImpactAnalysisFilters Filters
    {
        get => _filters;
        set => SetField(ref _filters, value);
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