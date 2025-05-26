using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RenameMediaScript
{
    public class FileInfo
    {
        /// <summary>
        /// Полный путь к оригинальному файлу.
        /// </summary>
        public string FileOriginalFullPath { get; }

        /// <summary>
        /// Наименование оригинального файла.
        /// </summary>
        public string FileOriginalName { get; }

        /// <summary>
        /// Расширение файла.
        /// </summary>
        public string FileExtension { get; }

        /// <summary>
        /// Наименование нового файла.
        /// </summary>
        public string FileNewName { get; set; }

        /// <summary>
        /// Дата создания файла/дата создания мультимедиа.
        /// </summary>
        public DateTime CreateMediaDateTime { get; set; }

        /// <summary>
        /// Необходимость обработки файла.
        /// </summary>
        public bool? BeProcessing { get; set; }

        public MediaFileType Type { get; set; }

        public FileInfo(string fileFullPath)
        {
            FileOriginalFullPath = fileFullPath;
            FileOriginalName = Path.GetFileNameWithoutExtension(fileFullPath);
            FileExtension = Path.GetExtension(fileFullPath);
        }

        public override string ToString()
        {
            StringBuilder outString = new StringBuilder();
            outString.Append($"Наименование файла: '{FileOriginalName}'. Расширение: '{FileExtension}'\n");
            //outString.Append($"Необходимость обработки: '{(BeProcessing ? "Да" : "Нет")}'\n");
            outString.Append($"Найденная дата формирования медиа: '{(CreateMediaDateTime == DateTime.MinValue ? "" : CreateMediaDateTime.ToString())}'\n");
            outString.Append($"Новое наименование: '{FileNewName}'\n");
            return outString.ToString();
        }

        public void CheckFileExtension(string[] imageExtensions, string[] videoExtensions)
        {
            if (imageExtensions.Any(f => f.Contains(FileExtension)))
            {
                Type = MediaFileType.Image;
                return;
            }
            if (videoExtensions.Any(f => f.Contains(FileExtension)))
            {
                Type = MediaFileType.Video;
                return;
            }
            BeProcessing = false;
        }

        /// <summary>
        /// Выполнить поиск даты и времени формирования медиафайла по его наименованию.
        /// </summary>
        public void WriteDateTime(string[] regexStringArray)
        {
            if (BeProcessing == false)
            {
                return;
            }

            foreach (string regexString in regexStringArray)
            {
                
                Regex regex = new Regex(regexString);
                Match match = regex.Match(FileOriginalName);
                if (match.Success)
                {
                    string tempDateTime = match.Groups[MediaDateTime.year.ToString()].Value
                        + match.Groups[MediaDateTime.month.ToString()].Value
                        + match.Groups[MediaDateTime.day.ToString()].Value
                        + match.Groups[MediaDateTime.hour.ToString()].Value
                        + match.Groups[MediaDateTime.minute.ToString()].Value
                        + match.Groups[MediaDateTime.second.ToString()].Value;
                    
                    const string formatDateTime = "yyyyMMddHHmmss";
                    if (DateTime.TryParseExact(tempDateTime, formatDateTime, null, System.Globalization.DateTimeStyles.None, out DateTime dateTimeResult))
                    {
                        CreateMediaDateTime = dateTimeResult;
                        BeProcessing = true;
                        break;
                    }
                    else
                    {
                        BeProcessing = false;
                    }
                }
            }
        }

        /// <summary>
        /// Записать новое наименование файла.
        /// </summary>
        public void WriteNewFileName()
        {
            if (BeProcessing == true)
            {
                StringBuilder fileNameBuild = new StringBuilder();
                fileNameBuild.Append(CreateMediaDateTime.ToString("yyyy.MM.dd_HH-mm-ss"));
                fileNameBuild.Append(GetFileType());
                FileNewName = fileNameBuild.ToString();
            }
            else
            {
                FileNewName = null;
            }
        }

        private string GetFileType()
        {
            if (string.IsNullOrWhiteSpace(FileExtension))
            {
                return null;
            }

            string[] imageFormats = new string[] { ".jpg", ".jpeg", ".png" };
            string[] videoFormats = new string[] { ".mp4" };

            if (imageFormats.Any(f => f.Contains(FileExtension)))
            {
                return "_IMG";
            }
            if (videoFormats.Any(f => f.Contains(FileExtension)))
            {
                return "_VID";
            }
            return null;
        }

        public void EditAndSaveNewFile(bool replaceFile, string exiftoolPath, string directorySave = null)
        {
            if (BeProcessing == false)
            {
                return;
            }
            if (directorySave == null)
            {
                directorySave = $"{Path.GetDirectoryName(FileOriginalFullPath)}\\Corrected files";
            }
            try
            {
                Directory.CreateDirectory(directorySave);
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось проверить или создать директорию по пути {directorySave}", ex);
            }
            string fileNewFullPath = $"{directorySave}\\{FileNewName}{FileExtension}";
            if (replaceFile)
            {
                File.Move(FileOriginalFullPath, fileNewFullPath);
            }
            else
            {
                File.Copy(FileOriginalFullPath, fileNewFullPath, true);
            }
            
            EditDateTimeTagsFile(fileNewFullPath, exiftoolPath);
        }

        private void EditDateTimeTagsFile(string filePath, string exiftoolPath)
        {
            // Дата и время в виде строки
            string dateTimeString = CreateMediaDateTime.ToString("yyyy:MM:dd HH:mm:ss");
            // Аргументы для Exiftool
            string arg = $"-overwrite_original -AllDates=\"{dateTimeString}\" -api QuickTimeUTC \"{filePath}\"";
            // Задать параметры для Exiftool
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exiftoolPath,
                Arguments = arg,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            // Запустить Exiftool
            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Exiftool error: {error}");
                }
            }
            // Изменить стандартные значения файла
            File.SetCreationTime(filePath, CreateMediaDateTime);
            File.SetLastWriteTime(filePath, CreateMediaDateTime);
            File.SetLastAccessTime(filePath, CreateMediaDateTime);
        }
    }

    public enum MediaDateTime
    {
        year,
        month,
        day,
        hour,
        minute,
        second
    }

    public enum  MediaFileType
    {
        [Description("_IMG")]
        Image,

        [Description("_VID")]
        Video
    }
}
