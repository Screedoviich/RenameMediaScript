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
            SearchRegexMediaDateTime();
            SearchExiftool();
        }

        private void SearchRegexMediaDateTime()
        {
            var elementRegexMediaDateTime = _document.Descendants(nameof(RegexMediaDateTime)).FirstOrDefault();
            if (elementRegexMediaDateTime == null)
            {
                throw new Exception($"Элемент {nameof(RegexMediaDateTime)} не найден в файле настроек.\nОтсутствуют регулярные значения для поиска даты и времени в наименовании медиа файла.");
            }

            var elementsRegex = elementRegexMediaDateTime.Elements();

            if (!elementsRegex.Any())
            {
                throw new Exception($"Отсутствуют регулярные выражения внутри элемента {nameof(RegexMediaDateTime)}");
            }

            RegexMediaDateTime = elementsRegex
                .Where(e => !string.IsNullOrWhiteSpace(e.Value))
                .Select(e => e.Value)
                .ToList();

            Console.WriteLine($"Количество загруженных регулярных выражений - {_regexMediaDateTime.Count}.");
        }

        private void SearchExiftool()
        {
            var elementExiftoolPath = _document.Descendants(nameof(ExiftoolPath)).FirstOrDefault();
            if (elementExiftoolPath == null)
            {
                throw new Exception($"Элемент {nameof(ExiftoolPath)} не найден в файле настроек.");
            }

            if (string.IsNullOrWhiteSpace(elementExiftoolPath.Value))
            {
                throw new Exception($"Элемент {nameof(ExiftoolPath)} найден в файле настроек, но не заполнен!");
            }

            ExiftoolPath = elementExiftoolPath.Value;
        }
    }
}
