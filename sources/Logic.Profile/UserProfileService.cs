using Logic.Shared;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Interfaces.User;

namespace Logic.Profile
{
    public class UserProfileService: ALogicBase, IUserProfileService
    {
        public UserProfileService(IUnitOfWork unitOfWork, IMessageLog messageLog): base(unitOfWork, messageLog)
        {
            
        }
    }
}
