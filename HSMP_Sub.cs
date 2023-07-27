/*********************************************************************
 *
 *                    Class of HSMP_Sub
 *
 *********************************************************************
 * FileName:        HSMP_Sub
 * Processor:       PC-Based
 * Complier:        Visual Studio 2013 for C#
 * Company:         HIWIN Corporation
 * Author:          Jhong-Jie Huang
 * Description:     EQVIEW for HMSP(CNC)
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

namespace HSMP_ApplicationModule
{
    class HSMP_Sub
    {
        /*Function return 0 is ok, Function return 1 is error*/
        private TcpClient clientSocket = null;
        private IPEndPoint endPointIP = null;
        private NetworkStream clientNetworkStream = null;

        //HSMP1、傳送訊息(EQView>HSMP)
        private int _SocketConnectFlag = 1;

        //HSMP2、(EQView>HSMP)
        private string _LotNumber = "Unknown"; //批號
        private string _PartNumber = "Unknown"; //料號 
        private string _EqMachineID = "Unknown"; //機台編號

        //HSMP2、(HSMP>EQView)
        /*機台狀態(0,1,2,3,4,5)to(Idle-閒置,Run-運行,Down-停機故障,PM-保養,Offline-離線關機)*/
        private string _sEqStatus = "Unknown"; //機台狀態(短)
        private string _EqStatus = "Unknown"; //機台狀態(長)
        private string _EqStartupTime = "Unknown"; //開機時間(yy,mm,dd hh,mm,ss)
        private string _EqProcessTime = "Unknown"; //加工時間(hh,mm,ss)
        private string _EqTotalProcessTime = "Unknown"; //總加工時間(hh,mm,ss)
        private string _EqActivation = "Unknown"; //稼動率(??%)

        //建構函式
        public HSMP_Sub() { }

        //[get]連線斷線狀態
        public int SocketConnectFlag { get { return _SocketConnectFlag; } }

        //[set/get]批號
        public string LotNumber
        {
            get { return _LotNumber; }
            set { _LotNumber = value; }
        }

        //[set/get]料號
        public string PartNumber
        {
            get { return _PartNumber; }
            set { _PartNumber = value; }
        }

        //[set/get]機台編號
        public string EqMachineID
        {
            get { return _EqMachineID; }
            set { _EqMachineID = value; }
        }

        //[get]CNC機台資訊
        public string sEqStatus { get { return _sEqStatus; } }
        public string EqStatus { get { return _EqStatus; } }
        public string EqStartupTime { get { return _EqStartupTime; } }
        public string EqProcessTime { get { return _EqProcessTime; } }
        public string EqTotalProcessTime { get { return _EqTotalProcessTime; } }
        public string EqActivation { get { return _EqActivation; } }

        //連線
        public int TcpOpen(string IP, string Port, double TimeOut)
        {
            DateTime t = DateTime.Now;
            try
            {
                int iRet;
                clientSocket = new TcpClient();
                endPointIP = new IPEndPoint(IPAddress.Parse(IP), Int32.Parse(Port));

                // Connect
                if (!clientSocket.Connected)
                {
                    //Old version
                    //clientSocket.Connect(endPointIP);

                    //Connect for clientSocket
                    IAsyncResult AsyncResult = clientSocket.BeginConnect(IP, Convert.ToInt32(Port), null, null);
                    while (!AsyncResult.IsCompleted)
                    {
                        if (DateTime.Now > t.AddSeconds(TimeOut))
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
                MessageBox.Show("EQView(HSMP) : IP位置輸入字串格式不正確!!", "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return Msg;
            }
            catch (SocketException)
            {
                //Return value
                _SocketConnectFlag = 1;
                int Msg = 1;
                MessageBox.Show("EQView(HSMP) : 連線建立失敗!!", "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return Msg;
            }
            catch (FormatException ex)
            {
                string str = "";
                //Return value
                _SocketConnectFlag = 1;
                int Msg = 1;
                if (ex.Message == "輸入字串格式不正確。")
                {
                    str = "連接埠";
                }
                MessageBox.Show("EQView(HSMP) : " + str + ex.Message, "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return Msg;
            }
            //catch (ArgumentOutOfRangeException)
            //{
            //    //Return value
            //    _SocketConnectFlag = 1;
            //    int Msg = 1;
            //    MessageBox.Show("EQView(HSMP) : 連接埠號碼無效!!", "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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

                //Return value(Disconnect)
                _SocketConnectFlag = 1;
                iRet = 0;
            }
            // Disconnect
            else
            {
                //Return value(Disconnect)
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
                    clientNetworkStream.Flush(); //排清資料流的資料                   
                }
            }
            catch (NullReferenceException)
            {
                //pass
                //ShowSendTryCatch("EQView(HSMP) : 已斷線!!，請重新連線s~" + "NullReferenceException");
            }
            catch (IOException)
            {
                SocketError();
                ShowSendTryCatch("EQView(HSMP) : 已斷線!!，請重新連線s~" + "IOException");
            }
            catch (SocketException)
            {
                ShowSendTryCatch("EQView(HSMP) : 已斷線!!，請重新連線s~" + "SocketException");
            }
            catch (ObjectDisposedException)
            {
                ShowSendTryCatch("EQView(HSMP) : 已斷線!!，請重新連線s~" + "ObjectDisposedException");
            }
            catch (ArgumentNullException)
            {
                ShowSendTryCatch("EQView(HSMP) : 已斷線!!，請重新連線s~" + "ArgumentNullException");
            }
            catch (ArgumentOutOfRangeException)
            {
                ShowSendTryCatch("EQView(HSMP) : 已斷線!!，請重新連線s~" + "ArgumentOutOfRangeException");
            }
            catch (InvalidOperationException)
            {
                ShowSendTryCatch("EQView(HSMP) : 已斷線!!，請重新連線s~" + "InvalidOperationException");
            }
        }

        //封包接收(HSMP)
        private void TcpRecv(ref string RecvResponse)
        {
            try
            {
                //Clear
                RecvResponse = "";
                //Uses the GetStream public method to return the NetworkStream.
                clientNetworkStream = clientSocket.GetStream();
                //Set the read timeout to 250 millseconds.
                //clientNetworkStream.ReadTimeout = 250;
                if (clientNetworkStream.CanRead)
                {
                    //Reads NetworkStream into a byte buffer.
                    byte[] MyReadBuffer = new byte[1024]; //空字串                            
                    //Recv，Translate data bytes to a ASCII string.
                    int StreamReader = clientNetworkStream.Read(MyReadBuffer, 0, MyReadBuffer.Length);
                    RecvResponse = Encoding.ASCII.GetString(MyReadBuffer, 0, StreamReader);
                    //Clear
                    clientNetworkStream.Flush(); //排清資料流的資料  
                }
                else
                {
                    RecvResponse = "";
                }
            }
            catch (NullReferenceException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "NullReferenceException", ref RecvResponse);
            }
            //斷線後傳送封包,接收時錯誤例外
            catch (IOException)
            {
                SocketError();
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "IOException", ref RecvResponse);
            }
            catch (SocketException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "SocketException", ref RecvResponse);
            }
            catch (ObjectDisposedException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "ObjectDisposedException", ref RecvResponse);
            }
            catch (ArgumentNullException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "ArgumentNullException", ref RecvResponse);
            }
            catch (ArgumentOutOfRangeException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "ArgumentOutOfRangeException", ref RecvResponse);
            }
            catch (InvalidOperationException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "InvalidOperationException", ref RecvResponse);
            }
        }

        //封包接收(HSMP2)
        private void TcpRecv2(ref string RecvResponse)
        {
            try
            {
                //Clear
                RecvResponse = "";
                //Uses the GetStream public method to return the NetworkStream.
                clientNetworkStream = clientSocket.GetStream();
                //Set the read timeout to 250 millseconds.
                //clientNetworkStream.ReadTimeout = 250;
                if (clientNetworkStream.CanRead)
                {
                    //Reads NetworkStream into a byte buffer.
                    byte[] MyReadBuffer = new byte[1024]; //空字串                            
                    //Recv，Translate data bytes to a ASCII string.
                    int StreamReader = clientNetworkStream.Read(MyReadBuffer, 0, MyReadBuffer.Length);
                    RecvResponse = Encoding.ASCII.GetString(MyReadBuffer, 0, StreamReader);
                    //Clear
                    clientNetworkStream.Flush(); //排清資料流的資料  
                }
                else
                {
                    RecvResponse = "";
                }
            }
            catch (NullReferenceException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "NullReferenceException", ref RecvResponse);
            }
            //斷線後傳送封包,接收時錯誤例外，只有它有SocketError();
            catch (IOException)
            {
                SocketError();
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "IOException", ref RecvResponse);
            }
            catch (SocketException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "SocketException", ref RecvResponse);
            }
            catch (ObjectDisposedException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "ObjectDisposedException", ref RecvResponse);
            }
            catch (ArgumentNullException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "ArgumentNullException", ref RecvResponse);
            }
            catch (ArgumentOutOfRangeException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "ArgumentOutOfRangeException", ref RecvResponse);
            }
            catch (InvalidOperationException)
            {
                ShowRecvTryCatch("EQView(HSMP) : 已斷線!!，請重新連線r~" + "InvalidOperationException", ref RecvResponse);
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
        //封包交握(EQView>HSMP)
        public int NcHandShake(string SendCommand, ref string Command_temp, ref string Response_temp)
        {
            //Package init
            Command_temp = "";
            Response_temp = "";

            //Bulid string
            Command_temp = SendCommand;
            //Send_command string
            TcpSend(Command_temp);
            //Recv_response string
            if (clientSocket != null && clientNetworkStream != null)
            {
                if (clientSocket.Connected && clientNetworkStream.CanRead)
                {
                    TcpRecv(ref Response_temp);
                    Response_temp = Response_temp.Trim();
                    if (Response_temp != "\0") { return 0; }
                    else { return 1; }
                }
            }
            else
            {
                Response_temp = "";
                return 1;
            }

            //if不成立
            return 1;
        }

        //訊息列印模組(主)
        public string PrintUploadPackMsg(string SendCommand, string RecvResponse)
        {
            //arySendCmd & aryRecvCmd
            string[] arySendCmd = SendCommand.Split(',');
            string[] aryRecvCmd = RecvResponse.Split(',');
            //Cmd
            string CmdSend1 = arySendCmd[0];
            string CmdRecv1 = aryRecvCmd[0];
            //Compare
            bool CmdMsg_compare;
            bool MachIDMsg_compare;

            try
            {
                //告知機台編號、更換加工程式 check
                if (aryRecvCmd.Length == 2 || aryRecvCmd.Length == 4)
                {
                    //MachineID
                    string MachIDSend2 = arySendCmd[1];
                    string MachIDRecv2 = aryRecvCmd[1];

                    //Cmd_compare
                    if ((CmdSend1 == "1001" && CmdRecv1 == "8001") || (CmdSend1 == "1002" && CmdRecv1 == "8002"))
                        CmdMsg_compare = true;
                    else { CmdMsg_compare = false; }

                    //MachID_compare
                    if (MachIDSend2 == MachIDRecv2)
                        MachIDMsg_compare = true;
                    else { MachIDMsg_compare = false; }
                }
                else { CmdMsg_compare = MachIDMsg_compare = false; }

                /**********************************************************************************************/
                //EQVIEW端判斷-HSMP連線逾時錯誤
                if (RecvResponse == "")
                {
                    string Msg = ShowMsg("錯誤訊息--" + "990001" + " HSMP連線逾時錯誤");
                    return Msg;
                }
                //EQVIEW端判斷-HSMP封包訊息長度錯誤
                else if (aryRecvCmd.Length != 2 && aryRecvCmd.Length != 4 && aryRecvCmd.Length != 1)
                {
                    string Msg = ShowMsg("錯誤訊息--" + "990002" + " HSMP格式封包錯誤");
                    return Msg;
                }
                //(主)Upload(告知機台編號、更換加工程式)
                else if ((aryRecvCmd.Length == 2 || aryRecvCmd.Length == 4) && CmdMsg_compare && MachIDMsg_compare)
                {
                    switch (arySendCmd.Length)
                    {
                        case 2:
                            return ShowUploadMachineID(arySendCmd);
                        case 4:
                            return ShowUploadNcProgram(arySendCmd);
                    }
                    return "";
                }
                //ErrorNcProgram(HSMP錯誤碼顯示)_接收HSMP響應錯誤訊息
                else if (aryRecvCmd.Length == 1)
                {
                    string ErrorCode = CmdRecv1;
                    string Msg = ShowErrorNcProgram(ErrorCode);
                    return Msg;
                }
                //EQVIEW端判斷-HSMP響應錯誤
                else if (CmdMsg_compare == false)
                {
                    string Msg = ShowMsg("錯誤訊息--" + "990003" + " HSMP響應錯誤");
                    return Msg;
                }
                //EQVIEW端判斷-HSMP機台編號錯誤
                else if (MachIDMsg_compare == false)
                {
                    string Msg = ShowMsg("錯誤訊息--" + "990004" + " HSMP機台編號錯誤");
                    return Msg;
                }
                //Unknown error(未知錯誤)
                else
                {
                    string Msg = ShowMsg("錯誤訊息--" + "??????" + " ????-Unknown error");
                    return Msg;
                }
            }
            //EQVIEW端判斷-HSMP連線逾時錯誤
            catch (ArgumentException)
            {
                string Msg = ShowMsg("990001" + " HSMP連線逾時錯誤");
                return Msg;
            }
        }

        //訊息列印模組(1)，上傳訊息
        private string ShowUploadMachineID(string[] aryString)
        {
            //機台編號
            string MachineID = aryString[1];
            string s_Msg = MachineID;
            //Output
            string UploadMsg = ShowMsg("成功訊息--機台編號上載完成" + "," + s_Msg);
            return UploadMsg;
        }

        private string ShowUploadNcProgram(string[] aryString)
        {
            //機台編號/料號/模具編號
            string MachineID = aryString[1];
            string PartNumber = aryString[2];
            string ModelNumber = aryString[3];
            string s_Msg = MachineID + "," + PartNumber + "," + ModelNumber;
            //Output
            string UploadMsg = ShowMsg("成功訊息--NC程式上載完成" + "," + s_Msg);
            return UploadMsg;
        }

        //訊息列印模組(2)，錯誤訊息
        private string ShowErrorNcProgram(string RecvResponse)
        {
            string ErrorMsg;
            switch (RecvResponse)
            {
                //命令響應錯誤
                case "991001":
                    ErrorMsg = ShowMsg("錯誤訊息--" + "991001" + " EQView-封包格式錯誤");
                    break;

                case "991002":
                    ErrorMsg = ShowMsg("錯誤訊息--" + "991002" + " EQView-命令錯誤");
                    break;

                case "991003":
                    ErrorMsg = ShowMsg("錯誤訊息--" + "991003" + " HSMP-CNC連線錯誤");
                    break;

                case "991004":
                    ErrorMsg = ShowMsg("錯誤訊息--" + "991004" + " HSMP-NC程式下載失敗");
                    break;

                //資料庫
                case "992001":
                    ErrorMsg = ShowMsg("錯誤訊息--" + "992001" + " HSMP-無產品類別");
                    break;

                case "992002":
                    ErrorMsg = ShowMsg("錯誤訊息--" + "992002" + " HSMP-無產品類別NC程式");
                    break;

                default:
                    ErrorMsg = ShowMsg("錯誤訊息--" + "??????" + " ????-Unknown error");
                    break;
            }
            return ErrorMsg;
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

        private string ShowMsg(string Message)
        {
            string Time = "(" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss") + ")";
            string Msg = Time + " " + Message;
            return Msg;
        }

        /*************************************************************************************************************************/
        /*封包交握(HSMP>EQView)*/
        //接收封包
        public int RecvHandShake(ref string Recv_temp)
        {
            //Package init
            Recv_temp = "";

            //Recv_response string
            if (clientSocket != null && clientNetworkStream != null)
            {
                if (clientSocket.Connected && clientNetworkStream.CanRead)
                {
                    TcpRecv(ref Recv_temp);
                    Recv_temp = Recv_temp.Trim();
                    if (Recv_temp != "\0") { return 0; }
                    else { return 1; }
                }
            }
            else
            {
                Recv_temp = "";
                return 1;
            }

            //if不成立
            return 1;
        }

        //傳送封包
        public int InfoHandShake(ref string Send_temp, ref string Recv_temp)
        {
            //Package init
            Send_temp = "";
            string pk = "";
            bool Cmd_compare = false;
            bool MachineID_compare = false;
            string[] aryRecvCmd = Recv_temp.Split(',');

            /*判斷HSMP封包格式*/
            //HSMP2連線逾時錯誤
            if (Send_temp == "" && Recv_temp == "")
            {
                pk = "980001";
                Send_temp = pk;
                return 1;
            }
            //HSMP2封包格式錯誤           
            else if (aryRecvCmd.Length != 3 && aryRecvCmd.Length != 5)
            {
                pk = "980002";
                TcpSend(pk);
                Send_temp = pk;
                return 1;
            }

            //info
            string RecvMsg1_Cmd = aryRecvCmd[0]; //機台命令
            string RecvMsg2_MachineID = aryRecvCmd[1]; //機台編號

            //Cmd_compare
            if (RecvMsg1_Cmd == "2001" || RecvMsg1_Cmd == "2002" || RecvMsg1_Cmd == "2003") { Cmd_compare = true; }
            //MachineID_compare
            if (RecvMsg2_MachineID == _EqMachineID) { MachineID_compare = true; }

            //判斷接收指令
            if (Cmd_compare == false)
            {
                //HSMP機台命令錯誤
                pk = "980003";
                TcpSend(pk);
                Send_temp = pk;
                return 1;
            }
            //判斷機台編號
            else if (MachineID_compare)
            {
                switch (RecvMsg1_Cmd)
                {
                    case "2001":
                        //Set parameter
                        SetEqStatus(aryRecvCmd);
                        //Send
                        pk = "9001," + _EqMachineID;
                        TcpSend(pk);
                        Send_temp = pk;
                        return 0;

                    case "2002":
                        //Set parameter
                        SetEqStartupTime(aryRecvCmd);
                        //Send
                        pk = "9002," + _EqMachineID;
                        TcpSend(pk);
                        Send_temp = pk;
                        return 0;

                    case "2003":
                        //Set parameter
                        SetEqProcessActivation(aryRecvCmd);
                        //Send
                        pk = "9003," + _EqMachineID;
                        TcpSend(pk);
                        Send_temp = pk;
                        return 0;
                }
            }
            //判斷HSMP機台編號錯誤
            else
            {
                pk = "980004";
                TcpSend(pk);
                Send_temp = pk;
                return 1;
            }

            //if不成立
            return 1;
        }

        //訊息列印模組(主)
        public string PrintInfoPackMsg(string Send_temp)
        {
            //aryRecvCmd
            string[] arySendResponse = Send_temp.Split(',');
            string SendMsg1 = arySendResponse[0];
            string Msg = ShowInfoPackMsg(SendMsg1);
            return Msg;
        }

        //訊息列印模組(從)
        public string ShowInfoPackMsg(string RecvMsg)
        {
            string Msg;
            switch (RecvMsg)
            {
                //錯誤
                case "980001":
                    Msg = ShowMsg("錯誤訊息--" + "980001" + " HSMP2連線逾時錯誤");
                    return Msg;
                case "980002":
                    Msg = ShowMsg("錯誤訊息--" + "980002" + " HSMP2封包格式錯誤");
                    return Msg;
                case "980003":
                    Msg = ShowMsg("錯誤訊息--" + "980003" + " HSMP2命令錯誤");
                    return Msg;
                case "980004":
                    Msg = ShowMsg("錯誤訊息--" + "980004" + " HSMP2機台編號錯誤");
                    return Msg;
                /**************************************************************/
                //成功
                case "9001":
                    Msg = ShowMsg("成功訊息--HSMP2機台狀態上載完成");
                    return Msg;

                case "9002":
                    Msg = ShowMsg("成功訊息--HSMP2開機時間上載完成");
                    return Msg;

                case "9003":
                    Msg = ShowMsg("成功訊息--HSMP2加工時間/總加工時間/稼動率上載完成");
                    return Msg;

                default:
                    Msg = ShowMsg("錯誤訊息--" + "Unknown" + " HSMP2未知訊息錯誤");
                    return Msg;
            }
        }

        //機台資訊set
        private void SetEqStatus(string[] aryRecvMsg)
        {
            //機台狀態:aryRecvMsg[2]
            string EqStatus = aryRecvMsg[2];
            switch (EqStatus)
            {
                case "0":
                    _sEqStatus = EqStatus;
                    _EqStatus = "Idle(閒置)";
                    break;

                case "1":
                    _sEqStatus = EqStatus;
                    _EqStatus = "Run(運行)";
                    break;

                case "2":
                    _sEqStatus = EqStatus;
                    _EqStatus = "Down(停機故障)";
                    break;

                case "3":
                    _sEqStatus = EqStatus;
                    _EqStatus = "PM(保養)";
                    break;

                case "4":
                    _sEqStatus = EqStatus;
                    _EqStatus = "Offline(離線關機)";
                    break;

                default:
                    _sEqStatus = "99";
                    _sEqStatus = "Unknown(未知狀態)";
                    break;
            }
        }

        private void SetEqStartupTime(string[] aryRecvMsg)
        {
            //開機時間
            _EqStartupTime = aryRecvMsg[2];
        }

        private void SetEqProcessActivation(string[] aryRecvMsg)
        {
            //加工時間、總加工時間、稼動率
            _EqProcessTime = aryRecvMsg[2];
            _EqTotalProcessTime = aryRecvMsg[3];
            _EqActivation = aryRecvMsg[4];
        }

    }
}
