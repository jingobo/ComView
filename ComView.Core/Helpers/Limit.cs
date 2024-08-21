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
        #region fields, ctor
        /// <summary>
        /// Минимум и максимум
        /// </summary>
        public T Min, Max;
        
        /// <summary>
        /// Пустое значение
        /// </summary>
        public static readonly Limit<T> Empty = new Limit<T>();

        /// <summary>
        /// Конструктор с указанием минимума и максимума
        /// </summary>
        public Limit(T min, T max)
        {
            Min = min;
            Max = max;
        }
        #endregion

        #region properties
        /// <summary>
        /// Получает интерфейс сравнения значений
        /// </summary>
        private static IComparer Comparer
        {
            get => Comparer<T>.Default;
        }

        /// <summary>
        /// Резмер окна предела
        /// </summary>
        public T Window
        {
            get
            {
                dynamic min = Min;
                dynamic max = Max;
                return (T)(max - min);
            }
        }

        /// <summary>
        /// Проверка на валидность
        /// </summary>
        public bool IsValid
        {
            get => Comparer.Compare(Min, Max) <= 0;
        }
        #endregion

        #region methods
        /// <summary>
        /// Проверка на входимость в предел
        /// </summary>
        public bool IsPassed(T value) =>
            Comparer.Compare(Min, value) <= 0 && Comparer.Compare(Max, value) >= 0;

        /// <summary>
        /// Нормализация значения
        /// </summary>
        public T Normalize(T value)
        {
            if (!IsValid)
                throw new InvalidOperationException("invalid limit");

            if (Comparer.Compare(Min, value) > 0)
                return Min;
            if (Comparer.Compare(Max, value) < 0)
                return Max;
            return value;
        }

        /// <summary>
        /// Нормализация значения
        /// </summary>
        public Limit<T> Normalize(Limit<T> limit)
        {
            if (!IsValid)
                throw new InvalidOperationException("invalid limit");
            
            limit.Min = Normalize(limit.Min);
            limit.Max = Normalize(limit.Max);
            return limit;
        }
        #endregion
    }
}
