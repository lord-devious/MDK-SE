using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using JetBrains.Annotations;

namespace Malware.MDKServices
{
    /// <summary>
    /// Represents a set of important paths for an MDK project
    /// </summary>
    public partial class MDKProjectPaths : INotifyPropertyChanged
    {
        /// <summary>
        /// A list of assembly references which are added to project path options by default.
        /// </summary>
        public static ReadOnlyCollection<AssemblyReference> DefaultAssemblyReferences = new ReadOnlyCollection<AssemblyReference>(new List<AssemblyReference>
            {
                new AssemblyReference
                {
                    Include = "Sandbox.Common",
                    HintPath = @"$(MDKGameBinPath)\Sandbox.Common.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "Sandbox.Game",
                    HintPath = @"$(MDKGameBinPath)\Sandbox.Game.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "Sandbox.Graphics",
                    HintPath = @"$(MDKGameBinPath)\Sandbox.Graphics.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "SpaceEngineers.Game",
                    HintPath = @"$(MDKGameBinPath)\SpaceEngineers.Game.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "SpaceEngineers.ObjectBuilders",
                    HintPath = @"$(MDKGameBinPath)\SpaceEngineers.ObjectBuilders.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "VRage",
                    HintPath = @"$(MDKGameBinPath)\VRage.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "VRage.Audio",
                    HintPath = @"$(MDKGameBinPath)\VRage.Audio.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "VRage.Game",
                    HintPath = @"$(MDKGameBinPath)\VRage.Game.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "VRage.Input",
                    HintPath = @"$(MDKGameBinPath)\VRage.Input.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "VRage.Library",
                    HintPath = @"$(MDKGameBinPath)\VRage.Library.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "VRage.Math",
                    HintPath = @"$(MDKGameBinPath)\VRage.Math.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "VRage.Render",
                    HintPath = @"$(MDKGameBinPath)\VRage.Render.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "VRage.Render11",
                    HintPath = @"$(MDKGameBinPath)\VRage.Render11.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "VRage.Scripting",
                    HintPath = @"$(MDKGameBinPath)\VRage.Scripting.dll",
                    Private = false
                },
                new AssemblyReference
                {
                    Include = "MDKUtilities",
                    HintPath = @"$(MDKInstallPath)\MDKUtilities.dll",
                    Private = true
                },
                new AssemblyReference
                {
                    Include = "System.Collections.Immutable",
                    HintPath = @"$(MDKGameBinPath)\System.Collections.Immutable.dll",
                    Private = false
                }
            });

        /// <summary>
        /// A list of analyzer references which are added to project path options by default.
        /// </summary>
        public static ReadOnlyCollection<AnalyzerReference> DefaultAnalyzerReferences = new ReadOnlyCollection<AnalyzerReference>(new List<AnalyzerReference>
        {
            new AnalyzerReference
            {
                Include = @"$(MDKInstallPath)\Analyzers\MDKAnalyzer.dll"
            }
        });

        const string Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";

        /// <summary>
        /// Loads path information for an MDK project
        /// </summary>
        /// <param name="fileName">The file name of this project paths file</param>
        /// <returns></returns>
        public static MDKProjectPaths Load([NotNull] string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(fileName));
            fileName = Path.GetFullPath(fileName);
            if (!File.Exists(fileName))
                return new MDKProjectPaths(fileName, false);
            try
            {
                using (var streamReader = File.OpenText(fileName))
                {
                    var readerSettings = new XmlReaderSettings
                    {
                        IgnoreWhitespace = true
                    };
                    var xmlReader = XmlReader.Create(streamReader, readerSettings);
                    var document = XDocument.Load(xmlReader);
                    var nameTable = xmlReader.NameTable;
                    if (nameTable == null)
                        return new MDKProjectPaths(fileName, false);
                    var nsm = new XmlNamespaceManager(nameTable);
                    nsm.AddNamespace("m", Xmlns);

                    // Check if this is a template options file
                    var versionElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKVersion", nsm);
                    var versionStr = (string)versionElement;
                    if (versionStr == "$MDKVersion$")
                        return new MDKProjectPaths(fileName, false);
                    Version.TryParse(versionStr, out var version);

                    var gameBinPathElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKGameBinPath", nsm);
                    var gameBinPath = ((string)gameBinPathElement)?.Trim();

                    var installPathElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKInstallPath", nsm);
                    var installPath = ((string)installPathElement)?.Trim();

                    var outputPathElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKOutputPath", nsm);
                    var outputPath = ((string)outputPathElement)?.Trim();

                    var paths = new MDKProjectPaths(fileName, true)
                    {
                        Version = version,
                        GameBinPath = gameBinPath,
                        InstallPath = installPath,
                        OutputPath = outputPath,
                        HasChanges = false
                    };
                    foreach (var reference in DefaultAssemblyReferences)
                        paths.AssemblyReferences.Add(reference);
                    foreach (var reference in DefaultAnalyzerReferences)
                        paths.AnalyzerReferences.Add(reference);
                    return paths;
                }
            }
            catch (Exception e)
            {
                throw new MDKProjectPropertiesException($"An error occurred while attempting to load project information from {fileName}.", e);
            }
        }

        bool _hasChanges;
        string _gameBinPath;
        string _installPath;
        string _outputPath;
        Version _version;

        MDKProjectPaths(string fileName, bool isValid)
        {
            FileName = fileName;
            IsValid = isValid;
            AssemblyReferences = new ObservableCollection<AssemblyReference>();
            AnalyzerReferences = new ObservableCollection<AnalyzerReference>();
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The MDK version this paths file was saved with
        /// </summary>
        public Version Version
        {
            get => _version;
            private set
            {
                if (Equals(value, _version))
                    return;
                _version = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines whether changes have been made to the options for this project
        /// </summary>
        public bool HasChanges
        {
            get => _hasChanges;
            private set
            {
                if (value == _hasChanges)
                    return;
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines whether this is a valid MDK project
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the project file name
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Determines the path to the game's installed binaries.
        /// </summary>
        public string GameBinPath
        {
            get => _gameBinPath;
            set
            {
                if (value == _gameBinPath)
                    return;
                _gameBinPath = value ?? "";
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines the installation path for the extension.
        /// </summary>
        public string InstallPath
        {
            get => _installPath;
            set
            {
                if (value == _installPath)
                    return;
                _installPath = value ?? "";
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines the output path where the finished deployed script will be stored
        /// </summary>
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (value == _outputPath)
                    return;
                _outputPath = value ?? "";
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A list of assembly references
        /// </summary>
        public ObservableCollection<AssemblyReference> AssemblyReferences { get; }

        /// <summary>
        /// A list of analyzer references
        /// </summary>
        public ObservableCollection<AnalyzerReference> AnalyzerReferences { get; }

        /// <summary>
        /// Commits all changes without saving. <see cref="HasChanges"/> will be false after this. This method is not required when calling <see cref="Save"/>.
        /// </summary>
        public void Commit()
        {
            Version = MDKProjectProperties.TargetPackageVersion;
            HasChanges = false;
        }

        /// <summary>
        /// Saves the options of this project
        /// </summary>
        public void Save()
        {
            try
            {
                XDocument document = null;
                XElement rootElement = null;
                XElement groupElement = null;
                XElement versionElement = null;
                XElement gameBinPathElement = null;
                XElement installPathElement = null;
                XElement outputPathElement = null;
                XElement itemGroupElement = null;
                if (File.Exists(FileName))
                {
                    using (var streamReader = File.OpenText(FileName))
                    {
                        var readerSettings = new XmlReaderSettings
                        {
                            IgnoreWhitespace = true
                        };
                        var xmlReader = XmlReader.Create(streamReader, readerSettings);
                        document = XDocument.Load(xmlReader);
                        var nameTable = xmlReader.NameTable;
                        if (nameTable != null)
                        {
                            var nsm = new XmlNamespaceManager(nameTable);
                            nsm.AddNamespace("m", Xmlns);

                            rootElement = document.XPathSelectElement("./m:Project", nsm);
                            groupElement = document.XPathSelectElement("./m:Project/m:PropertyGroup", nsm);
                            versionElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKVersion", nsm);
                            gameBinPathElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKGameBinPath", nsm);
                            installPathElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKInstallPath", nsm);
                            outputPathElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKOutputPath", nsm);
                            itemGroupElement = document.XPathSelectElement("./m:Project/m:ItemGroup", nsm);
                        }
                    }
                }

                if (document == null)
                    document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));

                if (rootElement == null)
                {
                    rootElement = new XElement(XName.Get("Project", Xmlns));
                    document.Add(rootElement);
                }

                if (groupElement == null)
                {
                    groupElement = new XElement(XName.Get("PropertyGroup", Xmlns));
                    rootElement.Add(groupElement);
                }

                if (versionElement == null)
                {
                    versionElement = new XElement(XName.Get("MDKVersion", Xmlns));
                    groupElement.Add(versionElement);
                }

                versionElement.Value = MDKProjectProperties.TargetPackageVersion.ToString();

                if (gameBinPathElement == null)
                {
                    gameBinPathElement = new XElement(XName.Get("MDKGameBinPath", Xmlns));
                    groupElement.Add(gameBinPathElement);
                }

                gameBinPathElement.Value = GameBinPath ?? "";
                

                if (installPathElement == null)
                {
                    installPathElement = new XElement(XName.Get("MDKInstallPath", Xmlns));
                    groupElement.Add(installPathElement);
                }

                installPathElement.Value = InstallPath;

                if (outputPathElement == null)
                {
                    outputPathElement = new XElement(XName.Get("MDKOutputPath", Xmlns));
                    groupElement.Add(outputPathElement);
                }

                outputPathElement.Value = OutputPath;

                if (itemGroupElement == null)
                {
                    itemGroupElement = new XElement(XName.Get("ItemGroup", Xmlns));
                    rootElement.Add(itemGroupElement);
                }

                var referenceElements = itemGroupElement.Elements(XName.Get("Reference", Xmlns)).Concat(itemGroupElement.Elements(XName.Get("Analyzer", Xmlns))).ToList();
                foreach (var element in referenceElements)
                    element.Remove();
                foreach (var reference in AssemblyReferences)
                {
                    var element = new XElement(XName.Get("Reference", Xmlns));
                    element.Add(new XAttribute("Include", reference.Include));
                    if (!string.IsNullOrWhiteSpace(reference.HintPath))
                        element.Add(new XElement(XName.Get("HintPath", Xmlns), reference.HintPath));
                    element.Add(new XElement(XName.Get("Private", Xmlns), reference.Private));
                    itemGroupElement.Add(element);
                }
                foreach (var reference in AnalyzerReferences)
                {
                    var element = new XElement(XName.Get("Analyzer", Xmlns));
                    element.Add(new XAttribute("Include", reference.Include));
                    itemGroupElement.Add(element);
                }

                document.Save(FileName, SaveOptions.OmitDuplicateNamespaces);
                Commit();
            }
            catch (Exception e)
            {
                throw new MDKProjectPropertiesException($"An error occurred while attempting to save project information to {FileName}.", e);
            }
        }

        /// <summary>
        /// Called whenever a trackable property changes
        /// </summary>
        /// <param name="propertyName"></param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Represents an assembly reference
        /// </summary>
        public class AssemblyReference : INotifyPropertyChanged
        {
            string _include;
            string _hintPath;
            bool _private;

            /// <summary>Occurs when a property value changes.</summary>
            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// The name of the assembly to include. This is its actual assembly name, not its filename.
            /// </summary>
            public string Include
            {
                get => _include;
                set
                {
                    if (value == _include)
                        return;
                    _include = value;
                    OnPropertyChanged();
                }
            }

            /// <summary>
            /// The complete path to the assembly to include, or null to use the default assembly discovery.
            /// </summary>
            public string HintPath
            {
                get => _hintPath;
                set
                {
                    if (value == _hintPath)
                        return;
                    _hintPath = value;
                    OnPropertyChanged();
                }
            }

            /// <summary>
            /// Whether this should be a private (copy local) reference.
            /// </summary>
            public bool Private
            {
                get => _private;
                set
                {
                    if (value == _private)
                        return;
                    _private = value;
                    OnPropertyChanged();
                }
            }

            /// <summary>
            /// Invokes the <see cref="PropertyChanged"/> event
            /// </summary>
            /// <param name="propertyName"></param>
            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Represents a reference to a project analyzer.
        /// </summary>
        public class AnalyzerReference : INotifyPropertyChanged
        {
            string _hintInclude;

            /// <summary>Occurs when a property value changes.</summary>
            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// The complete path to the assembly to include, or null to use the default assembly discovery.
            /// </summary>
            public string Include
            {
                get => _hintInclude;
                set
                {
                    if (value == _hintInclude)
                        return;
                    _hintInclude = value;
                    OnPropertyChanged();
                }
            }

            /// <summary>
            /// Invokes the <see cref="PropertyChanged"/> event
            /// </summary>
            /// <param name="propertyName"></param>
            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
