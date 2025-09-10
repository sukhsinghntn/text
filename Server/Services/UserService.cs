using NDAProcesses.Shared.Services;
using NDAProcesses.Shared.Models;
using System.DirectoryServices;

namespace NDAProcesses.Server.Services
{
    public class UserService : IUserService
    {
        private readonly IConfiguration _configuration;

        public UserService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> ValidateUser(UserModel user)
        {
            try
            {
                // Create a DirectoryEntry object with the provided credentials
                using (DirectoryEntry entry = new DirectoryEntry(
                    _configuration["LDAP:LDAPPath"], user.UserName, user.Password, AuthenticationTypes.Secure))
                {
                    // Access the NativeObject property to force authentication
                    object nativeObject = entry.NativeObject;

                    // If this point is reached, credentials are valid
                    return true;
                }
            }
            catch (DirectoryServicesCOMException ex)
            {
                Console.WriteLine($"Invalid credentials: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Log or handle unexpected errors
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
        }

        public async Task<UserModel> GetUserData(string userName)
        {
            try
            {
                // Create a DirectoryEntry object with the provided credentials
                using (DirectoryEntry entry = new DirectoryEntry(
                    _configuration["LDAP:LDAPPath"], _configuration["LDAP:UserName"], _configuration["LDAP:Password"], AuthenticationTypes.Secure))
                {
                    using (DirectorySearcher searcher = new DirectorySearcher(entry))
                    {
                        // Set the filter to search for the user's account
                        searcher.Filter = $"(sAMAccountName={userName})";

                        // Specify which attributes to load
                        searcher.PropertiesToLoad.Add("cn");
                        searcher.PropertiesToLoad.Add("title");
                        searcher.PropertiesToLoad.Add("mail");
                        searcher.PropertiesToLoad.Add("department");
                        searcher.PropertiesToLoad.Add("displayName");
                        searcher.PropertiesToLoad.Add("manager");
                        searcher.PropertiesToLoad.Add("physicalDeliveryOfficeName");

                        // Execute the search
                        SearchResult result = searcher.FindOne();

                        if (result != null)
                        {
                            UserModel user = new UserModel();
                            // Map the retrieved attributes to the UserModel
                            user.UserName = userName;
                            user.Name = result.Properties["cn"].Count > 0 ? result.Properties["cn"][0].ToString() : string.Empty;
                            user.Title = result.Properties["title"].Count > 0 ? result.Properties["title"][0].ToString() : string.Empty;
                            user.Email = result.Properties["mail"].Count > 0 ? result.Properties["mail"][0].ToString() : string.Empty;
                            user.Department = result.Properties["department"].Count > 0 ? result.Properties["department"][0].ToString() : string.Empty;
                            user.DisplayName = result.Properties["displayName"].Count > 0 ? result.Properties["displayName"][0].ToString() : string.Empty;
                            user.Manager = result.Properties["manager"].Count > 0 ? result.Properties["manager"][0].ToString() : string.Empty;
                            user.Manager = ExtractNameFromDN(user.Manager);
                            user.Location = result.Properties["physicalDeliveryOfficeName"].Count > 0 ? result.Properties["physicalDeliveryOfficeName"][0].ToString() : string.Empty;

                            // Return the populated user model
                            return user;
                        }
                        else
                        {
                            // User not found or invalid credentials
                            return null;
                        }
                    }
                }
            }
            catch (DirectoryServicesCOMException)
            {
                // Invalid credentials
                return null;
            }
            catch (Exception ex)
            {
                // Log or handle unexpected errors
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        static string ExtractNameFromDN(string distinguishedName)
        {
            if (string.IsNullOrEmpty(distinguishedName))
                return string.Empty;

            // Look for "CN=" and extract the value until the next comma
            var startIndex = distinguishedName.IndexOf("CN=") + 3;
            var endIndex = distinguishedName.IndexOf(",", startIndex);

            return endIndex > startIndex
                ? distinguishedName.Substring(startIndex, endIndex - startIndex)
                : distinguishedName[startIndex..]; // If no comma, take the rest
        }

    }
}
