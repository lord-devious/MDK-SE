using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Malware.MDKServices;

namespace MDK.Views.ProjectHealth.Fixes
{
    class MissingPathsFileFix: Fix
    {
        public MissingPathsFileFix() : base(2000, HealthCode.MissingPathsFile) { }

        public override void Apply(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Restoring missing paths file";
            analysis.Properties.Paths.InstallPath = analysis.AnalysisOptions.InstallPath;
            analysis.Properties.Paths.GameBinPath = analysis.AnalysisOptions.DefaultGameBinPath;
            analysis.Properties.Paths.OutputPath = analysis.AnalysisOptions.DefaultOutputPath;
            foreach (var reference in MDKProjectPaths.DefaultAssemblyReferences)
                analysis.Properties.Paths.AssemblyReferences.Add(reference);
            foreach (var reference in MDKProjectPaths.DefaultAnalyzerReferences)
                analysis.Properties.Paths.AnalyzerReferences.Add(reference);
            analysis.Properties.Paths.Save();
            var sb = new StringBuilder();
            sb.Append("Restored missing paths file");
            if (String.IsNullOrWhiteSpace(analysis.Properties.Paths.GameBinPath))
            {
                sb.Append(" -- update game path in MDK Options dialog");
            }

            status.Description = sb.ToString();
        }
    }
}
