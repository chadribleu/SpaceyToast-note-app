using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace SpaceyToast.Source.Helpers
{
    public enum UserInfo { FirstName, LastName, Email };

    /// <summary>
    /// Shared functions that are accessible for every classes of the SpaceyToast projet.
    /// </summary>
    public sealed class Helpers
    {
        public async static Task<string> GetUsername(UserInfo info)
        {
            IReadOnlyList<Windows.System.User> users = await Windows.System.User.FindAllAsync();
            var current = users.Where(p => p.AuthenticationStatus == UserAuthenticationStatus.LocallyAuthenticated &&
                                p.Type == UserType.LocalUser).FirstOrDefault();
            
            string username = string.Empty;
            switch (info)
            {
                case UserInfo.FirstName:
                    username += await current.GetPropertyAsync(KnownUserProperties.FirstName) as string;
                    break;
                case UserInfo.LastName:
                    username += await current.GetPropertyAsync(KnownUserProperties.LastName) as string;
                    break;
                case UserInfo.Email:
                    username += await current.GetPropertyAsync(KnownUserProperties.AccountName) as string;
                    break;
            }
            return username;
        }

        // Only for reference, was replaced by Guids
        public static string ComposeFilePath(int year, string month, int day)
        {
            return @"\" + "Board_Data" + @"\" + year.ToString() + @"\" + month + @"\" + day.ToString() + ".json";
        }
    }
}
