using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Client
{

    /// <summary>
    ///  После запуска первым делом нажимаем Получить. Потом выбор формата и Отправить(можно 3 раза подряд все форматы). Потом идти по пути C:\vse\Lab6\Serialization\Client\bin\Debug\downloads ( там находятся эти файлы).
    /// </summary>
    public partial class Form1 : Form
    {
        [Serializable]
        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public Person(string name, int age)
            {
                Name = name;
                Age = age;
            }
            public Person() { }
        }

        [DataContract]
        class Student
        {
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public string Group { get; set; }
            public Student(string name, string group)
            {
                Name = name;
                Group = group;
            }
        }

        static string pathToDownloads = @"C:\vse\Lab6\Serialization\Client\bin\Debug\downloads\";
        static int serverPort = default;
        static int clientPort = default;
        static string localHost = "127.0.0.1";
        static IPAddress ipAddress = IPAddress.Parse(localHost);
        static Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

        Thread receiveThread = new Thread(() => { });
        Thread startThreadListener = new Thread(() => { });
        NetworkStream ns;

        delegate void forInvoke(Socket clientSocket);
        forInvoke forInvoke_;
        public Form1()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;
            ServerPort_.Text = 8004.ToString();
            ClientPort_.Text = 8004.ToString();
            ServerPort_.KeyPress += textBox_KeyPress;
            ClientPort_.KeyPress += textBox_KeyPress;
        }
        private byte[] standartFile(string filename)
        {
            byte[] fileData = File.ReadAllBytes(filename);
            filename = Path.GetFileName(filename);
            byte[] fileNameByte = Encoding.ASCII.GetBytes(filename);
            byte[] fileNameLen = BitConverter.GetBytes(fileNameByte.Length);
            byte[] clientData = new byte[4 + fileNameByte.Length + fileData.Length];

            fileNameLen.CopyTo(clientData, 0);
            fileNameByte.CopyTo(clientData, 4);
            fileData.CopyTo(clientData, 4 + fileNameByte.Length);

            return clientData;
        }
        public void binaryFormatter(Socket clientSocket)
        {
            Person person = new Person("binaryVB", 19);
            BinaryFormatter formatter = new BinaryFormatter();
            ns = new NetworkStream(clientSocket);
            formatter.Serialize(ns, person);
        }
        public void jsonFormatter(Socket clientSocket)
        {
            var students = new List<Student>() { new Student("VB", "IB19-1"), new Student("VLAD", "1") };
            ns = new NetworkStream(clientSocket);
            var JsonFormatter = new DataContractJsonSerializer(typeof(List<Student>));
            JsonFormatter.WriteObject(ns, students);
        }
        public void xmlFormatter(Socket clientSocket)
        {
            var persons = new List<Person>() { new Person("VB", 19), new Person("VLAD", 29) };
            ns = new NetworkStream(clientSocket);
            var xmlFormatter = new XmlSerializer(typeof(List<Person>));
            xmlFormatter.Serialize(ns, persons);
        }
        public void Receive(Socket clientSocket)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    {
                        byte[] clientData = new byte[1024 * 5000];
                        int receivedBytesLen = clientSocket.Receive(clientData);
                        int fileNameLen = BitConverter.ToInt32(clientData, 0);
                        string fileName = Encoding.ASCII.GetString(clientData, 4, fileNameLen);
                        fileName = Path.GetFileName(fileName);
                        BinaryWriter bWrite = new BinaryWriter(File.Open(pathToDownloads + fileName, FileMode.Create));
                        bWrite.Write(clientData, 4 + fileNameLen, receivedBytesLen - 4 - fileNameLen);
                        MessageBox.Show($"Файл - {fileName} создан!");
                        bWrite.Close();
                        break;
                    }
                case 1:
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        Person person = (Person)formatter.Deserialize(ns = new NetworkStream(clientSocket));
                        using (FileStream fs = new FileStream(pathToDownloads + "file2.xml", FileMode.Create))
                        {
                            formatter.Serialize(fs, person);
                            Console.WriteLine("Объект сериализован");
                        }
                        ns.Close();
                        MessageBox.Show(person.Name + " " + person.Age);
                        break;
                    }
                case 2:
                    {
                        var JsonFormatter = new DataContractJsonSerializer(typeof(List<Student>));
                        var students = JsonFormatter.ReadObject(ns = new NetworkStream(clientSocket)) as List<Student>;
                        using(var file = new FileStream(pathToDownloads + "file.json", FileMode.Create))                        
                            JsonFormatter.WriteObject(file, students);
                        ns.Close();
                        MessageBox.Show($"file.json создан!");
                        break;
                    }
                case 3:
                    {
                        var xmlFormatter = new XmlSerializer(typeof(List<Person>));
                        var persons = xmlFormatter.Deserialize(ns = new NetworkStream(clientSocket)) as List<Person>;
                        using (var file = new FileStream(pathToDownloads + "file.xml", FileMode.Create))
                            xmlFormatter.Serialize(file, persons);
                        ns.Close();
                        MessageBox.Show($"file.xml создан!");
                        break;
                    }
            }
            
            clientSocket.Close();
            receiveThread.Abort();
        }
        private void Send(int port, string filename = "")
        {
            IPAddress ipAddress = IPAddress.Parse(localHost);
            IPEndPoint ipEnd = new IPEndPoint(ipAddress, port);
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            clientSocket.Connect(ipEnd);

            switch (comboBox1.SelectedIndex)
            {
                case 0: {clientSocket.Send(standartFile(filename)); break; }
                case 1: { binaryFormatter(clientSocket); ns.Close(); break; }
                case 2: { jsonFormatter(clientSocket); ns.Close(); break; }
                case 3: { xmlFormatter(clientSocket); ns.Close(); break; }
            }

            clientSocket.Close();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (ClientPort_.Text != string.Empty)
            {
                clientPort = Int32.Parse(ClientPort_.Text);
                switch (comboBox1.SelectedIndex)
                {
                    case 0: 
                        {
                            OpenFileDialog openFile = new OpenFileDialog();
                            openFile.Filter = "Text|*.txt|All|*.*";

                            if (openFile.ShowDialog() == DialogResult.Cancel)
                                return;
                            Send(clientPort, openFile.FileName);
                            break;
                        }
                    default: 
                        {
                            Send(clientPort);
                            break;
                        }
                }
            }

        }
        private void Releaze()
        {
            serverSocket.Close();
            startThreadListener.Abort();
            receiveThread.Abort();
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e) => Releaze();
        private void button2_Click(object sender, EventArgs e)
        {
            if (ServerPort_.Text != string.Empty)
            {
                button2.Enabled = false;
                serverPort = Int32.Parse(ServerPort_.Text);
                IPEndPoint ipEnd = new IPEndPoint(ipAddress, serverPort);
                forInvoke_ = Receive;

                startThreadListener = new Thread(() =>
                {
                    serverSocket.Bind(ipEnd);
                    serverSocket.Listen(serverPort);
                    while (true)
                    {
                        Socket clientSocket = serverSocket.Accept();
                        receiveThread = new Thread(() => { Invoke(forInvoke_, clientSocket); });
                        receiveThread.Start();
                    }
                });
                startThreadListener.Start();
            }
        }
        private void textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
