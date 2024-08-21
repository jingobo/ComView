using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComView.Core.Pool
{
    /// <summary>
    /// Интерфейс опросника портов
    /// </summary>
    public interface IComPooler
    {
        /// <summary>
        /// Получает управляемый список портов
        /// </summary>
        IList<ComPortModel> Ports
        {
            get;
        }

        /// <summary>
        /// Производит остановку с асинхронным ожиданием
        /// </summary>
        Task Stop();
    }

    /// <summary>
    /// Класс расширение для интерфейса опросника портов
    /// </summary>
    public static class ComPoolerHelper
    {
        /// <summary>
        /// Производит остановку списка опросников
        /// </summary>
        public static Task Stop(this IList<IComPooler> poolers) =>
            poolers.Count > 0 ?
                Task.WhenAny(poolers.Select(p => p.Stop())) :
                Task.Delay(0);

    }
}
