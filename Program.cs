using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;


//传向服务器的数据
//1：开始匹配
//2：放置完成，准备好了
//3：棋盘信息
//4：攻击信息

//服务器向客户端的信息
//1：匹配成功，可以开始部署了
//2：游戏终止（有一方退出游戏）
//3：两边都准备好了，可以开始了（传输棋盘）
//4：接收敌方棋盘
//5：本机进攻
//6：本机被攻击


namespace SocketServerAcceptMultipleClient
{
    public class SocketServer
    {
        // 创建一个和客户端通信的套接字
        static Socket socketwatch = null;

        //static int start = 0; // 0表示没有开始，1表示开始
        static int stage = 0;

        class Player
        {
            public Socket socket;
            public bool firstHand;
            public int BattleState;//0是正在对局，1是对局结束
            public int start;
        };

        // 定义一个玩家字典
        static Dictionary<Player, Player> player_dictionary = new Dictionary<Player, Player>();


        public static void Main(string[] args)
        {
            //定义一个套接字用于监听客户端发来的消息，包含三个参数（IP4寻址协议，流式连接，Tcp协议）  
            socketwatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //服务端发送信息需要一个IP地址和端口号  
            IPAddress address = IPAddress.Parse("127.0.0.1");
            //将IP地址和端口号绑定到网络节点point上  
            IPEndPoint point = new IPEndPoint(address, 8098);
            //此端口专门用来监听的  

            //监听绑定的网络节点  
            socketwatch.Bind(point);

            //将套接字的监听队列长度限制为20  
            socketwatch.Listen(20);

            //负责监听客户端的线程:创建一个监听线程  
            Thread threadwatch = new Thread(watchconnecting);

            //将窗体线程设置为与后台同步，随着主线程结束而结束  
            threadwatch.IsBackground = true;

            //启动线程     
            threadwatch.Start();

            Console.WriteLine("开启监听。。。");
            Console.WriteLine("点击输入任意数据回车退出程序。。。");
            Console.ReadKey();
            Console.WriteLine("退出监听，并关闭程序。");
        }

        //监听客户端发来的请求
        static void watchconnecting()
        {
            /*Socket connection_1 = null;
            Socket connection_2 = null;*/

            Player player_1;
            Player player_2;

            //持续不断监听客户端发来的请求     
            while (true)
            {
                try
                {

                    player_1 = new Player();
                    player_2 = new Player();

                    /*connection_1 = socketwatch.Accept();
                    player_count++;
                    connection_2 = socketwatch.Accept();
                    player_count++;
                    player_dic.Add(connection_1, connection_2);
                    player_dic.Add(connection_2, connection_1);*/

                    player_1.socket = socketwatch.Accept();
                    player_1.firstHand = true; // 先手
                    player_1.BattleState = 0;//正常
                    player_1.start = 0;



                    player_2.socket = socketwatch.Accept();
                    player_2.firstHand = false;
                    player_2.BattleState = 0;
                    player_2.start = 0;


                    player_dictionary.Add(player_1, player_2);
                    player_dictionary.Add(player_2, player_1);
                }
                catch (Exception ex)
                {
                    //提示套接字监听异常     
                    Console.WriteLine(ex.Message);
                    break;
                }

                //获取客户端的IP和端口号  
                IPAddress clientIP_1 = (player_1.socket.RemoteEndPoint as IPEndPoint).Address;
                IPAddress clientIP_2 = (player_2.socket.RemoteEndPoint as IPEndPoint).Address;

                int clientPort_1 = (player_1.socket.RemoteEndPoint as IPEndPoint).Port;
                int clientPort_2 = (player_2.socket.RemoteEndPoint as IPEndPoint).Port;

                //客户端网络结点号  
                string remoteEndPoint_1 = player_1.socket.RemoteEndPoint.ToString();
                string remoteEndPoint_2 = player_2.socket.RemoteEndPoint.ToString();

                //显示与客户端连接情况
                Console.WriteLine("成功与玩家1(地址： " + remoteEndPoint_1 + " )建立连接！\t\n");
                Console.WriteLine("成功与玩家2(地址： " + remoteEndPoint_2 + " )建立连接！\t\n");


                //IPEndPoint netpoint = new IPEndPoint(clientIP,clientPort); 
                IPEndPoint netpoint_1 = player_1.socket.RemoteEndPoint as IPEndPoint;
                IPEndPoint netpoint_2 = player_2.socket.RemoteEndPoint as IPEndPoint;

                //创建一个通信线程      
                ParameterizedThreadStart pts_1 = new ParameterizedThreadStart(recv);
                ParameterizedThreadStart pts_2 = new ParameterizedThreadStart(recv);

                Thread thread_1 = new Thread(pts_1);
                Thread thread_2 = new Thread(pts_2);

                //设置为后台线程，随着主线程退出而退出 
                thread_1.IsBackground = true;
                thread_2.IsBackground = true;

                //启动线程     
                thread_1.Start(player_1);
                thread_2.Start(player_2);

                string stringTmp1 = "1;";//匹配成功，可以开始部署了。
                string stringTmp2 = "1;";//匹配成功，可以开始部署了。
                stringTmp1 += remoteEndPoint_2;
                stringTmp2 += remoteEndPoint_1;
                byte[] byTmp1 = Encoding.UTF8.GetBytes(stringTmp1);
                byte[] byTmp2 = Encoding.UTF8.GetBytes(stringTmp2);
                player_1.socket.Send(byTmp1); // 成功匹配直接发送
                player_2.socket.Send(byTmp2);
            }
        }

        /// <summary>
        /// 接收客户端发来的信息，客户端套接字对象
        /// </summary>
        /// <param name="socketclientpara"></param>    
        static void recv(object socketclientpara)
        {
            // Socket socketServer = socketclientpara as Socket;
            Player currentPlayer = socketclientpara as Player;

            while (true)
            {
                //创建一个内存缓冲区，其大小为1024*1024字节  即1M     
                byte[] arrServerRecMsg = new byte[1024 * 1024];

                //将接收到的信息存入到内存缓冲区，并返回其字节数组的长度    
                try
                {
                    if (currentPlayer.BattleState == 1)//可以在这里释放资源
                    {

                        currentPlayer.socket.Close();
                        return;
                    }

                    int length = currentPlayer.socket.Receive(arrServerRecMsg);

                    //将机器接受到的字节数组转换为人可以读懂的字符串     
                    string strSRecMsg = Encoding.UTF8.GetString(arrServerRecMsg, 0, length);

                    Console.WriteLine("客户端:" + currentPlayer.socket.RemoteEndPoint + ",time:" + GetCurrentTime() + "\r\n" + strSRecMsg + "\r\n");

                    string strType;
                    string strMessage;
                    strType = MessageType(strSRecMsg); // MessageType函数
                    strMessage = GetMessage(strSRecMsg);

                    Console.WriteLine("strType:" + strType + "\r\n" + "message:" + strMessage);

                    if (strType.CompareTo("2") == 0)
                    {
                        currentPlayer.start++;
                        player_dictionary[currentPlayer].start++;
                        if (currentPlayer.start == 2)
                        {
                            string stringTmp3 = "3;";
                            byte[] byTmp3 = Encoding.UTF8.GetBytes(stringTmp3);
                            currentPlayer.socket.Send(byTmp3); // 给玩家0发送
                            player_dictionary[currentPlayer].socket.Send(byTmp3); // 给玩家1发送
                            continue;
                        }
                        continue;
                    }

                    if (strType.CompareTo("3") == 0)
                    {
                        /*string strTmp = "4;";
                        strTmp += strMessage;//棋盘信息
                        byte[] byTmp1 = Encoding.UTF8.GetBytes(strTmp);
                        stage++;
                        player_dic[socketServer].Send(byTmp1);
                        Console.WriteLine("state:" + stage + "\r\n");
                        if (stage == 2)
                        {
                            string strTmp4 = "5;";//发送进攻命令
                            byte[] byTmp14 = Encoding.UTF8.GetBytes(strTmp4);
                            socketServer.Send(byTmp14);
                        }*/
                        stage++;
                        string strTmp;
                        if (currentPlayer.firstHand) // 先手
                        {
                            strTmp = "4;";
                        }
                        else
                        {
                            strTmp = "41;";
                        }

                        strTmp += strMessage;//棋盘信息
                        byte[] byTmp1 = Encoding.UTF8.GetBytes(strTmp);

                        player_dictionary[currentPlayer].socket.Send(byTmp1);


                        //if (currentPlayer.firstHand) // 先手
                        //{
                        //    string strTmp = "4;";
                        //    strTmp += strMessage;//棋盘信息
                        //    byte[] byTmp1 = Encoding.UTF8.GetBytes(strTmp);

                        //    player_dictionary[currentPlayer].socket.Send(byTmp1);
                        //}
                        //else
                        //{
                        //    string strTmp = "41;";
                        //    strTmp += strMessage;//棋盘信息
                        //    byte[] byTmp1 = Encoding.UTF8.GetBytes(strTmp);
                        //    currentPlayer.socket.Send(byTmp1);
                        //}
                        Console.WriteLine("state:" + stage + "\r\n");
                    }

                    if (strType.CompareTo("4") == 0)
                    {
                        string strTmp4 = "6;";
                        strTmp4 = strTmp4 + strMessage + '\0';

                        byte[] byTmp14 = Encoding.UTF8.GetBytes(strTmp4);

                        Console.WriteLine("attack:" + strTmp4 + "\r\n");

                        player_dictionary[currentPlayer].socket.Send(byTmp14);

                        //if (currentPlayer.firstHand)
                        //{
                        //    player_dictionary[currentPlayer].socket.Send(byTmp14);
                        //}
                        //else
                        //{
                        //    currentPlayer.socket.Send(byTmp14);
                        //}
                    }
                    if (strType.CompareTo("5") == 0)
                    {
                        string strTmp4 = "7;";
                        byte[] byTmp14 = Encoding.UTF8.GetBytes(strTmp4);
                        player_dictionary[currentPlayer].socket.Send(byTmp14);

                        currentPlayer.BattleState = 1;
                        player_dictionary[currentPlayer].BattleState = 1;

                    }
                }
                catch (Exception ex)
                {
                    //提示套接字监听异常  

                    string strTmp4 = "7;";
                    byte[] byTmp14 = Encoding.UTF8.GetBytes(strTmp4);
                    player_dictionary[currentPlayer].socket.Send(byTmp14);

                    currentPlayer.BattleState = 1;
                    player_dictionary[currentPlayer].BattleState = 1;
                    Console.WriteLine("客户端" + currentPlayer.socket.RemoteEndPoint + "已经中断连接" + "\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n");
                    //关闭之前accept出来的和客户端进行通信的套接字 
                    currentPlayer.socket.Close();
                    break;
                }
            }
        }

        ///      
        /// 获取当前系统时间的方法    
        /// 当前时间     
        static DateTime GetCurrentTime()
        {
            DateTime currentTime = new DateTime();
            currentTime = DateTime.Now;
            return currentTime;
        }


        static string MessageType(string msg)
        {
            string strTmp = new string(msg);
            for (int i = 0; i < strTmp.Length; i++)
            {
                if (strTmp[i] == ';')
                {
                    strTmp = strTmp.Remove(i);
                    break;
                }
            }
            return strTmp;
        }
        static string GetMessage(string msg)
        {
            int i = 0;
            for (i = 0; i < msg.Length; i++)
            {
                if (msg[i] == ';')
                {
                    i++;
                    break;
                }
            }

            char[] cTmp = new char[1000];
            int count = 0;
            for (; i < msg.Length; i++)
            {
                cTmp[count] = msg[i];
                count++;
            }
            cTmp[count] = '\0';
            string strTmp = new string(cTmp);
            return strTmp;
        }
    }
}