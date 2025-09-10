using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace NDAProcesses.Server.Services
{
    public interface IFileLogger
    {
        void TextBee(string message);
        void Sql(string message);
        void System(string message);
    }

    public class FileLogger : IFileLogger
    {
        private readonly string _root;
        public FileLogger(IWebHostEnvironment env)
        {
            _root = Path.Combine(env.ContentRootPath, "Logs");
            Directory.CreateDirectory(_root);
        }

        private void Write(string fileName, string message)
        {
            var path = Path.Combine(_root, fileName);
            var line = $"[{DateTime.UtcNow:o}] {message}{Environment.NewLine}";
            File.AppendAllText(path, line);
        }

        public void TextBee(string message) => Write("textbee.log", message);
        public void Sql(string message) => Write("sql.log", message);
        public void System(string message) => Write("system.log", message);
    }
}
