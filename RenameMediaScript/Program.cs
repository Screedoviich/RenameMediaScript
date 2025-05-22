using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RenameMediaScript
{
    public class Program
    {
        private static bool ReplaceFile { get; set; } = false;

        public static void Main(string[] args)
        {
            const string exiftoolPath = @"Exiftool\exiftool.exe";
            ExiftoolExists(exiftoolPath);
            ArgsHandler(args);

            Settings settings = new Settings();
            settings.Load();

            // Получить директорию с файлами
            string path = InputPathHandler();
            // Получить путь ко всем файлам в папке
            string[] filesPath = Directory.GetFiles(path);

            // Работа с файлами
            for (int i = 0; i < filesPath.Length; i++)
            {
                Console.WriteLine($"{i + 1}.....");
                // Создание объекта
                FileInfo fileInfo = new FileInfo(filesPath[i]);
                fileInfo.WriteDateTime(settings.RegexMediaDateTime.ToArray());
                fileInfo.WriteNewFileName();
                // Вывести информацию о файлах
                if (fileInfo.BeProcessing)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                Console.WriteLine(fileInfo.ToString());
                Console.ForegroundColor = ConsoleColor.White;
                // Сохранить новый файл и изменить дату/время
                fileInfo.EditAndSaveNewFile(ReplaceFile, exiftoolPath);
            }

            Console.WriteLine("Программа завершила работу.");
            Console.ReadKey();
        }

        /// <summary>
        /// Обработчик наличия программного обеспечения Exiftool для корректной работы.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private static void ExiftoolExists(string exiftoolPath)
        {
            
            if (!Directory.Exists(Path.GetDirectoryName(exiftoolPath)))
            {
                throw new DirectoryNotFoundException($"Для работы необходимо ПО Exiftool!\nПоместите исполняемый файл по пути {Environment.CurrentDirectory}\\{exiftoolPath}");
            }
        }

        /// <summary>
        /// Обработчик входных параметров.
        /// </summary>
        /// <param name="args">Массив параметров.</param>
        private static void ArgsHandler(string[] args)
        {
            const string argReplaceFile = "-r";
            if (args.Any(s => s == argReplaceFile))
            {
                ReplaceFile = true;
                Console.WriteLine($"Найден параметр запуска {argReplaceFile}. Замена файлов разрешена.");
            }
        }

        /// <summary>
        /// Обработчик ввода директории. Возвращает строку с введенной директорией.
        /// </summary>
        /// <returns>Строка содержащая путь.</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private static string InputPathHandler()
        {
            Console.Write("Введите путь к папке с медиафайлами: ");
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
}
