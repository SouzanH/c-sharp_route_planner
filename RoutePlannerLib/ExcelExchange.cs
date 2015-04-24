﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;

namespace Fhnw.Ecnf.RoutePlanner.RoutePlannerLib.Export
{
    public class ExcelExchange
    {
        private Application excel;

        public void WriteToFile(String fileName, City from, City to, List<Link> links)
        {
            excel = new Application();

            if (excel == null)
            {
                Console.WriteLine("Excel not available");
                return;
            }
            var workbook = excel.Workbooks.Add();
            var ws = (Worksheet)workbook.Worksheets[1];

            
            var columns = 4;

            var data = new object[links.Count + 1, columns];
            data[0, 0] = "From";
            data[0, 1] = "To";
            data[0, 2] = "Distance";
            data[0, 3] = "Transporte \n Mode";

            var range = ws.get_Range("A1:D1");
            range.Font.Size = 14;
            range.Font.Bold = true;
            range.EntireColumn.ColumnWidth = 20;
            var borders = range.Borders;
            borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;
            borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous;
            borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous;
            borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous;
            borders[XlBordersIndex.xlInsideVertical].LineStyle = XlLineStyle.xlContinuous;


            int pos = 1;
            foreach (var link in links)
            {
                data[pos, 0] = link.FromCity.Name;
                data[pos, 1] = link.ToCity.Name;
                data[pos, 2] = link.Distance;
                data[pos, 3] = link.TransportMode;
                pos++;
            }

            var startCell = (Range)ws.Cells[1, 1];
            var endCell = (Range)ws.Cells[links.Count + 1, columns];
            var writeRange = ws.Range[startCell, endCell];

            writeRange.Value2 = data;
            
            /* Not Working with excel 2007
            ws.Cells[1, 0] = "From";
            ws.Cells[1, 1] = "To";
            ws.Cells[1, 2] = "Distance";
            ws.Cells[1, 3] = "Transporte \n Mode";

            int pos = 2;
            foreach (var link in links)
            {
                ws.Cells[pos, 0] = link.FromCity.Name;
                ws.Cells[pos, 1] = link.ToCity.Name;
                ws.Cells[pos, 2] = link.Distance;
                ws.Cells[pos, 3] = link.TransportMode;
                pos++;
            }
            */

            excel.DisplayAlerts = false;
            workbook.SaveAs(fileName);
            workbook.Close();
            excel.Quit();
        }

    }

}
