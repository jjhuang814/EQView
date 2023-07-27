/*********************************************************************
 *
 *                    Class of EQViewPLC_Sub
 *
 *********************************************************************
 * FileName:        EQViewPLC_Sub
 * Processor:       PC-Based
 * Complier:        Visual Studio 2013 for C#
 * Company:         HIWIN Corporation
 * Author:          Jhong-Jie Huang
 * Description:     EQView for PLC(FX3U-ENET-ADP)
 *********************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//add using System~
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections;

namespace EQViewPLC_ApplicationModule
{
    class EQViewPLC_Sub
    {
        /*Function return 0 is ok, Function return 1 is error*/
        private TcpClient clientSocket = null;
        private IPEndPoint endPointIP = null;
        private NetworkStream clientNetworkStream = null;
        private int _SocketConnectFlag = 1;

        //建構函式
        public EQViewPLC_Sub() { }

        //連線斷線狀態 , 0 is connect, 1 is disconnect
        public int SocketConnectFlag { get { return _SocketConnectFlag; } }

        //連線
        public int TcpOpen(string IP, string Port, double timeOut)
        {
            DateTime t = DateTime.Now;
            try
            {
                int iRet;
                clientSocket = new TcpClient();
                endPointIP = new IPEndPoint(IPAddress.Parse(IP), Int32.Parse(Port));

                //Connect
                if (!clientSocket.Connected)
                {
                    //Old version
                    //clientSocket.Connect(endPointIP);

                    //Connect for clientSocket
                    IAsyncResult AsyncResult = clientSocket.BeginConnect(IP, Convert.ToInt32(Port), null, null);
                    while (!AsyncResult.IsCompleted)
                    {
                        if (DateTime.Now > t.AddSeconds(timeOut))
                            throw new SocketException();
                        Thread.Sleep(100);
                    }
                    clientSocket.EndConnect(AsyncResult);

                    //若連線成功時，才會執行以下程式
                    clientNetworkStream = clientSocket.GetStream();

                    //Return value(connect)
                    _SocketConnectFlag = 0;
                    iRet = 0;
                }
                // Disconnect
                else
                {
                    //Return value
                    _SocketConnectFlag = 1;
                    iRet = 1;
                }
                return iRet;
            }
            catch (ArgumentException)
            {
                //Return value
                _SocketConnectFlag = 1;
                int Msg = 1;
                MessageBox.Show("EQView(PLC) : IP位置輸入字串格式不正確!!", "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return Msg;
            }
            catch (SocketException)
            {
                //Return value
                _SocketConnectFlag = 1;
                int Msg = 1;
                MessageBox.Show("EQView(PLC) : 連線建立失敗!!", "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return Msg;
            }
            catch (FormatException ex)
            {
                string str ="";
                //Return value
                _SocketConnectFlag = 1;
                int Msg = 1;
                if (ex.Message == "輸入字串格式不正確。")
                {
                    str = "連接埠";
                }
                MessageBox.Show("EQView(PLC) : " + str + ex.Message, "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return Msg;
            }
            //catch (ArgumentOutOfRangeException)
            //{
            //    //Return value
            //    _SocketConnectFlag = 1;
            //    int Msg = 1;
            //    MessageBox.Show("EQView(PLC) : 連接埠號碼無效!!", "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            //    return Msg;
            //}
        }

        //斷線
        public int TcpClose()
        {
            int iRet;

            //Connect
            if (clientSocket.Connected)
            {
                //NetworkStream close
                clientSocket.GetStream().Close();
                clientNetworkStream.Close();
                clientNetworkStream = null;

                //Client close
                clientSocket.Client.Close();
                clientSocket.Close();
                clientSocket = null;

                //Return value(disconnect)
                _SocketConnectFlag = 1;
                iRet = 0;
            }
            //Disconnect
            else
            {
                //Return value
                _SocketConnectFlag = 1;
                iRet = 1;
            }
            return iRet;
        }

        //封包傳送
        public void TcpSend(string SendCommand)
        {
            try
            {
                //Uses the GetStream public method to return the NetworkStream.
                clientNetworkStream = clientSocket.GetStream();
                //Set the read timeout to 100 millseconds.
                clientNetworkStream.WriteTimeout = 100;
                if (clientSocket.Connected && clientNetworkStream.CanWrite)
                {
                    //Translate data bytes to a ASCII string.
                    Byte[] StreamWriter = Encoding.ASCII.GetBytes(SendCommand);
                    //Send
                    clientNetworkStream.Write(StreamWriter, 0, StreamWriter.Length);
                    //Clear
                    clientNetworkStream.Flush();
                }
            }
            catch (NullReferenceException)
            {
                //Pass
                //ShowSendTryCatch("EQView(PLC) : 已斷線!!，請重新連線s~" + "NullReferenceException");
            }
            catch (IOException)
            {
                SocketError(); //2018/06/27新增
                ShowSendTryCatch("EQView(PLC) : 已斷線!!，請重新連線s~" + "IOException");
            }
            catch (SocketException)
            {
                ShowSendTryCatch("EQView(PLC) : 已斷線!!，請重新連線s~" + "SocketException");
            }
            catch (ObjectDisposedException)
            {
                ShowSendTryCatch("EQView(PLC) : 已斷線!!，請重新連線s~" + "ObjectDisposedException");
            }
            catch (ArgumentNullException)
            {
                ShowSendTryCatch("EQView(PLC) : 已斷線!!，請重新連線s~" + "ArgumentNullException");
            }
            catch (ArgumentOutOfRangeException)
            {
                ShowSendTryCatch("EQView(PLC) : 已斷線!!，請重新連線s~" + "ArgumentOutOfRangeException");
            }
            catch (InvalidOperationException)
            {
                SocketError(); //2018/06/27新增
                ShowSendTryCatch("EQView(PLC) : 已斷線!!，請重新連線s~" + "InvalidOperationException");
            }
        }

        //封包接收
        public void TcpRecv(ref string RecvResponse)
        {
            try
            {
                //Clear
                RecvResponse = "";
                //Uses the GetStream public method to return the NetworkStream.
                clientNetworkStream = clientSocket.GetStream();
                //Set the read timeout to 250 millseconds.
                clientNetworkStream.ReadTimeout = 250;
                if (clientNetworkStream.CanRead)
                {
                    //Reads NetworkStream into a byte buffer.
                    byte[] MyReadBuffer = new byte[1024];
                    //Recv，Translate data bytes to a ASCII string.
                    int StreamReader = clientNetworkStream.Read(MyReadBuffer, 0, MyReadBuffer.Length);
                    RecvResponse = Encoding.ASCII.GetString(MyReadBuffer, 0, StreamReader);
                    //Clear
                    clientNetworkStream.Flush();
                }
                else
                {
                    RecvResponse = "";
                }
            }
            catch (NullReferenceException)
            {
                ShowRecvTryCatch("EQView(PLC) : 已斷線!!，請重新連線r~" + "NullReferenceException", ref RecvResponse);
            }
            //斷線後傳送封包, 接收時錯誤例外
            catch (IOException)
            {
                SocketError();
                ShowRecvTryCatch("EQView(PLC) : 已斷線!!，請重新連線r~" + "IOException", ref RecvResponse);
            }
            catch (SocketException)
            {
                ShowRecvTryCatch("EQView(PLC) : 已斷線!!，請重新連線r~" + "SocketException", ref RecvResponse);
            }
            catch (ObjectDisposedException)
            {
                ShowRecvTryCatch("EQView(PLC) : 已斷線!!，請重新連線r~" + "ObjectDisposedException", ref RecvResponse);
            }
            catch (ArgumentNullException)
            {
                ShowRecvTryCatch("EQView(PLC) : 已斷線!!，請重新連線r~" + "ArgumentNullException", ref RecvResponse);
            }
            catch (ArgumentOutOfRangeException)
            {
                ShowRecvTryCatch("EQView(PLC) : 已斷線!!，請重新連線r~" + "ArgumentOutOfRangeException", ref RecvResponse);
            }
            catch (InvalidOperationException)
            {
                ShowRecvTryCatch("EQView(PLC) : 已斷線!!，請重新連線r~" + "InvalidOperationException", ref RecvResponse);
            }
        }

        //Socket Error
        private void SocketError()
        {
            //NetworkStream close
            try
            {
                clientSocket.GetStream().Close();
                clientNetworkStream.Close();
            }
            //Pass
            catch (NullReferenceException) { }
            catch (InvalidOperationException) { }

            //Client close
            try
            {
                clientSocket.Client.Close();
                clientSocket.Close();
            }
            //Pass
            catch (NullReferenceException) { }
            catch (InvalidOperationException) { }
            clientSocket = null;

            //Return value
            _SocketConnectFlag = 1;
        }

        /**************************************************************************************************************************/
        //連續讀出(位)_XYMS
        public int ReadDeviceBlock_Bit(string DeviceCode, int dcInit, int dcSize, ref string Command_temp, ref string Response_temp, ref int[] OutData)
        {
            //Package init
            Command_temp = "";
            Response_temp = "";

            string s_dcInit;
            string s_dcSize;
            int int_dcInit;

            //Bulid CommandArrayList
            ArrayList CommandArray = new ArrayList();
            CommandArray.Add("00"); //連續讀出(位單位)_(2)
            CommandArray.Add("FF"); //PC號_(2)
            CommandArray.Add("0001"); //響應時間_(4)

            switch (DeviceCode)
            {
                case "X":
                    CommandArray.Add("5820"); //X暫存器
                    //int_dcInit
                    int_dcInit = Convert.ToInt32(dcInit.ToString(), 8); //[數值]八進制轉十進制
                    //dcInit
                    s_dcInit = Convert.ToString(int_dcInit, 16); //[字串]10進位轉成16進位
                    s_dcInit = s_dcInit.PadLeft(8, '0'); //左方的不足位元補0
                    CommandArray.Add(s_dcInit); //初始值_(8)
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    break;

                case "Y":
                    CommandArray.Add("5920"); //Y暫存器
                    //int_dcInit
                    int_dcInit = Convert.ToInt32(dcInit.ToString(), 8); //八進制轉十進制
                    //dcInit
                    s_dcInit = Convert.ToString(int_dcInit, 16); //[字串]10進位轉成16進位
                    s_dcInit = s_dcInit.PadLeft(8, '0'); //左方的不足位元補0
                    CommandArray.Add(s_dcInit); //初始值_(8)
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    break;

                case "M":
                    CommandArray.Add("4D20"); //M暫存器_(4)    
                    //dcInit
                    s_dcInit = Convert.ToString(dcInit, 16); //[字串]10進位轉成16進位
                    s_dcInit = s_dcInit.PadLeft(8, '0');
                    CommandArray.Add(s_dcInit);
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    break;

                case "S":
                    CommandArray.Add("5320"); //S暫存器_(4)
                    //dcInit
                    s_dcInit = Convert.ToString(dcInit, 16);
                    s_dcInit = s_dcInit.PadLeft(8, '0');
                    CommandArray.Add(s_dcInit);
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    break;
            }

            //Bulid string
            Command_temp = string.Join(null, (string[])CommandArray.ToArray(typeof(string)));
            //Send_command string          
            TcpSend(Command_temp.ToUpper());
            //Recv_response string
            if (clientSocket != null && clientNetworkStream != null)
            {
                if (clientSocket.Connected && clientNetworkStream.CanRead)
                {
                    TcpRecv(ref Response_temp);
                }

                if (Response_temp != "")
                {
                    //若元件個數為奇數時，需將最後一位的空數據"0"刪除
                    if (dcSize % 2 != 0)
                    {
                        Response_temp = Response_temp.Substring(0, Response_temp.Length - 1);
                    }

                    //響應正確
                    if (Response_temp.Substring(0, 4) == "8000")
                    {
                        string s_data = Response_temp.Substring(4, Response_temp.Length - 4); //取8000後(第四位)的data value
                        OutData = new int[s_data.Length];
                        for (int i = 0; i < OutData.Length; i++)
                        {
                            int data = Convert.ToInt32(s_data.Substring(i, 1));
                            OutData[i] = data; //將讀取到的值存至陣列
                        }
                        return 0;
                    }
                    else { return 1; }
                }
            }

            //if不成立
            return 1;
        }

        //連續寫入(位)_YMS
        public int WriteDeviceBlock_Bit(string DeviceCode, int dcInit, int dcSize, ref string Command_temp, ref string Response_temp, int[] InData)
        {
            //Package init
            Command_temp = "";
            Response_temp = "";

            string s_dcInit;
            string s_dcSize;
            int int_dcInit;
            string s_dataArray;

            //Bulid CommandArrayList
            ArrayList CommandArray = new ArrayList();
            CommandArray.Add("02"); //連續寫入(位單位)_(2)
            CommandArray.Add("FF"); //PC號_(2)
            CommandArray.Add("0001"); //響應時間_(4)

            //DataArray
            ArrayList DataArray = new ArrayList();
            for (int i = 0; i < InData.Length; i++)
            {
                string data = InData[i].ToString().Substring(0, 1);
                DataArray.Add(data);
            }
            s_dataArray = string.Join(null, (string[])DataArray.ToArray(typeof(string)));

            switch (DeviceCode)
            {
                case "Y":
                    CommandArray.Add("5920"); //Y暫存器
                    //int_dcInit
                    int_dcInit = Convert.ToInt32(dcInit.ToString(), 8); //八進制轉十進制
                    //dcInit
                    s_dcInit = Convert.ToString(int_dcInit, 16); //[字串]10進位轉成16進位
                    s_dcInit = s_dcInit.PadLeft(8, '0'); //左方的不足位元補0
                    CommandArray.Add(s_dcInit); //初始值_(8)
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    //Data
                    CommandArray.Add(s_dataArray);
                    break;

                case "M":
                    CommandArray.Add("4D20"); //M暫存器_(4)    
                    //dcInit
                    s_dcInit = Convert.ToString(dcInit, 16);
                    s_dcInit = s_dcInit.PadLeft(8, '0');
                    CommandArray.Add(s_dcInit);
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    //Data
                    CommandArray.Add(s_dataArray);
                    break;

                case "S":
                    CommandArray.Add("5320"); //S暫存器_(4)
                    //dcInit
                    s_dcInit = Convert.ToString(dcInit, 16);
                    s_dcInit = s_dcInit.PadLeft(8, '0');
                    CommandArray.Add(s_dcInit);
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    //Data
                    CommandArray.Add(s_dataArray);
                    break;
            }

            //寫入奇數點時，需在最後附加空數據"0"
            if (s_dataArray.Length % 2 != 0)
            {
                CommandArray.Add("0");
            }

            //Bulid string
            Command_temp = string.Join(null, (string[])CommandArray.ToArray(typeof(string)));
            //Send_command string          
            TcpSend(Command_temp.ToUpper());
            //Recv_response string
            if (clientSocket != null && clientNetworkStream != null)
            {
                if (clientSocket.Connected && clientNetworkStream.CanRead)
                {
                    TcpRecv(ref Response_temp);
                }

                if (Response_temp != "")
                {
                    //響應正確
                    if (Response_temp.Substring(0, 4) == "8200") { return 0; }
                    else { return 1; }
                }
            }

            //if不成立
            return 1;
        }

        //連續讀出(字)_D
        public int ReadDeviceBlock_Word(string DeviceCode, int dcInit, int dcSize, ref string Command_temp, ref string Response_temp, ref int[] OutData)
        {
            //Package init
            Command_temp = "";
            Response_temp = "";

            string s_dcInit;
            string s_dcSize;

            //Bulid CommandArrayList
            ArrayList CommandArray = new ArrayList();
            CommandArray.Add("01"); //連續讀出(字單位)_(2)
            CommandArray.Add("FF"); //PC號_(2)
            CommandArray.Add("0001"); //響應時間_(4)

            switch (DeviceCode)
            {
                case "D":
                    CommandArray.Add("4420"); //D暫存器_(4) 
                    //dcInit
                    s_dcInit = Convert.ToString(dcInit, 16); //[字串]10進位轉成16進位
                    s_dcInit = s_dcInit.PadLeft(8, '0');
                    CommandArray.Add(s_dcInit);
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    break;
            }

            //Bulid string
            Command_temp = string.Join(null, (string[])CommandArray.ToArray(typeof(string)));
            //Send_command string          
            TcpSend(Command_temp.ToUpper());
            //Recv_response string
            if (clientSocket != null && clientNetworkStream != null)
            {
                if (clientSocket.Connected && clientNetworkStream.CanRead)
                {
                    TcpRecv(ref Response_temp);
                }

                //響應正確
                if (Response_temp != "")
                {
                    if (Response_temp.Substring(0, 4) == "8100")
                    {
                        string s_data = Response_temp.Substring(4, Response_temp.Length - 4); //取8000後(第四位)的data value
                        OutData = new int[s_data.Length / 4];
                        //取值的個數，，四個數字一組
                        int dataNumber = s_data.Length / 4;
                        //取數據，4個數字為一組D並進行解碼
                        for (int i = 0; i < dataNumber; i++)
                        {
                            string str_data = s_data.Substring(i * 4, 4);
                            int int_data = Convert.ToInt32(str_data, 16); //[字串]16進位轉成10進位
                            OutData[i] = int_data;
                        }
                        return 0;
                    }
                    else { return 1; }
                }
            }

            //if不成立
            return 1;
        }

        //連續寫入(字)_D
        public int WriteDeviceBlock_Word(string DeviceCode, int dcInit, int dcSize, ref string Command_temp, ref string Response_temp, int[] InData)
        {
            //Package init
            Command_temp = "";
            Response_temp = "";

            string s_dcInit;
            string s_dcSize;
            string s_dataArray;

            //Bulid CommandArrayList
            ArrayList CommandArray = new ArrayList();
            CommandArray.Add("03"); //連續寫入(字單位)_(2)
            CommandArray.Add("FF"); //PC號_(2)
            CommandArray.Add("0001"); //響應時間_(4)

            //DataArray
            ArrayList DataArray = new ArrayList();
            for (int i = 0; i < InData.Length; i++)
            {
                int int_data = InData[i];
                string s_data = Convert.ToString(int_data, 16); //[字串]10進位轉成16進位
                s_data = s_data.PadLeft(4, '0');
                DataArray.Add(s_data);
            }
            s_dataArray = string.Join(null, (string[])DataArray.ToArray(typeof(string)));

            switch (DeviceCode)
            {
                case "D":
                    CommandArray.Add("4420"); //D暫存器_(4) 
                    //dcInit
                    s_dcInit = Convert.ToString(dcInit, 16); //[字串]10進位轉成16進位
                    s_dcInit = s_dcInit.PadLeft(8, '0');
                    CommandArray.Add(s_dcInit);
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    //Data
                    CommandArray.Add(s_dataArray);
                    break;
            }

            //Bulid string
            Command_temp = string.Join(null, (string[])CommandArray.ToArray(typeof(string)));
            //Send_command string          
            TcpSend(Command_temp.ToUpper());
            //Recv_response string
            if (clientSocket != null && clientNetworkStream != null)
            {
                if (clientSocket.Connected && clientNetworkStream.CanRead)
                {
                    TcpRecv(ref Response_temp);

                    if (Response_temp != "")
                    {
                        //響應正確
                        if (Response_temp.Substring(0, 4) == "8300") { return 0; }
                        else { return 1; }
                    }
                }
            }

            //if不成立
            return 1;
        }

        //單獨讀出_XYMS_D(合)
        public int GetDeviceSingle(string DeviceCode, int dcInit, ref string Command_temp, ref string Response_temp, ref int OutData)
        {
            //Package init
            Command_temp = "";
            Response_temp = "";

            int dcSize = 1;
            int int_dcInit;
            string s_dcInit;
            string s_dcSize;

            //Bulid CommandArrayList_1 for XYMS,D
            ArrayList CommandArray = new ArrayList();
            switch (DeviceCode)
            {
                case "X":
                case "Y":
                case "M":
                case "S":
                    CommandArray.Add("00"); //(位單位)_(2)
                    break;
                case "D":
                    CommandArray.Add("01"); //(字單位)_(2)
                    break;
            }
            CommandArray.Add("FF"); //PC號_(2)
            CommandArray.Add("0001"); //響應時間_(4)

            //Bulid CommandArrayList_2 for XYMS,D
            switch (DeviceCode)
            {
                case "X":
                    CommandArray.Add("5820"); //X暫存器_(4) 
                    break;
                case "Y":
                    CommandArray.Add("5920"); //Y暫存器_(4) 
                    break;
                case "M":
                    CommandArray.Add("4D20"); //M暫存器_(4) 
                    break;
                case "S":
                    CommandArray.Add("5320"); //M暫存器_(4) 
                    break;
                case "D":
                    CommandArray.Add("4420"); //D暫存器_(4) 
                    break;
            }

            //Bulid CommandArrayList_3 for dcInit...//dcSize...//End
            switch (DeviceCode)
            {
                case "X":
                case "Y":
                    //int_dcInit
                    int_dcInit = Convert.ToInt32(dcInit.ToString(), 8); //[數值]八進制轉十進制
                    //dcInit
                    s_dcInit = Convert.ToString(int_dcInit, 16); //[字串]10進位轉成16進位
                    s_dcInit = s_dcInit.PadLeft(8, '0'); //左方的不足位元補0
                    CommandArray.Add(s_dcInit); //初始值_(8)
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    break;

                case "M":
                case "S":
                case "D":
                    //dcInit
                    s_dcInit = Convert.ToString(dcInit, 16);
                    s_dcInit = s_dcInit.PadLeft(8, '0');
                    CommandArray.Add(s_dcInit);
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    break;
            }

            //Bulid string
            Command_temp = string.Join(null, (string[])CommandArray.ToArray(typeof(string)));
            //Send_command string          
            TcpSend(Command_temp.ToUpper());
            //Recv_response string
            if (clientSocket != null && clientNetworkStream != null)
            {
                if (clientSocket.Connected && clientNetworkStream.CanRead)
                {
                    TcpRecv(ref Response_temp);
                }

                if (Response_temp != "")
                {
                    //響應正確
                    switch (DeviceCode)
                    {
                        case "X":
                        case "Y":
                        case "M":
                        case "S":
                            //若元件個數為奇數時，需將最後一位的空數據"0"刪除
                            if (dcSize % 2 != 0)
                            {
                                Response_temp = Response_temp.Substring(0, Response_temp.Length - 1);
                            }

                            //響應正確
                            if (Response_temp.Substring(0, 4) == "8000")
                            {
                                string s_data = Response_temp.Substring(4, Response_temp.Length - 4); //取8000後(第四位)的data value
                                for (int i = 0; i < dcSize; i++)
                                {
                                    int data = Convert.ToInt32(s_data.Substring(i, 1));
                                    OutData = data; //將讀取到的值存至陣列
                                }
                                return 0;
                            }
                            else { return 1; }

                        case "D":
                            //響應正確
                            if (Response_temp.Substring(0, 4) == "8100")
                            {
                                string s_data = Response_temp.Substring(4, Response_temp.Length - 4); //取8000後(第四位)的data value
                                //取值的個數，，四個數字一組
                                int dataNumber = s_data.Length / 4;
                                //取數據，4個數字為一組D並進行解碼
                                for (int i = 0; i < dataNumber; i++)
                                {
                                    string str_data = s_data.Substring(i * 4, 4);
                                    int int_data = Convert.ToInt32(str_data, 16); //[字串]16進位轉成10進位
                                    OutData = int_data;
                                }
                                return 0;
                            }
                            else { return 1; }
                    }
                }
            }

            //錯誤
            return 1;
        }

        //單獨寫入_YMS_D(合)
        public int SetDeviceSingle(string DeviceCode, int dcInit, ref string Command_temp, ref string Response_temp, int InData)
        {
            //Package init
            Command_temp = "";
            Response_temp = "";

            int dcSize = 1;
            int int_dcInit;
            string s_dcInit;
            string s_dcSize;
            string s_dataArray;

            //Bulid CommandArrayList_1
            ArrayList CommandArray = new ArrayList();
            switch (DeviceCode)
            {
                case "Y":
                case "M":
                case "S":
                    CommandArray.Add("02"); //(位單位)_(2)
                    break;
                case "D":
                    CommandArray.Add("03"); //(字單位)_(2)
                    break;
            }
            CommandArray.Add("FF"); //PC號_(2)
            CommandArray.Add("0001"); //響應時間_(4)

            //Bulid CommandArrayList_2 for XYMS,D
            switch (DeviceCode)
            {
                case "Y":
                    CommandArray.Add("5920"); //Y暫存器_(4) 
                    break;
                case "M":
                    CommandArray.Add("4D20"); //M暫存器_(4) 
                    break;
                case "S":
                    CommandArray.Add("5320"); //M暫存器_(4) 
                    break;
                case "D":
                    CommandArray.Add("4420"); //D暫存器_(4) 
                    break;
            }

            //Bulid CommandArrayList_3 for dcInit...//dcSize...//End
            switch (DeviceCode)
            {
                case "X":
                case "Y":
                    //int_dcInit
                    int_dcInit = Convert.ToInt32(dcInit.ToString(), 8); //[數值]八進制轉十進制
                    //dcInit
                    s_dcInit = Convert.ToString(int_dcInit, 16); //[字串]10進位轉成16進位
                    s_dcInit = s_dcInit.PadLeft(8, '0'); //左方的不足位元補0
                    CommandArray.Add(s_dcInit); //初始值_(8)
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    break;

                case "M":
                case "S":
                case "D":
                    //dcInit
                    s_dcInit = Convert.ToString(dcInit, 16);
                    s_dcInit = s_dcInit.PadLeft(8, '0');
                    CommandArray.Add(s_dcInit);
                    //dcSize
                    s_dcSize = Convert.ToString(dcSize, 16);
                    s_dcSize = s_dcSize.PadLeft(2, '0');
                    CommandArray.Add(s_dcSize);
                    //End
                    CommandArray.Add("00");
                    break;
            }

            //Bulid DataArrayList
            ArrayList DataArray = new ArrayList();
            switch (DeviceCode)
            {
                case "Y":
                case "M":
                case "S":
                    for (int i = 0; i < dcSize; i++)
                    {
                        string data = InData.ToString().Substring(0, 1);
                        DataArray.Add(data);
                    }
                    s_dataArray = string.Join(null, (string[])DataArray.ToArray(typeof(string)));
                    //Data
                    CommandArray.Add(s_dataArray);
                    //寫入奇數點時，需在最後附加空數據"0"
                    if (s_dataArray.Length % 2 != 0)
                    {
                        CommandArray.Add("0");
                    }
                    break;

                case "D":
                    for (int i = 0; i < dcSize; i++)
                    {
                        int int_data = InData;
                        string s_data = Convert.ToString(int_data, 16); //[字串]16進位轉成10進位
                        s_data = s_data.PadLeft(4, '0');
                        DataArray.Add(s_data);
                    }
                    s_dataArray = string.Join(null, (string[])DataArray.ToArray(typeof(string)));
                    //Data
                    CommandArray.Add(s_dataArray);
                    break;
            }

            //Bulid string
            Command_temp = string.Join(null, (string[])CommandArray.ToArray(typeof(string)));
            //Send_command string          
            TcpSend(Command_temp.ToUpper());
            //Recv_response string
            if (clientSocket != null && clientNetworkStream != null)
            {
                if (clientSocket.Connected && clientNetworkStream.CanRead)
                {
                    TcpRecv(ref Response_temp);

                    if (Response_temp != null)
                    {
                        //響應正確
                        switch (DeviceCode)
                        {
                            case "Y":
                            case "M":
                            case "S":
                                if (Response_temp.Substring(0, 4) == "8200") { return 0; }
                                else { return 1; }

                            case "D":
                                if (Response_temp.Substring(0, 4) == "8300") { return 0; }
                                else { return 1; }
                        }
                    }
                }
            }

            //錯誤
            return 1;
        }

        //遠端 RUN/STOP
        public int SetPlcStatus(string RemoteCommand, ref string Command_temp, ref string Response_temp, ref int PlcStatusValue)
        {
            //Package init
            Command_temp = "";
            Response_temp = "";

            //Bulid CommandArrayList
            ArrayList CommandArray = new ArrayList();
            switch (RemoteCommand)
            {
                case "RUN":
                    CommandArray.Add("13"); //RemoteRun_(2)
                    break;

                case "STOP":
                    CommandArray.Add("14"); //RemoteStop_(2)
                    break;
            }

            CommandArray.Add("FF"); //PC號_(2)
            CommandArray.Add("0001"); //響應時間_(4)

            //Bulid string
            Command_temp = string.Join(null, (string[])CommandArray.ToArray(typeof(string)));
            //Send_command string          
            TcpSend(Command_temp.ToUpper());
            //Recv_response string
            if (clientSocket != null && clientNetworkStream != null)
            {
                if (clientSocket.Connected && clientNetworkStream.CanRead)
                {
                    TcpRecv(ref Response_temp);
                }

                if (Response_temp != "")
                {
                    //響應正確 for RemoteRun
                    if (Response_temp == "9300")
                    {
                        PlcStatusValue = 0;
                        return 0;
                    }
                    //響應正確 for RemoteStop
                    else if (Response_temp == "9400")
                    {
                        PlcStatusValue = 1;
                        return 0;
                    }
                    else
                    {
                        PlcStatusValue = 9999;
                        return 1;
                    }
                }
            }

            //if不成立
            return 1;
        }

        //讀取PLC型號
        public int GetPlcType(ref string Command_temp, ref string Response_temp, ref string PlcType)
        {
            //Package init
            Command_temp = "";
            Response_temp = "";

            //Bulid CommandArrayList
            ArrayList CommandArray = new ArrayList();
            CommandArray.Add("15"); //CpuType
            CommandArray.Add("FF"); //PC號_(2)
            CommandArray.Add("0001"); //響應時間_(4)

            //Bulid string
            Command_temp = string.Join(null, (string[])CommandArray.ToArray(typeof(string)));
            //Send_command string          
            TcpSend(Command_temp.ToUpper());
            //Recv_response string
            if (clientSocket != null && clientNetworkStream != null)
            {
                if (clientSocket.Connected && clientNetworkStream.CanRead)
                {
                    TcpRecv(ref Response_temp);
                }

                if (Response_temp != null)
                {
                    //響應正確
                    if (Response_temp == "9500F300")
                    {
                        PlcType = "FX3U";
                        return 0;
                    }
                    else
                    {
                        PlcType = "N/A";
                        return 1;
                    }
                }
            }

            //if不成立
            return 1;
        }

        //讀取指定數據佔存器>轉換ASCII字串
        public string EQViewPLC_BarcodeASCII(int[] data)
        {
            string s_temp = "";
            int dataNumber = data.Length;
            for (int i = 0; i < dataNumber; i++)
            {
                //10進位轉成16進位並補成4個數
                int int_data = data[i];
                string s_data = Convert.ToString(int_data, 16);
                s_data = s_data.PadLeft(4, '0');
                //分割字串為FF,FF兩個再(16進位轉10進位、再轉ASCII)
                string s_data1_16 = s_data.Substring(0 * 2, 2);
                string s_data2_16 = s_data.Substring(1 * 2, 2);
                int s_data1_10 = Convert.ToInt32(s_data1_16, 16);
                int s_data2_10 = Convert.ToInt32(s_data2_16, 16);
                //ASCII
                string s_data1_ASCII = Convert.ToString((char)s_data1_10);
                string s_data2_ASCII = Convert.ToString((char)s_data2_10);
                //Sum
                s_temp += s_data1_ASCII + s_data2_ASCII;
            }
            return s_temp;
        }

        //**************************************************************************************************************************
        private void ShowSendTryCatch(string Msg)
        {
            MessageBox.Show(Msg, "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }

        private void ShowRecvTryCatch(string Msg, ref string RecvMsg)
        {
            RecvMsg = "";
            MessageBox.Show(Msg, "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }

        //**************************************************************************************************************************
    }
}
