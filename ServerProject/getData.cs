using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using FireSharp.Extensions;
using FireSharp.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Media.Animation;
using System.Net;
using System.Windows.Forms;

namespace ServerProject
{
    class Data
    {
        public string email { get; set; }
        public string password { get; set; }
        public string userName { get; set; }
        public string name { get; set; }
        public List<string> friends { get; set; }
        public Notification notification { get; set; }
    }
    public class Notification
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public DateTime datetime { get; set; }
        public string content { get; set; }
    }
    class tinNhan
    {
        public string sender { get; set; }
        public string contentMess { get; set; }
        public string receiver { get; set; }
        public string roomkey {  get; set; }
        public string groupName { get; set; }
    }
    class groupchat
    {
        public string[] receiver { get; set; }
        public string roomkey { get; set; }
        public string groupName { get; set; }

    }
    class Friend
    {
        public string username { get; set; }
    }
    class LoadInformation
    {

        public void getData(ref List<string> userList, IFirebaseClient client, ref Dictionary<string, string> check)
        {
            FirebaseResponse response = client.Get("users/");
            if (response.Body != null)
            {
                var users = JsonConvert.DeserializeObject<Dictionary<string, Data>>(response.Body);
                foreach (var item in users)
                {
                    if (item.Value.userName != null && item.Value != null && item.Key != null)
                    {
                        userList.Add(item.Value.userName);
                    }
                }
            }
            for (int j = 0; j < userList.Count(); j++)
            {
                if (userList[j] != null)
                {
                    FirebaseResponse response1 = client.Get("users/" + userList[j] + "/");
                    Data data1 = response1.ResultAs<Data>();
                    check.Add(data1.email, data1.password);
                }
            }
        }
    }
    class CheckDataBase : LoadInformation
    {

        FirebaseConfig config = new FirebaseConfig()
        {
            AuthSecret = "x8Z5vS17muGioNQZJgeGHU9V9nggI1dOcKDlzHmv",
            BasePath = "https://chat-application-of-team-12-default-rtdb.firebaseio.com/"
        };
        
        IFirebaseClient client;
        public void CompareData(ref Dictionary<string, string> check, List<string> userList, Data loginUser, ref bool success, string code, ref string usernameOnline)
        {
            if (code == "Login")
            {
                for (int i = 0; i < userList.Count; i++)
                {
                    if ((loginUser.email.CompareTo(check.ElementAt(i).Key) == 0) && (loginUser.password.CompareTo(check.ElementAt(i).Value) == 0))
                    {
                        success = true;
                        usernameOnline = userList[i];
                        break;
                    }
                }
            }
            else if (code == "Register")
            {
                success = false;
                for (int i = 0; i < userList.Count; i++)
                {
                    if ((loginUser.email.CompareTo(check.ElementAt(i).Key) == 0) || (loginUser.userName.CompareTo(userList[i]) == 0))
                    {
                        success = true;
                        return;
                    }
                }
                check.Add(loginUser.email, loginUser.password);
                userList.Add(loginUser.userName);
                usernameOnline = loginUser.userName;
                var newUser = new Data
                {
                    userName = loginUser.userName,
                    password = loginUser.password,
                    email = loginUser.email,
                    name = loginUser.userName,
                };
                client = new FireSharp.FirebaseClient(config);
                SetResponse rs;
                rs = client.Set<Data>($"users/{newUser.userName}", newUser);
            }
            else if (code == "ForgotPass")
            {
                for (int i = 0; i < userList.Count; i++)
                {
                    if ((loginUser.email.CompareTo(check.ElementAt(i).Key) == 0))
                    {
                        success = true;
                        usernameOnline = userList[i];
                        break;
                    }
                }
            }
            else if (code == "resetPass")
            {
                for (int i = 0; i < userList.Count; i++)
                {
                    if ((loginUser.email.CompareTo(check.ElementAt(i).Key) == 0))
                    {
                        success = true;

                        break;
                    }
                }
            }
        }
    }
}
