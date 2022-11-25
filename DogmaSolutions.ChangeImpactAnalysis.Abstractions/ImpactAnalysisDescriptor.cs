using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Msagl.Drawing;
using Newtonsoft.Json.Linq;

namespace DogmaSolutions.ChangeImpactAnalysis;

public class ImpactAnalysisDescriptor : INotifyPropertyChanged
{
    private Graph _architectureGraph;
    private IReadOnlyCollection<string> _impactedComponents;
    private string _report;
    private IReadOnlyCollection<string> _markedComponents;

    public Graph ArchitectureGraph
    {
        get => _architectureGraph;
        set
        {
            SetField(ref _architectureGraph, value);
            Report = null;
        }
    }

    public IReadOnlyCollection<string> ImpactedComponents
    {
        get => _impactedComponents;
        set
        {
            SetField(ref _impactedComponents, value);
            Report = null;
        }
    }

    public IReadOnlyCollection<string> MarkedComponents
    {
        get => _markedComponents;
        set
        {
            SetField(ref _markedComponents, value);
            Report = null;
        }
    }

    public string Report
    {
        get
        {
            var report = _report;
            if (string.IsNullOrWhiteSpace(report))
            {
                var r = new StringBuilder();
                r.Append("- Total nodes: ");
                r.AppendLine(ArchitectureGraph?.NodeCount.ToString(CultureInfo.InvariantCulture) ?? "0");

                r.Append("- Total edges: ");
                r.AppendLine(ArchitectureGraph?.EdgeCount.ToString(CultureInfo.InvariantCulture) ?? "0");


                var dic = ImpactedComponents?.ToArray();
                if (dic?.Length > 0)
                {
                    r.Append("- Directly impacted components: ");
                    r.AppendLine(dic.Length.ToString(CultureInfo.InvariantCulture));
                    foreach (var d in dic)
                    {
                        r.Append("  > ");
                        r.AppendLine(d);
                    }
                }
                else
                {
                    r.AppendLine("- Directly impacted components: NONE");
                }

                var md = MarkedComponents?.ToArray();
                if (md?.Length > 0)
                {
                    r.Append("- Globally impacted components: ");
                    r.AppendLine(md.Length.ToString(CultureInfo.InvariantCulture));
                    foreach (var d in md)
                    {
                        r.Append("  > ");
                        r.AppendLine(d);
                    }
                }
                else
                {
                    r.AppendLine("- Globally impacted components: NONE");
                }

                report = r.ToString();
                _report = report;
            }

            return report;
        }
        set => SetField(ref _report, value);
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