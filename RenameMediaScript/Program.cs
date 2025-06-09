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
        public static void Main()
        {
            Settings settings = new Settings();
            settings.Load();

            // Получить директорию с файлами
            string path = InputPathHandler();
            // Получить путь ко всем файлам в папке
            string[] filesPath = Directory.GetFiles(path);

            Console.WriteLine($"Найдено `{filesPath.Length}` файлов в папке.\n");

            // Работа с файлами
            for (int i = 0; i < filesPath.Length; i++)
            {
                Console.WriteLine($"{i + 1}.....");
                // Создание объекта
                FileInfo fileInfo = new FileInfo(filesPath[i]);
                fileInfo.CheckFileExtension(settings.ImageExtension, settings.VideoExtension);
                fileInfo.WriteDateTime(settings.RegexMediaDateTime.ToArray());
                fileInfo.WriteNewFileName();
                // Вывести информацию о файлах
                if (fileInfo.BeProcessing.Value)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                Console.WriteLine(fileInfo.ToString());
                Console.ForegroundColor = ConsoleColor.White;
                // Сохранить новый файл и изменить дату/время
                fileInfo.EditAndSaveNewFile(settings.AllowReplaceFile, settings.ExiftoolPath);
            }

            Console.WriteLine("Программа завершила работу.");
            Console.ReadKey();
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
                throw new DirectoryNotFoundException($"Не найдена директория по пути `{userPath}`.");
            }
        }
    }
}
