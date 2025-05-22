using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RenameMediaScript
{
    public class Settings
    {
        private XDocument _document;

        public List<string> RegexMediaDateTime
        {
            get
            {
                return _regexMediaDateTime;
            }
            set
            {
                foreach (var regexString in value)
                {
                    if (regexString.Contains("?'year'")
                        && regexString.Contains("?'month'")
                        && regexString.Contains("?'day'")
                        && regexString.Contains("?'hour'")
                        && regexString.Contains("?'minute'")
                        && regexString.Contains("?'second'"))
                    {
                        _regexMediaDateTime.Add(regexString);
                    }
                }
            }
        }
        private List<string> _regexMediaDateTime = new List<string>();


        public void Load(string path = "Settings.xml")
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Не найден файл с настройками по пути {path}");
            }
            _document = XDocument.Load(path);

            if (_document.Root == null)
            {
                throw new NullReferenceException("Корневой элемент XML документа с настройками отсутствует!");
            }

            SearchRegexMediaDateTime();
        }

        private void SearchRegexMediaDateTime()
        {
            if (_document.Root.Element(nameof(RegexMediaDateTime)) == null)
            {
                throw new Exception($"Элемент {nameof(RegexMediaDateTime)} не найден в файле настроек.\nОтсутствуют регулярные значения для поиска даты и времени в наименовании медиа файла.");
            }

            if (!_document.Root.Element(nameof(RegexMediaDateTime)).HasElements)
            {
                throw new Exception($"Отсутствуют регулярные выражения внутри элемента {nameof(RegexMediaDateTime)}");
            }

            var elementsRegex = _document.Root.Element(nameof(RegexMediaDateTime)).Elements();

            RegexMediaDateTime = elementsRegex
                .Where(e => !string.IsNullOrWhiteSpace(e.Value))
                .Select(e => e.Value)
                .ToList();

            Console.WriteLine($"Количество загруженных регулярных выражений - {_regexMediaDateTime.Count}.");
        }
    }
}
