﻿using APKInstaller.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace APKInstaller.Helpers
{
    public static class UpdateHelper
    {
        private const string KKPP_API = "https://v2.kkpp.cc/repos/{0}/{1}/releases/latest";
        private const string GITHUB_API = "https://api.github.com/repos/{0}/{1}/releases/latest";

        public static async Task<UpdateInfo> CheckUpdateAsync(string username, string repository, Version currentVersion = null)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username));

            if (string.IsNullOrEmpty(repository))
                throw new ArgumentNullException(nameof(repository));

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", username);
            string url = string.Format(GITHUB_API, username, repository);
            HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                url = string.Format(KKPP_API, username, repository);
                response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
            }
            string responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<UpdateInfo>(responseBody);

            if (result != null)
            {
                if (currentVersion == null)
                {
                    currentVersion = Assembly.GetEntryAssembly().GetName().Version;
                }

                var newVersionInfo = GetAsVersionInfo(result.TagName);
                int major = currentVersion.Major == -1 ? 0 : currentVersion.Major;
                int minor = currentVersion.Minor == -1 ? 0 : currentVersion.Minor;
                int build = currentVersion.Build == -1 ? 0 : currentVersion.Build;
                int revision = currentVersion.Revision == -1 ? 0 : currentVersion.Revision;

                var currentVersionInfo = new SystemVersionInfo(major, minor, build, revision);

                return new UpdateInfo
                {
                    Changelog = result?.Changelog,
                    CreatedAt = Convert.ToDateTime(result?.CreatedAt),
                    Assets = result.Assets,
                    IsPreRelease = result.IsPreRelease,
                    PublishedAt = Convert.ToDateTime(result?.PublishedAt),
                    TagName = result.TagName,
                    ApiUrl = result?.ApiUrl,
                    ReleaseUrl = result?.ReleaseUrl,
                    IsExistNewVersion = newVersionInfo > currentVersionInfo
                };
            }

            return null;
        }

        private static SystemVersionInfo GetAsVersionInfo(string version)
        {
            var nums = GetVersionNumbers(version).Split('.').Select(int.Parse).ToList();

            if (nums.Count <= 1)
                return new SystemVersionInfo(nums[0], 0, 0, 0);
            else if (nums.Count <= 2)
                return new SystemVersionInfo(nums[0], nums[1], 0, 0);
            else if (nums.Count <= 3)
                return new SystemVersionInfo(nums[0], nums[1], nums[2], 0);
            else
                return new SystemVersionInfo(nums[0], nums[1], nums[2], nums[3]);
        }

        private static string GetVersionNumbers(string version)
        {
            var allowedChars = "01234567890.";
            return new string(version.Where(c => allowedChars.Contains(c)).ToArray());
        }
    }
}
