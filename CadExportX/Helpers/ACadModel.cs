using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Serialization;
using Exc = Microsoft.Office.Interop.Excel;
using Reg = Microsoft.Win32;
using wf = System.Windows.Forms;

namespace ModelSpace
{
    public class ACadModel : IExtensionApplication
    {
        // Registry
        public string RegKeyName = "HKEY_CURRENT_USER" + "\\" + "OBJECT_TOOLS_SETT";

        public string RegProjectPath = "PROJ_PATH";

        //LOGS
        public string LogsFolderPath = $"{Environment.CurrentDirectory}\\Logs\\";

        public string LogFormat = "yyyy-dd-M--HH-mm-ss";

        public ACadViewModel ViewModel = null;

        // Cancel Token
        public CancellationTokenSource tokenSource = new CancellationTokenSource();

        public ObservableCollection<PageInfo> PageInfoList { get; set; }

        public ObservableCollection<Settings> SettList { get; set; }

        private Boolean IsElectrical { get; set; }
        private string ElectricalProjPath { get; set; }
        private List<Tuple<string, string>> ElectricalPaths { get; set; }

        public delegate void Message(string msg);

        public event Message Mess;

        private static Control syncCtrl;

        public void Initialize()
        {
            try
            {
                // Initialize syncCtrl FIRST before creating any WPF elements
                if (syncCtrl == null)
                    syncCtrl = new Control();

                System.Diagnostics.Debug.WriteLine("=== TK Helper: syncCtrl initialized ===");

                PageInfoList = new ObservableCollection<PageInfo>();
                SettList = new ObservableCollection<Settings>();

                System.Diagnostics.Debug.WriteLine("=== TK Helper: Creating ViewModel ===");
                ViewModel = new ACadViewModel(this);

                System.Diagnostics.Debug.WriteLine("=== TK Helper: ViewModel created successfully ===");

                Mess += AutoCadCablePlug_Mess;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== TK Helper ERROR: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nTK Helper Error: {ex.Message}\n");
            }
        }

        public void Terminate()
        {
            Mess -= AutoCadCablePlug_Mess;

            // Saving data
            string s = Reg.Registry.GetValue(RegKeyName, RegProjectPath, string.Empty).ToString();
            if (!string.IsNullOrEmpty(s) && PageInfoList.Count > 0)
                SaveDatabase(s + "Database.xml");
        }

        public async Task DatabaseUpdate(string pr)
        {
            try
            {
                CloseDocuments();
                ShowWait();

                #region Electrical Prepartion

                List<Tuple<string, string>> dwgs = new List<Tuple<string, string>>();
                foreach (var i in Directory.GetFiles(pr, "*.dwg", SearchOption.AllDirectories))
                    dwgs.Add(new Tuple<string, string>(i, Path.GetFileName(Path.GetDirectoryName(i))));

                if (dwgs.Count == 0)
                {
                    Mess?.Invoke("There is no any DWG files to process... ");
                    return;
                }

                ElectricalProjPath = Directory.GetFiles(pr, "*.wdp")?.Count() > 0 ? Directory.GetFiles(pr, "*.wdp").First() : "";
                ElectricalPaths = new List<Tuple<string, string>>();
                IsElectrical = false;

                if (!string.IsNullOrEmpty(ElectricalProjPath) && File.Exists(ElectricalProjPath))
                {
                    IsElectrical = true;
                    var lines = File.ReadAllLines(ElectricalProjPath).Where(x => x.ToLower().Contains(".dwg")).ToList();
                    var subs = File.ReadAllLines(ElectricalProjPath).Where(x => x.Contains("=====SUB=")).ToList();

                    if (subs.Count == 0)
                    {
                        for (int i = 0; i < lines.Count; i++)
                            ElectricalPaths.Add(new Tuple<string, string>(lines[i], "EMPTY"));
                    }
                    else
                    {
                        for (int i = 0; i < lines.Count; i++)
                            ElectricalPaths.Add(new Tuple<string, string>(lines[i], subs[i].Replace("=====SUB=", string.Empty)));
                    }

                    dwgs.Clear();

                    foreach (var k in ElectricalPaths)
                        dwgs.Add(new Tuple<string, string>($"{pr}{k.Item1}", k.Item2));
                }

                #endregion Electrical Prepartion

                ClearDatabase();

                await Task.Run(() =>
                {
                    Mess?.Invoke(" --------------------------------------------");
                    Mess?.Invoke(" ====     DATABASE UPDATE STARTED        ====");
                    Mess?.Invoke(" --------------------------------------------");

                    int i = 1;
                    foreach (var x in dwgs)
                    {
                        tokenSource.Token.ThrowIfCancellationRequested();

                        if (!File.Exists(x.Item1))
                            Mess?.BeginInvoke($"{Environment.NewLine} Drawing: {x.Item1}  Not Exists", null, null);
                        else
                        {
                            #region DB operation

                            Database db = new Database(false, true);

                            db.ReadDwgFile(x.Item1, FileOpenMode.OpenForReadAndAllShare, true, null);
                            db.CloseInput(true);
                            PageInfoList.Add(new PageInfo() { Path = x.Item1, Sub = x.Item2 });

                            Autodesk.AutoCAD.DatabaseServices.TransactionManager tm = db.TransactionManager;
                            using (tm.StartTransaction())
                            {
                                // Open the block table
                                BlockTable bt = (BlockTable)tm.GetObject(db.BlockTableId, OpenMode.ForRead, false);

                                foreach (ObjectId btrId in bt)
                                {
                                    BlockTableRecord btr = (BlockTableRecord)tm.GetObject(btrId, OpenMode.ForRead, false);
                                    foreach (ObjectId blId in btr.GetBlockReferenceIds(true, false))
                                    {
                                        if (!btr.IsLayout && !btr.IsAnonymous && !btr.IsFromExternalReference && !btr.IsErased && !btr.IsFromOverlayReference)
                                        {
                                            // Block reference
                                            BlockReference blkRef = (BlockReference)tm.GetObject(blId, OpenMode.ForRead, false);

                                            if (blkRef.BlockName.ToUpper() == "*MODEL_SPACE" && blkRef.Name != "WD_M") // Only those that are on the model
                                            {
                                                // Adding new block and its name
                                                PageInfoList.Last().Blocks.Add(new BlocksInfo()
                                                {
                                                    Id = blkRef.Handle.Value,
                                                    Name = blkRef.Name,
                                                    PagePath = x.Item1,
                                                    Sub = x.Item2,
                                                    X = blkRef.Position.X,
                                                    Y = blkRef.Position.Y
                                                });

                                                AttributeCollection attCol = blkRef.AttributeCollection;
                                                foreach (ObjectId attId in attCol)
                                                {
                                                    AttributeReference attRef = (AttributeReference)tm.GetObject(attId, OpenMode.ForRead);
                                                    PageInfoList.Last().Blocks.Last().Parementers.Add(new BlockParam() { Name = attRef.Tag, Value = attRef.TextString });
                                                }
                                            }
                                        }
                                    }
                                }

                                Mess?.Invoke($"Drawing Proceed: {x.Item1.Replace(pr, @" ... \")} [ {i} - {dwgs.Count} ]");

                                i++;
                            }
                            db.Dispose();
                            db = null;

                            #endregion DB operation
                        }
                    }

                    ViewModel.BlockAmount = PageInfoList.SelectMany(x => x.Blocks).Count();

                    Mess?.Invoke(" --------------------------------------------");
                    Mess?.Invoke(" ====    DATABASE UPDATE FINISHED       ==== ");
                    Mess?.Invoke(" --------------------------------------------");
                }, tokenSource.Token);

                // Database save
                syncCtrl.Dispatcher.Invoke(() =>
               {
                   // Saving data
                   string s = Reg.Registry.GetValue(RegKeyName, RegProjectPath, string.Empty).ToString();
                   if (!string.IsNullOrEmpty(s) && PageInfoList.Count > 0)
                       SaveDatabase(s + "Database.xml");

                   SettList.Clear();
                   foreach (var el in PageInfoList.SelectMany(x => x.Blocks).GroupBy(x => x.Name).Select(x => x.First()).OrderBy(x => x.Name))
                       SettList.Add(new Settings(el));

                   Mess?.Invoke(" Database was saved!");
               });

                CloseDocuments();
                ShowReady();
            }
            catch (OperationCanceledException ae)
            {
                PageInfoList.Clear();
                Mess?.Invoke($"Task was canceled: {ae.Message}");

                tokenSource.Dispose();
                tokenSource = new CancellationTokenSource();

                CloseDocuments();
                ShowReady();
            }
            catch (System.Exception Ex)
            {
                Mess?.Invoke($"{Ex.Message}:  {Ex.StackTrace}");
            }
        }

        public void SendInformation(string s)
        {
            Mess?.Invoke(s);
        }

        private void AutoCadCablePlug_Mess(string msg)
        {
            syncCtrl.Dispatcher.InvokeAsync(new Action(() =>
            {
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"{msg}{Environment.NewLine}");
            }));
        }

        public async Task DownloadChanges()
        {
            CloseDocuments();
            ShowWait();

            Mess?.Invoke(" --------------------------------------------");
            Mess?.Invoke(" ==== !!! SAVING ALL DRAWINGS STARTED !!!====");
            Mess?.Invoke(" --------------------------------------------");

            await Task.Run(() =>
            {
                syncCtrl.Dispatcher.Invoke(() =>
                {
                    int i = 1;

                    // Adapting new format

                    foreach (PageInfo p in PageInfoList)
                    {
                        try
                        {
                            Database db = new Database(false, true);
                            db.ReadDwgFile(p.Path, FileShare.ReadWrite, true, "");
                            db.CloseInput(true);
                            HostApplicationServices.WorkingDatabase = db;

                            #region Transation

                            Transaction tm = db.TransactionManager.StartTransaction();
                            using (tm)
                            {
                                //#region Style change
                                //string stl = "WD_IEC";
                                //TextStyleTable tst = (TextStyleTable)tm.GetObject(db.TextStyleTableId, OpenMode.ForWrite);
                                //if (tst.Has(stl))
                                //{
                                //    var newstyle = tst[stl];
                                //    db.Textstyle = newstyle;
                                //}
                                //#endregion

                                BlockTable bt = (BlockTable)tm.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                                foreach (BlocksInfo b in p.Blocks)
                                {
                                    if (bt.Has(b.Name))
                                    {
                                        BlockTableRecord btr = (BlockTableRecord)tm.GetObject(bt[b.Name], OpenMode.ForRead, false);
                                        var blkRefsIds = btr.GetBlockReferenceIds(true, true);
                                        if (blkRefsIds.Count > 0)
                                        {
                                            List<BlockReference> blkRef = (from x in blkRefsIds.Cast<ObjectId>()
                                                                           select (BlockReference)tm.GetObject(x, OpenMode.ForRead, false)).ToList();

                                            if (blkRef.Exists(x => x.Handle.Value == b.Id))
                                            {
                                                BlockReference bRef = blkRef.Find(x => x.Handle.Value == b.Id);
                                                if (bRef != null)
                                                {
                                                    AttributeCollection attCol = bRef.AttributeCollection;
                                                    foreach (ObjectId attId in attCol)
                                                    {
                                                        AttributeReference attRef = (AttributeReference)tm.GetObject(attId, OpenMode.ForWrite);
                                                        if (b.Parementers.ToList().Exists(x => x.Name == attRef.Tag))
                                                        {
                                                            BlockParam bp = b.Parementers.ToList().Find(x => x.Name == attRef.Tag);
                                                            attRef.UpgradeOpen();

                                                            // Parameter change
                                                            //if (attRef.Tag.Contains("SIG") && attRef.Tag.Contains("DESC2"))
                                                            // attRef.Invisible = false;

                                                            attRef.TextString = bp.Value;
                                                            bRef.RecordGraphicsModified(true);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                tm.Commit();
                            }

                            #endregion Transation

                            HostApplicationServices.WorkingDatabase = db;
                            db.SaveAs(p.Path, true, DwgVersion.Current, db.SecurityParameters);
                            Mess?.Invoke($"Drawing changed and saved: {p.Path}  [{i} -> {PageInfoList.Count}]");
                            db.Dispose();
                            HostApplicationServices.WorkingDatabase = null;

                            i++;
                        }
                        catch (System.Exception Ex)
                        {
                            Mess?.Invoke($"{p.Path}: {Ex.Message}");
                        }
                    }
                });
            });

            CloseDocuments();
            ShowReady();

            Mess?.Invoke(" --------------------------------------------");
            Mess?.Invoke(" ====!!! SAVING ALL DRAWINGS FINISHED !!!====");
            Mess?.Invoke(" --------------------------------------------");
        }

        public void ClearDatabase()
        {
            // Clearing data
            foreach (var p in PageInfoList)
            {
                foreach (var b in p.Blocks)
                    b.Parementers.Clear();

                p.Blocks.Clear();
            }
            PageInfoList.Clear();
        }

        public void CloseDocuments()
        {
            DocumentCollection docs = Application.DocumentManager;
            foreach (Document doc in docs)
            {
                // First cancel any running command
                if (doc.CommandInProgress != "" && doc.CommandInProgress != "CD")
                {
                    //AcadDocument oDoc = (AcadDocument)doc.GetAcadDocument();
                    //oDoc.SendCommand();
                    doc.SendStringToExecute("\x03\x03", true, false, false);
                }
                doc.CloseAndDiscard();
            }
        }

        private void ShowWait()
        {
            try
            {
                string m = $"{Environment.CurrentDirectory}\\wait.dwg";
                if (File.Exists(m))
                {
                    Application.DocumentManager.Open(m);
                }
            }
            catch (System.Exception Ex)
            {
                Mess?.Invoke($"{Ex.Message}: {Ex.StackTrace}");
            }
        }

        private void ShowReady()
        {
            string x = $"{Environment.CurrentDirectory}\\ready.dwg";
            if (File.Exists(x))
            {
                Application.DocumentManager.Open(x);
            }
        }

        public void LoadDatabse(string dbpath)
        {
            try
            {
                XmlSerializer XmlSer = new XmlSerializer(typeof(ObservableCollection<PageInfo>));
                using (Stream str = new FileStream(dbpath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    PageInfoList = (ObservableCollection<PageInfo>)XmlSer.Deserialize(str);

                    SettList.Clear();
                    foreach (var el in PageInfoList.SelectMany(x => x.Blocks).GroupBy(x => x.Name).Select(x => x.First()))
                        SettList.Add(new Settings(el));
                }
            }
            catch (System.Exception Ex)
            {
                Mess?.Invoke($"{Ex.Message}: {Ex.StackTrace}");
            }
        }

        public void SaveDatabase(string dbpath)
        {
            try
            {
                XmlSerializer XmlSer = new XmlSerializer(typeof(ObservableCollection<PageInfo>));
                using (Stream str = new FileStream(dbpath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    XmlSer.Serialize(str, PageInfoList);
                }
            }
            catch (System.Exception Ex)
            {
                Mess?.Invoke($"{Ex.Message}: {Ex.StackTrace}");
            }
        }

        // EXCEL LISTS

        public async Task GenerateExcelList(ACadViewModel VMod)
        {
            string doc_name = "excel_list";
            Exc.Application App = null;
            Exc.Workbook wrk = null;
            Exc.Worksheet wsh = null;

            try
            {
                // Closing all Excel instances opened in background
                foreach (var pr in Process.GetProcessesByName("EXCEL"))
                {
                    if (pr.MainWindowTitle == "" || pr.MainWindowTitle.Contains(doc_name + ".xlsx"))
                        pr.Kill();
                }

                if (!Directory.Exists(Path.GetDirectoryName($"{VMod.ProjDir}Lists\\")))
                    Directory.CreateDirectory(Path.GetDirectoryName($"{VMod.ProjDir}Lists\\"));

                Mess?.Invoke(" --------------------------------------------");
                Mess?.Invoke($" ==== EXCEL ALL LIST GENERATION STARTED ====");
                Mess?.Invoke(" --------------------------------------------");

                App = new Exc.Application() { DisplayAlerts = false };
                wrk = App.Workbooks.Add(Exc.XlWBATemplate.xlWBATWorksheet);
                wsh = (Exc.Worksheet)wrk.Worksheets[1];
                wsh.Name = "ALL";

                await Task.Run(() =>
                {
                    try
                    {
                        // Headers
                        var hds_first_col = new List<string>() { "ID", "BL_PATH", "BLOCK_NAME", "BL_SH" };

                        // Adding all parameters
                        string[] all_head = new string[] { };
                        all_head = SettList.SelectMany(x => x.Params).Where(x => x.Enable).Select(x => x.Name).OrderBy(x => x).ToArray();

                        // Excel header generation
                        List<string> buff = new List<string>(hds_first_col);
                        buff.AddRange(all_head);
                        int p = 1;
                        foreach (var a in buff)
                        {
                            ((Exc.Range)wsh.Cells[1, p]).Value2 = a;
                            p++;
                        }

                        // Sum
                        int sum = PageInfoList.SelectMany(x => x.Blocks).Count();
                        int i = 2;
                        foreach (var pg in PageInfoList)
                        {
                            //Rows
                            foreach (var bl in pg.Blocks.Where(x => SettList.ToList().Exists(m => m.Name == x.Name) && SettList.ToList().Find(m => m.Name == x.Name).Enable))
                            {
                                int j = 1;
                                foreach (var t in buff)
                                {
                                    ((Exc.Range)wsh.Cells[i, j]).Value2 = bl.GetValue(t);
                                    ((Exc.Range)wsh.Cells[i, j]).HorizontalAlignment = Exc.XlHAlign.xlHAlignCenter;
                                    j++;
                                }

                                i++;
                                Mess?.Invoke($"{i - 2} -> {sum}");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Mess?.Invoke($"Error during data processing: {ex.Message}");
                        throw;
                    }
                });

                Mess?.Invoke(" --------------------------------------------");
                Mess?.Invoke(" ==== EXCEL SG. LIST GENERATION FINISHED ====");
                Mess?.Invoke(" --------------------------------------------");

                ((Exc.Range)wsh.Columns[1]).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightGray);
                ((Exc.Range)wsh.Columns[2]).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightGray);
                ((Exc.Range)wsh.Columns[3]).Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightGray);

                App.Visible = true;

                ((Exc.Range)wsh.Cells[2, 4]).Select();
                wsh.Application.ActiveWindow.FreezePanes = true;

                wsh.EnableAutoFilter = true;
                wsh.Cells.AutoFilter(1);
                wsh.Columns.AutoFit();

                if (File.Exists($"{VMod.ProjDir}Lists\\{doc_name}.xlsx"))
                    File.Delete($"{VMod.ProjDir}Lists\\{doc_name}.xlsx");

                wrk.SaveAs($"{VMod.ProjDir}Lists\\{doc_name}.xlsx");

                App.WorkbookAfterSave += App_WorkbookAfterSave;
                App.WorkbookBeforeClose += App_WorkbookBeforeClose;
            }
            catch (System.Exception ex)
            {
                Mess?.Invoke($"Error generating Excel list: {ex.Message}");
                Mess?.Invoke($"Stack trace: {ex.StackTrace}");

                // Try to close Excel if it was opened
                if (App != null)
                {
                    try
                    {
                        App.Visible = false;
                        wrk?.Close(false);
                        App.Quit();
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            finally
            {
                // Clean up COM objects
                if (wsh != null)
                {
                    Marshal.ReleaseComObject(wsh);
                    wsh = null;
                }
                if (wrk != null)
                {
                    Marshal.ReleaseComObject(wrk);
                    wrk = null;
                }
                if (App != null)
                {
                    Marshal.ReleaseComObject(App);
                    App = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void App_WorkbookBeforeClose(Exc.Workbook Wb, ref bool Cancel)
        {
            Wb.Application.WorkbookAfterSave -= App_WorkbookAfterSave;
            Wb.Application.WorkbookBeforeClose -= App_WorkbookBeforeClose;
            GetExcelProcess(Wb.Application)?.Kill();
        }

        private void App_WorkbookAfterSave(Exc.Workbook Wb, bool Success)
        {
            Mess?.Invoke(" --------------------------------------------");
            Mess?.Invoke("  ====     EXCEL SAVING  STARTED         ====");
            Mess?.Invoke(" --------------------------------------------");

            Exc.Worksheet whs = (Exc.Worksheet)Wb.Worksheets[1];
            Mess?.Invoke($"{1} -> {whs.UsedRange.Rows.Count}");

            for (int i = 2; i < whs.UsedRange.Rows.Count + 1; i++)
            {
                long id = long.Parse(((Exc.Range)whs.Cells[i, 1]).Value2?.ToString() ?? "0");
                string pg = ((Exc.Range)whs.Cells[i, 2]).Value2?.ToString() ?? string.Empty;

                for (int j = 5; j < whs.UsedRange.Columns.Count + 1; j++)
                {
                    string param = ((Exc.Range)whs.Cells[1, j]).Value2?.ToString() ?? string.Empty;
                    string val = ((Exc.Range)whs.Cells[i, j]).Value2?.ToString() ?? string.Empty;

                    if (!string.IsNullOrEmpty(param))
                    {
                        var p = PageInfoList.ToList().Find(x => x.Path == pg);
                        var b = p.Blocks.Find(x => x.Id == id);

                        BlockParam par = b.Parementers.ToList().Find(y => y.Name == param);
                        if (par != null)
                        {
                            if (string.IsNullOrEmpty(val))
                                par.Value = string.Empty;
                            else
                                par.Value = val;
                        }
                    }
                }

                Mess?.Invoke($"{i} -> {whs.UsedRange.Rows.Count}");
            }

            Mess?.Invoke(" --------------------------------------------");
            Mess?.Invoke(" ====     EXCEL SAVING  FINISHED        =====");
            Mess?.Invoke(" --------------------------------------------");

            Marshal.FinalReleaseComObject(whs);
            whs = null;
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        private Process GetExcelProcess(Exc.Application excelApp)
        {
            int id;
            GetWindowThreadProcessId(excelApp.Hwnd, out id);
            return Process.GetProcessById(id);
        }

        public async Task ImportExcel(string initPath)
        {
            wf.OpenFileDialog ofd = new wf.OpenFileDialog();
            ofd.Filter = "Excel File|*.xlsx";
            ofd.InitialDirectory = initPath;
            if (ofd.ShowDialog() == wf.DialogResult.OK)
            {
                await Task.Run(() =>
                {
                    Mess?.Invoke(" --------------------------------------------");
                    Mess?.Invoke(" ====     EXCEL SAVING  STARTED          ====");
                    Mess?.Invoke(" --------------------------------------------");

                    Exc.Application App = new Exc.Application() { DisplayAlerts = false };
                    Exc.Workbook wrk = App.Workbooks.Open(ofd.FileName);
                    Exc.Worksheet whs = (Exc.Worksheet)wrk.Worksheets[1];

                    Mess?.Invoke($"{1} -> {whs.UsedRange.Rows.Count}");

                    for (int i = 2; i < whs.UsedRange.Rows.Count + 1; i++)
                    {
                        long id = long.Parse(((Exc.Range)whs.Cells[i, 1]).Value2?.ToString() ?? "0");
                        string pg = ((Exc.Range)whs.Cells[i, 2]).Value2?.ToString() ?? string.Empty;

                        for (int j = 5; j < whs.UsedRange.Columns.Count + 1; j++)
                        {
                            string param = ((Exc.Range)whs.Cells[1, j]).Value2?.ToString() ?? string.Empty;
                            string val = ((Exc.Range)whs.Cells[i, j]).Value2?.ToString() ?? string.Empty;

                            if (!string.IsNullOrEmpty(param))
                            {
                                var p = PageInfoList.ToList().Find(x => x.Path == pg);
                                var b = p.Blocks.Find(x => x.Id == id);

                                BlockParam par = b.Parementers.ToList().Find(y => y.Name == param);
                                if (par != null)
                                {
                                    if (string.IsNullOrEmpty(val))
                                        par.Value = string.Empty;
                                    else
                                        par.Value = val;
                                }
                            }
                        }

                        Mess?.Invoke($"{i} -> {whs.UsedRange.Rows.Count}");
                    }

                    Mess?.Invoke(" --------------------------------------------");
                    Mess?.Invoke(" ====     EXCEL SAVING  FINISHED         ====");
                    Mess?.Invoke(" --------------------------------------------");

                    Marshal.ReleaseComObject(whs);
                    Marshal.ReleaseComObject(wrk);
                    Marshal.ReleaseComObject(App);

                    whs = null;
                    wrk = null;
                    App = null;
                });
            }
        }
    }
}