using System;
using System.ComponentModel;

namespace JBMTS_PCD_Check.Model
{
    class CheckingInfo
    {

        public int ID { get; set; }
        public string Name { get; set; }
        [Description("Số PO")]
        public string PO_Name { get; set; }
        [Description("Số Order")]
        public string Order_No { get; set; }
        public int Size { get; set; }
        public string PCD { get; set; }
        [Description("Người nhập")]
        public string UserID { get; set; }
        [Description("Kết quả")]
        public string Result { get; set; }
        [Description("Ngày nhập")]
        public DateTime CheckingDate { get; set; }
        [Description("Sản phẩm được mạ")]
        public bool IsPlated { get; set; }
        [Description("Ghi chú")]
        public string Note { get; set; }
        [Description("Mô tả")]
        public string Description
        {
            get
            {
                string descript = GetNoteDescript(Note);
                return descript;
            }
        }

        [Description("Lần Scan")]
        public int CheckCount
        {
            get; set;
        }
        public int Quantity
        {
            get; set;
        }
        private string GetNoteDescript(string noteType)
        {
            string descript = "Thường";
            switch (noteType)
            {
                case "BF":
                    descript = "Trước mạ";
                    break;
                case "AT":
                    descript = "Sau mạ";
                    break;
                case "Repair":
                    descript = "Repair";
                    break;
                case "Normal":
                    descript = "Thường";
                    break;
                case "BF-Repair":
                    descript = "Trước mạ-sửa";
                    break;
                case "BF-Normal":
                    descript = "Trước mạ-thường";
                    break;
                case "AT-Repair":
                    descript = "Sau mạ-sửa";
                    break;
                case "AT-Normal":
                    descript = "Sau mạ-sửa";
                    break;
                default:
                    descript = "Thường";
                    break;
            }
            return descript;
        }
    }
}
