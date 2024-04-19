using JBMTO_PCD_Check.Provider;
using JBMTS_PCD_Check.Model;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace JBMTS_PCD_Check
{
    public partial class PCDInfor : Form
    {
        public PCDInfor()
        {
            InitializeComponent();
            SetUpNumberOfRows();
            dtpFrom.Value = DateTime.Now.Date;
            dtpTo.Value = DateTime.Now.Date;
        }
        private void SetUpNumberOfRows()
        {
            int[] itemSource = { 10, 15, 20, 50 };
            cboNumberOfRows.DataSource = itemSource;
            cboNumberOfRows.SelectedIndex = 0;
        }

        private void PCDInfor_Load(object sender, EventArgs e)
        {

        }

        private void btnView_Click(object sender, EventArgs e)
        {
            int numberOfRows = Convert.ToInt32(cboNumberOfRows.SelectedItem);
            GetPCDCheckingInfo(dtpFrom.Value.Date, dtpTo.Value.Date, 1, numberOfRows);
        }

        private void GetPCDCheckingInfo(DateTime from, DateTime to, int page = 1, int numberOfRows = 10, string search = "")
        {
            try
            {
                int totalRecords = Convert.ToInt32(MySQLProvider.Instance.ExecuteScalar(string.Format(@"
                                                                                          SELECT COUNT(*)
                                                                                          FROM CheckingInfo
                                                                                          WHERE DATE_FORMAT(CheckingDate,'%Y-%m-%d %k:%i:%s') >= '{0}' AND DATE_FORMAT(CheckingDate,'%Y-%m-%d %k:%i:%s') <= '{1}' AND (Name LIKE '%{2}%' OR PO_Name LIKE '%{2}%' OR Order_No LIKE '%{2}%' OR Size LIKE '%{2}%' OR PCD LIKE '%{2}%' OR UserID LIKE '%{2}%')", from.AddHours(6).ToString("yyyy-MM-dd hh:mm:ss"), to.AddHours(30).AddSeconds(-1).ToString("yyyy-MM-dd hh:mm:ss"), search)));
                double totalPages = Math.Ceiling((totalRecords * 1.0) / numberOfRows);
                int start = (page - 1) * numberOfRows;

                string query = string.Format(@"SELECT ID, Name, PO_Name, Order_No, Size, PCD, UserID, Result,IF(`IsPlated`=1,'Yes','No') IsPlated, Note, CheckCount, DATE_FORMAT(CheckingDate,'%Y-%m-%d %k:%i:%s') CheckingDate, Quantity
                                 FROM CheckingInfo
                                 WHERE DATE_FORMAT(CheckingDate,'%Y-%m-%d %k:%i:%s') >= '{0}' AND DATE_FORMAT(CheckingDate,'%Y-%m-%d %k:%i:%s') <= '{1}' AND (Name LIKE '%{2}%' OR PO_Name LIKE '%{2}%' OR Order_No LIKE '%{2}%' OR Size LIKE'%{2}%' OR PCD LIKE '%{2}%' OR UserID LIKE '%{2}%') ORDER BY ID DESC LIMIT {3} , {4}", from.AddHours(6).ToString("yyyy-MM-dd hh:mm:ss"), to.AddHours(30).AddSeconds(-1).ToString("yyyy-MM-dd hh:mm:ss"), search, start, numberOfRows);
                DataTable dt = new DataTable();
                dt = MySQLProvider.Instance.ExecuteQuery(query);

                dgvPCDCheckingInfo.DataSource = dt.DefaultView;

                Utility.Paging(stackpager, page, totalPages, onClickPager);
            }
            catch (Exception)
            {

            }
        }

        private void onClickPager(object sender, EventArgs e)
        {
            string search = txtSearch.Text;
            int numberOfRows = Convert.ToInt32(cboNumberOfRows.SelectedItem);
            Button bt = sender as Button;
            int page = Convert.ToInt32(bt.Tag);
            GetPCDCheckingInfo(dtpFrom.Value.Date, dtpTo.Value.Date, page, numberOfRows, search);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string search = txtSearch.Text;
            int numberOfRows = Convert.ToInt32(cboNumberOfRows.SelectedItem);
            GetPCDCheckingInfo(dtpFrom.Value.Date, dtpTo.Value.Date, 1, numberOfRows, search);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
        }

        private void btnExcel_Click(object sender, EventArgs e)
        {
            ExportExcel();
        }
        private void ExportExcel()
        {
            DataTable dt = new DataTable();
            List<CheckingInfo> lstPCD = new List<CheckingInfo>();
            try
            {
                dt = MySQLProvider.Instance.ExecuteQuery(string.Format("SELECT * FROM CheckingInfo WHERE DATE(CheckingDate) >= '{0}' AND DATE(CheckingDate) <= '{1}'", dtpFrom.Value.ToString("yyyy-MM-dd"), dtpTo.Value.ToString("yyyy-MM-dd")));
                foreach (DataRow r in dt.Rows)
                {
                    lstPCD.Add(new CheckingInfo()
                    {
                        ID = Convert.ToInt32(r["ID"]),
                        Name = r["Name"].ToString(),
                        PO_Name = r["PO_Name"].ToString(),
                        Order_No = r["Order_No"].ToString(),
                        Size = Convert.ToInt32(r["Size"].ToString()),
                        PCD = r["PCD"].ToString(),
                        UserID = r["UserID"].ToString(),
                        Result = r["Result"].ToString(),
                        CheckingDate = Convert.ToDateTime(r["CheckingDate"]),
                        IsPlated = Convert.ToBoolean(r["IsPlated"]),
                        Note = r["Note"].ToString(),
                        CheckCount = Convert.ToInt32(r["CheckCount"].ToString()),
                        Quantity = Convert.ToInt32(r["Quantity"].ToString())
                    });
                }

            }
            catch (Exception)
            {
                MessageBox.Show("Lấy dữ liệu thất bại, vui lòng thử lại sau", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (lstPCD.Count > 0)
            {
                try
                {
                    using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                    {
                        if (fbd.ShowDialog() == DialogResult.OK)
                        {
                            string filePath = fbd.SelectedPath + "\\PCDInfo_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".xlsx";

                            var newFile = new FileInfo(filePath);
                            using (var excelPackage = new ExcelPackage(newFile))
                            {
                                var worksheets = excelPackage.Workbook.Worksheets;
                                if (worksheets.Count == 0)
                                {
                                    // Tạo author cho file Excel
                                    excelPackage.Workbook.Properties.Author = "Misumi F4";
                                    // Tạo title cho file Excel
                                    excelPackage.Workbook.Properties.Title = "PCD Checking Info " + DateTime.Now.ToString("yy-MM-dd");
                                    // thêm tí comments vào làm màu 
                                    excelPackage.Workbook.Properties.Comments = "Only for using local";
                                    // Add Sheet vào file Excel                            
                                    excelPackage.Workbook.Worksheets.Add("PCD");
                                }
                                else if (worksheets.Count == 1)
                                {
                                    var ws = worksheets.AsEnumerable().Where(w => w.Name == "PCD").FirstOrDefault();
                                    if (ws != null)
                                    {
                                        ws.Name = "TempSheet";
                                        worksheets.Delete(ws);
                                    }
                                    // Tạo author cho file Excel
                                    excelPackage.Workbook.Properties.Author = "Misumi F4";
                                    // Tạo title cho file Excel
                                    excelPackage.Workbook.Properties.Title = "PCD Checking Info " + DateTime.Now.ToString("yy-MM-dd");
                                    // thêm tí comments vào làm màu 
                                    excelPackage.Workbook.Properties.Comments = "Only for using local";
                                    // Add Sheet vào file Excel                            
                                    excelPackage.Workbook.Worksheets.Add("PCD");
                                }
                                else if (worksheets.Count > 1)
                                {
                                    var ws = worksheets.AsEnumerable().Where(w => w.Name == "PCD").FirstOrDefault();
                                    worksheets.Delete(ws);

                                    // Add Sheet vào file Excel                            
                                    excelPackage.Workbook.Worksheets.Add("PCD");
                                }

                                // Lấy Sheet đầu tiên 
                                var workSheet = excelPackage.Workbook.Worksheets["PCD"];
                                // Đổ data vào Excel file                            
                                workSheet.Cells[1, 1].LoadFromCollection(lstPCD, true, OfficeOpenXml.Table.TableStyles.Dark9);
                                workSheet.Cells[workSheet.Dimension.Address].AutoFitColumns();

                                //format excel columns
                                BindingFormatExcel(workSheet, lstPCD);

                                excelPackage.Save();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }
        private void BindingFormatExcel(ExcelWorksheet excelWorkSheet, List<CheckingInfo> lstPCD)
        {
            //format cell CheckingDate
            var cellCheckingDate = excelWorkSheet.Cells[2, 9, lstPCD.Count + 4, 9];
            cellCheckingDate.Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
            cellCheckingDate.AutoFitColumns(15);

            //format cell IsPlated
            var cellIsPlatad = excelWorkSheet.Cells[2, 10, lstPCD.Count + 4, 10];
            //2019-05-06 Begin Lock
            //using (ExcelRange rng = excelWorkSheet.Cells[1,9,lstPCD.Count+4,9])
            //{
            //    ExcelTableCollection tbCollection = excelWorkSheet.Tables;
            //    ExcelTable table = tbCollection.Add(rng, "tblPCDInfo");
            //    table.TableStyle = TableStyles.Dark10;
            //}
            //2019-05-06 End lock
        }
    }
}
