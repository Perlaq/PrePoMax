﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Windows.Forms;
using System.Drawing;

namespace PrePoMax.Forms
{
    class FrmRotate : UserControls.FrmProperties, IFormBase
    {
        // Variables                                                                                                                
        private RotateParameters _rotateParameters;
        private Controller _controller;
        private string[] _partNames;
        private double[][] _coorNodesToDraw;
        private ContextMenuStrip cmsProperyGrid;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem tsmiResetAll;
        private double[][] _coorLinesToDraw;


        // Properties                                                                                                               
        public string[] PartNames { get { return _partNames; } set { _partNames = value; } }
        public double[] RotateCenter
        {
            get
            {
                return new double[] { _rotateParameters.X1, _rotateParameters.Y1, _rotateParameters.Z1 };
            }
        }
        public double[] RotateAxis
        {
            get
            {
                return new double[] { _rotateParameters.X2 - _rotateParameters.X1,
                                      _rotateParameters.Y2 - _rotateParameters.Y1,
                                      _rotateParameters.Z2 - _rotateParameters.Z1};
            }
        }


        // Constructors                                                                                                             
        public FrmRotate(Controller controller) 
            : base(2)
        {
            InitializeComponent();

            _controller = controller;
            _rotateParameters = new RotateParameters();
            propertyGrid.SelectedObject = _rotateParameters;

            _coorNodesToDraw = new double[1][];
            _coorNodesToDraw[0] = new double[3];

            _coorLinesToDraw = new double[2][];
            _coorLinesToDraw[0] = new double[3];
            _coorLinesToDraw[1] = new double[3];

            btnOK.Visible = false;
            btnOkAddNew.Width = btnOK.Width;
            btnOkAddNew.Left = btnOK.Left;
            btnOkAddNew.Text = "Apply";
            btnCancel.Text = "Close";
        }
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cmsProperyGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiResetAll = new System.Windows.Forms.ToolStripMenuItem();
            this.gbProperties.SuspendLayout();
            this.cmsProperyGrid.SuspendLayout();
            this.SuspendLayout();
            // 
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsProperyGrid;
            // 
            // cmsProperyGrid
            // 
            this.cmsProperyGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiResetAll});
            this.cmsProperyGrid.Name = "cmsProperyGrid";
            this.cmsProperyGrid.Size = new System.Drawing.Size(181, 48);
            // 
            // tsmiResetAll
            // 
            this.tsmiResetAll.Name = "tsmiResetAll";
            this.tsmiResetAll.Size = new System.Drawing.Size(180, 22);
            this.tsmiResetAll.Text = "Reset all";
            this.tsmiResetAll.Click += new System.EventHandler(this.tsmiResetAll_Click);
            // 
            // FrmRotate
            // 
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmRotate";
            this.Text = "Rotate Parameters";
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.gbProperties.ResumeLayout(false);
            this.cmsProperyGrid.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Overrides                                                                                                                
        protected override void OnPropertyGridPropertyValueChanged()
        {
            HighlightNodes();

            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            double angle = _rotateParameters.AngleDeg * Math.PI / 180;
            _controller.RotateModelPartsCommand(_partNames, RotateCenter, RotateAxis, angle, _rotateParameters.Copy);

            HighlightNodes();
        }


        // Methods                                                                                                                  
        public bool PrepareForm(string stepName, string partToEditName)
        {
            _controller.ClearSelectionHistory();
            _rotateParameters.Clear();
            

            // Get start point grid item
            GridItem gi = propertyGrid.EnumerateAllItems().First((item) =>
                              item.PropertyDescriptor != null &&
                              item.PropertyDescriptor.Name == "StartPointItemSet");

            // Select it
            gi.Select();

            propertyGrid.Refresh();

            return OnPrepareForm(stepName, partToEditName);
        }
        public void PickedIds(int[] ids)
        {
            this.Enabled = true;

            _controller.SelectBy = vtkSelectBy.Off;
            _controller.Selection.SelectItem = vtkSelectItem.None;
            _controller.ClearSelectionHistory();

            if (ids != null && ids.Length == 1)
            {
                FeNode node = _controller.Model.Mesh.Nodes[ids[0]];
                string propertyName = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                if (propertyName == "StartPointItemSet")
                {
                    _rotateParameters.X1 = node.X;
                    _rotateParameters.Y1 = node.Y;
                    _rotateParameters.Z1 = node.Z;
                }
                else if(propertyName == "EndPointItemSet")
                {
                    _rotateParameters.X2 = node.X;
                    _rotateParameters.Y2 = node.Y;
                    _rotateParameters.Z2 = node.Z;
                }

                propertyGrid.Refresh();

                HighlightNodes();
            }
        }
        private void HighlightNodes()
        {
            Color color = Color.Red;
            vtkControl.vtkRendererLayer layer = vtkControl.vtkRendererLayer.Selection;

            _coorNodesToDraw[0][0] = _rotateParameters.X2;
            _coorNodesToDraw[0][1] = _rotateParameters.Y2;
            _coorNodesToDraw[0][2] = _rotateParameters.Z2;

            _coorLinesToDraw[0][0] = _rotateParameters.X1;
            _coorLinesToDraw[0][1] = _rotateParameters.Y1;
            _coorLinesToDraw[0][2] = _rotateParameters.Z1;
            _coorLinesToDraw[1] = _coorNodesToDraw[0];

            _controller.DrawNodes("Rotate", _coorNodesToDraw, color, layer, 7);
            _controller.HighlightConnectedLines(_coorLinesToDraw);
        }

        private void tsmiResetAll_Click(object sender, EventArgs e)
        {
            _rotateParameters = new RotateParameters();
            propertyGrid.SelectedObject = _rotateParameters;
            _controller.ClearAllSelection();
        }
    }
}
