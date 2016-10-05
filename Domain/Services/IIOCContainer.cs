using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IIOCContainer : IDisposable
    {
        IIOCContainer CreateChildContainer();

        object Resolve(Type type);

        T Resolve<T>();
        void RegisterInstance<TI>(TI instance);
    }
}
