// Copyright (c) Timofei Zhakov. All rights reserved.

using System.Text;
using System.Text.RegularExpressions;

namespace HttpCatBot
{
    public class StatusCodeProvider : IStatusCodeProvider
    {
        private readonly Dictionary<int, StatusCodeData> statusCodes;
        private readonly Random random;

        private static readonly Regex htmlTagsRegex = new Regex(@"<.*?>|&nbsp;");

        public StatusCodeProvider()
        {
            random = new Random();

            statusCodes = new Dictionary<int, StatusCodeData>();

            foreach (string file in Directory.EnumerateFiles(@"Content"))
            {
                string text = File.ReadAllText(file);
                StringBuilder description = new StringBuilder();
                int statusCode = int.Parse(Path.GetFileName(file).Substring(0, 3));

                string[] lines = text.Split('\n');
                for (int i = 1; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("###"))
                    {
                        break;
                    }

                    if (lines[i] != "")
                    {
                        description.Append(lines[i]);
                        description.Append(' ');
                    }

                    if (i + 1 < lines.Length && lines[i] == "" && lines[i + 1] == "")
                    {
                        description.AppendLine();
                    }
                }

                statusCodes.Add(statusCode, new StatusCodeData
                {
                    StatusCode = statusCode,
                    Description = htmlTagsRegex.Replace(description.ToString(), ""),
                });
            }
        }

        public StatusCodeData? GetStatusCode(int statusCode)
        {
            return statusCodes.TryGetValue(statusCode, out StatusCodeData? result) ? result : null;
        }

        public StatusCodeData GetRandomStatusCode()
        {
            StatusCodeData[] array = statusCodes.Values.ToArray();
            return array[random.Next(array.Length)];
        }
    }

    public interface IStatusCodeProvider
    {
        StatusCodeData GetRandomStatusCode();
        StatusCodeData? GetStatusCode(int statusCode);
    }

    public class StatusCodeData
    {
        public required string Description { get; set; }
        public required int StatusCode { get; set; }
    }
}
