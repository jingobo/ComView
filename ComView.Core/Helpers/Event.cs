using System;
using System.Threading;

namespace ComView.Core.Helpers
{
    /// <summary>
    /// Класс расширение для работы с событиями
    /// </summary>
    internal static class EventHelper
    {
        /// <summary>
        /// Ожидание события с подавлением исключений
        /// </summary>
        public static void WaitSafe(this EventWaitHandle ev)
        {
            try
            {
                ev.WaitOne();
            }
            catch (ObjectDisposedException)
            { }
        }

        /// <summary>
        /// Производит установку и освобождение события
        /// </summary>
        public static void DisposeSafe(this EventWaitHandle ev) 
        {
            ev.Set();
            ev.Dispose();
        }
    }
}
