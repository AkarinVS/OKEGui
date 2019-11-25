﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OKEGui.JobProcessor;
using OKEGui.Utils;

namespace OKEGui
{
    // To be deprecated.
    public class LogBuffer
    {
        public bool Inf = false;
    }
    public class RpcResult
    {
        public List<(int index, double value)> Data = new List<(int index, double value)>();
        public (string src, string opt) FileNamePair;
        public LogBuffer Logs = new LogBuffer();
    }

    public class RpChecker : CommandlineJobProcessor
    {
        public enum RpcStatus { 等待中, 跳过, 错误, 未通过, 通过 };

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly double psnr_threashold = 30.0;

        private RpcStatus status = RpcStatus.等待中;
        public RpcStatus Status
        {
            get
            {
                finishMre.WaitOne();
                return status;
            }
            private set
            {
                status = value;
            }
        }

        private RpcResult result = new RpcResult();
        public RpcResult Result
        {
            get
            {
                finishMre.WaitOne();
                return result;
            }
            private set
            {
                result = value;
            }
        }

        private const string TemplateFile = ".\\tools\\rpc\\RpcTemplate.vpy";
        private RpcJob job;
        private ulong frameCount = 0;

        public RpChecker(RpcJob job) : base()
        {
            executable = Initializer.Config.vspipePath;
            commandLine = $" \"{GetRpcScript(job)}\" .";
            this.job = job;
            result.FileNamePair = (job.Input, job.RippedFile);
        }

        public string GetRpcScript(RpcJob job)
        {
            string scriptContent = File.ReadAllText(TemplateFile);
            scriptContent = scriptContent
                .Replace("SOURCE_SCRIPT_PLACEHOLDER", job.Input)
                .Replace("VIDEO_FILE_PLACEHOLDER", job.RippedFile);
            string fileName = job.RippedFile.Replace(Path.GetExtension(job.RippedFile), "_rpc.vpy");
            File.WriteAllText(fileName, scriptContent);

            return fileName;
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            base.ProcessLine(line, stream);
            if (line.Contains("Python exception: "))
            {
                status = RpcStatus.错误;
                finishMre.Set();
                OKETaskException ex = new OKETaskException(Constants.rpcErrorSmr);
                ex.Data["RPC_ERROR"] = line.Substring(18);
                throw ex;
            }
            else if (line.Contains("RPCOUT"))
            {
                frameCount++;
                job.Progress = 100.0 * frameCount / job.TotalFrame;
                string[] strNumbers = line.Substring(8).Split(new char[] { ' ' });
                int frameNo = int.Parse(strNumbers[0]);
                double psnr = double.Parse(strNumbers[1]);
                result.Data.Add((frameNo, psnr));
                if (psnr < psnr_threashold)
                {
                    status = RpcStatus.未通过;
                }
            }
            else if (line.StartsWith("Output "))
            {
                if (status == RpcStatus.等待中)
                {
                    status = RpcStatus.通过;
                }
                finishMre.Set();
            }
        }

        public override void waitForFinish()
        {
            base.waitForFinish();
            job.RpcStatus = Status;
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.None
            };
            job.Output = job.Output.Replace(".rpc", $"-{Status.ToString()}.rpc");
            using (StreamWriter fileWriter = new StreamWriter(job.Output))
            using (JsonTextWriter writer = new JsonTextWriter(fileWriter))
            {
                serializer.Serialize(writer, new RpcResult[] { Result });
            }
        }
    }
}
