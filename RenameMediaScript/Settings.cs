using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RenameMediaScript
{
    /// <summary>
    /// Содержит настройки приложения.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// XML документ содержащий файл с настройками.
        /// </summary>
        private XDocument _document;

        /// <summary>
        /// Путь к файлу Exiftool.
        /// </summary>
        public string ExiftoolPath
        {
            get { return _exiftoolPath; }
            set
            {
                if (!File.Exists(value))
                {
                    throw new FileNotFoundException($"Файл Exiftool не существует по пути {value}.\nРабота программы невозможна.");
                }
                _exiftoolPath = value;
            }
        }
        private string _exiftoolPath;

        /// <summary>
        /// Содержит регулярные выражения для поиска даты и времени в медиафайле.
        /// </summary>
        public List<string> RegexMediaDateTime
        {
            get { return _regexMediaDateTime; }
            set
            {
                foreach (var regexString in value)
                {
                    var dateTimeStrings = Enum.GetNames(typeof(MediaDateTime));

                    if (dateTimeStrings.All(s => regexString.Contains($"?'{s}'")))
                    {
                        _regexMediaDateTime.Add(regexString);
                    }
                }
            }
        }
        private List<string> _regexMediaDateTime = new List<string>();

        public string[] ImageExtension { get; set; }

        public string[] VideoExtension { get; set; }

        /// <summary>
        /// Разрешить замену оригинальных файлов.
        /// </summary>
        public bool AllowReplaceFile { get; set; }

        public Settings(string path = "Settings.xml")
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Не найден файл с настройками по пути {path}");
            }
            _document = XDocument.Load(path);
        }

        /// <summary>
        /// Загрузить параметры.
        /// </summary>
        public void Load()
        {
            ExiftoolPath = GetElementValue(nameof(ExiftoolPath));

            RegexMediaDateTime = GetElementsValue(nameof(RegexMediaDateTime)).ToList();
            Console.WriteLine($"Количество загруженных регулярных выражений - {_regexMediaDateTime.Count}.");

            ImageExtension = GetElementsValue(nameof(ImageExtension));
            Console.WriteLine($"Количество загруженных расширений для изображений - {ImageExtension.Length}.");

            VideoExtension = GetElementsValue(nameof(VideoExtension));
            Console.WriteLine($"Количество загруженных расширений для видео - {VideoExtension.Length}.");

            if (_document.Descendants(nameof(AllowReplaceFile)).Any())
            {
                AllowReplaceFile = true;
                Console.WriteLine($"ВНИМАНИЕ! Найден элемент {nameof(AllowReplaceFile)} в файле настроек. Оригинальные файлы будут заменены!");
            }
            else
            {
                AllowReplaceFile = false;
            }

        }

        /// <summary>
        /// Выполнить поиск элемента по названию, произвести обработку, получить значение элемента.
        /// </summary>
        /// <param name="elementName">Название элемента.</param>
        /// <returns>Значение элемента.</returns>
        /// <exception cref="Exception"></exception>
        private string GetElementValue(string elementName)
        {
            // Поиск элемента по названию
            XElement element = _document.Descendants(elementName).FirstOrDefault();
            // Если элемент не найден
            if (element == null)
            {
                throw new Exception($"Элемент {elementName} не найден в файле настроек.");
            }
            // Если элемент пустой
            if (string.IsNullOrWhiteSpace(element.Value))
            {
                throw new Exception($"Элемент {elementName} найден в файле настроек, но не заполнен!");
            }
            return element.Value;
        }

        private string[] GetElementsValue(string elementName)
        {
            var elements = _document.Descendants(elementName);
            if (!elements.Any())
            {
                throw new Exception($"Элемент {elementName} не найден в файле настроек.");
            }

            return elements
                .Where(e => !string.IsNullOrWhiteSpace(e.Value))
                .Select(e => e.Value)
                .ToArray();
        }
    }
}
