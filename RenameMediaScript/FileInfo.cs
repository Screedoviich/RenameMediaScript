using System;
using System.Collections.Generic;
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
        public bool BeProcessing { get; set; }

        public FileInfo(string fileFullPath)
        {
            FileOriginalFullPath = fileFullPath;
            FileOriginalName = Path.GetFileNameWithoutExtension(fileFullPath);
            FileExtension = Path.GetExtension(fileFullPath);
            if (string.IsNullOrWhiteSpace(FileExtension))
                FileExtension = null;
        }

        public override string ToString()
        {
            StringBuilder outString = new StringBuilder();
            outString.Append($"Полное наименование файла: '{FileOriginalName}'\n");
            outString.Append($"Необходимость обработки: '{(BeProcessing ? "Да" : "Нет")}'\n");
            outString.Append($"Найденная дата формирования медиа: '{(CreateMediaDateTime == DateTime.MinValue ? "" : CreateMediaDateTime.ToString())}'\n");
            outString.Append($"Новое наименование: '{FileNewName}'\n");
            return outString.ToString();
        }

        /// <summary>
        /// Выполнить поиск даты и времени формирования медиафайла по его наименованию.
        /// </summary>
        public void WriteDateTime()
        {
            // Получить дату и время в виде кортежа
            var dateTimeTuple = GetDateTimeTupleFromFileName(FileOriginalName);
            // Задать формат поиска даты из кортежа
            const string formatDateTimeTuple = "(yyyy, MM, dd, HH, mm, ss)";
            // Проверить и преобразовать строку в дату и время
            if (DateTime.TryParseExact(dateTimeTuple.ToString(), formatDateTimeTuple, null, System.Globalization.DateTimeStyles.None, out DateTime dateTimeResult))
            {
                CreateMediaDateTime = dateTimeResult;
                BeProcessing = true;
            }
            else
            {
                BeProcessing = false;
            }
        }

        /// <summary>
        /// Выполняет поиск даты и времени в строке с помощью регулярных выражений и возвращает кортеж с данными.
        /// </summary>
        /// <param name="fileName">Строка для поиска.</param>
        /// <returns>Кортеж содержащий дату и время. Год, месяц, день, час, минута, секунда.</returns>
        private (string year, string month, string day, string hour, string minute, string second) GetDateTimeTupleFromFileName(string fileName)
        {
            // Объявить кортеж
            (string year, string month, string day, string hour, string minute, string second) dateTimeTuple;
            dateTimeTuple = (year: null, month: null, day: null, hour: null, minute: null, second: null);
            // Начать поиск по регулярным выражениям
            Regex regex;
            #region Шаблон классический
            regex = new Regex(@"^(IMG_|VID_)\d{8}_\d{6}(\.\w+|)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (regex.IsMatch(fileName))
            {
                dateTimeTuple.year = fileName.Substring(4, 4);
                dateTimeTuple.month = fileName.Substring(8, 2);
                dateTimeTuple.day = fileName.Substring(10, 2);
                dateTimeTuple.hour = fileName.Substring(13, 2);
                dateTimeTuple.minute = fileName.Substring(15, 2);
                dateTimeTuple.second = fileName.Substring(17, 2);
                goto next;
            }
            #endregion
            #region Шаблон повторный
            regex = new Regex(@"^\d{4}\.\d{2}\.\d{2}_\d{2}-\d{2}-\d{2}(_VID|_IMG|)(\.\w+|)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (regex.IsMatch(fileName))
            {
                dateTimeTuple.year = fileName.Substring(0, 4);
                dateTimeTuple.month = fileName.Substring(5, 2);
                dateTimeTuple.day = fileName.Substring(8, 2);
                dateTimeTuple.hour = fileName.Substring(11, 2);
                dateTimeTuple.minute = fileName.Substring(14, 2);
                dateTimeTuple.second = fileName.Substring(17, 2);
                goto next;
            }
        #endregion
        next:
            return dateTimeTuple;
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
            if (!BeProcessing)
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
}
