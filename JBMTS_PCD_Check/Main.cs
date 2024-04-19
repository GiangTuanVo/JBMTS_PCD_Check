using CheckPCD_Jigbush.DataProvider;
using JBMTO_PCD_Check.Provider;
using JBMTS_PCD_Check.Model;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JBMTS_PCD_Check
{
    public partial class Main : Form
    {
        private string Paths = Path.Combine(Application.StartupPath, "Data");
        DataSet dataSet = new DataSet();
        DataTable Jig = new DataTable("Jig");
        DataTable Pin = new DataTable("Pin");
        private int quantityPO = 0;
        private string saveJig = "";
        private string savePin = "";
        private int quantityCheck = 0;
        private bool isPin = false;
        private bool isJig = false;
        private bool valid = false;

        public Main()
        {
            InitializeComponent();
            string filePath = AppDomain.CurrentDomain.BaseDirectory + "\\Data\\system.ini";
            INIFile ini = new INIFile(filePath);
            string portName = ini.Read("Parameter", "PortName");
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                cmbPortName.Items.Add(port);
            }
            if (cmbPortName.Items.Count > 0)
            {
                int index = 0;
                foreach (var item in cmbPortName.Items)
                {
                    if (item.ToString() == portName) cmbPortName.SelectedIndex = index;
                    ++index;
                }
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            EnableControl();
            FileHelper.FileToTBL(Paths, "Jig.csv", dataSet, Jig);
            FileHelper.FileToTBL(Paths, "Pin.csv", dataSet, Pin);
            if (cmbPortName.Text != "")
                btnConnect.PerformClick();
            if (comPort.IsOpen)
            {
                txtUserID.Invoke(new Action(() => { txtUserID.Select(); }));
            }
        }

        private void comPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(100);
            string text = comPort.ReadExisting();
            this.Invoke(new Action(() =>
            {
                lblWarning.Text = text.Trim();
            }));
            text = "";
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Dispose();
            Environment.Exit(0);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbPortName.Text != "")
                {
                    if (!comPort.IsOpen)
                    {
                        comPort.PortName = cmbPortName.Text;
                        comPort.Open();
                    }
                    if (comPort.IsOpen)
                    {
                        lblWarning.Text = $"{comPort.PortName} connected !";
                        lblWarning.ForeColor = Color.Blue;
                        btnConnect.Enabled = false;
                        btnDisconnect.Enabled = true;
                        cmbPortName.Enabled = false;
                        tblUser.Enabled = true;
                    }
                    else
                    {
                        lblWarning.Text = $"{comPort.PortName} connect fail !";
                        lblWarning.ForeColor = Color.Red;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (comPort.IsOpen)
            {
                comPort.Close();
                btnDisconnect.Enabled = false;
                btnConnect.Enabled = true;
                cmbPortName.Enabled = true;
            }
        }

        private void txtUserID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
                CheckUserID(txtUserID.Text);
        }
        private void CheckUserID(string userID)
        {
            if (userID.Length >= 4)
            {
                try
                {
                    DataTable dt = SQLProvider.Instance.ExecuteQuery("SELECT v.EmpID,v.Name FROM EmployeeServer v WHERE EmpID = @empID", new object[] { userID });
                    if (dt.Rows.Count > 0)
                    {
                        valid = true;
                        txtEmpID.Text = dt.Rows[0][0].ToString();
                        txtUserName.Text = dt.Rows[0][1].ToString();
                    }
                    else
                    {
                        valid = false;
                        txtEmpID.Text = "";
                        txtUserName.Text = "";
                    }
                }
                catch (Exception)
                {
                    valid = false;
                    txtEmpID.Text = "";
                    txtUserName.Text = "";

                }

                if (valid)
                {
                    txtUserID.Text = "";
                    lblIdStatus.Text = "User hợp lệ";
                    lblIdStatus.ForeColor = Color.Blue;
                    //lock CheckUser
                    //LockControls(true);
                    txtBarcode.Enabled = true;
                    txtBarcode.Focus();
                }
                else
                {
                    txtUserID.Text = "";
                    lblIdStatus.Text = "User không hợp lệ";
                    lblIdStatus.ForeColor = Color.Red;
                }

            }
            else
            {
                lblIdStatus.Text = "User không hợp lệ";
                lblIdStatus.ForeColor = Color.Red;
                //lock Barcode scanning and seri scanning
                LockControls(false);
                txtUserID.Clear();
                txtUserID.Focus();
            }
        }

        private void LockControls(bool v)
        {
            LayoutPanelQR.Enabled = v;
            LayoutPanelCheck.Enabled = v;
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            CheckUserID(txtUserID.Text);
        }

        private async void txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Tab)
            {
                lblStatus.Text = "";
                lblStatus.ForeColor = ColorTranslator.FromHtml("#FFFAFFC8");
                lblPCD.Text = "";
                lblQRStatus.Text = "Checking...";
                lblQRStatus.ForeColor = Color.Blue;
            }
            if (e.KeyCode == Keys.Return)
            {
                //
                lblStatus.BackColor = Color.PeachPuff;
                //
                string text = txtBarcode.Text;
                if (text.Split('@').Length > 2)
                {
                    lblQRStatus.Text = "";
                    string[] arrStr = text.Split('@');
                    string orderNo = arrStr[0];
                    string poNo = arrStr[1];
                    string partName = arrStr[2];
                    string Type = Regex.Match(partName.Split('-')[0], @"([a-zA-Z]+)").ToString();
                    int ExternalD = int.Parse(Regex.Match(partName.Split('-')[0], @"-?\d+(.\d+)?").Value);
                    int Length = int.Parse(partName.Split('-')[1]);
                    //
                    quantityPO = Convert.ToInt32(Math.Round(double.Parse(FileHelper.GetQty(poNo)), 2).ToString());
                    lblQuantityPO.Text = quantityPO.ToString();
                    quantityCheck = 0;
                    lblQuantityCheck.Text = quantityCheck.ToString();
                    //Jig check
                    DataView dvJig = new DataView(dataSet.Tables["Jig"]);
                    string str1 = partName;
                    int num1 = checked(str1.Length - 1);
                    int num2 = 0;
                    string jig = "";
                    string pin = "";
                    while (num2 <= num1)
                    {
                        try
                        {
                            dvJig.RowFilter = string.Format("Type LIKE '{0}'", str1);
                            DataTable dt = dvJig.ToTable();
                            if (dt.Rows.Count > 0)
                            {
                                jig = dt.Rows[0]["Jig"].ToString();
                                lblPCD.Text = jig;
                                lblJig.Text = $"{Type}-{ExternalD}";
                                lblJig.BackColor = Color.Peru;
                                if (comPort.IsOpen) comPort.Write(jig);
                                break;
                            }
                            str1 = str1.Remove(checked(str1.Length - 1), 1);
                            --num1;
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                    //Pin check
                    DataTable dtPin = dataSet.Tables["Pin"];
                    EnumerableRowCollection<DataRow> query = from dt in dtPin.AsEnumerable()
                                                             where double.Parse(dt.Field<string>("Length")) == Length &&
                                                             double.Parse(dt.Field<string>("DiaFrom")) <= ExternalD &&
                                                             double.Parse(dt.Field<string>("DiaTo")) >= ExternalD
                                                             select dt;
                    DataView view = query.AsDataView();
                    foreach (DataRowView item in view)
                    {
                        DataRow row = item.Row;
                        pin = row["Pin"].ToString();
                        lblPin.Text = pin;
                        lblPin.BackColor = Color.Peru;
                    }
                    //
                    CheckingInfo checkingInfo = null;
                    int checkcount = CountPCDByName(poNo);//Quantity check
                    //Condition plating
                    if (Type.Contains('M'))
                    {
                        //Scan before 1 time (before plating) or(PO not finish)
                        if (checkcount >= 1)
                        {
                            //Check PO before
                            if (GetResult(poNo) != "OK")
                            {
                                //Update PO not finish
                                lblQRStatus.Text = "PO này kiểm tra chưa đủ số lượng vui lòng kiểm tra tiếp.";
                                lblQRStatus.ForeColor = Color.Red;
                                checkingInfo = GetInforPO(poNo);
                                quantityCheck = checkingInfo.Quantity;
                                lblQuantityCheck.Text = quantityCheck.ToString();
                                if (quantityCheck == quantityPO)
                                {
                                    CheckOK(checkingInfo);
                                    return;
                                }
                            }
                            //Check after plating
                            else
                            {
                                lblQRStatus.Text = "PO này đã scan " + checkcount + " lần, vui lòng chọn loại sản phẩm";
                                lblQRStatus.ForeColor = Color.Red;
                                checkingInfo = new CheckingInfo()
                                {
                                    Name = partName,
                                    PO_Name = poNo,
                                    Order_No = orderNo,
                                    Size = ExternalD,
                                    PCD = jig,
                                    UserID = txtEmpID.Text,
                                    Result = "",
                                    IsPlated = true,
                                    CheckCount = checkcount + 1,
                                    Quantity = 0
                                };
                                AddNewCheckPCDInfo(checkingInfo);
                            }
                        }
                        //The first Scan
                        else
                        {
                            lblQRStatus.Text = "Vui lòng chọn loại sản phẩm";
                            lblQRStatus.ForeColor = Color.Red;
                            checkingInfo = new CheckingInfo()
                            {
                                Name = partName,
                                PO_Name = poNo,
                                Order_No = orderNo,
                                Size = ExternalD,
                                PCD = jig,
                                UserID = txtEmpID.Text,
                                Result = "",
                                IsPlated = true,
                                CheckCount = 1,
                                Quantity = 0
                            };
                            AddNewCheckPCDInfo(checkingInfo);
                        }
                    }
                    else
                    {
                        //Scan before 1 time or (PO not finish)
                        if (checkcount >= 1)
                        {
                            //Check PO before
                            if (GetResult(poNo) != "OK")
                            {
                                //Update PO not finish
                                lblQRStatus.Text = "PO này kiểm tra chưa đủ số lượng vui lòng kiểm tra tiếp.";
                                lblQRStatus.ForeColor = Color.Red;
                                checkingInfo = GetInforPO(poNo);
                                quantityCheck = checkingInfo.Quantity;
                                lblQuantityCheck.Text = quantityCheck.ToString();
                                if (quantityCheck == quantityPO)
                                {
                                    CheckOK(checkingInfo);
                                    return;
                                }
                            }
                            //Check after plating
                            else
                            {
                                lblQRStatus.Text = "PO này đã scan " + checkcount + " lần, vui lòng chọn loại sản phẩm";
                                lblQRStatus.ForeColor = Color.Red;
                                checkingInfo = new CheckingInfo()
                                {
                                    Name = partName,
                                    PO_Name = poNo,
                                    Order_No = orderNo,
                                    Size = ExternalD,
                                    PCD = jig,
                                    UserID = txtEmpID.Text,
                                    Result = "",
                                    IsPlated = false,
                                    CheckCount = checkcount + 1,
                                    Quantity = 0
                                };
                                AddNewCheckPCDInfo(checkingInfo);
                            }
                        }
                        //The first Scan
                        else
                        {
                            lblQRStatus.Text = "Vui lòng chọn loại sản phẩm";
                            lblQRStatus.ForeColor = Color.Red;
                            checkingInfo = new CheckingInfo()
                            {
                                Name = partName,
                                PO_Name = poNo,
                                Order_No = orderNo,
                                Size = ExternalD,
                                PCD = jig,
                                UserID = txtEmpID.Text,
                                Result = "",
                                IsPlated = false,
                                CheckCount = 1,
                                Quantity = 0
                            };
                            AddNewCheckPCDInfo(checkingInfo);
                        }
                    }
                    await Task.Delay(2000);
                    //Add data PO
                    txtSerial.Tag = checkingInfo;
                    //Check jig
                    CheckConditionAsync();
                }
                else
                {
                    lblQRStatus.Text = "Mã QR/Barcode không hợp lệ";
                    lblQRStatus.ForeColor = Color.Red;
                    txtBarcode.Select(0, txtBarcode.Text.Length);
                }
            }
        }
        private void CheckOK(CheckingInfo ckInfo)
        {
            //
            lblStatus.Text = "Finish";
            lblStatus.ForeColor = Color.White;
            lblStatus.BackColor = Color.Teal;
            //
            ckInfo.Result = "OK";
            int result = UpdateResult(ckInfo.Result, ckInfo.PO_Name);
            if (result == 1) EnableControl();
            if (comPort.IsOpen) comPort.Write("FINISH");
        }
        private async void CheckConditionAsync()
        {
            if (savePin == lblPin.Text)
            {
                lblPin.BackColor = SystemColors.Control;
                isPin = true;
                txtPin.Text = savePin;
                if (comPort.IsOpen)
                {
                    comPort.Write("CHOT");
                    await Task.Delay(2000);
                }
                if (saveJig == lblPCD.Text)
                {
                    if (comPort.IsOpen)
                    {
                        comPort.Write("GA");
                    }
                    isJig = true;
                    txtJig.Text = saveJig;
                    grbNormal.Enabled = true;
                    grbPlating.Enabled = true;
                    lblJig.BackColor = SystemColors.Control;
                    lblStatus.ForeColor = Color.Orange;
                    lblStatus.Text = "Vui lòng chọn loại hàng";
                }
                else
                {
                    lblStatus.Text = "Vui lòng kiểm tra JIG !";
                    lblStatus.ForeColor = Color.Orange;
                    txtJig.Enabled = true;
                    txtJig.Select();
                }
            }
            else
            {
                lblStatus.Text = "Vui lòng kiểm tra PIN !";
                lblStatus.ForeColor = Color.Orange;
                txtPin.Enabled = true;
                txtPin.Select();
            }
        }

        private void EnableControl()
        {
            txtBarcode.Text = "";
            //
            if (valid)
            {
                txtBarcode.Enabled = true;
                txtBarcode.Select();
            }
            else
            {
                txtBarcode.Enabled = false;
                txtUserID.Select();
            }
            txtPin.Enabled = false;
            txtJig.Enabled = false;
            txtSerial.Enabled = false;
            //
            lblPin.BackColor = SystemColors.Control;
            lblPCD.BackColor = SystemColors.Control;
            //
            grbNormal.Enabled = false;
            grbPlating.Enabled = false;
            //
            isPin = false;
            isJig = false;
            //
            txtPin.Text = "";
            txtJig.Text = "";
            //
            lblQRStatus.Text = "";
            //
            lblQuantityCheck.Text = "";
            lblQuantityPO.Text = "";
            //
            txtSerial.Tag = null;
        }

        private void btnInfor_Click(object sender, EventArgs e)
        {
            PCDInfor infor = new PCDInfor();
            infor.Show();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            valid = false;
            txtUserID.Text = txtEmpID.Text;
            txtUserID.Select(0, txtUserID.Text.Length);
            txtEmpID.Text = "";
            txtUserName.Text = "";
            txtUserID.Focus();
            EnableControl();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Dispose();
            Environment.Exit(0);
        }

        private void txtPin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                if (txtPin.Text == lblPin.Text)
                {
                    isPin = true;
                    txtPin.Enabled = false;
                    lblPin.BackColor = SystemColors.Control;
                    savePin = txtPin.Text;
                    CheckConditionAsync();
                }
                else
                {
                    isPin = false;
                    Thread.Sleep(50);
                    txtPin.Text = "";
                    lblStatus.Text = "Kiểm tra PIN bị lỗi!";
                    lblStatus.ForeColor = Color.Red;
                }
            }
        }

        private void txtJig_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                if (txtJig.Text == lblPCD.Text)
                {
                    if (comPort.IsOpen) comPort.Write("GA");
                    saveJig = txtJig.Text;
                    isJig = true;
                    txtJig.Enabled = false;
                    lblStatus.ForeColor = Color.Orange;
                    grbNormal.Enabled = true;
                    grbPlating.Enabled = true;
                    lblJig.BackColor = SystemColors.Control;
                    lblStatus.Text = "Vui lòng chọn loại hàng";
                }
                else
                {
                    isJig = false;
                    Thread.Sleep(50);
                    txtJig.Text = "";
                    lblStatus.Text = "Kiểm tra JIG bị lỗi !";
                    lblStatus.ForeColor = Color.Red;
                }
            }
        }
        private void ButtonPOType_Click(object sender, EventArgs e)
        {
            Button bt = sender as Button;
            CheckingInfo chkInfo = txtSerial.Tag as CheckingInfo;
            if (bt.Tag.Equals("BF"))
            {
                if (chkInfo.CheckCount == 1)
                {
                    chkInfo.Note = "BF";
                    txtSerial.Tag = chkInfo;
                    grbPlating.Enabled = false;
                    grbNormal.Enabled = false;
                    txtSerial.Enabled = true;
                    txtSerial.Select();
                    lblJig.BackColor = SystemColors.Control;
                    lblStatus.ForeColor = Color.Orange;
                    lblStatus.Text = "Tiến hành kiểm tra PCD";
                }
                else
                {
                    chkInfo.Note = "BF";
                    txtSerial.Tag = chkInfo;
                    grbNormal.Enabled = true;
                    grbPlating.Enabled = false;
                }
            }
            else if (bt.Tag.Equals("AT"))
            {
                if (chkInfo.CheckCount == 1)
                {
                    chkInfo.Note = "AT";
                    txtSerial.Tag = chkInfo;
                    grbNormal.Enabled = false;
                    grbPlating.Enabled = false;
                    txtSerial.Enabled = true;
                    txtSerial.Select();
                    lblJig.BackColor = SystemColors.Control;
                    lblStatus.ForeColor = Color.Orange;
                    lblStatus.Text = "Tiến hành kiểm tra PCD";
                }
                else
                {
                    chkInfo.Note = "AT";
                    txtSerial.Tag = chkInfo;
                    grbNormal.Enabled = true;
                    grbPlating.Enabled = false;
                }

            }
            else if (bt.Tag.Equals("Repair"))
            {
                if (chkInfo.Name.Contains("M"))
                {
                    if (chkInfo.Note.Contains("Normal") || chkInfo.Note.Contains("Repair"))
                    {
                        chkInfo.Note = chkInfo.Note.Remove(2) + "-Repair";
                    }
                    else
                    {
                        chkInfo.Note = chkInfo.Note + "-Repair";
                    }
                }
                else chkInfo.Note = "Repair";
                txtSerial.Tag = chkInfo;
                txtSerial.Enabled = true;
                txtSerial.Select();
                grbNormal.Enabled = false;
                lblJig.BackColor = SystemColors.Control;
                lblStatus.ForeColor = Color.Orange;
                lblStatus.Text = "Tiến hành kiểm tra PCD";
            }
            else if (bt.Tag.Equals("Normal"))
            {
                if (chkInfo.Name.Contains("M"))
                {
                    if (chkInfo.Note.Contains("Normal") || chkInfo.Note.Contains("Repair"))
                    {
                        chkInfo.Note = chkInfo.Note.Remove(2) + "-Normal";
                    }
                    else
                    {
                        chkInfo.Note = chkInfo.Note + "-Normal";
                    }
                }
                else chkInfo.Note = "Normal";
                txtSerial.Tag = chkInfo;
                txtSerial.Enabled = true;
                txtSerial.Select();
                grbNormal.Enabled = false;
                lblJig.BackColor = SystemColors.Control;

                lblStatus.Text = "Tiến hành kiểm tra PCD";
            }
            UpdateNote(chkInfo.Note, chkInfo.PO_Name);
        }
        private void txtSerial_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                lblStatus.ForeColor = Color.Olive;
                lblStatus.Text = "Checking...";
                txtSerial.Text = "";
                return;
            }
            if (e.KeyCode == Keys.Enter)
            {
                if (txtSerial.Tag != null
                                    && isJig == true
                                    && isPin == true)
                {
                    CheckingInfo ckInfo = txtSerial.Tag as CheckingInfo;
                    string seri = txtSerial.Text.ToUpper();
                    if (seri.Equals(ckInfo.PCD))
                    {
                        //Count
                        ++quantityCheck;
                        lblQuantityCheck.Text = quantityCheck.ToString();
                        UpdateQuantity(quantityCheck, ckInfo.PO_Name);
                        if (quantityCheck == quantityPO)
                        {
                            CheckOK(ckInfo);
                        }
                        txtSerial.Text = "";
                    }
                    else
                    {
                        lblStatus.Text = "Sai mã jig";
                        lblStatus.ForeColor = Color.Red;
                        txtSerial.Select(0, txtSerial.Text.Length);
                    }
                }
            }

        }

        //DataBase
        private CheckingInfo GetInforPO(string po)
        {
            CheckingInfo info = new CheckingInfo();
            string query = string.Format(@"SELECT * FROM CheckingInfo WHERE PO_Name = '{0}' ORDER BY ID DESC LIMIT 1", po);
            DataTable dta = MySQLProvider.Instance.ExecuteQuery(query);
            if (dta.Rows.Count > 0)
            {
                info.Name = dta.Rows[0]["Name"].ToString();
                info.PO_Name = dta.Rows[0]["PO_Name"].ToString();
                info.Order_No = dta.Rows[0]["Order_No"].ToString();
                info.Size = int.Parse(dta.Rows[0]["Size"].ToString());
                info.PCD = dta.Rows[0]["PCD"].ToString();
                info.UserID = dta.Rows[0]["UserID"].ToString();
                info.Result = dta.Rows[0]["Result"].ToString();
                info.IsPlated = bool.Parse(dta.Rows[0]["IsPlated"].ToString());
                info.CheckingDate = DateTime.Parse(dta.Rows[0]["CheckingDate"].ToString());
                info.Note = dta.Rows[0]["Note"].ToString();
                info.CheckCount = int.Parse(dta.Rows[0]["CheckCount"].ToString());
                info.Quantity = int.Parse(dta.Rows[0]["Quantity"].ToString());
            }
            return info;
        }
        private int GetQuantity(string po)
        {
            int result = 0;
            string query = string.Format(@"SELECT Quantity FROM CheckingInfo WHERE PO_Name = '{0}' ORDER BY ID DESC LIMIT 1", po);
            DataTable dta = MySQLProvider.Instance.ExecuteQuery(query);
            result = int.Parse(dta.Rows[0]["Quantity"].ToString());
            return result;
        }
        private string GetResult(string po)
        {
            string result = null;
            string query = string.Format(@"SELECT Result FROM CheckingInfo WHERE PO_Name = '{0}' ORDER BY ID DESC LIMIT 1", po);
            DataTable dta = MySQLProvider.Instance.ExecuteQuery(query);
            if (dta.Rows.Count > 0)
                result = dta.Rows[0]["Result"].ToString();
            return result;
        }
        private int AddNewCheckPCDInfo(CheckingInfo ckInfo)
        {
            int result = 0;
            try
            {
                string query = string.Format(@"INSERT INTO `CheckingInfo`(`Name`, `PO_Name`, `Order_No`, `Size`, `PCD`, `UserID`, `Result`, `IsPlated`, `CheckingDate`, Note, CheckCount, Quantity) VALUES('{0}', '{1}', '{2}', {3}, '{4}', '{5}', '{6}', {7}, NOW(), '{8}', '{9}', '{10}')",
                   ckInfo.Name, ckInfo.PO_Name, ckInfo.Order_No, ckInfo.Size, ckInfo.PCD, ckInfo.UserID, ckInfo.Result, ckInfo.IsPlated ? 1 : 0, ckInfo.Note, ckInfo.CheckCount, 0);
                result = MySQLProvider.Instance.ExecuteNonQuery(query);
                if (result > 0)
                    result = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                result = 0;
            }
            return result;
        }
        private int UpdateResult(string text, string po)
        {
            int result = 0;
            try
            {
                string query = string.Format(@"UPDATE CheckingInfo SET Result='{0}' WHERE PO_Name='{1}' ORDER BY ID DESC LIMIT 1", text, po);
                result = MySQLProvider.Instance.ExecuteNonQuery(query);
                if (result > 0)
                    result = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                result = 0;
            }

            return result;
        }
        private int UpdateQuantity(int quantity, string po)
        {
            int result = 0;
            try
            {
                string query = string.Format(@"UPDATE CheckingInfo SET Quantity='{0}' WHERE PO_Name='{1}' ORDER BY ID DESC LIMIT 1", quantity, po);
                result = MySQLProvider.Instance.ExecuteNonQuery(query);
                if (result > 0)
                    result = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                result = 0;
            }
            return result;
        }
        private int CountPCDByName(string po)
        {
            int result = 0;
            string query = string.Format(@"SELECT PO_Name,CheckCount,Note FROM CheckingInfo WHERE PO_Name = '{0}'", po);
            result = MySQLProvider.Instance.ExecuteQuery(query).Rows.Count;
            return result;
        }
        private int UpdateNote(string note, string po)
        {
            int result = 0;
            try
            {
                string query = string.Format(@"UPDATE CheckingInfo SET Note='{0}' WHERE PO_Name='{1}' ORDER BY ID DESC LIMIT 1", note, po);
                result = MySQLProvider.Instance.ExecuteNonQuery(query);
                if (result > 0)
                    result = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                result = 0;
            }

            return result;
        }
    }
}
