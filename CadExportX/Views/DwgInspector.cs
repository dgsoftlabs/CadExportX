using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Windows.Forms;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ModelSpace
{
    public partial class DwgInspector : Form
    {
        private static Document doc;
        private static Database db;
        private static Editor ed;

        public DwgInspector()
        {
            InitializeComponent();
            ApplyDarkTheme();
            FillBlockNames();
        }

        private void ApplyDarkTheme()
        {
            // Apply AutoCAD dark theme to the entire form
            AutoCADTheme.ApplyTo(this);
        }

        private void FillBlockNames()
        {
            doc = AcadApp.DocumentManager.MdiActiveDocument;
            db = doc.Database;
            ed = doc.Editor;

            using (Transaction trx = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                foreach (ObjectId objId in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)objId.GetObject(OpenMode.ForRead);

                    if (btr.IsLayout)
                    {
                        continue;
                    }
                    TreeNode node = treeViewBlocks.Nodes.Add(btr.Name);

                    if (btr.GetBlockReferenceIds(true, false).Count > 0)
                    {
                        node.Nodes.Add("Filler");
                    }
                }
                trx.Commit();
            }
        }

        private void treeViewBlocks_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode currentNode = e.Node;
            currentNode.Nodes.Clear();

            using (Transaction trx = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;

                BlockTableRecord btr = (BlockTableRecord)trx.GetObject(bt[currentNode.Text], OpenMode.ForRead);
                ObjectIdCollection objIds = btr.GetBlockReferenceIds(true, false);

                foreach (ObjectId id in objIds)
                {
                    TreeNode node = currentNode.Nodes.Add(String.Format("{0} - {1}", btr.Name, id));
                    node.Tag = id.GetObject(OpenMode.ForRead);
                }

                trx.Commit();
            }
        }

        private void treeViewBlocks_AfterSelect(object sender, TreeViewEventArgs e)
        {
            propertyGrid1.SelectedObject = e.Node?.Tag;
        }
    }
}