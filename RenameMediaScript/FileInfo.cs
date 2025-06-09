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

        /// <summary>
        /// Тип медиафайла.
        /// </summary>
        public MediaFileType Type { get; set; }

        public FileInfo(string fileFullPath)
        {
            FileOriginalFullPath = fileFullPath;
            FileOriginalName = Path.GetFileNameWithoutExtension(fileFullPath);
            FileExtension = Path.GetExtension(fileFullPath);
        }

        /// <summary>
        /// Вернуть строковое представление объекта.
        /// </summary>
        /// <returns>Строковое представление объекта.</returns>
        public override string ToString()
        {
            StringBuilder outString = new StringBuilder();
            outString.Append($"Наименование файла: `{FileOriginalName}`. Расширение: `{FileExtension}`\n");
            if (BeProcessing.HasValue)
            {
                outString.Append($"Необходимость обработки: `{(BeProcessing.Value ? "Да" : "Нет")}`\n");
            }
            outString.Append($"Найденная дата формирования медиа: `{(CreateMediaDateTime == DateTime.MinValue ? "" : CreateMediaDateTime.ToString())}`\n");
            outString.Append($"Новое наименование: `{FileNewName}`\n");
            return outString.ToString();
        }

        /// <summary>
        /// Проверить соответствие расширения файла необходимым заданным форматам.
        /// Исключить файл из обработки если не найден необходимый формат.
        /// </summary>
        /// <param name="imageExtensions">Расширения изображений.</param>
        /// <param name="videoExtensions">Расширения видеозаписей.</param>
        public void CheckFileExtension(string[] imageExtensions, string[] videoExtensions)
        {
            if (imageExtensions.Any(f => f.ToLower() == FileExtension.ToLower()))
            {
                Type = MediaFileType.Image;
                return;
            }
            if (videoExtensions.Any(f => f.ToLower() == FileExtension.ToLower()))
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
            // Пропустить обработку файла
            if (BeProcessing == false)
            {
                return;
            }
            // Проверить каждое регулярное выражение
            foreach (string regexString in regexStringArray)
            {
                Regex regex = new Regex(regexString);
                Match match = regex.Match(FileOriginalName);
                // Если регулярное выражение подошло
                if (match.Success)
                {
                    string tempDateTime = match.Groups[MediaDateTime.year.ToString()].Value
                        + match.Groups[MediaDateTime.month.ToString()].Value
                        + match.Groups[MediaDateTime.day.ToString()].Value
                        + match.Groups[MediaDateTime.hour.ToString()].Value
                        + match.Groups[MediaDateTime.minute.ToString()].Value
                        + match.Groups[MediaDateTime.second.ToString()].Value;
                    // Попытаться извлечь дату и время
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
                fileNameBuild.Append(Type.GetDescription());
                FileNewName = fileNameBuild.ToString();
            }
            else
            {
                FileNewName = null;
            }
        }

        /// <summary>
        /// Изменить метаданные создания медиафайла и сохранить файл.
        /// </summary>
        /// <param name="replaceFile">Разрешение замены оригинальных файлов.</param>
        /// <param name="exiftoolPath">Путь к программному обеспечению Exiftool.</param>
        /// <param name="directorySave">Директория для сохранения.</param>
        /// <exception cref="Exception"></exception>
        public void EditAndSaveNewFile(bool replaceFile, string exiftoolPath, string directorySave = null)
        {
            // Пропустить обработку файла
            if (BeProcessing == false)
            {
                return;
            }
            // Задать директорию сохранения 
            if (directorySave == null)
            {
                const string saveFolder = "Corrected files";
                directorySave = $"{Path.GetDirectoryName(FileOriginalFullPath)}\\{saveFolder}";
            }
            // Создать папку для сохранения файла
            try
            {
                Directory.CreateDirectory(directorySave);
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось проверить или создать директорию по пути `{directorySave}`.", ex);
            }
            // Задать новый путь для сохранения файла
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

        /// <summary>
        /// Изменить дату создания медиафайла.
        /// </summary>
        /// <param name="filePath">Полный путь к файлу.</param>
        /// <param name="exiftoolPath">Путь к программному обеспечению Exiftool.</param>
        /// <exception cref="Exception"></exception>
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
