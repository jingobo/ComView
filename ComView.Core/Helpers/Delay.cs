using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComView.Core.Helpers
{
    /// <summary>
    /// Класс измеряемой асинхронной задержки
    /// </summary>
    public sealed class Delay
    {
        #region fields, ctor
        /// <summary>
        /// Задержка в милисекундах
        /// </summary>
        private readonly int delay;
        /// <summary>
        /// Время начала отсчета
        /// </summary>
        private DateTime startTime = DateTime.Now;

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public Delay(int delay)
        {
            if (delay < 0)
                throw new ArgumentOutOfRangeException(nameof(delay));

            this.delay = delay;
        }
        #endregion

        #region methods
        /// <summary>
        /// Производит начало отсчета
        /// </summary>
        public void Start() =>
            startTime = DateTime.Now;

        /// <summary>
        /// Создает задачу на задержку
        /// </summary>
        public Task Pause()
        {
            // Перезапуск
            var lastTime = startTime;
            Start();

            // Дельта
            var elapsed = (int)(startTime - lastTime).TotalMilliseconds;
            elapsed = delay - elapsed;
            if (elapsed < 0)
                elapsed = 0;

            return Task.Delay(elapsed);
        }
        #endregion
    }
}
