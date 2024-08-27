using System;
using System.Collections;
using System.Collections.Generic;

namespace ComView.Core.Helpers
{
    /// <summary>
    /// Структура обобщенного предела
    /// </summary>
    public struct Limit<T> where T : struct
    {
        #region static
        /// <summary>
        /// Пустое значение
        /// </summary>
        public static readonly Limit<T> Empty = new Limit<T>();
        /// <summary>
        /// Интерфейс сравнения значений
        /// </summary>
        internal static readonly IComparer Comparer = Comparer<T>.Default;

        #endregion

        #region fields, ctor
        /// <summary>
        /// Минимум и максимум
        /// </summary>
        public T Min, Max;

        /// <summary>
        /// Конструктор с указанием минимума и максимума
        /// </summary>
        public Limit(T min, T max)
        {
            Min = min;
            Max = max;
        }
        #endregion
    }

    /// <summary>
    /// Класс расширение для структуры предела
    /// </summary>
    public static class LimitHelper
    {
        #region validation
        /// <summary>
        /// Проверка на входимость в предел
        /// </summary>
        public static bool IsValid<T>(this Limit<T> limit) where T : struct =>
            Limit<T>.Comparer.Compare(limit.Min, limit.Max) <= 0;

        /// <summary>
        /// Производит валидацию предела
        /// </summary>
        public static void Validate<T>(this Limit<T> limit) where T : struct
        {
            if (limit.IsValid())
                return;

            throw new InvalidOperationException("limit is invalid");
        }
        #endregion

        #region math
        /// <summary>
        /// Проверка на входимость в предел
        /// </summary>
        public static bool IsPassed<T>(this Limit<T> limit, T value) where T : struct =>
            Limit<T>.Comparer.Compare(limit.Min, value) <= 0 && 
            Limit<T>.Comparer.Compare(limit.Max, value) >= 0;

        /// <summary>
        /// Нормализация значения
        /// </summary>
        public static T Normalize<T>(this Limit<T> limit, T value) where T : struct
        {
            limit.Validate();

            if (Limit<T>.Comparer.Compare(limit.Min, value) > 0)
                return limit.Min;
            if (Limit<T>.Comparer.Compare(limit.Max, value) < 0)
                return limit.Max;

            return value;
        }

        /// <summary>
        /// Нормализация значения
        /// </summary>
        public static Limit<T> Normalize<T>(this Limit<T> limit) where T : struct
        {
            limit.Validate();

            limit.Min = limit.Normalize(limit.Min);
            limit.Max = limit.Normalize(limit.Max);
            return limit;
        }
        #endregion
    }
}
