using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace Malware.MDKServices
{
    /// <summary>
    /// A service to analyze the validity and health of a project, determining whether or not it's an MDK project in the first place
    /// and the general health state of the project if it is.
    /// </summary>
    public class HealthAnalysis
    {
        /// <summary>
        /// Analyze an individual project
        /// </summary>
        /// <param name="project"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<HealthAnalysis> AnalyzeAsync(Project project, HealthAnalysisOptions options) => System.Threading.Tasks.Task.Run(() => Analyze(project, options));

        /// <summary>
        /// Analyze an entire solution's worth of projects
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<HealthAnalysis[]> AnalyzeAsync(Solution solution, HealthAnalysisOptions options) => System.Threading.Tasks.Task.WhenAll(solution.Projects.Cast<Project>().Select(project => System.Threading.Tasks.Task.Run(() => Analyze(project, options))));

        static HealthAnalysis Analyze(Project project, HealthAnalysisOptions options)
        {
            options.Echo?.Invoke("Analyzing project...");
            if (!project.IsLoaded())
            {
                options.Echo?.Invoke("Project is not loaded.");
                return new HealthAnalysis(project, null, options);
            }

            var projectInfo = MDKProjectProperties.Load(project.FullName, project.Name, options.Echo);
            if (!projectInfo.IsValid && !(projectInfo.Options?.IsValid ?? false))
            {
                options.Echo?.Invoke($"{project.Name} contains invalid data.");
                return new HealthAnalysis(project, null, options);
            }

            var analysis = new HealthAnalysis(project, projectInfo, options);
            if (projectInfo.Options.Version < new Version(1, 2))
            {
                options.Echo?.Invoke($"{project.Name}: This project format is outdated.");
                analysis._problems.Add(new HealthProblem(HealthCode.Outdated, HealthSeverity.Critical, "This project format is outdated"));
            }

            var whitelistFileName = Path.Combine(Path.GetDirectoryName(project.FullName), "mdk\\whitelist.cache");

            if (!projectInfo.Paths.IsValid)
            {
                options.Echo?.Invoke($"{project.Name}: Missing paths file.");
                analysis._problems.Add(new HealthProblem(HealthCode.MissingPathsFile, HealthSeverity.Critical, "Missing paths file"));
            }
            else
            {
                var installPath = projectInfo.Paths.InstallPath.TrimEnd('/', '\\');
                var expectedInstallPath = options.InstallPath.TrimEnd('/', '\\');

                if (!string.Equals(installPath, expectedInstallPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    options.Echo?.Invoke($"{project.Name}: Invalid install path.");
                    analysis._problems.Add(new HealthProblem(HealthCode.BadInstallPath, HealthSeverity.Warning, "Invalid install path"));
                }

                var vrageRef = Path.Combine(projectInfo.Paths.GameBinPath, "vrage.dll");
                if (!File.Exists(vrageRef))
                {
                    options.Echo?.Invoke($"{project.Name}: Invalid game path.");
                    analysis._problems.Add(new HealthProblem(HealthCode.BadGamePath, HealthSeverity.Warning, "Invalid game path, check Manual Path in Options"));
                }

                var outputPath = projectInfo.Paths.OutputPath.TrimEnd('/', '\\');
                if (!Directory.Exists(outputPath))
                {
                    options.Echo?.Invoke($"{project.Name}: Invalid output path.");
                    analysis._problems.Add(new HealthProblem(HealthCode.BadOutputPath, HealthSeverity.Warning, "Invalid output path"));
                }

                var whitelistCacheFileName = Path.Combine(expectedInstallPath, "Analyzers\\whitelist.cache");
                if (File.Exists(whitelistCacheFileName))
                {
                    var cacheDate = File.GetLastWriteTime(whitelistCacheFileName);
                    var currentDate = File.GetLastWriteTime(whitelistFileName);
                    if (cacheDate > currentDate)
                    {
                        options.Echo?.Invoke($"{project.Name}: The whitelist cache must be updated.");
                        analysis._problems.Add(new HealthProblem(HealthCode.OutdatedWhitelist, HealthSeverity.Warning, "The whitelist cache must be updated"));
                    }
                }
            }

            if (!File.Exists(whitelistFileName))
            {
                options.Echo?.Invoke($"{project.Name}: Missing Whitelist Cache.");
                analysis._problems.Add(new HealthProblem(HealthCode.MissingWhitelist, HealthSeverity.Critical, "Missing Whitelist Cache"));
            }

            return analysis;
        }

        List<HealthProblem> _problems = new List<HealthProblem>();

        HealthAnalysis(Project project, MDKProjectProperties properties, HealthAnalysisOptions analysisOptions)
        {
            Project = project;
            try
            {
                FileName = project.FileName;
            }
            catch (NotImplementedException)
            {
                // Ignored. Ugly but necessary hack.
            }
            Properties = properties;
            AnalysisOptions = analysisOptions;
            IsMDKProject = properties != null;
            Problems = new ReadOnlyCollection<HealthProblem>(_problems);
            if (!IsMDKProject)
            {
                analysisOptions.Echo?.Invoke($"{project.Name}: This is not an MDK project.");
                _problems.Add(new HealthProblem(HealthCode.NotAnMDKProject, HealthSeverity.Critical, "This is not an MDK project."));
            }
        }

        /// <summary>
        /// Gets the file name of the analyzed project.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The project this health analysis is about
        /// </summary>
        public Project Project { get; }

        /// <summary>
        /// MDK project properties
        /// </summary>
        public MDKProjectProperties Properties { get; }

        /// <summary>
        /// Contains the options used when running this analysis
        /// </summary>
        public HealthAnalysisOptions AnalysisOptions { get; }

        /// <summary>
        /// Whether or not this is an MDK project
        /// </summary>
        public bool IsMDKProject { get; }

        /// <summary>
        /// Determines overall whether this project is a healthy MDK project.
        /// </summary>
        public bool IsHealthy => _problems.Count(s => s.Severity >= HealthSeverity.Critical) == 0;

        /// <summary>
        /// A list of problems in this project (if any)
        /// </summary>
        public ReadOnlyCollection<HealthProblem> Problems { get; }
    }
}
