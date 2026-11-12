using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Models.User
{
    public class UserCredentialsModelBase
    {
        public string Password { get; set; } = null!;
        public string PasswordReplication { get; set; } = null!;
    }
}
