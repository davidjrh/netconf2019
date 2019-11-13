﻿using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using AlexaBackendAPI.Graph.Models;
using System;
using System.Threading.Tasks;

namespace AlexaBackendAPI.Graph
{
    public class GraphClient
    {
        private const string aadInstance = "https://login.microsoftonline.com/";
        private const string aadGraphResourceId = "https://graph.windows.net/";
        private const string aadGraphEndpoint = "https://graph.windows.net/";
        private const string aadGraphVersion = "api-version=1.6";
        private const string msGraphResourceId = "https://graph.microsoft.com/";
        private const string msGraphEndpoint = "https://graph.microsoft.com/";
        private const string msGraphVersion = "v1.0";

        private enum GraphApiVersion
        {
            beta,
            latest
        }



        #region Properties
        private string Tenant { get; set; }
        private AuthenticationContext AuthContext { get; set; }
        private ClientCredential Credential { get; set; }
        private string AccessToken {get; set;}
        #endregion

        #region Constructors
        public GraphClient(string clientId, string clientSecret, string tenant)
        {
            // The client_id, client_secret, and tenant are pulled in from the App.config file
            Tenant = tenant;

            // The AuthenticationContext is ADAL's primary class, in which you indicate the direcotry to use.
            AuthContext = new AuthenticationContext(aadInstance + tenant);

            // The ClientCredential is where you pass in your client_id and client_secret, which are 
            // provided to Azure AD in order to receive an access_token using the app's identity.
            Credential = new ClientCredential(clientId, clientSecret);
        }
        public GraphClient(string tenant, string accessToken)
        {
            Tenant = tenant;
            AccessToken = accessToken;
        }
        #endregion

        public User GetUser(string objectId)
        {
            var result = SendAADGraphRequest("/users/" + objectId);
            return JsonConvert.DeserializeObject<User>(result);
        }

        public GraphList<User> GetAllUsers(string query)
        {
            var result = SendAADGraphRequest("/users", query);
            return JsonConvert.DeserializeObject<GraphList<User>>(result);
        }

        public void DeleteUser(string objectId)
        {
            _ = SendAADGraphRequest("/users/" + objectId, httpMethod: HttpMethod.Delete);
        }

        public User AddUser(NewUser newUser)
        {
            var body = JsonConvert.SerializeObject(newUser);
            var result = SendAADGraphRequest("/users", body: body, httpMethod: HttpMethod.Post);
            return JsonConvert.DeserializeObject<User>(result);
        }

        public void UpdateUser(User user)
        {
            var body = JsonConvert.SerializeObject(user);
            _ = SendAADGraphRequest("/users/" + user.ObjectId, body: body, httpMethod: new HttpMethod("PATCH"));
        }

        public GraphList<Group> GetAllGroups(string query)
        {
            var result = SendAADGraphRequest("/groups", query);
            return JsonConvert.DeserializeObject<GraphList<Group>>(result);
        }

        public GraphList<Group> GetUserGroups(string userId)
        {
            var result = SendAADGraphRequest($"/users/{userId}/memberOf");
            //var result = await SendGraphGetRequest($"/users/{userId}/memberOf?$select=displayName,description", null);
            return JsonConvert.DeserializeObject<GraphList<Group>>(result);
        }

        public GraphList<User> GetGroupMembers(string groupId)
        {
            var result = SendAADGraphRequest($"/groups/{groupId}/members");
            return JsonConvert.DeserializeObject<GraphList<User>>(result);
        }

        public string GetAADObjectReference(string objectId)
        {
            return aadGraphEndpoint + Tenant + "/directoryObjects/" + objectId;
        }
        public string GetObjectReference(string objectId)
        {
            return msGraphEndpoint + msGraphVersion + "/directoryObjects/" + objectId;
        }

        public void AddGroupMember(string groupId, string userId)
        {
            var body = "{'url':'" + GetAADObjectReference(userId) + "'}";
            _ = SendAADGraphRequest($"/groups/{groupId}/$links/members", body: body, httpMethod: HttpMethod.Post);
        }

        public void RemoveGroupMember(string groupId, string userId)
        {
            var result = SendAADGraphRequest($"/groups/{groupId}/$links/members/{userId}", httpMethod: HttpMethod.Delete);
        }

        public ProfilePictureMetadata GetUserProfilePictureMetadata(string userId)
        {
            try
            {
                var result = SendGraphRequest("/users/" + userId + "/photo", apiVersion: GraphApiVersion.beta);
                return JsonConvert.DeserializeObject<ProfilePictureMetadata>(result);
            }
            catch (WebException)
            {
                // When the user doesn't have profile picture, the request throws a WebException
                return null;
            }
        }

        public byte[] GetUserProfilePicture(string userId)
        {
            try
            {
                var metadata = GetUserProfilePictureMetadata(userId);
                return SendGraphBinaryRequest("/users/" + userId + "/photo/$value", null, GraphApiVersion.beta);
            }
            catch (WebException)
            {
                // When the user doesn't have profile picture, the request throws a WebException
                return null;
            }
        }

        public GraphList<Models.Application> GetApplications(string query)
        {
            var result = SendAADGraphRequest("/applications", query);
            return JsonConvert.DeserializeObject<GraphList<Models.Application>>(result);
        }

        public GraphList<Extension> RegisterExtension(string appObjectId, Extension extension)
        {
            var body = JsonConvert.SerializeObject(extension);
            var result = SendAADGraphRequest("/applications/" + appObjectId + "/extensionProperties", body: body, httpMethod: HttpMethod.Post);
            return JsonConvert.DeserializeObject<GraphList<Extension>>(result);
        }

        public void UnregisterExtension(string appObjectId, string extensionObjectId)
        {
            _ = SendAADGraphRequest("/applications/" + appObjectId + "/extensionProperties/" + extensionObjectId, httpMethod: HttpMethod.Delete);
        }

        public GraphList<Extension> GetExtensions(string appObjectId)
        {
            var result = SendAADGraphRequest("/applications/" + appObjectId + "/extensionProperties");
            return JsonConvert.DeserializeObject<GraphList<Extension>>(result);
        }

        public Models.Application GetB2CExtensionApplication()
        {
            return GetApplications("$filter=startswith(displayName, 'b2c-extensions-app')").Values?.FirstOrDefault();
        }

        public Calendar GetDefaultCalendar(string userId)
        {
            var result = SendGraphRequest($"/users/{userId}/calendar", apiVersion: GraphApiVersion.latest);
            return JsonConvert.DeserializeObject<Calendar>(result);
        }

        public async Task<GraphList<CalendarView>> GetCalendarViewAsync(string userId, DateTime _from, DateTime _to)
        {
            var result = await SendGraphRequestAsync($"/users/{userId}/calendarView?startDateTime={_from.ToString("o")}&endDateTime={_to.ToString("o")}").ConfigureAwait(false);
            return JsonConvert.DeserializeObject<GraphList<CalendarView>>(result);
        }

        private async Task<string> SendGraphRequestAsync(string api, string query = null, string body = null, GraphApiVersion apiVersion = GraphApiVersion.latest, HttpMethod httpMethod = null)
        {
            // First, use ADAL to acquire a token using the app's identity (the credential)
            // The first parameter is the resource we want an access_token for; in this case, the Graph API.
            var accessToken = AccessToken;
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = (await AuthContext.AcquireTokenAsync(msGraphResourceId, Credential).ConfigureAwait(false)).AccessToken;
            }

            // For B2C user managment, be sure to use the 1.6 Graph API version.
            using (var http = new HttpClient())
            {
                var url = msGraphEndpoint + (apiVersion == GraphApiVersion.latest ? msGraphVersion : "beta") + api;
                if (!string.IsNullOrEmpty(query))
                {
                    url += "&" + query;
                }

                // Append the access token for the Graph API to the Authorization header of the request, using the Bearer scheme.
                var request = new HttpRequestMessage(httpMethod ?? HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                if (!string.IsNullOrEmpty(body))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }
                var response = await http.SendAsync(request).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var formatted = JsonConvert.DeserializeObject(error);
                    throw new WebException("Error Calling the Graph API: \n" + JsonConvert.SerializeObject(formatted, Formatting.Indented));
                }
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        private string SendAADGraphRequest(string api, string query = null, string body = null, HttpMethod httpMethod = null)
        {
            // First, use ADAL to acquire a token using the app's identity (the credential)
            // The first parameter is the resource we want an access_token for; in this case, the Graph API.
            var accessToken = AccessToken;
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = AuthContext.AcquireTokenAsync(aadGraphResourceId, Credential).Result.AccessToken;
            }
            

            // For B2C user managment, be sure to use the 1.6 Graph API version.
            using (var http = new HttpClient())
            {
                var url = aadGraphEndpoint + Tenant + api + "?" + aadGraphVersion;
                if (!string.IsNullOrEmpty(query))
                {
                    url += "&" + query;
                }

                // Append the access token for the Graph API to the Authorization header of the request, using the Bearer scheme.
                var request = new HttpRequestMessage(httpMethod ?? HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                if (!string.IsNullOrEmpty(body))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }
                var response = http.SendAsync(request).Result;

                if (!response.IsSuccessStatusCode)
                {
                    var error = response.Content.ReadAsStringAsync().Result;
                    var formatted = JsonConvert.DeserializeObject(error);
                    throw new WebException("Error Calling the Graph API: \n" + JsonConvert.SerializeObject(formatted, Formatting.Indented));
                }
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        private string SendGraphRequest(string api, string query = null, string body = null, GraphApiVersion apiVersion = GraphApiVersion.latest, HttpMethod httpMethod = null)
        {
            // First, use ADAL to acquire a token using the app's identity (the credential)
            // The first parameter is the resource we want an access_token for; in this case, the Graph API.
            var accessToken = AccessToken;
            if (string.IsNullOrEmpty(accessToken))
            {                              
                accessToken = AuthContext.AcquireTokenAsync(msGraphResourceId, Credential).Result.AccessToken;
            }

            // For B2C user managment, be sure to use the 1.6 Graph API version.
            using (var http = new HttpClient())
            {
                var url = msGraphEndpoint + (apiVersion == GraphApiVersion.latest ? msGraphVersion : "beta") + api;
                if (!string.IsNullOrEmpty(query))
                {
                    url += "&" + query;
                }

                // Append the access token for the Graph API to the Authorization header of the request, using the Bearer scheme.
                var request = new HttpRequestMessage(httpMethod ?? HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                if (!string.IsNullOrEmpty(body))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }
                var response = http.SendAsync(request).Result;

                if (!response.IsSuccessStatusCode)
                {
                    var error = response.Content.ReadAsStringAsync().Result;
                    var formatted = JsonConvert.DeserializeObject(error);
                    throw new WebException("Error Calling the Graph API: \n" + JsonConvert.SerializeObject(formatted, Formatting.Indented));
                }
                return response.Content.ReadAsStringAsync().Result;
            }
        }


        private byte[] SendGraphBinaryRequest(string api, string query, GraphApiVersion apiVersion = GraphApiVersion.latest, HttpMethod httpMethod = null)
        {
            // First, use ADAL to acquire a token using the app's identity (the credential)
            // The first parameter is the resource we want an access_token for; in this case, the Graph API.
            var accessToken = AccessToken;
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = AuthContext.AcquireTokenAsync(msGraphResourceId, Credential).Result.AccessToken;
            }

            // For B2C user managment, be sure to use the 1.6 Graph API version.
            using (var http = new HttpClient())
            {
                var url = msGraphEndpoint + (apiVersion == GraphApiVersion.latest ? msGraphVersion : "beta") + api;
                if (!string.IsNullOrEmpty(query))
                {
                    url += "&" + query;
                }

                // Append the access token for the Graph API to the Authorization header of the request, using the Bearer scheme.
                var request = new HttpRequestMessage(httpMethod ?? HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = http.SendAsync(request).Result;

                if (!response.IsSuccessStatusCode)
                {
                    var error = response.Content.ReadAsStringAsync().Result;
                    var formatted = JsonConvert.DeserializeObject(error);
                    throw new WebException("Error Calling the Graph API: \n" + JsonConvert.SerializeObject(formatted, Formatting.Indented));
                }

                return response.Content.ReadAsByteArrayAsync().Result;
            }
        }
    }
}
