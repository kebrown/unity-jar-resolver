// <copyright file="Logger.cs" company="Google Inc.">
// Copyright (C) 2017 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>

namespace Google {
    using System;
    using System.IO;
    using System.Collections.Generic;

    /// <summary>
    /// Utility methods to assist with file management in Unity.
    /// </summary>
    internal class FileUtils {
        /// <summary>
        /// Extension of Unity metadata files.
        /// </summary>
        internal const string META_EXTENSION = ".meta";

        /// <summary>
        /// Delete a file or directory if it exists.
        /// </summary>
        /// <param name="path">Path to the file or directory to delete if it exists.</param>
        /// <param name="includeMetaFiles">Whether to delete Unity's associated .meta file(s).
        /// </param>
        /// <returns>true if *any* files or directories were deleted, false otherwise.</returns>
        public static bool DeleteExistingFileOrDirectory(string path,
                                                         bool includeMetaFiles = true)
        {
            bool deletedFileOrDirectory = false;
            if (includeMetaFiles && !path.EndsWith(META_EXTENSION)) {
                deletedFileOrDirectory = DeleteExistingFileOrDirectory(path + META_EXTENSION);
            }
            if (Directory.Exists(path)) {
                var di = new DirectoryInfo(path);
                di.Attributes &= ~FileAttributes.ReadOnly;
                foreach (string file in Directory.GetFileSystemEntries(path)) {
                    DeleteExistingFileOrDirectory(file, includeMetaFiles: includeMetaFiles);
                }
                Directory.Delete(path);
                deletedFileOrDirectory = true;
            }
            else if (File.Exists(path)) {
                File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
                File.Delete(path);
                deletedFileOrDirectory = true;
            }
            return deletedFileOrDirectory;
        }

        /// <summary>
        /// Copy the contents of a directory to another directory.
        /// </summary>
        /// <param name="sourceDir">Path to copy the contents from.</param>
        /// <param name="targetDir">Path to copy to.</param>
        public static void CopyDirectory(string sourceDir, string targetDir) {
            Func<string, string> sourceToTargetPath = (path) => {
                return Path.Combine(targetDir, path.Substring(sourceDir.Length + 1));
            };
            foreach (string sourcePath in
                     Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories)) {
                Directory.CreateDirectory(sourceToTargetPath(sourcePath));
            }
            foreach (string sourcePath in
                     Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)) {
                if (!sourcePath.EndsWith(META_EXTENSION)) {
                    File.Copy(sourcePath, sourceToTargetPath(sourcePath));
                }
            }
        }

        /// <summary>
        /// Perform a case insensitive search for a path relative to the current directory.
        /// </summary>
        /// <remarks>
        /// Directory.Exists() is case insensitive, so this method finds a directory using a case
        /// insensitive search returning the name of the first matching directory found.
        /// </remarks>
        /// <param name="pathToFind">Path to find relative to the current directory.</param>
        /// <returns>First case insensitive match for the specified path.</returns>
        public static string FindDirectoryByCaseInsensitivePath(string pathToFind) {
            var searchDirectory = ".";
            // Components of the path.
            var components = pathToFind.Replace(
                Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Split(
                    new [] { Path.DirectorySeparatorChar });
            for (int componentIndex = 0;
                 componentIndex < components.Length && searchDirectory != null;
                 componentIndex++) {
                var enumerateDirectory = searchDirectory;
                var expectedComponent = components[componentIndex];
                var expectedComponentLower = components[componentIndex].ToLowerInvariant();
                searchDirectory = null;
                var matchingPaths = new List<KeyValuePair<int, string>>();
                foreach (var currentDirectory in
                         Directory.GetDirectories(enumerateDirectory)) {
                    // Get the current component of the path we're traversing.
                    var currentComponent = Path.GetFileName(currentDirectory);
                    if (currentComponent.ToLowerInvariant() == expectedComponentLower) {
                        // Add the path to a list and remove "./" from the first component.
                        matchingPaths.Add(new KeyValuePair<int, string>(
                            Math.Abs(String.CompareOrdinal(expectedComponent, currentComponent)),
                            (componentIndex == 0) ? Path.GetFileName(currentDirectory) :
                                currentDirectory));
                        break;
                    }
                }
                if (matchingPaths.Count == 0) break;
                // Sort list in order of ordinal string comparison result.
                matchingPaths.Sort(
                    (KeyValuePair<int, string> lhs, KeyValuePair<int, string> rhs) => {
                        return lhs.Key - rhs.Key;
                    });
                searchDirectory = matchingPaths[0].Value;
            }
            return searchDirectory;
        }
    }
}
