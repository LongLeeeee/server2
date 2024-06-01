using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ServerProject
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private TcpListener tcpListener;
        private TcpClient tcpClient;
        private bool isServerRunninng = false;

        FirebaseConfig config = new FirebaseConfig()
        {
            AuthSecret = "JSe2prlcdWFSAjZFjeR3SSh4BFUnkbAbZ979GVx3",
            BasePath = "https://testfirebase-c58f8-default-rtdb.firebaseio.com/"
        };
        IFirebaseClient FirebaseClient;
        List<string> userList;
        Dictionary<string, TcpClient> tcpClients;
        Dictionary<string, string> dataUser;
        List<string> groupNameList;
        Image ptb = null;
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        // lắng nghe kết nối từ cient
        private void listen()
        {
            tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, 8080));
            tcpListener.Start();

            dataUser = new Dictionary<string, string>();
            userList = new List<string>();
            FirebaseClient = new FireSharp.FirebaseClient(config);
            LoadInformation infoDB = new LoadInformation();
            tcpClients = new Dictionary<string, TcpClient>();
            infoDB.getData(ref userList, FirebaseClient, ref dataUser);
            string usernamerq = "";
            while (isServerRunninng)
            {
                try
                {
                    tcpClient = tcpListener.AcceptTcpClient();
                    StreamReader reader = new StreamReader(tcpClient.GetStream());
                    StreamWriter writer = new StreamWriter(tcpClient.GetStream());
                    writer.AutoFlush = true;

                    // đọc dòng đầu tiên để xem yêu cầu từ client 
                    string rqFromClient = "";

                    rqFromClient = reader.ReadLine();


                    // xử lí nếu client yêu cầu đăng nhập
                    if (rqFromClient == "Login")
                    {
                        //đọc dữ liệu mà client gửi để đăng nhập 
                        string lgDatafromClient = reader.ReadLine();
                        Data loginData = JsonConvert.DeserializeObject<Data>(lgDatafromClient);
                        bool success = false;
                        //tạo đối tượng để kiểm tra thông tin đăng nhập trùng khớp hay chưa
                        CheckDataBase checkdb = new CheckDataBase();
                        // hàm so sánh với database
                        checkdb.CompareData(ref dataUser, userList, loginData, ref success, rqFromClient, ref usernamerq);
                        if (success)
                        {
                            string responseFromServer = usernamerq + ":Login successfully";
                            writer.WriteLine(responseFromServer);
                            tcpClients.Add(usernamerq, tcpClient);
                            Invoke(new Action(() =>
                            {
                                richTextBox1.AppendText(usernamerq + " vừa đăng nhập.\r\n");
                            }));
                            Thread receiveThread = new Thread(() => receiveFromCLient(usernamerq));
                            receiveThread.Start();
                            receiveThread.IsBackground = true;
                        }
                        else
                        {
                            string responseFromServer = usernamerq + ":Login Failed";
                            writer.WriteLine(responseFromServer);
                        }
                    }
                    else if (rqFromClient == "Register")
                    {
                        //đọc dữ liệu mà client gửi để đăng ký 
                        string RgDatafromClient = reader.ReadLine();
                        Data registerData = JsonConvert.DeserializeObject<Data>(RgDatafromClient);
                        bool success = true;
                        //tạo đối tượng để kiểm tra thông tin đăng ký có bị trùng hay không
                        CheckDataBase checkdb = new CheckDataBase();
                        checkdb.CompareData(ref dataUser, userList, registerData, ref success, rqFromClient, ref usernamerq);
                        //đã tồn tại username trong database
                        if (success)
                        {
                            string responseFromServer = usernamerq + ":Register failed";
                            writer.WriteLine(responseFromServer);
                        }
                        // đăng kí thành công
                        else if (!success)
                        {
                            string responseFromServer = usernamerq + ":Register successfully";
                            writer.WriteLine(responseFromServer);
                            Invoke(new Action(() =>
                            {
                                richTextBox1.AppendText(usernamerq + " vừa đăng kí.\r\n");
                            }));
                            tcpClients.Add(usernamerq, tcpClient);
                            Thread receiveThread = new Thread(() => receiveFromCLient(usernamerq));
                            receiveThread.Start();
                            receiveThread.IsBackground = true;
                        }
                    }
                    else if (rqFromClient == "ForgotPass")
                    {
                        string fp_fromclient = reader.ReadLine();
                        Data fp_data = JsonConvert.DeserializeObject<Data>(fp_fromclient);
                        bool success = false;
                        CheckDataBase checkdb = new CheckDataBase();
                        checkdb.CompareData(ref dataUser, userList, fp_data, ref success, rqFromClient, ref usernamerq);
                        if (success)
                        {
                            string responseFromServer = usernamerq + ":A_to_reset";
                            writer.WriteLine(responseFromServer);
                            Invoke(new Action(() =>
                            {
                                richTextBox1.AppendText(usernamerq + " yêu cầu đặt lại mật khẩu.\r\n");
                            }));

                        }
                        else
                        {
                            string responseFromServer = usernamerq + ":A_to_reset failed";
                            writer.WriteLine(responseFromServer);
                        }
                        string rqfromClient1 = reader.ReadLine();
                        if (rqfromClient1 == "resetPass")
                        {

                            string MessfromClient = reader.ReadLine();
                            Data reset_data = JsonConvert.DeserializeObject<Data>(MessfromClient);
                            bool success1 = false;
                            CheckDataBase checkdb1 = new CheckDataBase();
                            string usernamerq1 = reset_data.userName;
                            checkdb.CompareData(ref dataUser, userList, reset_data, ref success1, rqFromClient, ref usernamerq1);
                            if (success)
                            {
                                var newUser = new Data()
                                {
                                    userName = reset_data.userName,
                                    password = reset_data.password,
                                    email = reset_data.email,
                                };
                                FirebaseClient = new FireSharp.FirebaseClient(config);
                                dataUser[newUser.email] = newUser.password;
                                var rs = FirebaseClient.Set($"users/{newUser.userName}/password/", newUser.password);
                                string responseFromServer = usernamerq1 + ":reset_success";
                                writer.WriteLine(responseFromServer);
                                Invoke(new Action(() =>
                                {
                                    richTextBox1.AppendText(usernamerq1 + " đã đặt lại mật khẩu.\r\n");
                                }));
                            }
                            else
                            {
                                string responseFromServer = usernamerq + ":reset_failed";
                                writer.WriteLine(responseFromServer);
                            }
                        }
                    }
                }
                catch
                {

                }
            }
        }
        private void receiveFromCLient(string username)
        {
            TcpClient client = tcpClients[username];
            StreamReader reader = new StreamReader(client.GetStream());
            StreamWriter writer = new StreamWriter(client.GetStream());
            writer.AutoFlush = true;

            while (client.Connected)
            {
                try
                {
                    string rqFromClient = reader.ReadLine();

                    if (rqFromClient == "Message")
                    {
                        string messageFromClient = reader.ReadLine();
                        // tạo 1 một tượng kiểu tin nhắn
                        tinNhan newMessage = JsonConvert.DeserializeObject<tinNhan>(messageFromClient);
                        Invoke(new Action(() =>
                        {
                            richTextBox1.AppendText($"Nguoi gui: {newMessage.sender}," +
                                                $" Noi dung: {newMessage.contentMess}, " +
                                                $"Nguoi nhan: {newMessage.receiver} " +
                                                $"Room: {newMessage.roomkey}\r\n");
                        }));
                        //lấy thong tin người gửi, người nhận, nội dung
                        string receiverName = newMessage.receiver;
                        PushResponse pushMessToFireBase = FirebaseClient.Push<tinNhan>($"chatrooms/{newMessage.roomkey}/Messages/", newMessage);
                        //tìm người nhận 
                        foreach (var item in tcpClients)
                        {
                            if (item.Key.CompareTo(receiverName) == 0)
                            {
                                StreamWriter receiver = new StreamWriter(item.Value.GetStream());
                                receiver.AutoFlush = true;
                                receiver.WriteLine("Message");
                                receiver.WriteLine(messageFromClient);
                                break;
                            }
                        }
                    }
                    else if (rqFromClient == "MessageForGroup")
                    {
                        string sender = reader.ReadLine();
                        string content = reader.ReadLine();
                        string groupName = reader.ReadLine();
                        string roomKey = reader.ReadLine();
                        string list = reader.ReadLine();

                        Invoke(new Action(() =>
                        {
                            richTextBox1.AppendText($"Nguoi gui: {sender}," +
                                                $" Noi dung: {content}, " +
                                                $"Ten phong: {groupName} " +
                                                $"Room: {roomKey}\r\n");
                        }));
                        string[] userListForGroup = list.Split('|');
                        tinNhan temp = new tinNhan()
                        {
                            sender = sender,
                            contentMess = content,
                            //receiver = userListForGroup,
                            groupName = groupName,
                            roomkey = roomKey,
                        };

                        PushResponse pushMessToFireBase = FirebaseClient.Push<tinNhan>($"chatroomforgroup/{roomKey}/messages/", temp);

                        foreach (var item in userListForGroup)
                        {
                            if (tcpClients.ContainsKey(item) && !string.IsNullOrEmpty(item) && (sender != item))
                            {
                                StreamWriter receiver = new StreamWriter(tcpClients[item].GetStream());
                                receiver.AutoFlush = true;
                                receiver.WriteLine("MessageForGroup");
                                receiver.WriteLine(sender);
                                receiver.WriteLine(content);
                                receiver.WriteLine(groupName);
                            }
                        }
                    }
                    else if (rqFromClient == "ListUser")
                    {
                        // Lấy dữ liệu từ Firebase
                        var response = FirebaseClient.Get($"users/");

                        string ListUserstring = "";
                        if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(response.Body))
                        {
                            try
                            {
                                // Đọc dữ liệu từ phản hồi
                                var jsonData = response.Body;
                                // Deserialize dữ liệu thành một đối tượng Dictionary<string, YourDataModel>
                                var users = JsonConvert.DeserializeObject<Dictionary<string, YourDataModel>>(jsonData);
                                foreach (var user in users)
                                {
                                    ListUserstring += user.Value.userName + "|";
                                }
                                StreamWriter writer1 = new StreamWriter(tcpClient.GetStream());
                                writer1.AutoFlush = true;
                                writer1.WriteLine(ListUserstring);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Error occurred while fetching data from Firebase!");
                        }

                    }
                    else if (rqFromClient == "Listfriend")
                    {
                        string listFriendString = "";
                        var response = FirebaseClient.Get($"friendList/{username}/friends/");
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (!string.IsNullOrEmpty(response.Body))
                            {
                                StreamWriter wr = new StreamWriter(client.GetStream());
                                wr.AutoFlush = true;
                                var friendlist = JsonConvert.DeserializeObject<Dictionary<string, Friend>>(response.Body);
                                if (friendlist != null)
                                {
                                    foreach (var item1 in friendlist)
                                    {
                                        listFriendString += item1.Value.username + "|";
                                    }
                                    wr.WriteLine(listFriendString);
                                }
                                else
                                {
                                    wr.WriteLine("Null");
                                }
                            }
                        }
                    }
                    else if (rqFromClient == "LoadMessage")
                    {
                        //List<tinNhan> msglistforARoom = new List<tinNhan>();
                        string msglistforARoom = "";
                        StreamReader reader1 = new StreamReader(client.GetStream());
                        StreamWriter wri = new StreamWriter(client.GetStream());
                        wri.AutoFlush = true;
                        string temp_roomKeyList = reader1.ReadLine();
                        string[] roomKeyList = temp_roomKeyList.Split('|');
                        foreach (var item in roomKeyList)
                        {
                            if (item != null)
                            {
                                var response = FirebaseClient.Get($"chatrooms/{item}/Messages/");
                                if (response.StatusCode == HttpStatusCode.OK)
                                {
                                    if (!string.IsNullOrEmpty(response.Body))
                                    {
                                        var messages = JsonConvert.DeserializeObject<Dictionary<string, tinNhan>>(response.Body);
                                        if (messages != null)
                                        {
                                            foreach (var item1 in messages)
                                            {
                                                msglistforARoom = item1.Value.sender + ": " + item1.Value.contentMess;
                                                if (msglistforARoom != null)
                                                {
                                                    wri.WriteLine(item);
                                                    wri.WriteLine(msglistforARoom);
                                                }
                                                msglistforARoom = null;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        wri.WriteLine("Null");
                    }
                    else if (rqFromClient == "LoadMessageForGroup")
                    {
                        string msglistforARoom = "";
                        string temp_roomKeyList = reader.ReadLine();
                        string[] roomKeyList = temp_roomKeyList.Split('|');
                        if (temp_roomKeyList != null)
                        {
                            foreach (var item in roomKeyList)
                            {
                                if (item != null)
                                {
                                    var response = FirebaseClient.Get($"chatroomforgroup/{item}/messages/");
                                    if (response.StatusCode == HttpStatusCode.OK)
                                    {
                                        if (!string.IsNullOrEmpty(response.Body))
                                        {
                                            var messages = JsonConvert.DeserializeObject<Dictionary<string, tinNhan>>(response.Body);
                                            if (messages != null)
                                            {
                                                foreach (var item1 in messages)
                                                {
                                                    msglistforARoom = item1.Value.sender + ": " + item1.Value.contentMess;
                                                    if (msglistforARoom != null)
                                                    {
                                                        writer.WriteLine(item);
                                                        writer.WriteLine(msglistforARoom);
                                                    }
                                                    msglistforARoom = null;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            writer.WriteLine("Null");
                        }
                        else
                        {
                            writer.WriteLine("Null");
                        }
                    }
                    else if (rqFromClient == "GroupName")
                    {
                        string groupListString = "";
                        string userforGroup;
                        var response = FirebaseClient.Get($"groupchat/{username}/listgroupchat/");
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (!string.IsNullOrEmpty(response.Body))
                            {
                                var grouplist = JsonConvert.DeserializeObject<Dictionary<string, groupchat>>(response.Body);
                                if (grouplist != null)
                                {
                                    foreach (var item in grouplist)
                                    {
                                        groupListString += item.Key + "|";
                                    }
                                    writer.WriteLine(groupListString);
                                    foreach (var item in grouplist)
                                    {
                                        userforGroup = "";
                                        foreach (var item2 in item.Value.receiver)
                                        {
                                            if (item2 != "")
                                            {
                                                userforGroup += item2 + "|";
                                            }
                                        }
                                        writer.WriteLine(userforGroup);
                                    }
                                    writer.WriteLine("Null");
                                }
                                else
                                {
                                    writer.WriteLine("Null");
                                }
                            }
                            else
                            {
                                writer.WriteLine("Null");
                            }
                        }
                    }
                    else if (rqFromClient == "LoadNotification")
                    {
                        string notiListString = "";
                        StreamWriter writer1 = new StreamWriter(client.GetStream());
                        writer1.AutoFlush = true;
                        var response = FirebaseClient.Get($"notificationData/{username}/notificationList/");
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (!string.IsNullOrEmpty(response.Body))
                            {
                                var notiList = JsonConvert.DeserializeObject<Dictionary<string, Notification>>(response.Body);
                                if (notiList != null)
                                {
                                    foreach (var item in notiList)
                                    {
                                        notiListString += item.Value.Sender + "|";
                                    }
                                    if (string.IsNullOrEmpty(notiListString))
                                    {
                                        writer1.WriteLine("Null");
                                    }
                                    else
                                    {
                                        writer1.WriteLine(notiListString);
                                    }
                                }
                                else
                                {
                                    writer1.WriteLine("Null");
                                }
                            }
                        }
                    }
                    else if (rqFromClient == "Image")
                    {
                        string receive = reader.ReadLine();
                        string senderName = receive.Substring(0, receive.IndexOf("|"));
                        string receiverName = receive.Substring(receive.IndexOf("|") + 1);
                        string imageDataString = reader.ReadLine();
                        ptb = StringToImage(imageDataString);
                        string ImageDataString1 = ImageToString(ptb);
                        Invoke(new Action(() =>
                        {
                            richTextBox1.AppendText(senderName + " vừa gửi 1 ảnh đến " + receiverName + ".\r\n");

                        }));

                        foreach (var item in tcpClients)
                        {
                            if (item.Key.CompareTo(receiverName) == 0)
                            {
                                StreamWriter writer1 = new StreamWriter(tcpClients[receiverName].GetStream());
                                writer1.AutoFlush = true;
                                writer1.WriteLine("Image");
                                writer1.WriteLine(senderName);
                                writer1.WriteLine(imageDataString);
                                break;
                            }
                        }
                    }
                    else if (rqFromClient == "Icon")
                    {
                        string receive = reader.ReadLine();
                        string senderName = receive.Substring(0, receive.IndexOf("|"));
                        string receiverName = receive.Substring(receive.IndexOf("|") + 1);
                        string IconLocation = reader.ReadLine();
                        
                        
                        Invoke(new Action(() =>
                        {
                            richTextBox1.AppendText(senderName + " vừa gửi 1 icon đến " + receiverName + ".\r\n");

                        }));

                        foreach (var item in tcpClients)
                        {
                            if (item.Key.CompareTo(receiverName) == 0)
                            {
                                StreamWriter writer1 = new StreamWriter(tcpClients[receiverName].GetStream());
                                writer1.AutoFlush = true;
                                writer1.WriteLine("Icon");
                                writer1.WriteLine(senderName);
                                writer1.WriteLine(IconLocation);
                                break;
                            }
                        }
                    }
                    else if (rqFromClient == "File")
                    {

                        NetworkStream networkStream = client.GetStream();

                        string receive = reader.ReadLine();
                        string senderName = receive.Substring(0, receive.IndexOf("|"));
                        string receiverName = receive.Substring(receive.IndexOf("|") + 1);
                        string fileName = reader.ReadLine();
                        long fileSize = Convert.ToInt64(reader.ReadLine());
                        string filePath = Path.Combine("Resources\\", fileName);


                        byte[] buffer = new byte[52428800];
                        int bytesRead;
                        long bytesReceived = 0;
                        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {

                            while (bytesReceived < fileSize &&
                               (bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                            {

                                fs.Write(buffer, 0, bytesRead);
                                bytesReceived += bytesRead;

                            }

                        }
                        Invoke(new Action(() =>
                        {
                            richTextBox1.AppendText(senderName + " vừa gửi 1 file: " + fileName + " đến " + receiverName + ".\r\n");

                        }));
                        foreach (var item in tcpClients)
                        {
                            if (item.Key.CompareTo(receiverName) == 0)
                            {
                                NetworkStream st = tcpClients[receiverName].GetStream();
                                StreamWriter swriter = new StreamWriter(tcpClients[receiverName].GetStream());
                                swriter.AutoFlush = true;
                                swriter.WriteLine("File");
                                swriter.WriteLine(senderName);
                                swriter.WriteLine(fileName);
                                swriter.WriteLine(fileSize.ToString());

                                using (FileStream ft = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                                {
                                    while ((bytesRead = ft.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        st.Write(buffer, 0, bytesRead);
                                    }
                                }
                            }
                        }
                    }
                    else if (rqFromClient == "AddFriend")
                    {
                        StreamReader rd = new StreamReader(client.GetStream());
                        string sender = rd.ReadLine();
                        string receiver = rd.ReadLine();
                        Invoke(new Action(() =>
                        {
                            richTextBox1.AppendText(sender + " vừa gửi lời mời kết bạn đến " + receiver + ".\r\n");

                        }));
                        if (tcpClients.ContainsKey(receiver))
                        {
                            StreamWriter wr = new StreamWriter(tcpClients[receiver].GetStream());
                            wr.AutoFlush = true;
                            wr.WriteLine("AddFriend");
                            wr.WriteLine(sender);
                        }
                        else
                        {
                            Notification notification = new Notification()
                            {
                                Sender = sender,
                                Receiver = receiver,
                                datetime = DateTime.Now,
                                content = $"{sender} đã gửi lời mời kết bạn đến bạn."
                            };
                            PushResponse pushNotiToFireBase = FirebaseClient.Push<Notification>($"notificationData/{receiver}/notificationList/", notification);

                        }
                    }
                    else if (rqFromClient == "Accepted")
                    {
                        StreamReader rd = new StreamReader(client.GetStream());
                        string sender = rd.ReadLine();
                        string receiver = rd.ReadLine();
                        if (tcpClients.ContainsKey(receiver))
                        {
                            StreamWriter wr = new StreamWriter(tcpClients[receiver].GetStream());
                            wr.AutoFlush = true;
                            StreamWriter wr1 = new StreamWriter(client.GetStream());
                            wr1.AutoFlush = true;

                            wr.WriteLine("AcceptedSuccessfullyForReceiver");
                            wr.WriteLine(sender);
                            Friend friend1 = new Friend()
                            {
                                username = sender
                            };

                            wr1.WriteLine("AcceptedSuccessfullyForSender");
                            wr1.WriteLine(receiver);
                            Friend friend2 = new Friend()
                            {
                                username = receiver
                            };
                            PushResponse pushFriendToFireBase = FirebaseClient.Push<Friend>($"friendList/{receiver}/friends/", friend1);
                            PushResponse pushFriendToFireBase1 = FirebaseClient.Push<Friend>($"friendList/{sender}/friends/", friend2);
                        }
                        else
                        {
                            StreamWriter wr1 = new StreamWriter(client.GetStream());
                            wr1.AutoFlush = true;

                            wr1.WriteLine("AcceptedSuccessfullyForSender");
                            wr1.WriteLine(receiver);
                            Friend friend1 = new Friend()
                            {
                                username = sender
                            };
                            Friend friend2 = new Friend()
                            {
                                username = receiver
                            };
                            PushResponse pushFriendToFireBase = FirebaseClient.Push<Friend>($"friendList/{receiver}/friends/", friend1);
                            PushResponse pushFriendToFireBase1 = FirebaseClient.Push<Friend>($"friendList/{sender}/friends/", friend2);
                        }
                    }
                    else if (rqFromClient == "CreateGroup")
                    {
                        string sender = reader.ReadLine();
                        string receivers = reader.ReadLine();
                        string groupName = reader.ReadLine();
                        string[] receiverList = receivers.Split('|');
                        Invoke(new Action(() =>
                        {
                            richTextBox1.AppendText(sender + " vừa gửi yêu cầu tạo nhóm với tên " + groupName + ".\r\n");

                        }));
                        var response = FirebaseClient.Get("groupnamelist/list/");
                        var temp1 = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Body);
                        groupNameList = new List<string>();
                        foreach (var item in temp1)
                        {
                            groupNameList.Add(item.Value);
                        }
                        if (groupNameList.Contains(groupName))
                        {
                            writer.WriteLine("CreatedFailure");
                            break;
                        }
                        else
                        {
                            writer.WriteLine("CreatedSuccessfully");
                            writer.WriteLine(sender);
                            writer.WriteLine(receivers);
                            writer.WriteLine(groupName);
                            foreach (var item in receiverList)
                            {
                                foreach (var item2 in tcpClients)
                                {
                                    if (item == item2.Key && item != sender)
                                    {
                                        StreamWriter wr = new StreamWriter(tcpClients[item].GetStream());
                                        wr.AutoFlush = true;
                                        wr.WriteLine("CreatedSuccessfully");
                                        wr.WriteLine(sender);
                                        wr.WriteLine(receivers);
                                        wr.WriteLine(groupName);
                                    }
                                }
                            }
                            groupchat temp = new groupchat()
                            {
                                groupName = groupName,
                                receiver = receiverList,
                                roomkey = getRoomKey(groupName),
                            };
                            foreach (var item in temp.receiver)
                            {
                                if (!string.IsNullOrEmpty(item))
                                {
                                    SetResponse groupchat = FirebaseClient.Set<groupchat>($"groupchat/{item}/listgroupchat/{groupName}", temp);
                                }
                            }
                            PushResponse groupname = FirebaseClient.Push<string>($"groupnamelist/list/", groupName);
                        }

                    }
                    else if (rqFromClient == "quit")
                    {
                        Invoke(new Action(() =>
                        {
                            richTextBox1.AppendText(username + " vừa offline.\r\n");
                        }));
                        reader.Close();
                        tcpClient.Close();
                        tcpClients.Remove(username);
                    }
                    else if (rqFromClient == "LogOut")
                    {
                        Invoke(new Action(() =>
                        {
                            richTextBox1.AppendText(username + " vừa đăng xuất.\r\n");
                        }));
                        reader.Close();
                        tcpClient.Close();
                        tcpClients.Remove(username);
                    }
                }
                catch
                {

                }
            }
        }
        private Image StringToImage(string imageDataString)
        {
            try
            {
                // Chuyển đổi chuỗi base64 thành mảng byte
                byte[] imageBytes = Convert.FromBase64String(imageDataString);
                // Tạo một MemoryStream từ mảng byte
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    // Tạo một đối tượng hình ảnh từ MemoryStream
                    Image image = Image.FromStream(ms);
                    return image;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
                return null;
            }
        }
        private string ImageToString(Image image)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    // Lưu hình ảnh vào MemoryStream dưới dạng JPEG
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    // Chuyển đổi sang chuỗi base64
                    byte[] imageBytes = ms.ToArray();
                    string base64String = Convert.ToBase64String(imageBytes);
                    return base64String;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
                return null;
            }
        }
        private string getRoomKey(string username1 = "", string username2 = "")
        {
            int total = 0;

            foreach (char item in username1)
            {
                total += (int)item;
            }
            foreach (char item in username2)
            {
                total += (int)item;
            }
            return total.ToString();
        }
        private void btn_listen_Click(object sender, EventArgs e)
        {
            if (isServerRunninng)
            {
                //tcpClient.Close();
                isServerRunninng = false;
                tcpListener.Stop();
                Invoke(new Action(() =>
                {
                    richTextBox1.AppendText($"{DateTime.Now} : Server stop listen on port 8080.\r\n");
                }));
                btn_listen.Text = "Listen";
            }
            else
            {
                isServerRunninng = true;
                Thread serverThread = new Thread(listen);
                serverThread.Start();
                serverThread.IsBackground = true;
                Invoke(new Action(() =>
                {
                    richTextBox1.AppendText($"{DateTime.Now} : Server is listening on port 8080.\r\n");
                }));
                btn_listen.Text = "Stop";
            }
        }
    }
}
