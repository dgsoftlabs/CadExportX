using Autodesk.AutoCAD.Windows;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Exc = Microsoft.Office.Interop.Excel;
using Reg = Microsoft.Win32;
using wf = System.Windows.Forms;

namespace ModelSpace
{
    public class ACadViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }

        private static PaletteSet PsManager = null;
        private static ACadView View = null;
        public ACadModel Plg { get; set; }

        private string ProjDir_;

        public string ProjDir
        {
            get { return ProjDir_; }
            set { ProjDir_ = value; RaisePropertyChanged(); }
        }

        private int BlockAmount_;

        public int BlockAmount
        {
            get { return BlockAmount_; }
            set
            {
                BlockAmount_ = value;
                RaisePropertyChanged();
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value)
                    return;

                _isBusy = value;
                RaisePropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ACadViewModel(ACadModel Plg)
        {
            this.Plg = Plg;

            ((INotifyPropertyChanged)this).PropertyChanged += Plugin_PropertyChanged;

            // Create View - syncCtrl is already initialized in ACadModel.Initialize()
            View = new ACadView(Plg);
            View.DataContext = this;

            ShowManager();

            if (Reg.Registry.GetValue(Plg.RegKeyName, Plg.RegProjectPath, string.Empty) == null)
                Reg.Registry.SetValue(Plg.RegKeyName, Plg.RegProjectPath, string.Empty);

            string PrDir = Reg.Registry.GetValue(Plg.RegKeyName, Plg.RegProjectPath, string.Empty).ToString();
            if (Directory.Exists(PrDir))
            {
                ProjDir = PrDir;
                if (File.Exists(ProjDir + "Database.xml"))
                    Plg.LoadDatabse(ProjDir + "Database.xml");
            }

            BlockAmount = Plg.PageInfoList.SelectMany(x => x.Blocks).Count();
        }

        ~ACadViewModel()
        {
        }

        private void Plugin_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ACadViewModel.ProjDir))
            {
            }
        }

        public void ShowManager()
        {
            if (PsManager == null)
            {
                PsManager = new PaletteSet($"TK Helper", new Guid("939E435B-E76F-48F9-829A-CC8D926DF257"));
                PsManager.SetLocation(new System.Drawing.Point(0, 0));
                PsManager.SetSize(new System.Drawing.Size(500, 700));
                PsManager.DockEnabled = DockSides.Bottom;
                PsManager.PaletteSetMoved += ps_PaletteSetMoved;
                PsManager.AddVisual($"TK Helper", View);
            }
            PsManager.Visible = true;
        }

        private void ps_PaletteSetMoved(object sender, PaletteSetMoveEventArgs e)
        {
            PaletteSet pt = sender as PaletteSet;
            pt.PaletteSetMoved -= ps_PaletteSetMoved;
            pt.DockEnabled = DockSides.Bottom | DockSides.Left | DockSides.Top | DockSides.Right;
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        private Process GetExcelProcess(Exc.Application excelApp)
        {
            int id;
            GetWindowThreadProcessId(excelApp.Hwnd, out id);
            return Process.GetProcessById(id);
        }

        // === UTILITY COMMANDS ===

        #region Show Inspector

        private RelayCommand _InspectorShowCommand = null;

        public ICommand InspectorShowCommand
        {
            get
            {
                if (_InspectorShowCommand == null)
                {
                    _InspectorShowCommand = new RelayCommand((p) => OnInspectorShow(p), (p) => CanInspectorShow(p));
                }

                return _InspectorShowCommand;
            }
        }

        private bool CanInspectorShow(object parameter)
        {
            return true;
        }

        private void OnInspectorShow(object parameter)
        {
            DwgInspector dwg = new DwgInspector();
            dwg.ShowDialog();
        }

        #endregion Show Inspector

        #region Select Project Directory

        private RelayCommand _ProjectDirCommand = null;

        public ICommand ProjectDirCommand
        {
            get
            {
                if (_ProjectDirCommand == null)
                {
                    _ProjectDirCommand = new RelayCommand((p) => OnProjectDir(p), (p) => CanProjectDir(p));
                }

                return _ProjectDirCommand;
            }
        }

        private bool CanProjectDir(object parameter)
        {
            return true;
        }

        private void OnProjectDir(object parameter)
        {
            try
            {
                string PrDir = Reg.Registry.GetValue(Plg.RegKeyName, Plg.RegProjectPath, string.Empty).ToString();
                wf.FolderBrowserDialog fbd = new wf.FolderBrowserDialog();

                if (!string.IsNullOrEmpty(PrDir))
                    fbd.SelectedPath = PrDir;

                if (fbd.ShowDialog() == wf.DialogResult.OK)
                {
                    ProjDir = fbd.SelectedPath + "\\";
                    Reg.Registry.SetValue(Plg.RegKeyName, Plg.RegProjectPath, fbd.SelectedPath + "\\");

                    if (File.Exists(ProjDir + "Database.xml"))
                        Plg.LoadDatabse(ProjDir + "Database.xml");
                }
            }
            catch (Exception Ex)
            {
                if (!Directory.Exists(Plg.LogsFolderPath))
                    Directory.CreateDirectory(Plg.LogsFolderPath);

                File.WriteAllText($"{Plg.LogsFolderPath}Log_{DateTime.Now.ToString(Plg.LogFormat)}.txt"
                  , Ex.StackTrace);

                Plg.SendInformation(Ex.Message);
            }
        }

        #endregion Select Project Directory

        #region Open Project Folder

        private RelayCommand _ExploreProjCommand = null;

        public ICommand ExploreProjCommand
        {
            get
            {
                if (_ExploreProjCommand == null)
                {
                    _ExploreProjCommand = new RelayCommand((p) => OnExploreProj(p), (p) => CanExploreProj(p));
                }

                return _ExploreProjCommand;
            }
        }

        private bool CanExploreProj(object parameter)
        {
            return ProjDir?.Length > 0;
        }

        private void OnExploreProj(object parameter)
        {
            if (!string.IsNullOrEmpty(ProjDir))
                System.Diagnostics.Process.Start(ProjDir);
        }

        #endregion Open Project Folder

        // === PRIMARY OPERATIONS ===

        #region Read All Drawings

        private RelayCommand _ReadAllDrgsCommand = null;

        public ICommand ReadAllDrgsCommand
        {
            get
            {
                if (_ReadAllDrgsCommand == null)
                {
                    _ReadAllDrgsCommand = new RelayCommand((p) => OnReadAllDrgs(p), (p) => CanReadAllDrgs(p));
                }

                return _ReadAllDrgsCommand;
            }
        }

        private bool CanReadAllDrgs(object parameter)
        {
            return !IsBusy && Directory.Exists(ProjDir);
        }

        private async void OnReadAllDrgs(object parameter)
        {
            await RunBusyOperation(() => Plg.DatabaseUpdate(ProjDir));
        }

        #endregion Read All Drawings

        #region Save All Drawings

        private RelayCommand _SaveAllDrvsCommand = null;

        public ICommand SaveAllDrvsCommand
        {
            get
            {
                if (_SaveAllDrvsCommand == null)
                {
                    _SaveAllDrvsCommand = new RelayCommand((p) => OnSaveAllDrvs(p), (p) => CanSaveAllDrvs(p));
                }

                return _SaveAllDrvsCommand;
            }
        }

        private bool CanSaveAllDrvs(object parameter)
        {
            return !IsBusy;
        }

        private async void OnSaveAllDrvs(object parameter)
        {
            if (MessageBox.Show("Do you want to apply changes to the drawings?", "Attention", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                await RunBusyOperation(() => Plg.DownloadChanges());
        }

        #endregion Save All Drawings

        #region Export Parts to Excel

        private RelayCommand _ExportPartExclSigCommand = null;

        public ICommand ExportPartExclSigCommand
        {
            get
            {
                if (_ExportPartExclSigCommand == null)
                {
                    _ExportPartExclSigCommand = new RelayCommand((p) => OnExportPartExclSig(p), (p) => CanExportPartExclSig(p));
                }

                return _ExportPartExclSigCommand;
            }
        }

        private bool CanExportPartExclSig(object parameter)
        {
            return !IsBusy;
        }

        private async void OnExportPartExclSig(object parameter)
        {
            await RunBusyOperation(() => Plg.GenerateExcelList(this));
        }

        #endregion Export Parts to Excel

        #region Import from Excel

        private RelayCommand _ImportExcelCommand = null;

        public ICommand ImportExcelCommand
        {
            get
            {
                if (_ImportExcelCommand == null)
                {
                    _ImportExcelCommand = new RelayCommand((p) => OnImportExcel(p), (p) => CanImportExcel(p));
                }

                return _ImportExcelCommand;
            }
        }

        private bool CanImportExcel(object parameter)
        {
            return !IsBusy;
        }

        private async void OnImportExcel(object parameter)
        {
            await RunBusyOperation(() => Plg.ImportExcel(ProjDir));
        }

        #endregion Import from Excel

        #region Select All Blocks

        private RelayCommand _BlSelAllCommand = null;

        public ICommand BlSelAllCommand
        {
            get
            {
                if (_BlSelAllCommand == null)
                {
                    _BlSelAllCommand = new RelayCommand((p) => OnBlSelAll(p), (p) => CanBlSelAll(p));
                }

                return _BlSelAllCommand;
            }
        }

        private bool CanBlSelAll(object parameter)
        {
            return View.LbBlk.Items.Count > 0;
        }

        private void OnBlSelAll(object parameter)
        {
            foreach (var el in View.LbBlk.Items)
                ((Settings)el).Enable = true;
        }

        #endregion Select All Blocks

        #region Unselect All Blocks

        private RelayCommand _BlUnSelAllCommand = null;

        public ICommand BlUnSelAllCommand
        {
            get
            {
                if (_BlUnSelAllCommand == null)
                {
                    _BlUnSelAllCommand = new RelayCommand((p) => OnBlUnSelAll(p), (p) => CanBlUnSelAll(p));
                }

                return _BlUnSelAllCommand;
            }
        }

        private bool CanBlUnSelAll(object parameter)
        {
            return View.LbBlk.Items.Count > 0;
        }

        private void OnBlUnSelAll(object parameter)
        {
            foreach (var el in View.LbBlk.Items)
                ((Settings)el).Enable = false;
        }

        #endregion Unselect All Blocks

        #region Select All Attributes

        private RelayCommand _AtSelAllCommand = null;

        public ICommand AtSelAllCommand
        {
            get
            {
                if (_AtSelAllCommand == null)
                {
                    _AtSelAllCommand = new RelayCommand((p) => OnAtSelAll(p), (p) => CanAtSelAll(p));
                }

                return _AtSelAllCommand;
            }
        }

        private bool CanAtSelAll(object parameter)
        {
            try
            {
                return View.lbAttr.Items.Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void OnAtSelAll(object parameter)
        {
            foreach (ParamSettings i in View.lbAttr.Items)
                i.Enable = true;
        }

        #endregion Select All Attributes

        #region Unselect All Attributes

        private RelayCommand _AtUnSelAllCommand = null;

        public ICommand AtUnSelAllCommand
        {
            get
            {
                if (_AtUnSelAllCommand == null)
                {
                    _AtUnSelAllCommand = new RelayCommand((p) => OnAtUnSelAll(p), (p) => CanAtUnSelAll(p));
                }

                return _AtUnSelAllCommand;
            }
        }

        private bool CanAtUnSelAll(object parameter)
        {
            try
            {
                return View.lbAttr.Items.Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void OnAtUnSelAll(object parameter)
        {
            foreach (ParamSettings i in View.lbAttr.Items)
                i.Enable = false;
        }

        #endregion Unselect All Attributes

        private async Task RunBusyOperation(Func<Task> operation)
        {
            if (operation == null || IsBusy)
                return;

            IsBusy = true;
            try
            {
                await operation();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}