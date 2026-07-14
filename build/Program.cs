using System;
using System.Collections.Generic;
using System.IO;
using GlobExpressions;
using static Bullseye.Targets;
using static SimpleExec.Command;

const string Clean = "clean";
const string Build = "build";
const string Test = "test";
const string Format = "format";
const string Publish = "publish";

Target(
    Clean,
    ["publish", "**/bin", "**/obj"],
    dir =>
    {
        IEnumerable<string> GetDirectories(string d) => Glob.Directories(".", d);

        void RemoveDirectory(string d)
        {
            if (Directory.Exists(d))
            {
                Console.WriteLine($"Cleaning {d}");
                Directory.Delete(d, true);
            }
        }

        foreach (var d in GetDirectories(dir))
        {
            RemoveDirectory(d);
        }
    }
);

Target(
    Format,
    () =>
    {
        Run("dotnet", "tool restore");
        Run("dotnet", "csharpier format .");
    }
);

Target(Build, [Format], () => Run("dotnet", "build Conduit.sln -c Release"));

Target(
    Test,
    [Build],
    () =>
    {
        IEnumerable<string> GetFiles(string d) => Glob.Files(".", d);

        foreach (var file in GetFiles("tests/**/*.csproj"))
        {
            Run("dotnet", $"test {file} -c Release --no-restore --no-build --verbosity=normal");
        }
    }
);

Target(
    Publish,
    [Test],
    ["src/Conduit"],
    project =>
    {
        Run(
            "dotnet",
            $"publish {project} -c Release -f net10.0 -o ./publish --no-restore --no-build --verbosity=normal"
        );
    }
);

Target("default", [Publish], () => Console.WriteLine("Done!"));
await RunTargetsAndExitAsync(args);
