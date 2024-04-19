using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace JBMTS_PCD_Check.Model
{
    class Utility
    {
        public static Stream CreateExcelFile(List<CheckingInfo> dataSource, Stream stream = null)
        {
            using (var excelPackage = new ExcelPackage(stream ?? new MemoryStream()))
            {
                excelPackage.Workbook.Properties.Author = "Misumi F4";
                excelPackage.Workbook.Properties.Title = "PCD Checking Info " + DateTime.Now.ToString("yy-MM-dd");
                excelPackage.Workbook.Properties.Comments = "Only for using local";
                excelPackage.Workbook.Worksheets.Add("PCD Info");
                var workSheet = excelPackage.Workbook.Worksheets[1];
                workSheet.Cells[1, 1].LoadFromCollection(dataSource, true, OfficeOpenXml.Table.TableStyles.Dark9);
                workSheet.Cells[workSheet.Dimension.Address].AutoFitColumns();
                excelPackage.Save();
                return excelPackage.Stream;
            }
        }

        public static void Paging(FlowLayoutPanel stackpager, int page, double totalPages, EventHandler onClickPager)
        {
            //paging
            Font fonts = new Font("Century Gothic", 10, FontStyle.Regular);
            Button bt;
            if (totalPages > 0)
            {
                stackpager.Controls.Clear();
                if (page == 1)
                {
                    stackpager.Controls.Add(bt = new Button()
                    {
                        Text = "First",
                        Enabled = false,
                        Width = TextRenderer.MeasureText("First", fonts).Width + 7,
                        Height = 30,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(0)
                    });
                }
                else
                {
                    stackpager.Controls.Add(bt = new Button()
                    {
                        Text = "First",
                        Tag = 1,
                        Width =
                        TextRenderer.MeasureText("First", fonts).Width + 7,
                        Height = 30,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(0)
                    });
                    bt.Click += onClickPager;
                }

                var i = (page > 5 ? page - 4 : 1);
                if (i != 1)
                {
                    stackpager.Controls.Add(bt = new Button()
                    {
                        Text = "...",
                        Enabled = false,
                        Width = TextRenderer.MeasureText("...", fonts).Width + 7,
                        Height = 30,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(0)
                    });
                }

                for (; i <= (page + 4) && i <= totalPages; i++)
                {
                    if (i == page)
                        stackpager.Controls.Add(bt = new Button()
                        {
                            Text = i.ToString(),
                            Tag = i,
                            BackColor = Color.LightBlue,
                            Width = TextRenderer.MeasureText(i.ToString(), fonts).Width + 7,
                            Height = 30,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Margin = new Padding(0)
                        });
                    else
                    {
                        stackpager.Controls.Add(bt = new Button()
                        {
                            Text = i.ToString(),
                            Tag = i,
                            Width = TextRenderer.MeasureText(i.ToString(), fonts).Width + 7,
                            Height = 30,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Margin = new Padding(0)
                        });
                        bt.Click += onClickPager;
                    }

                    if (i == page + 4 && i < totalPages)
                        stackpager.Controls.Add(bt = new Button()
                        {
                            Text = "...",
                            Enabled = false,
                            Width = TextRenderer.MeasureText("...", fonts).Width + 7,
                            Height = 30,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Margin = new Padding(0)
                        });
                }
                if (page == totalPages)
                {

                    stackpager.Controls.Add(bt = new Button()
                    {
                        Text = "Last",
                        Enabled = false,
                        Width = TextRenderer.MeasureText("Last", fonts).Width + 7,
                        Height = 30,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(0)
                    });
                }
                else
                {
                    stackpager.Controls.Add(bt = new Button()
                    {
                        Text = "Last",
                        Tag = totalPages,
                        Width = TextRenderer.MeasureText("Last", fonts).Width + 7,
                        Height = 30,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(0)
                    });
                    bt.Click += onClickPager;
                }

            }
        }
    }
}
