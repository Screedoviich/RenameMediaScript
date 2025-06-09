using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RenameMediaScript
{
    /// <summary>
    /// Содержит методы расширения для перечислений.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Возвращает описание перечисления. Если описание отсутствует возвращает строковое представление перечисления.
        /// </summary>
        /// <param name="value">Значение перечисления.</param>
        /// <returns>Описание значения перечисления в противном случае его строковое представление.</returns>
        public static string GetDescription(this Enum value)
        {
            // Получить данные перечисления
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
            // Получить атрибут из данных
            DescriptionAttribute attribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
            // Если атрибут найден и не пуст вернуть его значение в противном случае вернуть строкое представление перечисления
            return attribute?.Description ?? value.ToString();
        }
    }
}
