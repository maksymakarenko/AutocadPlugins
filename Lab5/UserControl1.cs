using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab5
{
    public partial class UserControl1 : UserControl
    {
        public UserControl1()
        {
            InitializeComponent();

            DragLabel.MouseMove += new System.Windows.Forms.MouseEventHandler(DragLabel_MouseMove);
        }

        private void DragLabel_MouseMove(Object sender, MouseEventArgs e)
        {
            if(System.Windows.Forms.Control.MouseButtons == System.Windows.Forms.MouseButtons.Left)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.DoDragDrop(this, this, DragDropEffects.All, new MyDropTarget());
            }
        }

        private void UserControl1_Load(object sender, EventArgs e)
        {

        }
    }

    public class MyDropTarget : Autodesk.AutoCAD.Windows.DropTarget
    {
        public override void OnDrop(DragEventArgs e)
        {
            Editor editor = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                using (DocumentLock document = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                {
                    Lab5.Class1.AddAnEntity();
                }
            }
            catch (Exception ex)
            {
                editor.WriteMessage($"ERROR OnDrop: {ex.ToString()}");
            }
        }
    }
}
