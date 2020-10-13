﻿using System.Diagnostics;
using System.Threading.Tasks;
using Cupscale.IO;
using Cupscale.UI;

namespace Cupscale
{
    internal class FFmpeg
    {
        public static async Task Run(string args)
        {
            var ffmpeg = new Process();
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.RedirectStandardError = true;
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.FileName = "cmd.exe";
            ffmpeg.StartInfo.Arguments = "/C cd /D " + Paths.esrganPath.WrapPath() +
                                         " & ffmpeg.exe -hide_banner -loglevel warning -y -stats " + args;
            Logger.Log("Running ffmpeg...");
            Logger.Log("cmd.exe " + ffmpeg.StartInfo.Arguments);
            ffmpeg.OutputDataReceived += OutputHandler;
            ffmpeg.ErrorDataReceived += OutputHandler;
            ffmpeg.Start();
            ffmpeg.BeginOutputReadLine();
            ffmpeg.BeginErrorReadLine();
            while (!ffmpeg.HasExited)
                await Task.Delay(100);
            Logger.Log("Done running ffmpeg.");
        }

        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            Logger.Log("[FFmpeg] " + outLine.Data);
        }

        public static async Task RunGifski(string args)
        {
            var ffmpeg = new Process();
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.RedirectStandardError = true;
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.FileName = "cmd.exe";
            ffmpeg.StartInfo.Arguments = "/C cd /D " + Paths.esrganPath.WrapPath()
                                                     + " & gifski.exe " + args;
            Logger.Log("Running gifski...");
            Logger.Log("cmd.exe " + ffmpeg.StartInfo.Arguments);
            ffmpeg.OutputDataReceived += OutputHandlerGifski;
            ffmpeg.ErrorDataReceived += OutputHandlerGifski;
            ffmpeg.Start();
            ffmpeg.BeginOutputReadLine();
            ffmpeg.BeginErrorReadLine();
            while (!ffmpeg.HasExited)
                await Task.Delay(100);
            Logger.Log("Done running gifski.");
        }

        private static void OutputHandlerGifski(object sendingProcess, DataReceivedEventArgs outLine)
        {
            Logger.Log("[gifski] " + outLine.Data);
        }

        public static string RunAndGetOutput(string args)
        {
            var ffmpeg = new Process();
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.RedirectStandardError = true;
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.FileName = "cmd.exe";
            ffmpeg.StartInfo.Arguments = "/C cd /D " + Paths.esrganPath.WrapPath() +
                                         " & ffmpeg.exe -hide_banner -y -stats " + args;
            ffmpeg.Start();
            ffmpeg.WaitForExit();
            var output = ffmpeg.StandardOutput.ReadToEnd();
            var err = ffmpeg.StandardError.ReadToEnd();
            return output + "\n" + err;
        }
    }
}