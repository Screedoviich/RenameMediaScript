using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RenameMediaScript
{
    public class Program
    {
        public static void Main()
        {
            if (!Directory.Exists("Exiftool"))
            {
                throw new DirectoryNotFoundException("Для работы необходимо ПО Exiftool!");
            }

            // Получить директорию с файлами
            string path = InputPathHandler();
            // Получить путь ко всем файлам в папке
            string[] filesPath = Directory.GetFiles(path);
            // Работа с файлами
            for (int i = 0; i < filesPath.Length; i++)
            {
                Console.WriteLine($"{i + 1}.....");
                // Создание объекта
                FileInfo fileInfo = new FileInfo(path, Path.GetFileName(filesPath[i]));
                // Вывести информацию о файлах
                if (fileInfo.BeProcessing)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                Console.WriteLine(fileInfo.ToString());
                Console.ForegroundColor = ConsoleColor.White;
                // Сохранить новый файл и изменить дату/время
                fileInfo.EditAndSaveNewFile();
            }
        }

        /// <summary>
        /// Обработчик ввода директории. Возвращает строку с введенной директорией.
        /// </summary>
        /// <returns>Строка содержащая путь.</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public static string InputPathHandler()
        {
            Console.Write("Введите путь к папке: ");
            string userPath = Console.ReadLine();
            // Удалить лишние пробелы и апострофы
            string resultPath = userPath.Trim().Replace("\"", "");
            // Если директория существует
            if (Directory.Exists(resultPath))
            {
                return resultPath;
            }
            else
            {
                throw new DirectoryNotFoundException("Не найдена директория по пути " + userPath);
            }
        }
    }

    public class FileInfo
    {
        /// <summary>
        /// Каталог расположения файла.
        /// </summary>
        public string DirectoryPath { get; }

        /// <summary>
        /// Расширение файла.
        /// </summary>
        public string FileExension { get; }

        /// <summary>
        /// Полное наименование оригинального файла с расширением.
        /// </summary>
        public string FileOldFullName { get; }

        /// <summary>
        /// Наименование нового файла без расширения.
        /// </summary>
        public string FileNewName { get; set; }

        /// <summary>
        /// Дата создания файла/дата создания мультимедиа.
        /// </summary>
        public DateTime DateTimeCreate { get; set; }

        /// <summary>
        /// Необходимость обработки файла.
        /// </summary>
        public bool BeProcessing { get; set; }

        public FileInfo(string directory, string fileFullName)
        {
            DirectoryPath = directory;
            FileExension = fileFullName.Contains('.') ? fileFullName.Substring(fileFullName.LastIndexOf('.') + 1).ToLower() : null;
            FileOldFullName = fileFullName;
            WriteDateTime();
            WriteNewFileName();
        }

        public override string ToString()
        {
            StringBuilder outString = new StringBuilder();
            outString.Append($"Полное наименование файла: '{FileOldFullName}'\n");
            outString.Append($"Необходимость обработки: '{(BeProcessing ? "Да" : "Нет")}'\n");
            outString.Append($"Найденная дата формирования медиа: '{(DateTimeCreate == DateTime.MinValue ? "" : DateTimeCreate.ToString())}'\n");
            outString.Append($"Новое наименование: '{FileNewName}'\n");
            return outString.ToString();
        }

        public void EditAndSaveNewFile(string directorySave = null)
        {
            if (!BeProcessing)
            {
                return;
            }
            if (directorySave == null)
            {
                directorySave = $"{DirectoryPath}\\Corrected files";
            }
            try
            {
                Directory.CreateDirectory(directorySave);
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось проверить или создать директорию по пути {directorySave}", ex);
            }
            string fileNewFullPath = $"{directorySave}\\{FileNewName}.{FileExension}";
            File.Copy($"{DirectoryPath}\\{FileOldFullName}", fileNewFullPath, true);
            EditDateTimeTagsFile(fileNewFullPath);
        }

        private void EditDateTimeTagsFile(string filePath)
        {
            // Дата и время в виде строки
            string dateTimeString = DateTimeCreate.ToString("yyyy:MM:dd HH:mm:ss");
            // Аргументы для Exiftool
            string arg = $"-overwrite_original -AllDates=\"{dateTimeString}\" -api QuickTimeUTC \"{filePath}\"";
            // Задать параметры для Exiftool
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = @"Exiftool\exiftool.exe",
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
            File.SetCreationTime(filePath, DateTimeCreate);
            File.SetLastWriteTime(filePath, DateTimeCreate);
            File.SetLastAccessTime(filePath, DateTimeCreate);
        }

        /// <summary>
        /// Поиск и запись в свойство класса значения DateTime.
        /// </summary>
        private void WriteDateTime()
        {
            // Получить дату и время в виде кортежа
            var dateTimeTuple = GetDateTimeTupleFromFileName(FileOldFullName);
            // Задать формат поиска даты из кортежа
            const string formatDateTimeTuple = "(yyyy, MM, dd, HH, mm, ss)";
            // Проверить и преобразовать строку в дату и время
            if (DateTime.TryParseExact(dateTimeTuple.ToString(), formatDateTimeTuple, null, System.Globalization.DateTimeStyles.None, out DateTime dateTimeResult))
            {
                DateTimeCreate = dateTimeResult;
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
        /// Записать в свойство класса новое наименование файла.
        /// </summary>
        private void WriteNewFileName()
        {
            if (BeProcessing == true)
            {
                StringBuilder fileNameBuild = new StringBuilder();
                fileNameBuild.Append(DateTimeCreate.ToString("yyyy.MM.dd_HH-mm-ss"));
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
            if (string.IsNullOrWhiteSpace(FileExension))
            {
                return null;
            }

            string[] imageFormats = new string[] { "jpg", "jpeg", "png" };
            string[] videoFormats = new string[] { "mp4" };

            if (imageFormats.Any(f => f.Contains(FileExension)))
            {
                return "_IMG";
            }
            if (videoFormats.Any(f => f.Contains(FileExension)))
            {
                return "_VID";
            }
            return null;
        }
    }
}
