using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;

namespace Logic.Shared
{
    public class ALogicBase
    {
        protected readonly IUnitOfWork UnitOfWork;
        protected readonly IMessageLog MessageLog;

        public ALogicBase(IUnitOfWork unitOfWork, IMessageLog messageLog)
        {
            UnitOfWork = unitOfWork;
            MessageLog = messageLog;
        }
    }
}
