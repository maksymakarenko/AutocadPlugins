using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


[assembly: ExtensionApplication(typeof(Lab5.Class1))]
namespace Lab5
{
    public class Class1 : IExtensionApplication
    {
        ContextMenuExtension contextMenu;

        private void AddContextMenu()
        {
            Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                contextMenu = new ContextMenuExtension();

                contextMenu.Title = "Circle Jig";

                MenuItem menuItem = new MenuItem("Run Circle Jig");

                menuItem.Click += CallbackOnClick;

                contextMenu.MenuItems.Add(menuItem);

                Application.AddDefaultContextMenuExtension(contextMenu);
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage($"ERROR: {ex.Message}");
            }
        }

        public void RemoveContextMenu()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;

            try
            {
                if(contextMenu != null)
                {
                    Application.RemoveDefaultContextMenuExtension(contextMenu);

                    contextMenu = null;
                }
            }
            catch (System.Exception ex)
            {
                if(document != null)
                {
                    document.Editor.WriteMessage($"ERROR: {ex.Message}");
                }
            }
        }

        private void CallbackOnClick(Object sender, EventArgs e)
        {
            using (DocumentLock documentLock = Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                CircleJig();

                acedPostCommand("CANCELCMD");
            }
        }

        public void Initialize()
        {
            AddContextMenu();

            AddTabDialog();
        }

        public void Terminate()
        {
            RemoveContextMenu();


        }

        [DllImport("acad.exe", CharSet = CharSet.Unicode, EntryPoint = "?acedPostCommand@@YAHPB_W@Z")]
        public static extern bool acedPostCommand(string cmd);

        public static String myVariable;

        public static void AddTabDialog()
        {
            Application.DisplayingOptionDialog += TabHandler;
        }

        public static void TabHandler(Object sender, TabbedDialogEventArgs e)
        {
            UserControl2 userControl2 = new UserControl2();

            TabbedDialogAction tabbedDialogAction = new TabbedDialogAction(userControl2.OnOk);

            TabbedDialogExtension tabbedDialogExtension = new TabbedDialogExtension(userControl2, tabbedDialogAction);

            e.AddTab("Value for custom variable", tabbedDialogExtension);
        }

        [CommandMethod("testTab")]
        public void TestTab()
        {
            Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

            editor.WriteMessage(myVariable.ToString());
        }

        [CommandMethod("addAnEnt")]
        public static void AddAnEntity()
        {
            Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

            PromptKeywordOptions promptKeywordOptions = new PromptKeywordOptions("Which entity do you want to create? [Circle/Block]: ", "Circle Block");

            PromptResult promptResult = editor.GetKeywords(promptKeywordOptions);

            if (promptResult.Status == PromptStatus.OK)
            {
                switch (promptResult.StringResult)
                {
                    case "Circle":
                        PromptPointOptions getPoint = new PromptPointOptions("Pick center: ");
                        PromptPointResult promptPointResult = editor.GetPoint(getPoint);

                        if (promptPointResult.Status == PromptStatus.OK)
                        {
                            PromptDistanceOptions promptDistanceOptions = new PromptDistanceOptions("Write radius: ");
                            promptDistanceOptions.BasePoint = promptDistanceOptions.BasePoint;
                            promptDistanceOptions.UseBasePoint = true;
                            PromptDoubleResult promptDoubleResult = editor.GetDistance(promptDistanceOptions);

                            if (promptDoubleResult.Status == PromptStatus.OK)
                            {
                                Database database = editor.Document.Database;

                                Transaction transaction = database.TransactionManager.StartTransaction();

                                try
                                {
                                    Circle circle = new Circle(promptPointResult.Value, Vector3d.ZAxis, promptDoubleResult.Value);

                                    BlockTableRecord btr = (BlockTableRecord)transaction.GetObject(database.CurrentSpaceId, OpenMode.ForWrite);
                                    btr.AppendEntity(circle);

                                    transaction.AddNewlyCreatedDBObject(circle, true);
                                    transaction.Commit();
                                }
                                catch (System.Exception ex)
                                {
                                    editor.WriteMessage($"ERROR: {ex.Message}");
                                }
                                finally
                                {
                                    transaction.Dispose();
                                }
                            }
                        }
                        break;

                    case "Block":
                        PromptStringOptions promptStringOptions = new PromptStringOptions("Enter a name of the block to create: ");
                        promptStringOptions.AllowSpaces = false;
                        PromptResult promptResultBlock = editor.GetString(promptStringOptions);

                        if (promptResultBlock.Status == PromptStatus.OK)
                        {
                            Database db = editor.Document.Database;

                            Transaction transaction = db.TransactionManager.StartTransaction();

                            try
                            {
                                BlockTableRecord block = new BlockTableRecord();

                                block.Name = promptResultBlock.StringResult;

                                BlockTable blockTable = (BlockTable)transaction.GetObject(db.BlockTableId, OpenMode.ForRead);

                                if (blockTable.Has(promptResultBlock.StringResult) == false)
                                {
                                    blockTable.UpgradeOpen();

                                    blockTable.Add(block);

                                    transaction.AddNewlyCreatedDBObject(block, true);

                                    Circle circle1 = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10);
                                    block.AppendEntity(circle1);

                                    Circle circle2 = new Circle(new Point3d(20, 10, 0), Vector3d.ZAxis, 10);
                                    block.AppendEntity(circle2);

                                    transaction.AddNewlyCreatedDBObject(circle1, true);
                                    transaction.AddNewlyCreatedDBObject(circle2, true);

                                    PromptPointOptions promptPointOptions = new PromptPointOptions("Pick insertion point of BlockReference: ");
                                    PromptPointResult blockReferencePointResult = editor.GetPoint(promptPointOptions);

                                    if (blockReferencePointResult.Status == PromptStatus.OK)
                                    {
                                        transaction.Dispose();
                                        return;
                                    }

                                    BlockReference blockReference = new BlockReference(blockReferencePointResult.Value, block.ObjectId);

                                    BlockTableRecord currentSpace = (BlockTableRecord)transaction.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                                    currentSpace.AppendEntity(blockReference);
                                    transaction.AddNewlyCreatedDBObject(blockReference, true);

                                    transaction.Commit();
                                }
                            }
                            catch (System.Exception ex)
                            {
                                editor.WriteMessage($"ERROR: {ex.Message}");
                            }
                            finally
                            {
                                transaction.Dispose();
                            }
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public PaletteSet paletteSet;

        public Lab5.UserControl1 userControl;

        [CommandMethod("palette")]
        public void palette()
        {
            if (paletteSet == null)
            {
                paletteSet = new PaletteSet("Palette", new System.Guid("E05F4A1B-F03C-422E-A9ED-C6F64C5F6973"));

                userControl = new Lab5.UserControl1();

                paletteSet.Add("Palette1", userControl);
            }

            paletteSet.Visible = true;
        }

        [CommandMethod("addDBEvents")]
        public void addDBEvents()
        {
            if (userControl == null)
            {
                Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

                editor.WriteMessage("\nPlease call the 'palette' command");

                return;
            }

            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            db.ObjectAppended += new ObjectEventHandler(callback_ObjectAppended);
            db.ObjectErased += new ObjectErasedEventHandler(callback_ObjectErased);
            db.ObjectReappended += new ObjectEventHandler(callback_ObjectReappended);
            db.ObjectUnappended += new ObjectEventHandler(callback_ObjectUnappended);
        }

        [CommandMethod("addData")]
        public void addData()
        {
            Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

            PromptEntityResult promptEntityResult = editor.GetEntity("Pick an entity: ");

            if(promptEntityResult.Status == PromptStatus.OK)
            {
                Transaction transaction = editor.Document.Database.TransactionManager.StartTransaction();

                try
                {
                    Entity entity = (Entity)transaction.GetObject(promptEntityResult.ObjectId, OpenMode.ForRead);

                    if(entity.ExtensionDictionary.IsNull)
                    {
                        entity.UpgradeOpen();

                        entity.CreateExtensionDictionary();
                    }

                    DBDictionary dictionary = (DBDictionary)transaction.GetObject(entity.ExtensionDictionary, OpenMode.ForRead);

                    if(dictionary.Contains("Data"))
                    {
                        ObjectId entryId = dictionary.GetAt("Data");

                        editor.WriteMessage("\nThis entity already has data");

                        Xrecord xrecord = default(Xrecord);

                        xrecord = (Xrecord)transaction.GetObject(entryId, OpenMode.ForRead);

                        foreach(TypedValue value in xrecord.Data) 
                        {
                            editor.WriteMessage($"\n{value.TypeCode.ToString()} . {value.Value.ToString()}");
                        }
                    }
                    else
                    {
                        dictionary.UpgradeOpen();

                        Xrecord xrecord = new Xrecord();

                        ResultBuffer buffer = new ResultBuffer(
                                new TypedValue((int)DxfCode.Int16, 1),
                                new TypedValue((int)DxfCode.Text, "MyStockData"),
                                new TypedValue((int)DxfCode.Real, 51.9),
                                new TypedValue((int)DxfCode.Real, 100.0),
                                new TypedValue((int)DxfCode.Real, 320.6)
                            );  

                        xrecord.Data = buffer;

                        dictionary.SetAt("Data", xrecord);

                        transaction.AddNewlyCreatedDBObject(xrecord, true);

                        if(userControl != null) 
                        {
                            foreach (System.Windows.Forms.TreeNode node in userControl.treeView1.Nodes)
                            {
                                if(node.Tag.ToString() == entity.ObjectId.ToString())
                                {
                                    System.Windows.Forms.TreeNode childNode = node.Nodes.Add("Extension Dictionary");

                                    foreach (TypedValue value in xrecord.Data)
                                    {
                                        childNode.Nodes.Add(value.ToString());
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (System.Exception ex)
                {
                    editor.WriteMessage($"ERROR: {ex.Message}");
                }
                finally
                {
                    transaction.Dispose();
                }
            }
        }

        [CommandMethod("addDataToNOD")]
        public void addDataToNOD()
        {
            Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
            Transaction transaction = editor.Document.TransactionManager.StartTransaction();

            try
            {
                DBDictionary nod = (DBDictionary)transaction.GetObject(editor.Document.Database.NamedObjectsDictionaryId, OpenMode.ForRead);

                if(nod.Contains("Data"))
                {
                    ObjectId entryId = nod.GetAt("Data");

                    editor.WriteMessage("\nThis entity already has data");

                    Xrecord xrecord = null;

                    xrecord = (Xrecord)transaction.GetObject(entryId, OpenMode.ForRead);

                    foreach (TypedValue value in xrecord.Data)
                    {
                        editor.WriteMessage($"\n{value.TypeCode.ToString()} . {value.Value.ToString()}");
                    }
                }
                else
                {
                    nod.UpgradeOpen();

                    Xrecord xrecord = new Xrecord();

                    ResultBuffer buffer = new ResultBuffer(
                            new TypedValue((int)DxfCode.Int16, 1),
                            new TypedValue((int)DxfCode.Text, "MyCompanyDefaultSettings"),
                            new TypedValue((int)DxfCode.Real, 51.9),
                            new TypedValue((int)DxfCode.Real, 100.0),
                            new TypedValue((int)DxfCode.Real, 320.6)
                        );

                    xrecord.Data = buffer;

                    nod.SetAt("Data", xrecord);

                    transaction.AddNewlyCreatedDBObject(xrecord, true);
                }

                transaction.Commit();
            }
            catch (System.Exception ex) 
            {
                editor.WriteMessage($"ERROR: {ex.Message}");
            }
            finally
            {
                transaction.Dispose();
            }
        }

        [CommandMethod("addPointMonitor")]
        public void startMonitor()
        {
            Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

            editor.PointMonitor += new PointMonitorEventHandler(MyPointMonitor);
        }

        [CommandMethod("newInput")]
        public void newInput()
        {
            Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

            editor.PointMonitor += new PointMonitorEventHandler(MyInputMonitor);

            editor.TurnForcedPickOn();

            PromptPointOptions promptPointOptions = new PromptPointOptions("Pick a point: ");
            PromptPointResult promptPointResult = editor.GetPoint(promptPointOptions);

            //if(promptPointResult.Status == PromptStatus.OK)
            //{

            //}

            editor.PointMonitor -= new PointMonitorEventHandler(MyInputMonitor);
        }

        [CommandMethod("circleJig")]
        public void CircleJig()
        {
            Circle circle = new Circle(Point3d.Origin, Vector3d.ZAxis, 10);

            MyCircleJig myCircleJig = new MyCircleJig(circle);

            for(int i = 0; i < 1; i++)
            {
                myCircleJig.CurrentInput = i;

                Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

                PromptResult promptResult = editor.Drag(myCircleJig);

                if(promptResult.Status == PromptStatus.Cancel | promptResult.Status == PromptStatus.Error)
                {
                    return;
                }

                Database db = Application.DocumentManager.MdiActiveDocument.Database;

                Transaction transaction = db.TransactionManager.StartTransaction();

                try
                {
                    BlockTableRecord blockTableRecord = (BlockTableRecord)transaction.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    blockTableRecord.AppendEntity(circle);

                    transaction.AddNewlyCreatedDBObject(circle, true);

                    transaction.Commit();
                }
                catch (System.Exception ex)
                {

                }
                finally
                {
                    transaction.Dispose();
                }
            }
        }


        private void callback_ObjectAppended(Object sender, ObjectEventArgs e)
        {
            System.Windows.Forms.TreeNode newNode = userControl.treeView1.Nodes.Add(e.DBObject.GetType().ToString());

            newNode.Tag = e.DBObject.ObjectId.ToString();
        }

        private void callback_ObjectErased(Object sender, ObjectErasedEventArgs e)
        {
            if (e.Erased)
            {
                foreach (System.Windows.Forms.TreeNode node in userControl.treeView1.Nodes)
                {
                    if (node.Tag.ToString() == e.DBObject.ObjectId.ToString())
                    {
                        node.Remove();

                        break;
                    }
                }
            }
            else
            {
                System.Windows.Forms.TreeNode newNode = userControl.treeView1.Nodes.Add(e.DBObject.GetType().ToString());

                newNode.Tag = e.DBObject.ObjectId.ToString();
            }
        }

        private void callback_ObjectReappended(Object sender, ObjectEventArgs e)
        {
            System.Windows.Forms.TreeNode newNode = userControl.treeView1.Nodes.Add(e.DBObject.GetType().ToString());

            newNode.Tag = e.DBObject.ObjectId.ToString();
        }

        private void callback_ObjectUnappended(Object sender, ObjectEventArgs e)
        {
            foreach (System.Windows.Forms.TreeNode node in userControl.treeView1.Nodes)
            {
                if (node.Tag.ToString() == e.DBObject.ObjectId.ToString())
                {
                    node.Remove();

                    break;
                }
            }
        }

        public void MyPointMonitor(object sender, PointMonitorEventArgs e)
        {
            if (e.Context == null)
                return;

            FullSubentityPath[] fullEntPath = e.Context.GetPickedEntities();

            if(fullEntPath.Length > 0) 
            {
                Transaction transaction = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();

                try
                {
                    Entity entity = (Entity)transaction.GetObject((ObjectId)fullEntPath[0].GetObjectIds()[0], OpenMode.ForRead);

                    e.AppendToolTipText($"The Entity is: {entity.GetType().ToString()}");

                    if (userControl == null)
                        return;

                    System.Drawing.Font fontRegular = new System.Drawing.Font("Microsoft Sans Serif", 8, System.Drawing.FontStyle.Regular);
                    System.Drawing.Font fontBold = new System.Drawing.Font("Microsoft Sans Serif", 8, System.Drawing.FontStyle.Bold);

                    this.userControl.treeView1.SuspendLayout();

                    foreach (System.Windows.Forms.TreeNode node in userControl.treeView1.Nodes)
                    {
                        if(node.Tag.ToString() == entity.ObjectId.ToString())
                        {
                            if (!fontBold.Equals(node.NodeFont))
                            {
                                node.NodeFont = fontBold;
                                node.Text = node.Text;
                            }
                        }
                        else
                        {
                            if(!fontRegular.Equals(node.NodeFont))
                                node.NodeFont = fontRegular;
                        }
                    }

                    this.userControl.treeView1.ResumeLayout();
                    this.userControl.treeView1.Refresh();
                    this.userControl.treeView1.Update();

                    transaction.Commit();
                }
                catch (System.Exception ex)
                {
                    
                }
                finally
                {
                    transaction.Dispose();
                }
            }
        }

        public void MyInputMonitor(object sender, PointMonitorEventArgs e)
        {
            if(e.Context == null)
                return;

            FullSubentityPath[] fullEndPath = e.Context.GetPickedEntities();

            if(fullEndPath.Length > 0)
            {
                Transaction transaction = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();

                try
                {
                    Curve entity = (Curve)transaction.GetObject(fullEndPath[0].GetObjectIds()[0], OpenMode.ForRead);

                    if(entity.ExtensionDictionary.IsValid) 
                    {
                        DBDictionary extensionDictionary = (DBDictionary)transaction.GetObject(entity.ExtensionDictionary, OpenMode.ForRead);

                        ObjectId entryId = extensionDictionary.GetAt("Data");

                        Xrecord xrecord;

                        xrecord = (Xrecord)transaction.GetObject(entryId, OpenMode.ForRead);

                        foreach (TypedValue value in xrecord.Data)
                        {
                            if(value.TypeCode == (short)DxfCode.Real)
                            {
                                Point3d point = entity.GetPointAtDist((double)value.Value);

                                Point2d pixels = e.Context.DrawContext.Viewport.GetNumPixelsInUnitSquare(point);

                                double xDistance = (10 / pixels.X);
                                double yDistance = (10 / pixels.Y);

                                Circle circle = new Circle(point, Vector3d.ZAxis, xDistance);
                                e.Context.DrawContext.Geometry.Draw(circle);

                                DBText text = new DBText();

                                text.SetDatabaseDefaults();

                                text.Position = (point + new Vector3d(xDistance, yDistance, 0));

                                text.TextString = value.Value.ToString();
                                text.Height = yDistance;
                                text.ColorIndex = 5;

                                e.Context.DrawContext.Geometry.Draw(text);
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (System.Exception ex)
                { 

                }
                finally
                {
                    transaction.Dispose();
                }
            }
        }
    }

    class MyCircleJig : EntityJig
    {
        private Point3d centerPoint;
        private double radius;
        private int currentInputValue;

        public int CurrentInput
        {
            get { return currentInputValue; }
            set { currentInputValue = value; }
        }

        public MyCircleJig(Entity entity) : base(entity)
        {

        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            switch (currentInputValue)
            {
                case 0:
                    Point3d oldPnt = centerPoint;

                    PromptPointResult jigPromptResult = prompts.AcquirePoint("Pick center point: ");

                    if(jigPromptResult.Status == PromptStatus.OK)
                    {
                        centerPoint = jigPromptResult.Value;

                        if (oldPnt.DistanceTo(centerPoint) < 0.001)
                            return SamplerStatus.NoChange;
                    }

                    return SamplerStatus.OK;

                case 1:
                    double oldRadius = radius;

                    JigPromptDistanceOptions jigPromptDistanceOptions = new JigPromptDistanceOptions("Pick radius: ");
                    jigPromptDistanceOptions.BasePoint = centerPoint;

                    PromptDoubleResult promptDoubleResult = prompts.AcquireDistance(jigPromptDistanceOptions);

                    if(promptDoubleResult.Status == PromptStatus.OK)
                    {
                        radius = promptDoubleResult.Value;

                        if (System.Math.Abs(radius) < 0.1)
                            radius = 1;

                        if (System.Math.Abs(oldRadius - radius) < 0.001)
                            return SamplerStatus.NoChange;
                    }

                    return SamplerStatus.OK;

                default:
                    return SamplerStatus.NoChange;
            }
        }

        protected override bool Update()
        {
            switch (currentInputValue)
            {
                case 0:
                    ((Circle)this.Entity).Center = centerPoint;
                    break;

                case 1:
                    ((Circle)this.Entity).Radius = radius;
                    break;

                default:
                    break;
            }

            return true;
        }
    }
}
