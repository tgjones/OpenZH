using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OpenSage.Data;

namespace OpenSage.Utilities
{
    public static class LanguageUtility
    {
        private const string DefaultLanguage = "english";

        /// <summary>
        /// Detect the language for the current installation
        /// </summary>
        /// <param name="gameDefinition"></param>
        /// <param name="rootDirectory"></param>
        /// <returns>language as string</returns>
        public static string ReadCurrentLanguage(IGameDefinition gameDefinition, string rootDirectory)
        {
            if (PlatformUtility.IsWindowsPlatform())
            {
                if (gameDefinition.LanguageRegistryKeys != null && gameDefinition.LanguageRegistryKeys.Any())
                {
                    if(ReadFromRegistry(gameDefinition.LanguageRegistryKeys, out var language))
                    {
                        return language;
                    }
                }
            }

            string detectedLanguage = DefaultLanguage;
            switch (gameDefinition.Game)
            {
                case SageGame.CncGenerals:
                    DetectFromFileSystem(rootDirectory, "Audio", ".big", out detectedLanguage);
                    break;
                case SageGame.CncGeneralsZeroHour:
                    DetectFromFileSystem(rootDirectory, "Audio", "ZH.big", out detectedLanguage);
                    break;
                case SageGame.Bfme:
                case SageGame.Bfme2:
                case SageGame.Bfme2Rotwk:
                    if (DetectFromFileSystem(Path.Combine(rootDirectory, "lang"), "", "Audio.big", out detectedLanguage))
                    {
                        break;
                    }
                    DetectFromFileSystem(rootDirectory, "", "Audio.big", out detectedLanguage);
                    break;
            }

            return detectedLanguage;
        }

        /// <summary>
        /// Used to read the installed language version from registry
        /// </summary>
        /// <param name="registryKeys"></param>
        /// <param name="language"></param>
        /// <returns>true if an appropriate registry key is found, false otherwise</returns>
        private static bool ReadFromRegistry(IEnumerable<RegistryKeyPath> registryKeys, out string language)
        {
            language = DefaultLanguage;
            var registryValues = registryKeys.Select(RegistryUtility.GetRegistryValue);
            foreach (var registryValue in registryValues)
            {
                if (!string.IsNullOrEmpty(registryValue))
                {
                    language = registryValue;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Used to identify the language based on the filesystem and an identifier file e.g. AudioEnglish.big
        /// </summary>
        /// <param name="rootDirectory"></param>
        /// <param name="filePrefix"></param>
        /// <param name="fileSuffix"></param>
        /// <param name="detectedLanguage"></param>
        /// <returns>returns true if an appropriate file is found, false otherwise</returns>
        private static bool DetectFromFileSystem(string rootDirectory, string filePrefix, string fileSuffix, out string detectedLanguage)
        {
            detectedLanguage = DefaultLanguage;
            if (string.IsNullOrEmpty(filePrefix) && string.IsNullOrEmpty(fileSuffix) || !Directory.Exists(rootDirectory))
            {
                return false;
            }

            var files = Directory.GetFiles(rootDirectory, $"{filePrefix}*{fileSuffix}", SearchOption.TopDirectoryOnly) // there's no sense in searching subfolders
                .Select(x => Path.GetFileName(x))
                .Select(x => string.IsNullOrEmpty(filePrefix) ? x : x[filePrefix.Length..])
                .Select(x => string.IsNullOrEmpty(fileSuffix) ? x : x[..^fileSuffix.Length]);
            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    continue;
                }
                detectedLanguage = file;
                return true;
            }
            return false;
        }
    }
}
