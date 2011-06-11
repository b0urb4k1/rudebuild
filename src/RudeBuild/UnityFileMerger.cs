using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace RudeBuild
{
    public class UnityFileMerger
    {
        private GlobalSettings _globalSettings;
        private string _cachePath;
        private IList<string> _unityFilePaths;
        public IList<string> UnityFilePaths
        {
            get { return _unityFilePaths; }
        }

        public UnityFileMerger(GlobalSettings globalSettings)
        {
            _globalSettings = globalSettings;
        }

        private static string GetMD5Hash(string input)
        {
            MD5 md5Hasher = MD5.Create();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Convert to a 32 character hexadecimal output string.
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                result.Append(data[i].ToString("x2"));
            }
            return result.ToString();
        }

        private void CreateCachePath(ProjectInfo projectInfo)
        {
            string solutionDirectory = projectInfo.Solution.Name + "_" + GetMD5Hash(projectInfo.Solution.FilePath);
            string config = _globalSettings.RunOptions.Config.Replace('|', '-');

            _cachePath = Path.Combine(_globalSettings.CachePath, solutionDirectory, config);
            Directory.CreateDirectory(_cachePath);
        }

        private void WritePrefix(StringBuilder text)
        {
            text.AppendLine("#ifdef _MSC_VER");
            text.AppendLine("#define RUDE_BUILD_SUPPORTS_PRAGMA_MESSAGE");
            text.AppendLine("#endif");
            text.AppendLine();
        }

        private void WritePostfix(StringBuilder text)
        {
            text.AppendLine();
            text.AppendLine("#ifdef RUDE_BUILD_SUPPORTS_PRAGMA_MESSAGE");
            text.AppendLine("#undef RUDE_BUILD_SUPPORTS_PRAGMA_MESSAGE");
            text.AppendLine("#endif");
        }

        private void WriteUnityFile(ProjectInfo projectInfo, StringBuilder text, int fileIndex)
        {
            WritePostfix(text);

            string destFileName = Path.Combine(_cachePath, projectInfo.Name + fileIndex + ".cpp");
            ModifiedTextFileWriter writer = new ModifiedTextFileWriter(destFileName);
            if (writer.Write(text.ToString()))
            {
                _globalSettings.Output.WriteLine("Creating unity file " + projectInfo.Name + fileIndex);
            }

            _unityFilePaths.Add(destFileName);
        }

        public void Process(ProjectInfo projectInfo)
        {
            CreateCachePath(projectInfo);

            _unityFilePaths = new List<string>();

            StringBuilder currentUnityFileContents = new StringBuilder();
            WritePrefix(currentUnityFileContents);

            int currentUnityFileIndex = 1;
            long currentUnityFileSize = 0;

            foreach (string cppFileName in projectInfo.CppFileNames)
            {
                string cppFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectInfo.FileName), cppFileName));
                if (!File.Exists(cppFilePath))
                {
                    _globalSettings.Output.WriteLine("Input file '" + cppFileName + "' does not exist. Skipping.");
                    continue;
                }

                FileInfo fileInfo = new FileInfo(cppFilePath);
                currentUnityFileSize += fileInfo.Length;
                if (currentUnityFileSize > _globalSettings.MaxUnityFileSize)
                {
                    WriteUnityFile(projectInfo, currentUnityFileContents, currentUnityFileIndex);

                    currentUnityFileSize = 0;
                    currentUnityFileContents.Clear();
                    WritePrefix(currentUnityFileContents);
                    ++currentUnityFileIndex;
                }

                currentUnityFileContents.AppendLine("#ifdef RUDE_BUILD_SUPPORTS_PRAGMA_MESSAGE");
                currentUnityFileContents.AppendLine("#pragma message(\"" + Path.GetFileName(cppFileName) + "\")");
                currentUnityFileContents.AppendLine("#endif");
                currentUnityFileContents.AppendLine("#include \"" + cppFileName + "\"");
            }

            if (currentUnityFileSize > 0)
            {
                WriteUnityFile(projectInfo, currentUnityFileContents, currentUnityFileIndex);
            }
        }
    }
}
