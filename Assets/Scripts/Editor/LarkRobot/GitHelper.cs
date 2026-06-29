using System;
using System.Diagnostics;

public class GitHelper
{
    public static string GetGitUserName()
    {
        var startInfo = new ProcessStartInfo("git");
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardInput = true;
        startInfo.CreateNoWindow = true;

        var workingDirectory = Environment.CurrentDirectory;
        startInfo.WorkingDirectory = workingDirectory;
        startInfo.RedirectStandardOutput = true;
        startInfo.Arguments = "config user.name";
        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();
        char[] linecharsToTrim = {'\r', '\n'};
        var name = process.StandardOutput.ReadToEnd().Trim(linecharsToTrim);
        process.Close();
        return name;
    }
}