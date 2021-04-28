using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ExcelDataReader;
using Newtonsoft.Json;
using Data_Vizualizer.Models;
using Accord.Statistics;
using Accord.Statistics.Analysis;
namespace Data_Vizualizer.Controllers
{
    public class MainController : Controller
    {
        // GET: Main
        public ActionResult Index()
        {
            TempData["imp"] = 1;
            TempData.Keep("imp");
            return View();
        }

        [HttpPost]
        // Post: Main/Index
        public ActionResult Index(string data)
        {
            ViewBag.YD = data;
            //get all the rows
            string[] Result = data.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            
            //ok now here we have the Result zero as labels
            string[] Labels = Result[0].Split(new string[] { "," }, StringSplitOptions.None);
            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            string[] tempr1 = Result[1].Split(new string[] { "," }, StringSplitOptions.None);
            List<string> dataTypeR1 = new List<string>();

            foreach (string item in tempr1)
            {
                DateTime t1;
                double t2;
                int t3;

                if (double.TryParse(item, out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (int.TryParse(item, out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (DateTime.TryParse(item, CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.d = dataTypeR1;
            //row count error
            List<int> CountErrList = new List<int>();
            //data type mismatch error
            List<List<int>> dataTypeErrList = new List<List<int>>();
            //Validate Data
            for (int i = 1; i < Result.Length; i++)
            {
                DateTime t1;
                double t2;
                int t3;
                string[] temp = Result[i].Split(new string[] { "," }, StringSplitOptions.None);
                if (temp.Length != Labels.Length)
                {
                    CountErrList.Add(i);
                    continue;
                }
                for (int j = 0; j < dataTypeR1.Count; j++)
                {
                    if (dataTypeR1[j] == "Date" && !(DateTime.TryParse(temp[j], CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1) || DateTime.TryParse(temp[j], CultureInfo.CreateSpecificCulture("fr-FR"), DateTimeStyles.None, out t1)))
                    {
                        List<int> te = new List<int>();
                        te.Add(i);
                        te.Add(j);
                        te.Add(11);
                        dataTypeErrList.Add(te);
                    }
                    else if (dataTypeR1[j] == "Double" && !double.TryParse(temp[j], out t2))
                    {
                        List<int> te = new List<int>();
                        te.Add(i);
                        te.Add(j);
                        te.Add(22);
                        dataTypeErrList.Add(te);
                    }
                    else if (dataTypeR1[j] == "Integer" && !int.TryParse(temp[j], out t3))
                    {
                        List<int> te = new List<int>();
                        te.Add(i);
                        te.Add(j);
                        te.Add(33);
                        dataTypeErrList.Add(te);
                    }

                }
            }

            if (CountErrList.Count > 0)
            {
                //error occured dont let user chose the labels for plotting the data
                //just prompt the error

                //set flag
                bool err = true;
                ViewBag.cerr = err;

                //set error message
                ViewBag.cerrMessage = "You have mismatch of #attributes at line(s):-";
                ViewBag.celist = CountErrList;


            }
            if (dataTypeErrList.Count > 0)
            {
                //set flagw

                ViewBag.dterr = true;

                //set error message
                ViewBag.dterrMessage = "You have mismatch of datatype at line and collumn:-";
                ViewBag.dtelist = dataTypeErrList;

            }
            DataTable dt = new DataTable();
            if (dataTypeErrList.Count == 0 && CountErrList.Count == 0)
            {
                //both are zero means no error just move to new view
                //keep all the things that you need with you
                //we need whole data in next view
                //store that in TempData
                
                foreach(string item in Labels) {
                    dt.Columns.Add(item);
                }
                for (int i = 1; i < Result.Length; i++)
                {
                    string[] tempr = Result[i].Split(new string[] { "," }, StringSplitOptions.None);
                    dt.Rows.Add(tempr);
                }
                TempData["data"] = dt;
                return RedirectToAction("Plot");

            }



            ViewBag.data = dt;
            return View();
        }

        public ActionResult FileUpload()
        {
            TempData["imp"] = 1;
            TempData.Keep("imp");
            return View();
        }
        [ActionName("FileUpload")]
        [HttpPost]
        public ActionResult FileUpload1(HttpPostedFileBase FileUpload1)
        {
            if (FileUpload1 != null && FileUpload1.ContentLength > 0)
            {
                // ExcelDataReader works with the binary Excel file, so it needs a FileStream
                // to get started. This is how we avoid dependencies on ACE or Interop:
                Stream stream = FileUpload1.InputStream;

                // We return the interface, so that
                IExcelDataReader reader = null;

                string extension = System.IO.Path.GetExtension(FileUpload1.FileName).ToLower();
                string query = null;
                string connString = "";

                string[] validFileTypes = { ".xls", ".xlsx", ".csv" };
                string path1 = string.Format("{0}/{1}", Server.MapPath("~/Content/Uploads"), FileUpload1.FileName);
                if (!Directory.Exists(path1))
                {
                    Directory.CreateDirectory(Server.MapPath("~/Content/Uploads"));
                }
                if (validFileTypes.Contains(extension))
                {
                    if (System.IO.File.Exists(path1))
                    {
                        System.IO.File.Delete(path1);
                    }
                    Request.Files["FileUpload1"].SaveAs(path1);
                    if (FileUpload1.FileName.EndsWith(".csv"))
                    {
                        DataTable dt = Utility.ConvertCSVtoDataTable(path1);
                        ViewBag.Data = dt;
                    }


                    else if (FileUpload1.FileName.EndsWith(".xls"))
                    {
                        reader = ExcelReaderFactory.CreateBinaryReader(stream);
                        DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true
                            }
                        });
                        reader.Close();

                        ViewBag.Data = result.Tables[0];
                    }
                    else if (FileUpload1.FileName.EndsWith(".xlsx"))
                    {
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                        DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true
                            }
                        });
                        reader.Close();

                        ViewBag.Data = result.Tables[0];
                    }
                    DataTable nwd = ViewBag.Data;
                    string[] Result;
                    ArrayList re = new ArrayList();
                    /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
                    {
                        re.Add(dr);
                    }*/
                    string[] Labels;
                    ArrayList ls = new ArrayList();
                    foreach (DataColumn column in (ViewBag.Data as System.Data.DataTable).Columns)
                    {
                        ls.Add(column.ColumnName);
                    }

                    //I need to know the data type of all the collumns 
                    //so i will iterate through all the attributes of 
                    //the first row as the reference to check other rows
                    for (int w = 0; w < ls.Count; w++)
                    {
                        re.Add(nwd.Rows[0][ls[w].ToString()]);
                    }

                    List<string> dataTypeR1 = new List<string>();

                    foreach (var item in re)
                    {
                        DateTime t1;
                        double t2;
                        int t3;

                        if (double.TryParse(item.ToString(), out t2))
                        {
                            dataTypeR1.Add("Double");
                        }
                        else if (int.TryParse(item.ToString(), out t3))
                        {
                            dataTypeR1.Add("Integer");
                        }
                        else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                        {
                            dataTypeR1.Add("Date");
                        }
                        else
                        {
                            dataTypeR1.Add("String");
                        }


                    }
                    ViewBag.d = dataTypeR1;
                    //row count error
                    List<int> CountErrList = new List<int>();
                    //data type mismatch error
                    List<List<int>> dataTypeErrList = new List<List<int>>();
                    //Validate Data
                    for (int i = 0; i < nwd.Rows.Count; i++)
                    {
                        DateTime t1;
                        double t2;
                        int t3;
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(nwd.Rows[i][ls[w].ToString()]);
                        }

                        for (int j = 0; j < dataTypeR1.Count; j++)
                        {
                            if (dataTypeR1[j] == "Date" && !(DateTime.TryParse(temp[j].ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1) || DateTime.TryParse(temp[j].ToString(), CultureInfo.CreateSpecificCulture("fr-FR"), DateTimeStyles.None, out t1)))
                            {
                                List<int> te = new List<int>();
                                te.Add(i);
                                te.Add(j);
                                te.Add(11);
                                dataTypeErrList.Add(te);
                            }
                            else if (dataTypeR1[j] == "Double" && !double.TryParse(temp[j].ToString(), out t2))
                            {
                                List<int> te = new List<int>();
                                te.Add(i);
                                te.Add(j);
                                te.Add(22);
                                dataTypeErrList.Add(te);
                            }
                            else if (dataTypeR1[j] == "Integer" && !int.TryParse(temp[j].ToString(), out t3))
                            {
                                List<int> te = new List<int>();
                                te.Add(i);
                                te.Add(j);
                                te.Add(33);
                                dataTypeErrList.Add(te);
                            }

                        }
                    }

                    if (CountErrList.Count > 0)
                    {
                        //error occured dont let user chose the labels for plotting the data
                        //just prompt the error

                        //set flag
                        bool err = true;
                        ViewBag.cerr = err;

                        //set error message
                        ViewBag.cerrMessage = "You have mismatch of #attributes at line(s):-";
                        ViewBag.celist = CountErrList;


                    }
                    if (dataTypeErrList.Count > 0)
                    {
                        //set flag

                        ViewBag.dterr = true;

                        //set error message
                        ViewBag.dterrMessage = "You have mismatch of datatype at line and collumn:-";
                        ViewBag.dtelist = dataTypeErrList;

                    }
                    if (dataTypeErrList.Count == 0 && CountErrList.Count == 0)
                    {
                        //both are zero means no error just move to new view
                        //keep all the things that you need with you
                        //we need whole data in next view
                        //store that in TempData
                        TempData["data"] = ViewBag.Data;
                        return RedirectToAction("Plot");

                    }



                    ViewBag.data = ViewBag.Data;


                    //reader.IsFirstRowAsColumnNames = true;
                }
                else
                {
                    ViewBag.Error = "Please Upload Files in .xls, .xlsx or .csv format";
                }
            }
            else
            {
                ViewBag.Error = "Please Select A File First.";
            }
            return View();
        }
        [HttpPost]
        public JsonResult SaveSvg(string imageData, string inText, string gtype, string desc)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            UserTitles ut = new UserTitles();

            ut.Name = User.Identity.Name;
            ut.Title = inText;
            ut.Type = gtype;
            ut.Description = desc;
            db.UserTitles.Add(ut);
            db.SaveChanges();
            string filename = inText + ".png";

            string asd = Path.Combine(Server.MapPath("~/Content/Uploads/" + User.Identity.Name + "/" + gtype));
            if (!Directory.Exists(asd))
            {
                Directory.CreateDirectory(asd);
            }
            string filep = Path.Combine(Server.MapPath("~/Content/Uploads/" + User.Identity.Name + "/" + gtype), filename);
            using (FileStream fs = new FileStream(filep, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))

                {
                    char[] sep = { ',' };
                    string idata = imageData.Split(sep)[1];
                    byte[] data = Convert.FromBase64String(idata);
                    bw.Write(data);
                    bw.Close();
                }
            }
            return Json("uploaded");
        }

        public ActionResult Plot()
        {
            TempData["imp"] = 1;
            TempData.Keep("imp");
            //hey I got my data in temp data
            //now i will get my data !remember to just use peek
            //this is get request so just show user the option of the graph which he want to 

            //try using value property in view and then post so we can plot the specific graph


            DataTable data = new DataTable();


            if (TempData["data"] != null)
            {
                data = (DataTable)(TempData.Peek("data"));
                TempData.Keep("data");
            }
            ViewBag.Datas = TempData["data"];
            //step2: just present the view and get the data from user regarding the graph he want to plot
            DataTable nwd = ViewBag.Datas;
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (ViewBag.Datas as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(nwd.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            ViewBag.Data = data;
            return View();
        }

        [HttpPost]
        public ActionResult Plot(FormCollection fc)
        {
            //selected x axis
            string schart = fc["schart"];
            string xs = fc["xaxis_select"];
            string ys = fc["yaxis_select"];

            if (schart == "Scatter plot")
            {
                return RedirectToAction("Scatter");
            }

            if (schart == "Bar graph")
            {
                return RedirectToAction("Column");
            }

            if (schart == "Pie chart")
            {
                return RedirectToAction("Pie");
            }

            if (schart == "Spline area")
            {
                return RedirectToAction("Spline");
            }

            if (schart == "Double Scatter plot")
            {
                return RedirectToAction("DSP");
            }

            if (schart == "Boxplot")
            {
                return RedirectToAction("Box");
            }
            if (schart == "3-D scatter plot")
            {

                return RedirectToAction("TDscatter");
            }
            if (schart == "Histogram")
            {

                return RedirectToAction("Histogram");
            }

            return View();
        }
        [HttpPost]
        public ActionResult SaveGraph(string imageData, string inText, string gtype, string desc)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            UserTitles ut = new UserTitles();

            ut.Name = User.Identity.Name;
            ut.Title = inText;
            ut.Type = gtype;
            ut.Description = desc;
            db.UserTitles.Add(ut);
            db.SaveChanges();
            //string fileNameWitPath = @"D:\" + DateTime.Now.ToString().Replace("/", "-").Replace(" ", "- ").Replace(":", "") + ".png";
            string filename = inText + ".png";

            string asd = Path.Combine(Server.MapPath("~/Content/Uploads/" + User.Identity.Name + "/" + gtype));
            if (!Directory.Exists(asd))
            {
                Directory.CreateDirectory(asd);
            }
            string filep = Path.Combine(Server.MapPath("~/Content/Uploads/" + User.Identity.Name + "/" + gtype), filename);
            using (FileStream fs = new FileStream(filep, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))

                {
                    byte[] data = Convert.FromBase64String(imageData);
                    bw.Write(data);
                    bw.Close();
                }
            }
            return View(gtype);
        }

        public ActionResult TDscatter()
        {
            DataTable data = (DataTable)TempData.Peek("data");

            ViewBag.datalen = data.Rows.Count;
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();

            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;
                if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            return View();
        }
        [HttpPost]
        public ActionResult TDscatter(FormCollection fc)
        {

            string xs = fc["xaxis_select"];
            string ys = fc["yaxis_select"];
            string zs = fc["zaxis_select"];
            string ls1 = fc["Label_select"];
            ViewBag.xselected = xs;
            ViewBag.yselected = ys;
            ViewBag.zselected = zs;

            DataTable data = (DataTable)TempData.Peek("data");

            ViewBag.datalen = data.Rows.Count;

            int strange = int.Parse(fc["strange"]);
            int endrange = int.Parse(fc["endrange"]);

            if (strange < 1 || strange.ToString() == null)
            {
                strange = 1;
            }
            if (endrange > ViewBag.datalen || endrange.ToString() == null)
            {
                endrange = ViewBag.datalen;
            }
            TempData["strange"] = strange;
            TempData["endrange"] = endrange;
            TempData.Keep("strange");
            TempData.Keep("endrange");

            //Result === to find dtatype of all other rows
            string[] Result;
            ArrayList re = new ArrayList();

            //Labels === label name of each collumns
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }


            //dataTypeR1 === data type of all the colllumns
            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }

                else if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }


            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;

            int xindex = -1;
            int yindex = -1;
            int zindex = -1;
            int lindex = -1;
            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].ToString() == xs)
                {
                    xindex = i;
                }
                if (ls[i].ToString() == ys)
                {
                    yindex = i;
                }
                if (ls[i].ToString() == zs)
                {
                    zindex = i;
                }
                if (ls[i].ToString() == ls1)
                {
                    lindex = i;
                }
            }

            /*for each row in data table get the above index values*/
            /*1) check if data of lindex is already in our dictionary if not create new entry*/
            /*2) as a value we will have the list of 3 lists x y and z*/
            Dictionary<string, List<List<double>>> dict = new Dictionary<string, List<System.Collections.Generic.List<double>>>();
            Dictionary<string, string> dtest = new Dictionary<string, string>();
            for (int i = strange-1; i < endrange; i++)
            {
                if (dict.ContainsKey(data.Rows[i][lindex].ToString()))
                {
                    //get this row's x y and z data
                    double tx = Double.Parse(data.Rows[i][xindex].ToString());
                    double ty = Double.Parse(data.Rows[i][yindex].ToString());
                    double tz = Double.Parse(data.Rows[i][zindex].ToString());
                    //add this data to their respective list
                    List<List<double>> temp1 = dict[data.Rows[i][lindex].ToString()];
                    temp1[0].Add(tx);
                    temp1[1].Add(ty);
                    temp1[2].Add(tz);

                    dict[data.Rows[i][lindex].ToString()] = temp1;
                }
                else
                {
                    //create a list of 3 lists
                    List<List<double>> mainl = new List<List<double>>();
                    List<double> sublist1 = new List<double>();
                    List<double> sublist2 = new List<double>();
                    List<double> sublist3 = new List<double>();
                    mainl.Add(sublist1);
                    mainl.Add(sublist2);
                    mainl.Add(sublist3);

                    //now do normal stuff again
                    double tx = Double.Parse(data.Rows[i][xindex].ToString());
                    double ty = Double.Parse(data.Rows[i][yindex].ToString());
                    double tz = Double.Parse(data.Rows[i][zindex].ToString());
                    //add this data to their respective list

                    mainl[0].Add(tx);
                    mainl[1].Add(ty);
                    mainl[2].Add(tz);

                    dict[data.Rows[i][lindex].ToString()] = mainl;
                }
            }
            //CreateActionInvoker a dictionary of key valuepair
            //key == Label selected by user
            ViewBag.tddata = dict;
            ViewBag.flagdata = true;
            return View();
        }
        public ActionResult Pca()
        {
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();

            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;
                if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            return View();
        }
        [HttpPost]
        public ActionResult Pca(FormCollection fc)
        {
            DataTable data = (DataTable)TempData.Peek("data");
          //  ViewBag.datalen = data.Rows.Count;
            int rlen = data.Rows.Count;
            int clen = data.Columns.Count;
            double[,] inidata = new double[rlen, clen];
           
            for (int i = 0; i < rlen; i++)
            {

                for (int j = 0; j < clen; j++)
                {
                    DataRow r = data.Rows[i];
                    double d;
                    try
                    {
                        d = double.Parse(r[j].ToString());
                    }
                    catch
                    {
                        d = 0;
                    }
                    inidata[i, j] = d;
                }
            }


            var pca = new PrincipalComponentAnalysis(inidata);
            pca.Compute();
            pca.Method = PrincipalComponentMethod.Standardize;
            pca.NumberOfOutputs = 3;

            double[,] finalData = pca.Transform(inidata);


            DataTable dataTable = new DataTable();
            DataColumn dc = new DataColumn("col1");
            DataColumn dc1 = new DataColumn("col2");
            DataColumn dc2 = new DataColumn("col3");

            dataTable.Columns.Add(dc);
            dataTable.Columns.Add(dc1);
            dataTable.Columns.Add(dc2);

            for (int i = 0; i < (finalData.Length / 3); i++)
            {
                DataRow dr2 = dataTable.NewRow();
                for (int j = 0; j < 3; j++)
                {
                    dr2[j] = finalData[i, j];
                }
                dataTable.Rows.Add(dr2);
            }

            var x = dataTable;



            //now after we get our final data table we will give it to view 3-d scatter plot

            //what do 3d scatter plot view require lets take his view and change as per our requirement

            string ls1 = fc["Label_select"];
            ViewBag.xselected = "x-axis";
            ViewBag.yselected = "y-axis";
            ViewBag.zselected = "z-axis";




            //Result === to find dtatype of all other rows
            string[] Result;
            ArrayList re = new ArrayList();

            //Labels === label name of each collumns
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }


            //dataTypeR1 === data type of all the colllumns
            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }

                else if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }


            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;


            int lindex = -1;
            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].ToString() == ls1)
                {
                    lindex = i;
                }
            }

            Dictionary<string, List<List<double>>> dict = new Dictionary<string, List<System.Collections.Generic.List<double>>>();
            Dictionary<string, string> dtest = new Dictionary<string, string>();
            for (int i = 0; i < data.Rows.Count; i++)
            {
                if (dict.ContainsKey(data.Rows[i][lindex].ToString()))
                {
                    //get this row's x y and z data
                    double tx = finalData[i, 0];
                    double ty = finalData[i, 1];
                    double tz = finalData[i, 2];
                    //add this data to their respective list
                    List<List<double>> temp1 = dict[data.Rows[i][lindex].ToString()];
                    temp1[0].Add(tx);
                    temp1[1].Add(ty);
                    temp1[2].Add(tz);

                    dict[data.Rows[i][lindex].ToString()] = temp1;
                }
                else
                {
                    //create a list of 3 lists
                    List<List<double>> mainl = new List<List<double>>();
                    List<double> sublist1 = new List<double>();
                    List<double> sublist2 = new List<double>();
                    List<double> sublist3 = new List<double>();
                    mainl.Add(sublist1);
                    mainl.Add(sublist2);
                    mainl.Add(sublist3);

                    //now do normal stuff again
                    double tx = finalData[i, 0];
                    double ty = finalData[i, 1];
                    double tz = finalData[i, 2];
                    //add this data to their respective list

                    mainl[0].Add(tx);
                    mainl[1].Add(ty);
                    mainl[2].Add(tz);

                    dict[data.Rows[i][lindex].ToString()] = mainl;
                }
            }
            //CreateActionInvoker a dictionary of key valuepair
            //key == Label selected by user
            ViewBag.tddata = dict;
            ViewBag.flagdata = true;



            return View();
        }
        public ActionResult Histogram()
        {
            TempData["imp"] = 1;
            TempData.Keep("imp");
            System.Data.DataTable data = (System.Data.DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;

            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            return View();
        }
        [HttpPost]
        public ActionResult Histogram(FormCollection fc)
        {
            string xs = fc["xaxis_select"];
            ViewBag.ya = xs;
            ViewBag.xa = xs;
            //now from selected chart 
            //in future we may use a non action method but now we are doing it here
            //to plot scatter plot we just have to send data points to the view
            //set a flag so that view can know if data points are set or not
            ViewBag.splotflag = true;
            ViewBag.splotflag1 = false;
            ViewBag.splotflag2 = false;
            ViewBag.splotflag3 = false;
            ViewBag.splotflag4 = false;
            ViewBag.splotflag5 = false;

            //god sake we will be having the tempdata to peek in it
            System.Data.DataTable data = (System.Data.DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;
            int strange = int.Parse(fc["strange"]);
            int endrange = int.Parse(fc["endrange"]);

            if (strange < 1 || strange.ToString() == null)
            {
                strange = 1;
            }
            if (endrange > ViewBag.datalen || endrange.ToString() == null)
            {
                endrange = ViewBag.datalen;
            }
            TempData["strange"] = strange;
            TempData["endrange"] = endrange;
            TempData.Keep("strange");
            TempData.Keep("endrange");

            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            int xindex = -1;

            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].ToString() == xs)
                {
                    xindex = i;
                }
            }
            ArrayList lst = new ArrayList();
            //loop
            for (int i = strange-1; i < endrange; i++)
            {
                ArrayList temp = new ArrayList();
                for (int w = 0; w < ls.Count; w++)
                {
                    temp.Add(data.Rows[i][ls[w].ToString()]);
                }
                //here it won't always be just temp[0] or temp[1] it totally depends on against
                //which labels user want to plot the graphs
                try
                {
                    lst.Add(temp[xindex]);
                }
                catch (Exception)
                {
                    //set err flag
                    bool perr = true;
                    //set message
                    string perrmsg = "";
                    //render view
                }

            }

            ViewBag.DataPoints = lst;
            TempData["DataPoints"] = ViewBag.DataPoints;
            TempData.Peek("DataPoints");
            TempData.Keep("DataPoints");
            return View();
        }
        public ActionResult Scatter()
        {
            TempData["imp"] = 1;
            TempData.Keep("imp");
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
           
            return View();
        }
        [HttpPost]
        public ActionResult Scatter(FormCollection fc)
        {
            string xs = fc["xaxis_select"];
            string ys = fc["yaxis_select"];
            ViewBag.xa = xs;
            ViewBag.ya = ys;
            ViewBag.con = true;
            //now from selected chart 
            //in future we may use a non action method but now we are doing it here
            //to plot scatter plot we just have to send data points to the view
            //set a flag so that view can know if data points are set or not
            ViewBag.splotflag = true;
            ViewBag.splotflag1 = false;
            ViewBag.splotflag2 = false;
            ViewBag.splotflag3 = false;
            ViewBag.splotflag4 = false;
            ViewBag.splotflag5 = false;

            //god sake we will be having the tempdata to peek in it
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;

            int strange = int.Parse(fc["strange"]);
            int endrange = int.Parse(fc["endrange"]);

            if (strange < 1 || strange.ToString() == null)
            {
                strange = 1;
            }
            if (endrange > ViewBag.datalen || endrange.ToString() == null)
            {
                endrange = ViewBag.datalen;
            }
            TempData["strange"] = strange;
            TempData["endrange"] = endrange;
            TempData.Keep("strange");
            TempData.Keep("endrange");
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            int xindex = -1;
            int yindex = -1;
            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].ToString() == xs)
                {
                    xindex = i;
                }
                if (ls[i].ToString() == ys)
                {
                    yindex = i;
                }
            }
            List<DataPoint> lst = new List<DataPoint>();
            //loop
            for (int i = strange-1; i < endrange; i++)
            {
                ArrayList temp = new ArrayList();
                for (int w = 0; w < ls.Count; w++)
                {
                    temp.Add(data.Rows[i][ls[w].ToString()]);
                }
                //here it won't always be just temp[0] or temp[1] it totally depends on against
                //which labels user want to plot the graphs
                try
                {
                    lst.Add(new DataPoint(double.Parse(temp[xindex].ToString()), double.Parse(temp[yindex].ToString())));
                }
                catch (Exception)
                {
                    //set err flag
                    bool perr = true;
                    //set message
                    string perrmsg = "";
                    //render view
                }

            }

            ViewBag.DataPoints = JsonConvert.SerializeObject(lst);
            TempData["DataPoints"] = ViewBag.DataPoints;
            TempData.Peek("DataPoints");
            TempData.Keep("DataPoints");
            return View();
        }
        public ActionResult Column()
        {
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;

            TempData["imp"] = 1;
            TempData.Keep("imp");
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            return View();
        }
        [HttpPost]
        public ActionResult Column(FormCollection fc)
        {
            string xs = fc["xaxis_select"];
            string ys = fc["yaxis_select"];
            ViewBag.xa = xs;
            ViewBag.ya = ys;
            //now from selected chart 
            //in future we may use a non action method but now we are doing it here
            //to plot scatter plot we just have to send data points to the view
            //set a flag so that view can know if data points are set or not
            ViewBag.splotflag = false;
            ViewBag.splotflag1 = true;
            ViewBag.splotflag2 = false;
            ViewBag.splotflag3 = false;
            ViewBag.splotflag4 = false;
            ViewBag.splotflag5 = false;

            //god sake we will be having the tempdata to peek in it
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;
            int strange = int.Parse(fc["strange"]);
            int endrange = int.Parse(fc["endrange"]);

            if (strange < 1 || strange.ToString() == null)
            {
                strange = 1;
            }
            if (endrange > ViewBag.datalen || endrange.ToString() == null)
            {
                endrange = ViewBag.datalen;
            }
            TempData["strange"] = strange;
            TempData["endrange"] = endrange;
            TempData.Keep("strange");
            TempData.Keep("endrange");

            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            int xindex = -1;
            int yindex = -1;
            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].ToString() == xs)
                {
                    xindex = i;
                }
                if (ls[i].ToString() == ys)
                {
                    yindex = i;
                }
            }
            List<DataPoint> lst = new List<DataPoint>();
            //loop
            for (int i = strange-1; i < endrange; i++)
            {
                ArrayList temp = new ArrayList();
                for (int w = 0; w < ls.Count; w++)
                {
                    temp.Add(data.Rows[i][ls[w].ToString()]);
                }
                //here it won't always be just temp[0] or temp[1] it totally depends on against
                //which labels user want to plot the graphs
                try
                {
                    lst.Add(new DataPoint(5 + i, double.Parse(temp[yindex].ToString()), temp[xindex].ToString()));
                }
                catch (Exception)
                {
                    //set err flag
                    bool perr = true;
                    //set message
                    string perrmsg = "";
                    //render view
                }

            }

            ViewBag.DataPoints = JsonConvert.SerializeObject(lst);
            TempData["DataPoints"] = ViewBag.DataPoints;
            TempData.Peek("DataPoints");
            TempData.Keep("DataPoints");
            return View();
        }
        public ActionResult Pie()
        {
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            return View();
        }
        [HttpPost]
        public ActionResult Pie(FormCollection fc)
        {
            string xs = fc["xaxis_select"];
            string ys = fc["yaxis_select"];
            ViewBag.xa = xs;
            ViewBag.ya = ys;
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;
            
            int strange = int.Parse(fc["strange"]);
            int endrange = int.Parse(fc["endrange"]);
            
            if (strange < 1 || strange.ToString() == null)
            {
                strange = 1;
            }
            if (endrange > ViewBag.datalen || endrange.ToString() == null)
            {
                endrange = ViewBag.datalen;
            }
            TempData["strange"] = strange;
            TempData["endrange"] = endrange;
            TempData.Keep("strange");
            TempData.Keep("endrange");
            //now from selected chart 
            //in future we may use a non action method but now we are doing it here
            //to plot scatter plot we just have to send data points to the view
            //set a flag so that view can know if data points are set or not
            ViewBag.splotflag = false;
            ViewBag.splotflag1 = false;
            ViewBag.splotflag2 = true;
            ViewBag.splotflag3 = false;
            ViewBag.splotflag4 = false;
            ViewBag.splotflag5 = false;

            //god sake we will be having the tempdata to peek in it
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            int xindex = -1;
            int yindex = -1;
            double sum = 0.0;
            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].ToString() == xs)
                {
                    xindex = i;

                }
                if (ls[i].ToString() == ys)
                {
                    yindex = i;
                    for (int w = strange - 1; w < endrange; w++)
                    {
                        sum += double.Parse(data.Rows[w][yindex].ToString());
                    }
                    // for 
                }
            }
            
            List<DataPoint> lst = new List<DataPoint>();
            //loop

            for (int i = strange - 1; i < endrange; i++)
            {
                ArrayList temp = new ArrayList();
                for (int w = 0; w < ls.Count; w++)
                {
                    temp.Add(data.Rows[i][ls[w].ToString()]);
                }
                //here it won't always be just temp[0] or temp[1] it totally depends on against
                //which labels user want to plot the graphs
                try
                {
                    lst.Add(new DataPoint(Math.Round(double.Parse(temp[yindex].ToString()) * 100 / sum, 2), temp[xindex].ToString(), temp[xindex].ToString()));
                }

                catch (Exception)
                {
                    //set err flag
                    bool perr = true;
                    //set message
                    string perrmsg = "";
                    //render view
                }

            }
            for (int i = 0; i < lst.Count; i++)
            {
                var tmp = lst[i].Label;
                for (int j = i + 1; j < lst.Count; j++)
                {
                    if (lst[j].Label == tmp)
                    {
                        lst[i].Y += lst[j].Y;
                        lst.RemoveAt(j);
                        j--;
                    }
                }
            }
            ViewBag.DataPoints = JsonConvert.SerializeObject(lst);
            TempData["DataPoints"] = ViewBag.DataPoints;
            TempData.Peek("DataPoints");
            TempData.Keep("DataPoints");
            return View();
        }
        public ActionResult Spline()
        {
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;
            TempData["imp"] = 1;
            TempData.Keep("imp");
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            return View();
        }
        [HttpPost]
        public ActionResult Spline(FormCollection fc)
        {
            string xs = fc["xaxis_select"];
            string ys = fc["yaxis_select"];
            ViewBag.xa = xs;
            ViewBag.ya = ys;
            //now from selected chart 
            //in future we may use a non action method but now we are doing it here
            //to plot scatter plot we just have to send data points to the view
            //set a flag so that view can know if data points are set or not
            ViewBag.splotflag = false;
            ViewBag.splotflag1 = false;
            ViewBag.splotflag2 = false;
            ViewBag.splotflag3 = true;
            ViewBag.splotflag4 = false;
            ViewBag.splotflag5 = false;

            //god sake we will be having the tempdata to peek in it
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;
            int strange = int.Parse(fc["strange"]);
            int endrange = int.Parse(fc["endrange"]);

            if (strange < 1 || strange.ToString() == null)
            {
                strange = 1;
            }
            if (endrange > ViewBag.datalen || endrange.ToString() == null)
            {
                endrange = ViewBag.datalen;
            }
            TempData["strange"] = strange;
            TempData["endrange"] = endrange;
            TempData.Keep("strange");
            TempData.Keep("endrange");

            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            int xindex = -1;
            int yindex = -1;
            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].ToString() == xs)
                {
                    xindex = i;
                }
                if (ls[i].ToString() == ys)
                {
                    yindex = i;
                }
            }
            List<DataPoint> lst = new List<DataPoint>();
            //loop
            for (int i = strange-1; i < endrange; i++)
            {
                ArrayList temp = new ArrayList();
                for (int w = 0; w < ls.Count; w++)
                {
                    temp.Add(data.Rows[i][ls[w].ToString()]);
                }
                //here it won't always be just temp[0] or temp[1] it totally depends on against
                //which labels user want to plot the graphs
                try
                {
                    lst.Add(new DataPoint(double.Parse(temp[xindex].ToString()), double.Parse(temp[yindex].ToString())));
                }
                catch (Exception)
                {
                    //set err flag
                    bool perr = true;
                    //set message
                    string perrmsg = "";
                    //render view
                }

            }

            ViewBag.DataPoints = JsonConvert.SerializeObject(lst);
            TempData["DataPoints"] = ViewBag.DataPoints;
            TempData.Peek("DataPoints");
            TempData.Keep("DataPoints");
            return View();
        }
        public ActionResult DSP()
        {
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;
            TempData["imp"] = 1;
            TempData.Keep("imp");
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            return View();
        }
        [HttpPost]
        public ActionResult DSP(FormCollection fc)
        {
            string xs1 = fc["xaxis_select1"];
            string ys1 = fc["yaxis_select1"];
            string xs2 = fc["xaxis_select2"];
            string ys2 = fc["yaxis_select2"];
            ViewBag.xa = xs1;
            ViewBag.ya = ys1;
            ViewBag.xa1 = xs2;
            ViewBag.ya1 = ys2;
            //now from selected chart 
            //in future we may use a non action method but now we are doing it here
            //to plot scatter plot we just have to send data points to the view
            //set a flag so that view can know if data points are set or not
            ViewBag.splotflag = false;
            ViewBag.splotflag1 = false;
            ViewBag.splotflag2 = false;
            ViewBag.splotflag3 = false;
            ViewBag.splotflag4 = true;
            ViewBag.splotflag5 = false;

            //god sake we will be having the tempdata to peek in it
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.datalen = data.Rows.Count;
            int strange = int.Parse(fc["strange"]);
            int endrange = int.Parse(fc["endrange"]);

            if (strange < 1 || strange.ToString() == null)
            {
                strange = 1;
            }
            if (endrange > ViewBag.datalen || endrange.ToString() == null)
            {
                endrange = ViewBag.datalen;
            }
            TempData["strange"] = strange;
            TempData["endrange"] = endrange;
            TempData.Keep("strange");
            TempData.Keep("endrange");

            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            int xindex1 = -1;
            int yindex1 = -1;
            int xindex2 = -1;
            int yindex2 = -1;
            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].ToString() == xs1)
                {
                    xindex1 = i;
                }
                if (ls[i].ToString() == ys1)
                {
                    yindex1 = i;
                }
                if (ls[i].ToString() == xs2)
                {
                    xindex2 = i;
                }
                if (ls[i].ToString() == ys2)
                {
                    yindex2 = i;
                }
            }
            List<DataPoint> lst = new List<DataPoint>();
            List<DataPoint> lst1 = new List<DataPoint>();
            //loop
            for (int i = strange-1; i < endrange; i++)
            {
                ArrayList temp = new ArrayList();
                for (int w = 0; w < ls.Count; w++)
                {
                    temp.Add(data.Rows[i][ls[w].ToString()]);
                }
                //here it won't always be just temp[0] or temp[1] it totally depends on against
                //which labels user want to plot the graphs
                try
                {
                    lst.Add(new DataPoint(double.Parse(temp[xindex1].ToString()), double.Parse(temp[yindex1].ToString())));
                    lst1.Add(new DataPoint(double.Parse(temp[xindex2].ToString()), double.Parse(temp[yindex2].ToString())));
                }
                catch (Exception)
                {
                    //set err flag
                    bool perr = true;
                    //set message
                    string perrmsg = "";
                    //render view
                }

            }

            ViewBag.DataPoints1 = JsonConvert.SerializeObject(lst);
            TempData["DataPoints"] = ViewBag.DataPoints1;
            TempData.Peek("DataPoints");
            TempData.Keep("DataPoints");
            ViewBag.DataPoints2 = JsonConvert.SerializeObject(lst1);
            TempData["DataPoints5"] = ViewBag.DataPoints2;
            TempData.Peek("DataPoints5");
            TempData.Keep("DataPoints5");
            return View();
        }
        public ActionResult Box()
        {
            DataTable data = (DataTable)TempData.Peek("data");
            
            ViewBag.datalen = data.Rows.Count;
            TempData["imp"] = 1;
            TempData.Keep("imp");
            //step2: just present the view and get the data from user regarding the graph he want to plot
            

            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            return View();
        }
        [HttpPost]
        public ActionResult Box(FormCollection fc)
        {
            string xs = fc["xaxis_select"];
            string ys1 = fc["yaxis_select"];
            ViewBag.xa = xs;
            ViewBag.ya = ys1;

            //now from selected chart 
            //in future we may use a non action method but now we are doing it here
            //to plot scatter plot we just have to send data points to the view
            //set a flag so that view can know if data points are set or not
            ViewBag.splotflag5 = true;

            //god sake we will be having the tempdata to peek in it
            DataTable data = (DataTable)TempData.Peek("data");
            
            ViewBag.datalen = data.Rows.Count;

            int strange = int.Parse(fc["strange"]);
            int endrange = int.Parse(fc["endrange"]);

            if (strange < 1 || strange.ToString() == null)
            {
                strange = 1;
            }
            if (endrange > ViewBag.datalen || endrange.ToString() == null)
            {
                endrange = ViewBag.datalen;
            }
            TempData["strange"] = strange;
            TempData["endrange"] = endrange;
            TempData.Keep("strange");
            TempData.Keep("endrange");
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            int xindex = -1;
            int yindex1 = -1;
            /*int yindex2 = -1;
            int yindex3 = -1;
            int yindex4 = -1;
            int yindex5 = -1;*/
            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].ToString() == xs)
                {
                    xindex = i;
                }
                if (ls[i].ToString() == ys1)
                {
                    yindex1 = i;
                }

            }

            Dictionary<string, double[]> dict = new Dictionary<string, double[]>();
            //loop
            for (int i = strange-1; i < endrange; i++)
            {
                ArrayList temp = new ArrayList();
                for (int w = 0; w < ls.Count; w++)
                {
                    temp.Add(data.Rows[i][ls[w].ToString()]);
                }
                //here it won't always be just temp[0] or temp[1] it totally depends on against
                //which labels user want to plot the graphs
                try
                {
                    //check if the value of x index is already present in dictionary
                    if (dict.ContainsKey(temp[xindex].ToString()))
                    {
                        //if key is already present then to its value we have arraywe need to add an item in there
                        List<double> temp12 = new List<double>();
                        temp12 = dict[temp[xindex].ToString()].ToList();
                        temp12.Add(double.Parse(temp[yindex1].ToString()));
                        dict[temp[xindex].ToString()] = temp12.ToArray();
                    }
                    else
                    {
                        double[] darr = { double.Parse(temp[yindex1].ToString()) };
                        dict.Add(temp[xindex].ToString(), darr);
                    }


                    /*double var1 = double.Parse(temp[yindex1].ToString());
                    double var2 = double.Parse(temp[yindex2].ToString());
                    double var3 = double.Parse(temp[yindex3].ToString());
                    double var4 = double.Parse(temp[yindex4].ToString());
                    double var5 = double.Parse(temp[yindex5].ToString());
                    lst.Add(new DataPoint1(temp[xindex].ToString(), new double[] { var1, var2, var3, var4, var5 }));*/
                }
                catch (Exception)
                {
                    //set err flag
                    bool perr = true;
                    //set message
                    string perrmsg = "";
                    //render view
                }

            }


            List<DataPoint1> lst = new List<DataPoint1>();

            //now make data points of that dictionary
            foreach (var item in dict)
            {
                //first pre process value
                double[] newvalue = { -1, -1, -1, -1, -1 };
                List<double> l = new List<double>();
                l = item.Value.ToList();
                l.Sort();
                newvalue[0] = l[0];
                newvalue[3] = l.Last();
                int median_ind = (l.Count - 1) / 2;
                try { 
                if (l.Count % 2 != 0)
                {

                    newvalue[4] = l[median_ind];
                }
                else
                {
                    newvalue[4] = (l[median_ind] + l[median_ind + 1]) / 2;
                }
            }
                catch (Exception e)
            {
                TempData["errormessage"] = "The selected column doesn't have more than #3 data for some category.";
                return Redirect("/Error");
            }
            try
                {
                    if (median_ind % 2 == 0)
                    {
                        newvalue[1] = l[median_ind / 2];
                        newvalue[2] = l[median_ind + (median_ind / 2) + 1];
                    }
                    else
                    {
                        newvalue[1] = (l[median_ind / 2] + l[(median_ind / 2) + 1]) / 2;
                        newvalue[2] = (l[median_ind + (median_ind / 2) + 1] + l[median_ind + (median_ind / 2) + 2]) / 2;
                    }
                }
                catch (Exception e) {
                    TempData["errormessage"] = "The selected column doesn't have more than #3 data for some category.";
                    return Redirect("/Error");
                }

                lst.Add(new DataPoint1(item.Key, newvalue));
            }

            ViewBag.DataPoints = JsonConvert.SerializeObject(lst);
            TempData["DataPoints"] = ViewBag.DataPoints;
            TempData.Peek("DataPoints");
            TempData.Keep("DataPoints");
            return View();
        }

        [HttpGet]
        public ActionResult Normalize()
        {
            TempData["imp"] = 1;
            TempData.Keep("imp");
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.Param = Request.QueryString["p1"];
            ViewBag.Param1 = Request.QueryString["type"];
            ViewBag.Param2 = Request.QueryString["xaxis"];
            ViewBag.Param3 = Request.QueryString["yaxis"];
            ViewBag.Param4 = Request.QueryString["xaxis1"];
            ViewBag.Param5 = Request.QueryString["yaxis1"];
            TempData["param"] = ViewBag.Param;
            string param = TempData.Peek("param").ToString();
            TempData.Keep("param");
            TempData["param1"] = ViewBag.Param1;
            TempData.Peek("param1").ToString();
            TempData.Keep("param1");
            TempData["param2"] = ViewBag.Param2;
            TempData.Peek("param2").ToString();
            TempData.Keep("param2");
            TempData["param3"] = ViewBag.Param3;
            if (TempData["param3"] != null)
                TempData.Peek("param3").ToString();
            TempData.Keep("param3");
            TempData["param4"] = ViewBag.Param4;
            if (TempData["param4"] != null)
                TempData.Peek("param4").ToString();
            TempData.Keep("param4");
            TempData["param5"] = ViewBag.Param5;
            if (TempData["param5"] != null)
                TempData.Peek("param5").ToString();
            TempData.Keep("param5");
            TempData.Keep("strange");
            TempData.Keep("endrange");
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            ViewBag.Data = data;
            return View();
        }
        [HttpPost]
        public ActionResult Normalize(FormCollection fc)
        {
            TempData.Keep("param5");
            TempData.Keep("param4");
            TempData.Keep("param3");
            TempData.Keep("param2");
            TempData.Keep("param1");
            TempData.Keep("param");
            TempData.Keep("DataPoints");
            TempData.Keep("DataPoints5");
            TempData.Keep("strange");
            TempData.Keep("endrange");
            string nmeth = fc["nmeth"];

            //now from selected chart 
            //in future we may use a non action method but now we are doing it here
            //to plot scatter plot we just have to send data points to the view
            //set a flag so that view can know if data points are set or not

            //god sake we will be having the tempdata to peek in it
            try
            {
                DataTable data = (DataTable)TempData["data"];
                TempData.Keep("data");
                DataTable data1 = new DataTable();
                data1 = data.Copy();
                //step2: just present the view and get the data from user regarding the graph he want to plot
                string[] Result;
                ArrayList re = new ArrayList();
                /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
                {
                    re.Add(dr);
                }*/
                string[] Labels;
                ArrayList ls = new ArrayList();
                foreach (DataColumn column in (data1 as System.Data.DataTable).Columns)
                {
                    ls.Add(column.ColumnName);
                }

                //I need to know the data type of all the collumns 
                //so i will iterate through all the attributes of 
                //the first row as the reference to check other rows
                for (int w = 0; w < ls.Count; w++)
                {
                    re.Add(data1.Rows[0][ls[w].ToString()]);
                }

                List<string> dataTypeR1 = new List<string>();

                foreach (var item in re)
                {
                    DateTime t1;
                    double t2;
                    int t3;

                    if (int.TryParse(item.ToString(), out t3))
                    {
                        dataTypeR1.Add("Integer");
                    }
                    else if (double.TryParse(item.ToString(), out t2))
                    {
                        dataTypeR1.Add("Double");
                    }
                    else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                    {
                        dataTypeR1.Add("Date");
                    }
                    else
                    {
                        dataTypeR1.Add("String");
                    }


                }
                string xs = TempData["param2"].ToString();
                string ys = TempData["param3"].ToString();

                ArrayList temp_a = new ArrayList();
                ArrayList temp_b = new ArrayList();

                for (int i = 0; i < data1.Rows.Count; i++)
                {
                    temp_a.Add(data1.Rows[i][xs]);
                    temp_b.Add(data1.Rows[i][ys]);
                }
                string xs1 = null;
                string ys1 = null;
                ArrayList temp_c = new ArrayList();
                ArrayList temp_d = new ArrayList();
                if (TempData["param4"] != null && TempData["param5"] != null)
                {
                    xs1 = TempData["param4"].ToString();
                    ys1 = TempData["param5"].ToString();
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        temp_c.Add(data1.Rows[i][xs1]);
                        temp_d.Add(data1.Rows[i][ys1]);
                    }
                }
                if (TempData["param1"].ToString() != "column" && TempData["param1"].ToString() != "pie" && TempData["param1"].ToString() != "boxAndWhisker" && TempData["param1"].ToString() != "histogram")
                {
                    if (nmeth == "Decimal Scaling")
                    {
                        double max = double.Parse((int.MinValue).ToString());
                        foreach (var type in temp_a)
                        {
                            if (double.Parse(type.ToString()) > max)
                            {
                                max = double.Parse(type.ToString());
                            }
                        }
                        int round = int.Parse(Decimal.Round(Decimal.Parse(max.ToString())).ToString());
                        int length = round.ToString().Length;
                        double divisor = Math.Pow(10, length);
                        double max1 = double.Parse((int.MinValue).ToString());
                        foreach (var type in temp_b)
                        {
                            if (double.Parse(type.ToString()) > max1)
                            {
                                max1 = double.Parse(type.ToString());
                            }
                        }
                        int round1 = int.Parse(Decimal.Round(Decimal.Parse(max1.ToString())).ToString());
                        int length1 = round1.ToString().Length;
                        double divisor1 = Math.Pow(10, length1);
                        ArrayList temp4 = new ArrayList();
                        ArrayList temp5 = new ArrayList();

                        if (TempData["param4"] != null && TempData["param5"] != null)
                        {

                            double max2 = double.Parse((int.MinValue).ToString());
                            foreach (var type in temp_c)
                            {
                                if (double.Parse(type.ToString()) > max2)
                                {
                                    max2 = double.Parse(type.ToString());
                                }
                            }
                            int round2 = int.Parse(Decimal.Round(Decimal.Parse(max2.ToString())).ToString());
                            int length2 = round2.ToString().Length;
                            double divisor2 = Math.Pow(10, length2);
                            double max3 = double.Parse((int.MinValue).ToString());
                            foreach (var type in temp_d)
                            {
                                if (double.Parse(type.ToString()) > max3)
                                {
                                    max3 = double.Parse(type.ToString());
                                }
                            }
                            int round3 = int.Parse(Decimal.Round(Decimal.Parse(max3.ToString())).ToString());
                            int length3 = round3.ToString().Length;
                            double divisor3 = Math.Pow(10, length3);

                            for (int i = 0; i < data1.Rows.Count; i++)
                            {
                                temp4.Add(double.Parse(data1.Rows[i][xs1].ToString()) / divisor2);
                                temp5.Add(double.Parse(data1.Rows[i][ys1].ToString()) / divisor3);
                            }
                        }
                        ArrayList temp2 = new ArrayList();
                        ArrayList temp3 = new ArrayList();

                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            temp2.Add(double.Parse(data1.Rows[i][xs].ToString()) / divisor);
                            temp3.Add(double.Parse(data1.Rows[i][ys].ToString()) / divisor1);
                        }
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            data1.Rows[i][xs] = temp2[i];
                            data1.Rows[i][ys] = temp3[i];
                            if (TempData["param4"] != null && TempData["param5"] != null)
                            {
                                data1.Rows[i][xs1] = temp4[i];
                                data1.Rows[i][ys1] = temp5[i];
                            }
                        }
                    }
                    else if (nmeth == "Min Max")
                    {

                        double max = double.Parse(int.MinValue.ToString());
                        foreach (var type in temp_a)
                        {
                            if (double.Parse(type.ToString()) > max)
                            {
                                max = double.Parse(type.ToString());
                            }
                        }
                        double min = double.Parse(int.MaxValue.ToString());
                        foreach (var type in temp_a)
                        {
                            if (double.Parse(type.ToString()) < min)
                            {
                                min = double.Parse(type.ToString());
                            }
                        }
                        double divisor = max - min;
                        double max1 = double.Parse(int.MinValue.ToString());
                        foreach (var type in temp_b)
                        {
                            if (double.Parse(type.ToString()) > max1)
                            {
                                max1 = double.Parse(type.ToString());
                            }
                        }
                        double min1 = double.Parse(int.MaxValue.ToString());
                        foreach (var type in temp_b)
                        {
                            if (double.Parse(type.ToString()) < min1)
                            {
                                min1 = double.Parse(type.ToString());
                            }
                        }
                        double divisor1 = max1 - min1;
                        ArrayList temp4 = new ArrayList();
                        ArrayList temp5 = new ArrayList();

                        if (TempData["param4"] != null && TempData["param5"] != null)
                        {
                            double max2 = double.Parse(int.MinValue.ToString());
                            foreach (var type in temp_c)
                            {
                                if (double.Parse(type.ToString()) > max2)
                                {
                                    max2 = double.Parse(type.ToString());
                                }
                            }
                            double min2 = double.Parse(int.MaxValue.ToString());
                            foreach (var type in temp_c)
                            {
                                if (double.Parse(type.ToString()) < min2)
                                {
                                    min2 = double.Parse(type.ToString());
                                }
                            }
                            double divisor2 = max2 - min2;
                            double max3 = double.Parse(int.MinValue.ToString());
                            foreach (var type in temp_d)
                            {
                                if (double.Parse(type.ToString()) > max3)
                                {
                                    max3 = double.Parse(type.ToString());
                                }
                            }
                            double min3 = double.Parse(int.MaxValue.ToString());
                            foreach (var type in temp_d)
                            {
                                if (double.Parse(type.ToString()) < min3)
                                {
                                    min3 = double.Parse(type.ToString());
                                }
                            }
                            double divisor3 = max3 - min3;
                            for (int i = 0; i < data1.Rows.Count; i++)
                            {
                                temp4.Add((double.Parse(data1.Rows[i][xs1].ToString()) - min2) / divisor2);
                                temp5.Add((double.Parse(data1.Rows[i][ys1].ToString()) - min3) / divisor3);
                            }
                        }
                        ArrayList temp2 = new ArrayList();
                        ArrayList temp3 = new ArrayList();
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            temp2.Add((double.Parse(data1.Rows[i][xs].ToString()) - min) / divisor);
                            temp3.Add((double.Parse(data1.Rows[i][ys].ToString()) - min1) / divisor1);
                        }
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            data1.Rows[i][xs] = temp2[i];
                            data1.Rows[i][ys] = temp3[i];
                            if (TempData["param4"] != null && TempData["param5"] != null)
                            {
                                data1.Rows[i][xs1] = temp4[i];
                                data1.Rows[i][ys1] = temp5[i];
                            }
                        }
                    }
                    else if (nmeth == "z score")
                    {

                        double sum = 0;
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            sum += double.Parse(data1.Rows[i][xs].ToString());
                        }
                        double mean = sum / data1.Rows.Count;
                        double stdsum = 0;
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            stdsum += Math.Pow(double.Parse(data1.Rows[i][xs].ToString()) - mean, 2);
                        }
                        double div = data1.Rows.Count - 1;
                        double stddev = Math.Sqrt(stdsum / div);
                        double divisor = stddev;
                        double sum1 = 0;
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            sum1 += double.Parse(data1.Rows[i][ys].ToString());
                        }
                        double mean1 = sum1 / data1.Rows.Count;
                        double stdsum1 = 0;
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            stdsum1 += Math.Pow(double.Parse(data1.Rows[i][ys].ToString()) - mean1, 2);
                        }
                        double div1 = data1.Rows.Count - 1;
                        double stddev1 = Math.Sqrt(stdsum1 / div1);
                        double divisor1 = stddev1;
                        ArrayList temp4 = new ArrayList();
                        ArrayList temp5 = new ArrayList();

                        if (TempData["param4"] != null && TempData["param5"] != null)
                        {
                            double sum2 = 0;
                            for (int i = 0; i < data1.Rows.Count; i++)
                            {
                                sum2 += double.Parse(data1.Rows[i][xs1].ToString());
                            }
                            double mean2 = sum2 / data1.Rows.Count;
                            double stdsum2 = 0;
                            for (int i = 0; i < data1.Rows.Count; i++)
                            {
                                stdsum2 += Math.Pow(double.Parse(data1.Rows[i][xs1].ToString()) - mean2, 2);
                            }
                            double div2 = data1.Rows.Count - 1;
                            double stddev2 = Math.Sqrt(stdsum2 / div2);
                            double divisor2 = stddev2;
                            double sum3 = 0;
                            for (int i = 0; i < data1.Rows.Count; i++)
                            {
                                sum3 += double.Parse(data1.Rows[i][ys1].ToString());
                            }
                            double mean3 = sum3 / data1.Rows.Count;
                            double stdsum3 = 0;
                            for (int i = 0; i < data1.Rows.Count; i++)
                            {
                                stdsum3 += Math.Pow(double.Parse(data1.Rows[i][ys1].ToString()) - mean3, 2);
                            }
                            double div3 = data1.Rows.Count - 1;
                            double stddev3 = Math.Sqrt(stdsum3 / div3);
                            double divisor3 = stddev3;
                            for (int i = 0; i < data1.Rows.Count; i++)
                            {
                                temp4.Add((double.Parse(data1.Rows[i][xs1].ToString()) - mean2) / divisor2);
                                temp5.Add((double.Parse(data1.Rows[i][ys1].ToString()) - mean3) / divisor3);
                            }
                        }
                        ArrayList temp2 = new ArrayList();
                        ArrayList temp3 = new ArrayList();
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            temp2.Add((double.Parse(data1.Rows[i][xs].ToString()) - mean) / divisor);
                            temp3.Add((double.Parse(data1.Rows[i][ys].ToString()) - mean1) / divisor1);
                        }
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            data1.Rows[i][xs] = temp2[i];
                            data1.Rows[i][ys] = temp3[i];
                            if (TempData["param4"] != null && TempData["param5"] != null)
                            {
                                data1.Rows[i][xs1] = temp4[i];
                                data1.Rows[i][ys1] = temp5[i];
                            }
                        }
                    }
                    ViewBag.Labels = ls;
                    ViewBag.datatype = dataTypeR1;
                    ViewBag.Types = TempData["param1"];
                }
                else
                {
                    if (nmeth == "Decimal Scaling")
                    {
                        double max1 = double.Parse((int.MinValue).ToString());
                        foreach (var type in temp_b)
                        {
                            if (double.Parse(type.ToString()) > max1)
                            {
                                max1 = double.Parse(type.ToString());
                            }
                        }
                        int round1 = int.Parse(Decimal.Round(Decimal.Parse(max1.ToString())).ToString());
                        int length1 = round1.ToString().Length;
                        double divisor1 = Math.Pow(10, length1);
                        ArrayList temp3 = new ArrayList();

                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            temp3.Add(double.Parse(data1.Rows[i][ys].ToString()) / divisor1);
                        }
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {

                            data1.Rows[i][ys] = temp3[i];

                        }
                    }
                    else if (nmeth == "Min Max")
                    {

                        double max1 = double.Parse(int.MinValue.ToString());
                        foreach (var type in temp_b)
                        {
                            if (double.Parse(type.ToString()) > max1)
                            {
                                max1 = double.Parse(type.ToString());
                            }
                        }
                        double min1 = double.Parse(int.MaxValue.ToString());
                        foreach (var type in temp_b)
                        {
                            if (double.Parse(type.ToString()) < min1)
                            {
                                min1 = double.Parse(type.ToString());
                            }
                        }
                        double divisor1 = max1 - min1;

                        ArrayList temp3 = new ArrayList();
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {

                            temp3.Add((double.Parse(data1.Rows[i][ys].ToString()) - min1) / divisor1);
                        }
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {

                            data1.Rows[i][ys] = temp3[i];

                        }
                    }
                    else if (nmeth == "z score")
                    {

                        double sum1 = 0;
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            sum1 += double.Parse(data1.Rows[i][ys].ToString());
                        }
                        double mean1 = sum1 / data1.Rows.Count;
                        double stdsum1 = 0;
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {
                            stdsum1 += Math.Pow(double.Parse(data1.Rows[i][ys].ToString()) - mean1, 2);
                        }
                        double div1 = data1.Rows.Count - 1;
                        double stddev1 = Math.Sqrt(stdsum1 / div1);
                        double divisor1 = stddev1;
                        ArrayList temp3 = new ArrayList();
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {

                            temp3.Add((double.Parse(data1.Rows[i][ys].ToString()) - mean1) / divisor1);
                        }
                        for (int i = 0; i < data1.Rows.Count; i++)
                        {

                            data1.Rows[i][ys] = temp3[i];

                        }
                    }
                    ViewBag.Labels = ls;
                    ViewBag.datatype = dataTypeR1;
                    ViewBag.Types = TempData["param1"];
                }
                List<DataPoint> lst = new List<DataPoint>();
                if (TempData["param"].ToString() == "Scatter")
                {
                    ViewBag.Sflag = 0;
                    int xindex = -1;
                    int yindex = -1;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex = i;
                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex = i;
                        }
                    }

                    //loop
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lst.Add(new DataPoint(double.Parse(temp[xindex].ToString()), double.Parse(temp[yindex].ToString())));
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }
                }
                else if (TempData["param1"].ToString() == "column")
                {
                    int xindex = -1;
                    int yindex = -1;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex = i;
                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex = i;
                        }
                    }

                    //loop
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lst.Add(new DataPoint(5 + i, double.Parse(temp[yindex].ToString()), temp[xindex].ToString()));
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }
                }
                else if (TempData["param1"].ToString() == "pie")
                {
                    int xindex = -1;
                    int yindex = -1;
                    double sum = 0.0;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex = i;

                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex = i;
                            for (int w = int.Parse(TempData["strange"].ToString()) - 1; w < int.Parse(TempData["endrange"].ToString()); w++)
                            {
                                sum += double.Parse(data1.Rows[w][yindex].ToString());
                            }
                            // for 
                        }
                    }

                    //loop

                    for (int i = int.Parse(TempData["strange"].ToString()) - 1; i < int.Parse(TempData["endrange"].ToString()); i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lst.Add(new DataPoint(Math.Round(double.Parse(temp[yindex].ToString()) * 100 / sum, 2), temp[xindex].ToString(), temp[xindex].ToString()));
                        }

                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }
                    for (int i = 0; i < lst.Count; i++)
                    {
                        var tmp = lst[i].Label;
                        for (int j = i + 1; j < lst.Count; j++)
                        {
                            if (lst[j].Label == tmp)
                            {
                                lst[i].Y += lst[j].Y;
                                lst.RemoveAt(j);
                                j--;
                            }
                        }
                    }
                }
                else if (TempData["param1"].ToString() == "splineArea")
                {
                    int xindex = -1;
                    int yindex = -1;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex = i;
                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex = i;
                        }
                    }

                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lst.Add(new DataPoint(double.Parse(temp[xindex].ToString()), double.Parse(temp[yindex].ToString())));
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }
                }
                else if (TempData["param"].ToString() == "DSP")
                {
                    ViewBag.Sflag = 1;
                    int xindex1 = -1;
                    int yindex1 = -1;
                    int xindex2 = -1;
                    int yindex2 = -1;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex1 = i;
                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex1 = i;
                        }
                        if (ls[i].ToString() == TempData["param4"].ToString())
                        {
                            xindex2 = i;
                        }
                        if (ls[i].ToString() == TempData["param5"].ToString())
                        {
                            yindex2 = i;
                        }
                    }
                    List<DataPoint> lst1 = new List<DataPoint>();
                    //loop
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lst.Add(new DataPoint(double.Parse(temp[xindex1].ToString()), double.Parse(temp[yindex1].ToString())));
                            lst1.Add(new DataPoint(double.Parse(temp[xindex2].ToString()), double.Parse(temp[yindex2].ToString())));
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }
                    ViewBag.DataPoints3 = JsonConvert.SerializeObject(lst1);
                }
                else if (TempData["param1"].ToString() == "boxAndWhisker")
                {
                    int xindex = -1;
                    int yindex1 = -1;
                    /*int yindex2 = -1;
                    int yindex3 = -1;
                    int yindex4 = -1;
                    int yindex5 = -1;*/
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex = i;
                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex1 = i;
                        }

                    }

                    Dictionary<string, double[]> dict = new Dictionary<string, double[]>();
                    //loop
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            //check if the value of x index is already present in dictionary
                            if (dict.ContainsKey(temp[xindex].ToString()))
                            {
                                //if key is already present then to its value we have arraywe need to add an item in there
                                List<double> temp12 = new List<double>();
                                temp12 = dict[temp[xindex].ToString()].ToList();
                                temp12.Add(double.Parse(temp[yindex1].ToString()));
                                dict[temp[xindex].ToString()] = temp12.ToArray();
                            }
                            else
                            {
                                double[] darr = { double.Parse(temp[yindex1].ToString()) };
                                dict.Add(temp[xindex].ToString(), darr);
                            }


                            /*double var1 = double.Parse(temp[yindex1].ToString());
                            double var2 = double.Parse(temp[yindex2].ToString());
                            double var3 = double.Parse(temp[yindex3].ToString());
                            double var4 = double.Parse(temp[yindex4].ToString());
                            double var5 = double.Parse(temp[yindex5].ToString());
                            lst.Add(new DataPoint1(temp[xindex].ToString(), new double[] { var1, var2, var3, var4, var5 }));*/
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }


                    List<DataPoint1> lst2 = new List<DataPoint1>();

                    //now make data points of that dictionary
                    foreach (var item in dict)
                    {
                        //first pre process value
                        double[] newvalue = { -1, -1, -1, -1, -1 };
                        List<double> l = new List<double>();
                        l = item.Value.ToList();
                        l.Sort();
                        newvalue[0] = l[0];
                        newvalue[3] = l.Last();
                        int median_ind = (l.Count - 1) / 2;
                        if (l.Count % 2 != 0)
                        {

                            newvalue[4] = l[median_ind];
                        }
                        else
                        {
                            newvalue[4] = (l[median_ind] + l[median_ind + 1]) / 2;
                        }

                        if (median_ind % 2 == 0)
                        {
                            newvalue[1] = l[median_ind / 2];
                            newvalue[2] = l[median_ind + (median_ind / 2) + 1];
                        }
                        else
                        {
                            newvalue[1] = (l[median_ind / 2] + l[(median_ind / 2) + 1]) / 2;
                            newvalue[2] = (l[median_ind + (median_ind / 2) + 1] + l[median_ind + (median_ind / 2) + 2]) / 2;
                        }

                        lst2.Add(new DataPoint1(item.Key, newvalue));
                    }
                    ViewBag.DataPoints4 = JsonConvert.SerializeObject(lst2);
                }
                else if (TempData["param1"].ToString() == "histogram")
                {
                    int xindex = -1;

                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == ys)
                        {
                            xindex = i;
                        }
                    }
                    ArrayList lstn = new ArrayList();
                    //loop
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lstn.Add(temp[xindex]);
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }
                        ViewBag.DataPoints6 = lstn;
                    }
                }
                ViewBag.DataPoints2 = JsonConvert.SerializeObject(lst);
                ViewBag.DataPoints1 = TempData["DataPoints"];
                ViewBag.DataPoints5 = TempData["DataPoints5"];
                return View();
            }
            catch (Exception e) {
                TempData["errormessage"] = "Normalization Not Possible";
                return Redirect("/Error/Index");
            }
        }
        [HttpPost]
        public ActionResult ReturnToGraph()
        {

            return RedirectToAction(TempData["param"].ToString());
        }
        [HttpGet]
        public ActionResult Transform()
        {
            DataTable data = (DataTable)TempData.Peek("data");
            TempData.Keep("imp");
            if (int.Parse(TempData["imp"].ToString()) == 1)
            {
                ViewBag.Param = Request.QueryString["pt1"];
                ViewBag.Param1 = Request.QueryString["typet"];
                ViewBag.Param2 = Request.QueryString["xaxist"];
                ViewBag.Param3 = Request.QueryString["yaxist"];
                ViewBag.Param4 = Request.QueryString["xaxist1"];
                ViewBag.Param5 = Request.QueryString["yaxist1"];
                TempData["param"] = ViewBag.Param;
                TempData["param1"] = ViewBag.Param1;
                TempData["param2"] = ViewBag.Param2;
                TempData["param3"] = ViewBag.Param3;
                TempData["param4"] = ViewBag.Param4;
                TempData["param5"] = ViewBag.Param5;
            }
            string param;
            if (TempData["param"] != null)
                param = TempData.Peek("param").ToString();
            TempData.Keep("param");

            if (TempData["param1"] != null)
                TempData.Peek("param1").ToString();
            TempData.Keep("param1");

            if (TempData["param2"] != null)
                TempData.Peek("param2").ToString();
            TempData.Keep("param2");

            if (TempData["param3"] != null)
                TempData.Peek("param3").ToString();
            TempData.Keep("param3");

            if (TempData["param4"] != null)
                TempData.Peek("param4").ToString();
            TempData.Keep("param4");

            if (TempData["param5"] != null)
                TempData.Peek("param5").ToString();
            TempData.Keep("param5");

            TempData.Keep("strange");
            TempData.Keep("endrange");
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ls = new ArrayList();
            if (TempData["param2"] != null)
                ls.Add(TempData["param2"]);
            if (TempData["param3"] != null)
                ls.Add(TempData["param3"]);
            if (TempData["param4"] != null)
                ls.Add(TempData["param4"]);
            if (TempData["param5"] != null)
                ls.Add(TempData["param5"]);


            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            ViewBag.Types = TempData["param1"];
            return View();
        }
        [HttpPost]
        public ActionResult Transform(FormCollection fc)
        {
            if (ViewBag.Flag != null)
            {
                TempData.Keep("param5");
                TempData.Keep("param4");
                TempData.Keep("param3");
                TempData.Keep("param2");
                TempData.Keep("param1");
                TempData.Keep("param");
                TempData.Keep("imp");
                TempData.Keep("DataPoints");
                TempData.Keep("DataPoints5");
                TempData.Keep("strange");
                TempData.Keep("endrange");
                ViewBag.Flag = null;
                DataTable data = (DataTable)TempData["data"];
                TempData.Keep("data");
                DataTable data1 = new DataTable();
                data1 = data.Copy();
                //step2: just present the view and get the data from user regarding the graph he want to plot
                string[] Result;
                ArrayList re = new ArrayList();
                /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
                {
                    re.Add(dr);
                }*/
                string[] Labels;
                ArrayList ls = new ArrayList();
                foreach (DataColumn column in (data1 as System.Data.DataTable).Columns)
                {
                    ls.Add(column.ColumnName);
                }

                //I need to know the data type of all the collumns 
                //so i will iterate through all the attributes of 
                //the first row as the reference to check other rows
                for (int w = 0; w < ls.Count; w++)
                {
                    re.Add(data1.Rows[0][ls[w].ToString()]);
                }

                List<string> dataTypeR1 = new List<string>();

                foreach (var item in re)
                {
                    DateTime t1;
                    double t2;
                    int t3;

                    if (int.TryParse(item.ToString(), out t3))
                    {
                        dataTypeR1.Add("Integer");
                    }
                    else if (double.TryParse(item.ToString(), out t2))
                    {
                        dataTypeR1.Add("Double");
                    }
                    else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                    {
                        dataTypeR1.Add("Date");
                    }
                    else
                    {
                        dataTypeR1.Add("String");
                    }


                }
                ls = new ArrayList();
                if (TempData["param2"] != null)
                    ls.Add(TempData["param2"]);
                if (TempData["param3"] != null)
                    ls.Add(TempData["param3"]);
                if (TempData["param4"] != null)
                    ls.Add(TempData["param4"]);
                if (TempData["param5"] != null)
                    ls.Add(TempData["param5"]);


                ViewBag.Labels = ls;
                ViewBag.datatype = dataTypeR1;
                ViewBag.Types = TempData["param1"];

            }
            else
            {
                TempData.Keep("param5");
                TempData.Keep("param4");
                TempData.Keep("param3");
                TempData.Keep("param2");
                TempData.Keep("param1");
                TempData.Keep("param");
                TempData.Keep("DataPoints");
                TempData.Keep("DataPoints5");
                TempData.Keep("strange");
                TempData.Keep("endrange");
                string ptmeth = fc["ptmeth"];
                string pnmeth = fc["pnmeth"];
                string pometh = fc["pometh"];

                //now from selected chart 
                //in future we may use a non action method but now we are doing it here
                //to plot scatter plot we just have to send data points to the view
                //set a flag so that view can know if data points are set or not

                //god sake we will be having the tempdata to peek in it
                DataTable data = (DataTable)TempData["data"];
                TempData.Keep("data");
                DataTable data1 = new DataTable();
                data1 = data.Copy();
                //step2: just present the view and get the data from user regarding the graph he want to plot
                string[] Result;
                ArrayList re = new ArrayList();
                /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
                {
                    re.Add(dr);
                }*/
                string[] Labels;
                ArrayList ls = new ArrayList();
                foreach (DataColumn column in (data1 as System.Data.DataTable).Columns)
                {
                    ls.Add(column.ColumnName);
                }

                //I need to know the data type of all the collumns 
                //so i will iterate through all the attributes of 
                //the first row as the reference to check other rows
                for (int w = 0; w < ls.Count; w++)
                {
                    re.Add(data1.Rows[0][ls[w].ToString()]);
                }

                List<string> dataTypeR1 = new List<string>();

                foreach (var item in re)
                {
                    DateTime t1;
                    double t2;
                    int t3;

                    if (int.TryParse(item.ToString(), out t3))
                    {
                        dataTypeR1.Add("Integer");
                    }
                    else if (double.TryParse(item.ToString(), out t2))
                    {
                        dataTypeR1.Add("Double");
                    }
                    else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                    {
                        dataTypeR1.Add("Date");
                    }
                    else
                    {
                        dataTypeR1.Add("String");
                    }


                }
                if (ptmeth == "croot" || pnmeth == "croot")
                {
                    string xs = TempData["field"].ToString();

                    ArrayList temp1 = new ArrayList();
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        double x = double.Parse(data1.Rows[i][xs].ToString());
                        if (x < 0)
                            temp1.Add(-Math.Pow(-x, 1d / 3d));
                        else
                            temp1.Add(Math.Pow(x, 1d / 3d));
                    }
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        data1.Rows[i][xs] = temp1[i];
                    }
                }
                else if (ptmeth == "log" || pnmeth == "log")
                {
                    string xs = TempData["field"].ToString();

                    ArrayList temp1 = new ArrayList();
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        if (double.Parse(data1.Rows[i][xs].ToString()) > 0)
                            temp1.Add(Math.Log(double.Parse(data1.Rows[i][xs].ToString())));
                        else
                            temp1.Add(double.Parse(data1.Rows[i][xs].ToString()));
                    }
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        data1.Rows[i][xs] = temp1[i];
                    }
                }
                else if (ptmeth == "sroot")
                {
                    string xs = TempData["field"].ToString();

                    ArrayList temp1 = new ArrayList();
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        if (double.Parse(data1.Rows[i][xs].ToString()) >= 0)
                            temp1.Add(Math.Sqrt(double.Parse(data1.Rows[i][xs].ToString())));
                    }
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        data1.Rows[i][xs] = temp1[i];
                    }
                }
                else if (pnmeth == "sr")
                {
                    string xs = TempData["field"].ToString();

                    ArrayList temp1 = new ArrayList();
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        temp1.Add(Math.Pow(double.Parse(data1.Rows[i][xs].ToString()), 2));
                    }
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        data1.Rows[i][xs] = temp1[i];
                    }
                }
                else if (ptmeth == "rol" || pnmeth == "rol" || pometh == "rol")
                {
                    string xs = TempData["field"].ToString();
                    List<decimal> temp = new List<decimal>();
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        temp.Add(decimal.Parse(data1.Rows[i][xs].ToString()));
                    }
                    List<decimal> nt = new List<decimal>();
                    nt = temp;
                    int n = nt.Count;
                    int k, j;
                    decimal tem;
                    bool swapped;
                    for (k = 0; k < n - 1; k++)
                    {
                        swapped = false;
                        for (j = 0; j < n - k - 1; j++)
                        {
                            if (double.Parse(nt[j].ToString()) > double.Parse(nt[j + 1].ToString()))
                            {
                                // swap arr[j] and arr[j+1] 
                                tem = decimal.Parse(nt[j].ToString());
                                nt[j] = nt[j + 1];
                                nt[j + 1] = tem;
                                swapped = true;
                            }
                        }
                        if (swapped == false)
                            break;
                    }
                    List<decimal> q1 = new List<decimal>();
                    List<decimal> q3 = new List<decimal>();
                    decimal IQR;
                    if (ArrayListSize(nt) % 2 == 0)
                    {
                        for (int i = 0; i < nt.Count / 2; i++)
                        {
                            q1.Add(nt[i]);
                        }
                        for (int i = nt.Count / 2; i < nt.Count; i++)
                        {
                            q3.Add(nt[i]);
                        }
                        decimal Q1 = FindMedian(q1);
                        decimal Q3 = FindMedian(q3);
                        IQR = Q3 - Q1;
                    }
                    else
                    {
                        decimal lim = FindMedian(nt);
                        foreach (var item in nt)
                        {
                            if (decimal.Parse(item.ToString()) < lim)
                            {
                                q1.Add(item);
                            }
                            else if (decimal.Parse(item.ToString()) > lim)
                            {
                                q3.Add(item);
                            }
                        }
                        decimal Q1 = FindMedian(q1);
                        decimal Q3 = FindMedian(q3);
                        IQR = Q3 - Q1;
                    }
                    decimal maxRange = (1.3m * IQR);
                    decimal minRange = -(1.5m * IQR);
                    decimal sum = 0;
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        sum += decimal.Parse(data1.Rows[i][xs].ToString());
                    }
                    decimal mean = sum / data1.Rows.Count;
                    foreach (decimal furthestTo in nt)
                    {

                        if (Math.Abs(furthestTo - mean) > maxRange || Math.Abs(furthestTo - mean) < minRange)
                        {
                            ViewBag.ins = 1;
                            DataRow[] result = data1.Select(xs + "= '" + furthestTo + "'");
                            foreach (DataRow row in result)
                            {
                                if (row[xs].ToString().Trim().ToUpper().Contains(furthestTo.ToString()))
                                    data1.Rows.Remove(row);
                            }
                            ViewBag.outlier = "Outlier: " + furthestTo + " removed.";
                        }
                        else
                        {
                            ViewBag.outlier = "No Outlier.";
                            ViewBag.ins = 0;
                        }
                    }
                    
                }


                ViewBag.datatype = dataTypeR1;
                ViewBag.Types = TempData["param1"];
                List<DataPoint> lst = new List<DataPoint>();
                if (TempData["param"].ToString() == "Scatter")
                {
                    ViewBag.Sflag = 0;
                    int xindex = -1;
                    int yindex = -1;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex = i;
                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex = i;
                        }
                    }

                    //loop
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lst.Add(new DataPoint(double.Parse(temp[xindex].ToString()), double.Parse(temp[yindex].ToString())));
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }
                }
                else if (TempData["param1"].ToString() == "column")
                {
                    int xindex = -1;
                    int yindex = -1;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex = i;
                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex = i;
                        }
                    }

                    //loop
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lst.Add(new DataPoint(5 + i, double.Parse(temp[yindex].ToString()), temp[xindex].ToString()));
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }
                }
                else if (TempData["param1"].ToString() == "pie")
                {
                    int xindex = -1;
                    int yindex = -1;
                    double sum = 0.0;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex = i;
                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex = i;
                            for (int w = int.Parse(TempData["strange"].ToString()) - 1; w < int.Parse(TempData["endrange"].ToString())-1; w++)
                            {
                                sum += double.Parse(data1.Rows[w][yindex].ToString());
                            }
                            // for 
                        }
                    }

                    //loop

                    for (int i = int.Parse(TempData["strange"].ToString()) - 1; i < int.Parse(TempData["endrange"].ToString()); i++)
                    {
                        ArrayList temp = new ArrayList();

                        for (int w = 0; w < ls.Count; w++)
                        {
                            try
                            {
                                temp.Add(data1.Rows[i][ls[w].ToString()]);
                            }
                            catch (Exception e) {
                                continue;
                            }
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lst.Add(new DataPoint(Math.Round(double.Parse(temp[yindex].ToString()) * 100 / sum, 2), temp[xindex].ToString(), temp[xindex].ToString()));
                        }

                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }
                    for (int i = 0; i < lst.Count; i++)
                    {
                        var tmp = lst[i].Label;
                        for (int j = i + 1; j < lst.Count; j++)
                        {
                            if (lst[j].Label == tmp)
                            {
                                lst[i].Y += lst[j].Y;
                                lst.RemoveAt(j);
                                j--;
                            }
                        }
                    }
                }
                else if (TempData["param1"].ToString() == "splineArea")
                {
                    int xindex = -1;
                    int yindex = -1;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex = i;
                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex = i;
                        }
                    }

                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lst.Add(new DataPoint(double.Parse(temp[xindex].ToString()), double.Parse(temp[yindex].ToString())));
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }
                }
                else if (TempData["param"].ToString() == "DSP")
                {
                    ViewBag.Sflag = 1;
                    int xindex1 = -1;
                    int yindex1 = -1;
                    int xindex2 = -1;
                    int yindex2 = -1;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex1 = i;
                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex1 = i;
                        }
                        if (ls[i].ToString() == TempData["param4"].ToString())
                        {
                            xindex2 = i;
                        }
                        if (ls[i].ToString() == TempData["param5"].ToString())
                        {
                            yindex2 = i;
                        }
                    }
                    List<DataPoint> lst1 = new List<DataPoint>();
                    //loop
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lst.Add(new DataPoint(double.Parse(temp[xindex1].ToString()), double.Parse(temp[yindex1].ToString())));
                            lst1.Add(new DataPoint(double.Parse(temp[xindex2].ToString()), double.Parse(temp[yindex2].ToString())));
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }
                    ViewBag.DataPoints3 = JsonConvert.SerializeObject(lst1);
                }
                else if (TempData["param1"].ToString() == "boxAndWhisker")
                {
                    int xindex = -1;
                    int yindex1 = -1;
                    /*int yindex2 = -1;
                    int yindex3 = -1;
                    int yindex4 = -1;
                    int yindex5 = -1;*/
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param2"].ToString())
                        {
                            xindex = i;
                        }
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            yindex1 = i;
                        }

                    }

                    Dictionary<string, double[]> dict = new Dictionary<string, double[]>();
                    //loop
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            //check if the value of x index is already present in dictionary
                            if (dict.ContainsKey(temp[xindex].ToString()))
                            {
                                //if key is already present then to its value we have arraywe need to add an item in there
                                List<double> temp12 = new List<double>();
                                temp12 = dict[temp[xindex].ToString()].ToList();
                                temp12.Add(double.Parse(temp[yindex1].ToString()));
                                dict[temp[xindex].ToString()] = temp12.ToArray();
                            }
                            else
                            {
                                double[] darr = { double.Parse(temp[yindex1].ToString()) };
                                dict.Add(temp[xindex].ToString(), darr);
                            }


                            /*double var1 = double.Parse(temp[yindex1].ToString());
                            double var2 = double.Parse(temp[yindex2].ToString());
                            double var3 = double.Parse(temp[yindex3].ToString());
                            double var4 = double.Parse(temp[yindex4].ToString());
                            double var5 = double.Parse(temp[yindex5].ToString());
                            lst.Add(new DataPoint1(temp[xindex].ToString(), new double[] { var1, var2, var3, var4, var5 }));*/
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }

                    }


                    List<DataPoint1> lst2 = new List<DataPoint1>();

                    //now make data points of that dictionary
                    foreach (var item in dict)
                    {
                        //first pre process value
                        double[] newvalue = { -1, -1, -1, -1, -1 };
                        List<double> l = new List<double>();
                        l = item.Value.ToList();
                        l.Sort();
                        newvalue[0] = l[0];
                        newvalue[3] = l.Last();
                        int median_ind = (l.Count - 1) / 2;
                        if (l.Count % 2 != 0)
                        {

                            newvalue[4] = l[median_ind];
                        }
                        else
                        {
                            newvalue[4] = (l[median_ind] + l[median_ind + 1]) / 2;
                        }

                        if (median_ind % 2 == 0)
                        {
                            newvalue[1] = l[median_ind / 2];
                            newvalue[2] = l[median_ind + (median_ind / 2) + 1];
                        }
                        else
                        {
                            newvalue[1] = (l[median_ind / 2] + l[(median_ind / 2) + 1]) / 2;
                            newvalue[2] = (l[median_ind + (median_ind / 2) + 1] + l[median_ind + (median_ind / 2) + 2]) / 2;
                        }

                        lst2.Add(new DataPoint1(item.Key, newvalue));
                    }
                    ViewBag.DataPoints4 = JsonConvert.SerializeObject(lst2);
                }
                else if (TempData["param1"].ToString() == "histogram")
                {
                    int xindex = -1;

                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (ls[i].ToString() == TempData["param3"].ToString())
                        {
                            xindex = i;
                        }
                    }
                    ArrayList lstn = new ArrayList();
                    //loop
                    for (int i = 0; i < data1.Rows.Count; i++)
                    {
                        ArrayList temp = new ArrayList();
                        for (int w = 0; w < ls.Count; w++)
                        {
                            temp.Add(data1.Rows[i][ls[w].ToString()]);
                        }
                        //here it won't always be just temp[0] or temp[1] it totally depends on against
                        //which labels user want to plot the graphs
                        try
                        {
                            lstn.Add(temp[xindex]);
                        }
                        catch (Exception)
                        {
                            //set err flag
                            bool perr = true;
                            //set message
                            string perrmsg = "";
                            //render view
                        }
                        ViewBag.DataPoints6 = lstn;
                    }
                }
                ViewBag.DataPoints2 = JsonConvert.SerializeObject(lst);
                ViewBag.DataPoints1 = TempData["DataPoints"];
                ViewBag.DataPoints5 = TempData["DataPoints5"];
                ls = new ArrayList();
                if (TempData["param2"] != null)
                    ls.Add(TempData["param2"]);
                if (TempData["param3"] != null)
                    ls.Add(TempData["param3"]);
                if (TempData["param4"] != null)
                    ls.Add(TempData["param4"]);
                if (TempData["param5"] != null)
                    ls.Add(TempData["param5"]);


                ViewBag.Labels = ls;
                ViewBag.datatype = dataTypeR1;
                ViewBag.Types = TempData["param1"];
            }
            return View();
        }
        [NonAction]
        public decimal FindMedian(List<decimal> nt)
        {
            decimal middle = nt.Count / 2;
            if (nt.Count % 2 == 0)
            {
                middle = (decimal.Parse(nt[nt.Count / 2].ToString()) + decimal.Parse(nt[nt.Count / 2 - 1].ToString())) / 2;
            }
            else
            {
                middle = decimal.Parse(nt[nt.Count / 2].ToString());
            }
            return middle;
        }
        [NonAction]
        public int ArrayListSize(List<decimal> nt)
        {
            return nt.Count;
        }
        [HttpPost]
        public ActionResult GetSkew(FormCollection fc)
        {
            TempData.Keep("param5");
            TempData.Keep("param4");
            TempData.Keep("param3");
            TempData.Keep("param2");
            TempData.Keep("param1");
            TempData.Keep("param");
            TempData.Keep("DataPoints");
            TempData.Keep("DataPoints5");
            TempData["imp"] = 0;
            TempData.Keep("imp");
            string field = fc["xaxis_select"];

            //now from selected chart 
            //in future we may use a non action method but now we are doing it here
            //to plot scatter plot we just have to send data points to the view
            //set a flag so that view can know if data points are set or not

            //god sake we will be having the tempdata to peek in it
            DataTable data = (DataTable)TempData.Peek("data");

            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }
            }
            ArrayList temp = new ArrayList();
            for (int i = 0; i < data.Rows.Count; i++)
            {
                temp.Add(data.Rows[i][field]);
            }
            decimal sum = 0;
            try
            {
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    sum += decimal.Parse(data.Rows[i][field].ToString());
                }
            }
            catch (Exception e) {
                TempData["errormessage"] = "The selected column cannot be transformed.";
                return Redirect("/Error");
            }
            decimal mean = sum / data.Rows.Count;
            ArrayList nt = new ArrayList();
            nt = temp;
            int n = nt.Count;
            int k, j;
            double tem;
            bool swapped;
            for (k = 0; k < n - 1; k++)
            {
                swapped = false;
                for (j = 0; j < n - k - 1; j++)
                {
                    if (double.Parse(nt[j].ToString()) > double.Parse(nt[j + 1].ToString()))
                    {
                        // swap arr[j] and arr[j+1] 
                        tem = double.Parse(nt[j].ToString());
                        nt[j] = nt[j + 1];
                        nt[j + 1] = tem;
                        swapped = true;
                    }
                }

                // IF no two elements were  
                // swapped by inner loop, then break 
                if (swapped == false)
                    break;
            }
            TempData["t"] = nt;
            TempData.Peek("t");
            TempData.Keep("t");
            decimal middle = nt.Count / 2;
            if (nt.Count % 2 == 0)
            {
                middle = (decimal.Parse(nt[nt.Count / 2].ToString()) + decimal.Parse(nt[nt.Count / 2 - 1].ToString())) / 2;
            }
            else
            {
                middle = decimal.Parse(nt[nt.Count / 2].ToString());
            }
            string skew;
            if (middle < mean)
            {
                skew = "Pskew";
            }
            else if (middle > mean)
            {
                skew = "Nskew";
            }
            else
            {
                skew = "Symmetric";
            }
            ViewBag.Flag = 1;
            TempData["skewtype"] = skew;
            TempData.Peek("skewtype");
            TempData.Keep("skewtype");
            TempData["mid"] = middle;
            TempData.Peek("mid");
            TempData.Keep("mid");
            TempData["m"] = mean;
            TempData.Peek("m");
            TempData.Keep("m");
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            ViewBag.Field = field;
            TempData["field"] = ViewBag.Field;
            TempData.Peek("field");
            TempData.Keep("field");
            return RedirectToAction("Transform");
        }
        [HttpGet]
        public ActionResult Description()
        {
            DataTable data = (DataTable)TempData.Peek("data");
            ViewBag.Param = Request.QueryString["p1"];
            TempData["param"] = ViewBag.Param;
            string param = TempData.Peek("param").ToString();
            TempData.Keep("param");
            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;
            return View();
        }
        [HttpPost]
        public ActionResult Description(FormCollection fc)
        {

            string xs = fc["xaxis_select"];

            //now from selected chart 
            //in future we may use a non action method but now we are doing it here
            //to plot scatter plot we just have to send data points to the view
            //set a flag so that view can know if data points are set or not

            //god sake we will be having the tempdata to peek in it
            DataTable data = (DataTable)TempData.Peek("data");

            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }
            ArrayList temp = new ArrayList();
            for (int i = 0; i < data.Rows.Count; i++)
            {

                temp.Add(data.Rows[i][xs]);
            }
            double max = double.Parse(int.MinValue.ToString());
            foreach (var type in temp)
            {
                if (double.Parse(type.ToString()) > max)
                {
                    max = double.Parse(type.ToString());
                }
            }
            double min = double.Parse(int.MaxValue.ToString());
            foreach (var type in temp)
            {
                if (double.Parse(type.ToString()) < min)
                {
                    min = double.Parse(type.ToString());
                }
            }

            double sum = 0;
            for (int i = 0; i < data.Rows.Count; i++)
            {
                sum += double.Parse(data.Rows[i][xs].ToString());
            }
            double mean = sum / data.Rows.Count;
            double stdsum = 0;
            for (int i = 0; i < data.Rows.Count; i++)
            {
                stdsum += Math.Pow(double.Parse(data.Rows[i][xs].ToString()) - mean, 2);
            }
            double div = data.Rows.Count - 1;
            double stddev = Math.Sqrt(stdsum / div);
            List<decimal> temps = new List<decimal>();
            for (int i = 0; i < data.Rows.Count; i++)
            {
                temps.Add(decimal.Parse(data.Rows[i][xs].ToString()));
            }
            List<decimal> nt = new List<decimal>();
            nt = temps;
            int n = nt.Count;
            int k, j;
            decimal tem;
            bool swapped;
            for (k = 0; k < n - 1; k++)
            {
                swapped = false;
                for (j = 0; j < n - k - 1; j++)
                {
                    if (double.Parse(nt[j].ToString()) > double.Parse(nt[j + 1].ToString()))
                    {
                        // swap arr[j] and arr[j+1] 
                        tem = decimal.Parse(nt[j].ToString());
                        nt[j] = nt[j + 1];
                        nt[j + 1] = tem;
                        swapped = true;
                    }
                }
                if (swapped == false)
                    break;
            }
            List<decimal> q1 = new List<decimal>();
            List<decimal> q3 = new List<decimal>();
            List<decimal> q2 = new List<decimal>();
            decimal IQR, Q1, Q3;
            if (ArrayListSize(nt) % 2 == 0)
            {
                for (int i = 0; i < nt.Count / 2; i++)
                {
                    q1.Add(nt[i]);
                }
                for (int i = nt.Count / 2; i < nt.Count; i++)
                {
                    q3.Add(nt[i]);
                }
                Q1 = FindMedian(q1);
                Q3 = FindMedian(q3);
                IQR = Q3 - Q1;
            }
            else
            {
                decimal lim = FindMedian(nt);
                foreach (var item in nt)
                {
                    if (decimal.Parse(item.ToString()) < lim)
                    {
                        q1.Add(item);
                    }
                    else if (decimal.Parse(item.ToString()) > lim)
                    {
                        q3.Add(item);
                    }
                    else if (decimal.Parse(item.ToString()) == lim)
                    {
                        q2.Add(item);
                    }
                }
                Q1 = FindMedian(q1);
                Q3 = FindMedian(q3);
                IQR = Q3 - Q1;
            }
            decimal middle = nt.Count / 2;
            if (nt.Count % 2 == 0)
            {
                middle = (decimal.Parse(nt[nt.Count / 2].ToString()) + decimal.Parse(nt[nt.Count / 2 - 1].ToString())) / 2;
            }
            else
            {
                middle = decimal.Parse(nt[nt.Count / 2].ToString());
            }
            string skew;
            if (middle < decimal.Parse(mean.ToString()))
            {
                skew = "Positive Skew";
            }
            else if (middle > decimal.Parse(mean.ToString()))
            {
                skew = "Negative Skew";
            }
            else
            {
                skew = "Symmetric";
            }
            DataTable desc = new DataTable();
            desc.Columns.Add("Field");
            desc.Columns.Add("Value");
            desc.Rows.Add(new Object[] { "MAX", max });
            desc.Rows.Add(new Object[] { "MIN", min });
            desc.Rows.Add(new Object[] { "MEAN", mean });
            desc.Rows.Add(new Object[] { "STANDARD DEVIATION", stddev });
            desc.Rows.Add(new Object[] { "MEDIAN", middle });
            desc.Rows.Add(new Object[] { "QUARTILE-1", Q1 });
            desc.Rows.Add(new Object[] { "QUARTILE-3", Q3 });
            desc.Rows.Add(new Object[] { "INTER QUARTILE RANGE", IQR });
            desc.Rows.Add(new Object[] { "SKEWNESS", skew });
            ViewBag.Labels = ls;
            ViewBag.datatype = dataTypeR1;

            ViewBag.Desc = desc;


            return View();
        }
        [HttpGet]
        public ActionResult Correlation()
        {
            TempData["imp"] = 1;
            TempData.Keep("imp");
            System.Data.DataTable data = (System.Data.DataTable)TempData.Peek("data");

            //step2: just present the view and get the data from user regarding the graph he want to plot
            string[] Result;
            ArrayList re = new ArrayList();
            /*foreach (DataRow dr in (ViewBag.Data as System.Data.DataTable).Rows)
            {
                re.Add(dr);
            }*/
            string[] Labels;
            ArrayList ls = new ArrayList();
            foreach (DataColumn column in (data as System.Data.DataTable).Columns)
            {
                ls.Add(column.ColumnName);
            }

            //I need to know the data type of all the collumns 
            //so i will iterate through all the attributes of 
            //the first row as the reference to check other rows
            for (int w = 0; w < ls.Count; w++)
            {
                re.Add(data.Rows[0][ls[w].ToString()]);
            }

            List<string> dataTypeR1 = new List<string>();

            foreach (var item in re)
            {
                DateTime t1;
                double t2;
                int t3;

                if (int.TryParse(item.ToString(), out t3))
                {
                    dataTypeR1.Add("Integer");
                }
                else if (double.TryParse(item.ToString(), out t2))
                {
                    dataTypeR1.Add("Double");
                }
                else if (DateTime.TryParse(item.ToString(), CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out t1))
                {
                    dataTypeR1.Add("Date");
                }
                else
                {
                    dataTypeR1.Add("String");
                }


            }

            ViewBag.datatype = dataTypeR1;
            ArrayList ls1 = new ArrayList();
            for (int i = 0; i < ls.Count; i++)
            {
                if (ViewBag.datatype[i] == "Integer" || ViewBag.datatype[i] == "Double")
                {
                    ls1.Add(ls[i]);
                }
            }
            ViewBag.Labels = ls1;
            List<double>[] temp = new List<double>[ls1.Count];
            for (int i = 0; i < temp.Length; i++)
            {
                temp[i] = new List<double>();
            }

            /*List<decimal>[] tr = new List<decimal>[ls1.Count];
            for (int j = 0; j < tr.Length; j++)
            {
                tr[j] = new List<decimal>();
            }*/
            for (int w = 0; w < ls1.Count; w++)
            {
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    temp[w].Add(double.Parse(data.Rows[i][ls1[w].ToString()].ToString()));
                }
            }

            ArrayList final = new ArrayList();
            for (int l1 = 0; l1 < ls1.Count; l1++)
            {
                for (int l2 = 0; l2 < ls1.Count; l2++)
                {
                    var values1 = temp[l1];
                    var values2 = temp[l2];


                    if (values1.Count != values2.Count)
                        throw new ArgumentException("values must be the same length");

                    var avg1 = values1.Average();
                    var avg2 = values2.Average();

                    var sum1 = values1.Zip(values2, (x1, y1) => (x1 - avg1) * (y1 - avg2)).Sum();

                    var sumSqr1 = values1.Sum(x => Math.Pow((x - avg1), 2.0));
                    var sumSqr2 = values2.Sum(y => Math.Pow((y - avg2), 2.0));

                    var result = sum1 / Math.Sqrt(sumSqr1 * sumSqr2);

                    final.Add(result);
                }
            }
            ViewBag.Data = final;
            ViewBag.Coun = ls1.Count;
            return View();
        }
    }
}