using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.Windows.Forms;
using CaeGlobals;

namespace PrePoMax.Forms
{
    class FrmElementSet : UserControls.FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private HashSet<string> _allExistingNames;
        private string _elementSetToEditName;
        private ViewElementSet _viewElementSet;
        private List<SelectionNode> _prevSelectionNodes;
        private SelectionNodeIds _selectionNodeIds;
        private Controller _controller;


        // Properties                                                                                                               
        public FeElementSet ElementSet
        {
            get { return _viewElementSet.GetBase(); }
            set { _viewElementSet = new ViewElementSet(value.DeepClone()); }
        }


        // Constructors                                                                                                             
        public FrmElementSet(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewElementSet = null;
            _allExistingNames = new HashSet<string>();
        }
        private void InitializeComponent()
        {
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // FrmElementSet
            // 
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmElementSet";
            this.Text = "Edit Element Set";
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);
        }


        // Overrides                                                                                                                
        protected override void OnApply(bool onOkAddNew)
        {
            _viewElementSet = (ViewElementSet)propertyGrid.SelectedObject;            
            //
            if ((_elementSetToEditName == null && _allExistingNames.Contains(_viewElementSet.Name)) ||                   // named to existing name
                (_viewElementSet.Name != _elementSetToEditName && _allExistingNames.Contains(_viewElementSet.Name)))     // renamed to existing name
                throw new CaeException("The selected name already exists.");
            //
            if (ElementSet.Labels == null || ElementSet.Labels.Length <= 0)
                throw new CaeException("The element set must contain at least one item.");
            //
            if (_elementSetToEditName == null)
            {
                // Create
                _controller.AddElementSetCommand(ElementSet);
            }
            else
            {
                // Replace
                if (_propertyItemChanged || !ElementSet.Valid)
                {
                    // replace the ids by the previous selection
                    Selection selection = ElementSet.CreationData;
                    if (selection.Nodes[0] is SelectionNodeIds sn && sn.Equals(_selectionNodeIds))
                    {
                        selection.Nodes.RemoveAt(0);
                        selection.Nodes.InsertRange(0, _prevSelectionNodes);
                    }
                    //
                    ElementSet.Valid = true;
                    _controller.ReplaceElementSetCommand(_elementSetToEditName, ElementSet);
                }
            }
            // If all is successful close the ItemSetSelectionForm - except for OKAddNew
            if (!onOkAddNew) ItemSetDataEditor.SelectionForm.Hide();
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string elementSetToEditName)
        {
            this.DialogResult = DialogResult.None;      // to prevent the call to frmMain.itemForm_VisibleChanged when minimized
            this.btnOkAddNew.Visible = elementSetToEditName == null;
            //
            _controller.SetSelectItemToElement();
            //
            _propertyItemChanged = false;
            _allExistingNames.Clear();
            _elementSetToEditName = null;
            _viewElementSet = null;
            propertyGrid.SelectedObject = null;
            _prevSelectionNodes = null;
            _selectionNodeIds = null;
            //
            _allExistingNames.UnionWith(_controller.GetAllMeshEntityNames());
            _elementSetToEditName = elementSetToEditName;
            //
            if (_elementSetToEditName == null)
            {
                ElementSet = new FeElementSet(GetElementSetName(), null);
                _controller.Selection.Clear();
            }
            else
            {
                ElementSet = _controller.GetElementSet(_elementSetToEditName);  // to clone
                int[] ids = ElementSet.Labels;
                if (ElementSet.CreationData == null && ids != null)
                {
                    // Add creation data                    
                    ElementSet.CreationData = new Selection();
                    ElementSet.CreationData.SelectItem = vtkSelectItem.Element;
                    ElementSet.CreationData.Add(new SelectionNodeIds(vtkSelectOperation.Add, false, ids));
                }
                // Change node selection history to ids to speed up
                _selectionNodeIds = new SelectionNodeIds(vtkSelectOperation.None, false, ids);
                _prevSelectionNodes = ElementSet.CreationData.Nodes;
                _controller.CreateNewSelection(ElementSet.CreationData.CurrentView, _selectionNodeIds, true);
                ElementSet.CreationData = _controller.Selection.DeepClone();
            }
            //
            propertyGrid.SelectedObject = _viewElementSet;
            propertyGrid.Select();
            // Show ItemSetDataForm
            ItemSetDataEditor.SelectionForm.ItemSetData = new ItemSetData(ElementSet.Labels);
            ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
            //
            _controller.HighlightSelection();
            //
            return true;
        }


        // Methods                                                                                                                  
        private string GetElementSetName()
        {
            return NamedClass.GetNewValueName(_allExistingNames, "Element_set-");
        }
        private void HighlightElementSet()
        {
            try
            {
                if (ElementSet.CreationData != null)
                {
                    _controller.Selection = ElementSet.CreationData.DeepClone();
                    _controller.HighlightSelection();
                }
            }
            catch { }
        }
        //
        public void SelectionChanged(int[] ids)
        {
            ElementSet.Labels = ids;
            ElementSet.CreationData = _controller.Selection.DeepClone();
            //
            propertyGrid.Refresh();
            //
            _propertyItemChanged = true;
        }

        // IFormHighlight
        public void Highlight()
        {
            HighlightElementSet();
        }
        
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            if (_elementSetToEditName == null) return true;
            //
            FeElementSet elementSet = _controller.GetElementSet(_elementSetToEditName);
            if (elementSet.CreationData == null) return false;
            else return elementSet.CreationData.IsGeometryBased(); // ElementSet was modified for speed up
        }
    }
}
