using System.Collections.Generic;

namespace System
{
    public static class StringExtensions
    {
        public static string ReplacePlaceholdersWithSystemPaths(this string input)
        {
            var placeholderMappings = new Dictionary<string, string>
            {
                { "[ProgramFilesFolder]", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) },
                { "[ProgramFiles64Folder]", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) },
                { "[ProgramFilesX86Folder]", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) },
                { "[SystemFolder]", Environment.GetFolderPath(Environment.SpecialFolder.System) },
                { "[WindowsFolder]", Environment.GetFolderPath(Environment.SpecialFolder.Windows) },
                { "[DesktopFolder]", Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
                { "[LocalAppDataFolder]", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) },
                { "[CommonAppDataFolder]", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) },
                { "[UserProfileFolder]", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) },
                { "[DocumentsFolder]", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
                { "[AppDataFolder]", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) }
                // Add more mappings as needed
            };

            foreach (var mapping in placeholderMappings) input = input.Replace(mapping.Key, mapping.Value);

            return input;
        }
    }
}