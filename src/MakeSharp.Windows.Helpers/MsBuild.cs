using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace MakeSharp.Windows.Helpers
{
    public class MsBuild
    {
        private readonly string _projFile;
        private readonly NameValueCollection _properties;
        private readonly MsBuildVerbosity _verbosity;

        static string Parse(MsBuildVerbosity verbosity)
        {
            return verbosity.ToString().ToLowerInvariant();
        }
        static string GetExePath()
        {
            var version = Environment.Version;
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\" + version.Major + "." + version.Minor);
            var path = key.GetValue("MSBuildToolsPath") as string;
            key.Dispose();
            return Path.Combine(path, "MSBuild.exe");
        }

        private static string path;
        private int _count=System.Environment.ProcessorCount/2;

        public static string ExePath
        {
            get
            {
                if (path == null)
                {
                    path = GetExePath();
                }
                return path;
            }
        }

        public static NameValueCollection ConfigurationRelease
        {
            get
            {
                var result = new NameValueCollection();
                result.Add("Configuration", "Release");
                return result;
            }
        }

        public static NameValueCollection ConfigurationDebug
        {
            get
            {
                var result = new NameValueCollection();
                result.Add("Configuration", "Release");
                return result;
            }
        }

        public NameValueCollection Properties
        {
            get { return _properties; }
        }

        public MsBuild MaxCpuCount(int count)
        {
            count.MustComplyWith(d=>d>0,"CPU count needs to be greater than 0");
            _count = count;
            return this;
        }

        public MsBuild(string projFile, NameValueCollection properties, MsBuildVerbosity verbosity = MsBuildVerbosity.Normal)
        {
            projFile.MustNotBeEmpty();
            _projFile = projFile;
            _properties = properties;
            _verbosity = verbosity;
        }

        public void Build()
        {
            RunTargets("Build");
        }

        public void Clean()
        {
            RunTargets("Clean");
        }
        public void RunTargets(params string[] targets)
        {
            using (var p = new Process())
            {
                p.StartInfo.FileName = ExePath;
                p.StartInfo.Arguments = BuildArguments(targets);
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                Console.WriteLine(p.StandardOutput.ReadToEnd());
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    throw new ApplicationException("MSBuild finished with error code {0}".ToFormat(p.ExitCode));
                }
            }
        }

        string BuildArguments(string[] targets)
        {
            var sb = new StringBuilder();
            sb.Append(_projFile);
            sb.AppendFormat(" /m:{0}",_count<=1?1:_count);
            sb.Append(" /v:" + Parse(_verbosity));
            if (Properties != null)
            {
                sb.Append(" /p:");
                foreach (var name in Properties.AllKeys)
                {
                    sb.AppendFormat("{0}={1};", name, Properties[name]);
                }
                sb.RemoveLast();
            }

            if (targets.Length > 0)
            {
                sb.Append(" /t:");
                foreach (var t in targets)
                {
                    sb.Append(t + ";");
                }
                sb.RemoveLast();
            }

            sb.Append(" /clp:ErrorsOnly;PerformanceSummary");
            return sb.ToString();
        }
    }
}