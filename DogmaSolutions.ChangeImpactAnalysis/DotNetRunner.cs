using System.Diagnostics;

namespace DogmaSolutions.ChangeImpactAnalysis
{
    public class DotNetRunner
    {
        public int Run(string workingDirectory, string[] arguments)
        {
            using var p = new Process();

            p.StartInfo = new ProcessStartInfo("dotnet", string.Join(" ", arguments))
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            p.Start();
            p.WaitForExit();

            return p.ExitCode;
        }
    }
}