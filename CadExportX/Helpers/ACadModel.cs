using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Serialization;
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

                                            if (blkRef.BlockName.ToUpper() == "*MODEL_SPACE" && blkRef.Name != "WD_M")
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
                if (doc.CommandInProgress != "" && doc.CommandInProgress != "CD")
                {
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

            try
            {
                if (!Directory.Exists($"{VMod.ProjDir}Lists\\"))
                    Directory.CreateDirectory($"{VMod.ProjDir}Lists\\");

                Mess?.Invoke(" --------------------------------------------");
                Mess?.Invoke($" ==== EXCEL LIST GENERATION STARTED ====");
                Mess?.Invoke(" --------------------------------------------");

                // Save file dialog
                wf.SaveFileDialog sfd = new wf.SaveFileDialog();
                sfd.Filter = "Excel Files|*.xlsx";
                sfd.Title = "Save Excel List";
                sfd.FileName = doc_name;
                sfd.InitialDirectory = $"{VMod.ProjDir}Lists\\";

                if (sfd.ShowDialog() != wf.DialogResult.OK)
                {
                    Mess?.Invoke("Export canceled by user.");
                    return;
                }

                string filePath = sfd.FileName;

                await Task.Run(() =>
                {
                    try
                    {
                        // Validate data before processing
                        if (PageInfoList == null || PageInfoList.Count == 0)
                        {
                            Mess?.Invoke("Error: No data to export. Please update database first.");
                            return;
                        }

                        if (SettList == null || SettList.Count == 0)
                        {
                            Mess?.Invoke("Error: No block settings available.");
                            return;
                        }

                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("ALL");

                            // Headers
                            var hds_first_col = new List<string>() { "ID", "BL_PATH", "BLOCK_NAME", "BL_SH" };

                            // Adding all parameters
                            string[] all_head = SettList
                                .Where(x => x != null && x.Params != null)
                                .SelectMany(x => x.Params)
                                .Where(x => x != null && x.Enable)
                                .Select(x => x.Name)
                                .Distinct()
                                .OrderBy(x => x)
                                .ToArray();

                            // Excel header generation
                            List<string> buff = new List<string>(hds_first_col);
                            buff.AddRange(all_head);
                            
                            int p = 1;
                            foreach (var a in buff)
                            {
                                if (!string.IsNullOrEmpty(a))
                                {
                                    worksheet.Cell(1, p).Value = a;
                                    p++;
                                }
                            }

                            // Sum
                            int sum = PageInfoList
                                .Where(x => x != null && x.Blocks != null)
                                .SelectMany(x => x.Blocks)
                                .Count();

                            Mess?.Invoke($"Total blocks to export: {sum}");

                            int i = 2;
                            foreach (var pg in PageInfoList)
                            {
                                if (pg == null || pg.Blocks == null)
                                    continue;

                                foreach (var bl in pg.Blocks.Where(x => 
                                    x != null && 
                                    SettList.ToList().Exists(m => m != null && m.Name == x.Name) && 
                                    SettList.ToList().Find(m => m.Name == x.Name).Enable))
                                {
                                    try
                                    {
                                        int j = 1;
                                        foreach (var t in buff)
                                        {
                                            if (string.IsNullOrEmpty(t))
                                                continue;

                                            string value = bl.GetValue(t) ?? "...";
                                            worksheet.Cell(i, j).Value = value;
                                            worksheet.Cell(i, j).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                            j++;
                                        }

                                        i++;
                                        
                                        if ((i - 2) % 100 == 0)
                                            Mess?.Invoke($"{i - 2} -> {sum}");
                                    }
                                    catch (System.Exception ex)
                                    {
                                        Mess?.Invoke($"Error processing block {bl?.Name ?? "unknown"}: {ex.Message}");
                                    }
                                }
                            }

                            Mess?.Invoke($"Total rows exported: {i - 2}");

                            // Styling
                            worksheet.Column(1).Style.Fill.BackgroundColor = XLColor.LightGray;
                            worksheet.Column(2).Style.Fill.BackgroundColor = XLColor.LightGray;
                            worksheet.Column(3).Style.Fill.BackgroundColor = XLColor.LightGray;

                            // Freeze panes
                            worksheet.SheetView.FreezeRows(1);
                            worksheet.SheetView.FreezeColumns(3);

                            // AutoFilter
                            worksheet.RangeUsed().SetAutoFilter();

                            // AutoFit columns
                            worksheet.Columns().AdjustToContents();

                            // Save workbook
                            if (File.Exists(filePath))
                                File.Delete(filePath);

                            workbook.SaveAs(filePath);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Mess?.Invoke($"Error during data processing: {ex.Message}");
                        Mess?.Invoke($"Stack trace: {ex.StackTrace}");
                        throw;
                    }
                });

                Mess?.Invoke(" --------------------------------------------");
                Mess?.Invoke(" ==== EXCEL LIST GENERATION FINISHED ====");
                Mess?.Invoke(" --------------------------------------------");
                Mess?.Invoke($" File saved: {filePath}");

                // Open file with default application (Excel/LibreOffice)
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (System.Exception ex)
            {
                Mess?.Invoke($"Error generating Excel list: {ex.Message}");
                Mess?.Invoke($"Stack trace: {ex.StackTrace}");
            }
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
                    try
                    {
                        Mess?.Invoke(" --------------------------------------------");
                        Mess?.Invoke(" ====     EXCEL IMPORT STARTED          ====");
                        Mess?.Invoke(" --------------------------------------------");

                        using (var workbook = new XLWorkbook(ofd.FileName))
                        {
                            var worksheet = workbook.Worksheet(1);
                            var rowCount = worksheet.RangeUsed().RowCount();
                            var columnCount = worksheet.RangeUsed().ColumnCount();

                            Mess?.Invoke($"Reading {rowCount} rows...");

                            for (int i = 2; i <= rowCount; i++)
                            {
                                long id = long.Parse(worksheet.Cell(i, 1).GetValue<string>() ?? "0");
                                string pg = worksheet.Cell(i, 2).GetValue<string>() ?? string.Empty;

                                for (int j = 5; j <= columnCount; j++)
                                {
                                    string param = worksheet.Cell(1, j).GetValue<string>() ?? string.Empty;
                                    string val = worksheet.Cell(i, j).GetValue<string>() ?? string.Empty;

                                    if (!string.IsNullOrEmpty(param))
                                    {
                                        var p = PageInfoList.ToList().Find(x => x.Path == pg);
                                        if (p != null)
                                        {
                                            var b = p.Blocks.Find(x => x.Id == id);
                                            if (b != null)
                                            {
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
                                    }
                                }

                                if (i % 100 == 0)
                                    Mess?.Invoke($"{i} -> {rowCount}");
                            }
                        }

                        Mess?.Invoke(" --------------------------------------------");
                        Mess?.Invoke(" ====     EXCEL IMPORT FINISHED         ====");
                        Mess?.Invoke(" --------------------------------------------");
                    }
                    catch (System.Exception ex)
                    {
                        Mess?.Invoke($"Error importing Excel: {ex.Message}");
                        Mess?.Invoke($"Stack trace: {ex.StackTrace}");
                    }
                });
            }
        }
    }
}
