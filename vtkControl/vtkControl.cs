using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Kitware.VTK;
using System.Runtime.InteropServices;
using System.Linq;
using CaeGlobals;
using System.Threading.Tasks;
using System.Management;

namespace vtkControl
{
    /// <summary>
    /// UserControl derived implementation of vtkRenderWindow for use
    /// in Windows Forms applications.
    /// The client area of this UserControl is completely filled with
    /// an instance of a vtkRenderWindow.
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true), System.Runtime.InteropServices.ClassInterface(System.Runtime.InteropServices.ClassInterfaceType.AutoDual)]
    public partial class vtkControl : UserControl
    {
        int countError;


        // Variables                                                                                                                
        private bool _renderingOn;
        private vtkRenderer _renderer;
        private vtkRenderer _selectionRenderer;
        private vtkRenderer _overlayRenderer;
        private vtkRenderWindow _renderWindow;
        private vtkRenderWindowInteractor _renderWindowInteractor;
        private vtkOrientationMarkerWidget _coorSys;
        private vtkMaxScaleWidget _scaleWidget;
        private vtkLookupTable _lookupTable;
        private vtkMaxScalarBarWidget _scalarBarWidget;
        private vtkMaxStatusBlockWidget _statusBlockWidget;
        private vtkInteractorStyleControl _style;
        private bool _drawCoorSys;
        private vtkEdgesVisibility _edgesVisibility;
        //
        private vtkMaxColorSpectrum _colorSpectrum;
        //
        private vtkMaxTextWithArrowWidget _minValueWidget;
        private vtkMaxTextWithArrowWidget _maxValueWidget;
        private vtkMaxTextWidget _probeWidget;
        //
        private Dictionary<string, vtkMaxActor> _actors;
        private List<vtkMaxActor> _selectedActors;
        private Dictionary<string, vtkActor> _overlayActors;
        private Dictionary<string, vtkMaxActor[]> _animationActors;
        private vtkMaxAnimationFrameData _animationFrameData;
        private bool _animationAcceleration;
        private Color _primaryHighlightColor;
        private Color _secundaryHighlightColor;
        private double _maxSymbolSize;
        private bool _drawSymbolEdges;
        //
        private bool _animating;
        private bool _mouseIn;
        //
        OrderedDictionary<vtkTransform, int> _transforms;
        //
        private bool _sectionView;
        private vtkPlane _sectionViewPlane;
        // Selection
        private vtkSelectItem _selectItem;
        private vtkSelectBy _selectBy;
        private vtkPropPicker _propPicker;
        private vtkPointPicker _pointPicker;
        private vtkCellPicker _cellPicker;
        private vtkRenderedAreaPicker _areaPicker;
        //
        private vtkMaxActor _mouseSelectionActorCurrent;
        private HashSet<int> _mouseSelectionAllIds;
        private int[] _mouseSelectionCurrentIds;
        //
        private HashSet<string> _selectableActorsFilter;
        //
        private object myLock = new object();


        // Properties                                                                                                               
        public bool RenderingOn 
        { 
            get { return _renderingOn; }
            set 
            {
                if (value != _renderingOn)
                {
                    _renderingOn = value;
                    if (_renderingOn) this.Invalidate();
                }
            }
        }
       
        public vtkEdgesVisibility EdgesVisibility
        {
            get { return _edgesVisibility; }
            set
            {
                if (_edgesVisibility != value)
                {
                    _edgesVisibility = value;
                    ApplyEdgesVisibilityAndBackfaceCulling();
                }
            }
        }
        public bool ShowMinValueLocation
        { 
            get { return _minValueWidget.GetVisibility() == 1; } 
            set
            {
                if (value) _minValueWidget.VisibilityOn();
                else
                {
                    _minValueWidget.VisibilityOff();
                    _minValueWidget.ResetInitialPosition();
                }
            } 
        }
        public bool ShowMaxValueLocation 
        {
            get { return _maxValueWidget.GetVisibility() == 1; }
            set
            {
                if (value) _maxValueWidget.VisibilityOn();
                else
                {
                    _maxValueWidget.VisibilityOff();
                    _maxValueWidget.ResetInitialPosition();
                }
            }
        }
        public vtkSelectBy SelectBy
        {
            get { return _selectBy; }
            set
            {
                if (_selectBy != value)
                {
                    _selectBy = value;
                    //
                    if (_selectBy == vtkSelectBy.Off) _style.Selection = false;
                    else _style.Selection = true;
                    //
                    switch (_selectBy)
                    {
                        // General
                        case vtkSelectBy.Off:
                        // Mesh based
                        case vtkSelectBy.Node:
                        case vtkSelectBy.Element:
                        case vtkSelectBy.Part:
                        // Geometry
                        case vtkSelectBy.Geometry:
                            _style.RubberBandEnabled = true;
                            break;
                        // Mesh based
                        case vtkSelectBy.Id:
                        case vtkSelectBy.Edge:
                        case vtkSelectBy.Surface:
                        case vtkSelectBy.EdgeAngle:
                        case vtkSelectBy.SurfaceAngle:
                        // Geometry
                        case vtkSelectBy.GeometryVertex:
                        case vtkSelectBy.GeometryEdge:
                        case vtkSelectBy.GeometrySurface:
                        case vtkSelectBy.GeometryEdgeAngle:
                        case vtkSelectBy.GeometrySurfaceAngle:
                        // Querry
                        case vtkSelectBy.QueryNode:
                        case vtkSelectBy.QueryElement:
                        case vtkSelectBy.QueryEdge:
                        case vtkSelectBy.QuerySurface:
                        case vtkSelectBy.QueryPart:
                            _style.RubberBandEnabled = false;
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    ClearCurrentMouseSelection();
                }
            }
        }
        public vtkSelectItem SelectItem { get { return _selectItem; } set { if (_selectItem != value) { _selectItem = value; } } }
        

        // Setters                                                                                                                  
        public void SetSelectBy(vtkSelectBy selectBy)
        {
            SelectBy = selectBy;
        }
        public void SetAnimationAcceleration(bool animationAcceleration)
        {
            _animationAcceleration = animationAcceleration;
        } 
        public void SetSelectableActorsFilter(string[] selectableActors)
        {
            if (selectableActors != null) _selectableActorsFilter = new HashSet<string>(selectableActors);
            else _selectableActorsFilter = null;
        }


        // Callbacks                                                                                                                
        public Func<int[], vtkMaxActorData> Controller_GetNodeActorData;
        public Func<int[], int[], vtkMaxActorData> Controller_GetCellActorData;
        public Func<int, int[], vtkMaxActorData> Controller_GetCellFaceActorData;
        public Func<int, int[], vtkMaxActorData> Controller_GetEdgeActorData;
        public Func<int, int[], vtkMaxActorData> Controller_GetSurfaceEdgesActorDataFromElementId;
        public Func<int[], int[], vtkMaxActorData> Controller_GetSurfaceEdgesActorDataFromNodeAndElementIds;
        public Func<int[], vtkMaxActorData> Controller_GetPartActorData;
        public Func<double[], int, int[], int[], vtkMaxActorData> Controller_GetGeometryActorData;
        public Func<double[], int, int[], int[], vtkMaxActorData> Controller_GetGeometryVertexActorData;

        public Action<MouseEventArgs, Keys, string[]> Controller_ActorsPicked;
        public Action Controller_ShowLegendSettings;
        public Action Controller_ShowStatusBlockSettings;


        // Events                                                                                                                   
        public event Action<double[], double[][], vtkSelectOperation> OnMouseLeftButtonUpSelection;
        public event Action<sbyte> KeyPressEvent;


        // Constructors                                                                                                             
        public vtkControl()
        {
            InitializeComponent();
            //
            Globals.Initialize();
            //
            Controller_GetNodeActorData = null;
            Controller_GetCellActorData = null;
            //
            _renderingOn = true;
            _drawCoorSys = true;
            _edgesVisibility = vtkEdgesVisibility.ElementEdges;
            _colorSpectrum = new vtkMaxColorSpectrum();
            //
            _actors = new Dictionary<string, vtkMaxActor>();
            _selectedActors = new List<vtkMaxActor>();
            _overlayActors = new Dictionary<string, vtkActor>();
            _animationActors = new Dictionary<string, vtkMaxActor[]>();
            _animationFrameData = null;
            _primaryHighlightColor = Color.Red;
            _secundaryHighlightColor = Color.Violet;
            _drawSymbolEdges = true;
            //
            _animating = false;
            //
            _transforms = new OrderedDictionary<vtkTransform, int>();
            //
            _sectionView = false;
            _sectionViewPlane = null;
            //
            SelectBy = vtkSelectBy.Off;
            _selectItem = vtkSelectItem.None;
            //
            _propPicker = vtkPropPicker.New();
            _propPicker.PickFromListOn();
            _propPicker.InitializePickList();
            //
            _pointPicker = vtkPointPicker.New();
            _pointPicker.SetTolerance(0.01);
            //
            _cellPicker = vtkCellPicker.New();
            _cellPicker.SetTolerance(0.01);
            //
            _areaPicker = vtkRenderedAreaPicker.New();
            _mouseSelectionActorCurrent = null;
            _mouseSelectionCurrentIds = null;
            _mouseSelectionAllIds = new HashSet<int>();
            //
            _selectableActorsFilter = null;
        }


        // Event handlers                                                                                                           
        private void vtkControl_Resize(object sender, EventArgs e)
        {
            if (_renderWindow != null) _renderWindow.Modified();     // this has to be here in order for the vtkMAx widgets to work on maximize/minimize
            //if (_renderWindowInteractor != null) _renderWindowInteractor.Render();

            if (_coorSys != null ) SetCoorSysVisibility(_drawCoorSys);
            if (_renderer != null)
            {
                //float scale = vtkTextActor.GetFontScale(_renderer);
                //Console.WriteLine(scale.ToString());

                //int[] size = _renderer.GetSize();
                //foreach (var actor in _actors)
                //{
                //    if (actor is vtkScalarBarActor)
                //    {
                //        vtkScalarBarActor sb = (vtkScalarBarActor)actor;
                //        int height = 300;
                //        int border = 20;
                //        sb.SetDisplayPosition(border, size[1] - border - height);
                //    }
                //}
            }
        }
        private void vtkControl_Load(object sender, EventArgs e)
        {
            InitializeControl();
        }

        void _renderWindowInteractor_ModifiedEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_style.GetState() == vtkInteractorStyleControl.VTKIS_ROTATE && _probeWidget != null && _probeWidget.GetVisibility() == 1)
            {
                _probeWidget.VisibilityOff();
            }
        }

        void _style_EnterEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (!_mouseIn) _mouseIn = true;
        }
        void _style_LeaveEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_mouseIn) _mouseIn = false;
            _renderWindow.SetCurrentCursor(0);  // Default
        }

        private void widget_ShowPostSettings(object sender)
        {
            if (sender is vtkMaxScalarBarWidget) Controller_ShowLegendSettings?.Invoke();
            else if (sender is vtkMaxStatusBlockWidget) Controller_ShowStatusBlockSettings?.Invoke();
        }

        // Mouse Events - Style                                                                                                     
        private void _style_PointPickedOnMouseMoveEvt(int x1, int y1, bool rubberBandSelection, int x2, int y2)
        {
            try
            {
                if (_probeWidget == null) return;
                // Default selection of parts
                else if (_selectBy == vtkSelectBy.Off)
                {
                    // Area selection
                    if (rubberBandSelection)
                    {
                        string[] pickedActorNames = null;
                        PickByArea(x1, y1, x2, y2, false, out pickedActorNames);
                        //
                        MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, x1, y1, 0);
                        //
                        Controller_ActorsPicked?.Invoke(e, ModifierKeys, pickedActorNames.ToArray());
                    }
                }
                //
                else if (rubberBandSelection)
                {
                    ClearCurrentMouseSelection();
                    PickByArea(x1, y1, x2, y2, true, out string[] pickedActorNAmes);
                }
                else
                {
                    vtkActor pickedActor = null;
                    //
                    ClearCurrentMouseSelection();
                    //
                    switch (_selectBy)
                    {
                        case vtkSelectBy.Off:
                        case vtkSelectBy.Id:
                            break;
                        case vtkSelectBy.Node:
                            PickByNode(out pickedActor, x1, y1, false);
                            break;
                        case vtkSelectBy.Element:
                            PickByCell(out pickedActor, x1, y1, false);
                            break;
                        case vtkSelectBy.Edge:
                            PickByEdge(out pickedActor, x1, y1, false);
                            break;
                        case vtkSelectBy.Surface:
                            PickBySurface(out pickedActor, x1, y1, false);
                            break;
                        case vtkSelectBy.EdgeAngle:
                            PickByEdgeAngle(out pickedActor, x1, y1);
                            break;
                        case vtkSelectBy.SurfaceAngle:
                            PickBySurfaceAngle(out pickedActor, x1, y1);
                            break;
                        case vtkSelectBy.Part:
                            PickByActor(out pickedActor, x1, y1);
                            break;
                        case vtkSelectBy.Geometry:
                            PickByGeometry(out pickedActor, x1, y1);
                            break;
                        case vtkSelectBy.GeometryVertex:
                            PickByGeometryVertex(out pickedActor, x1, y1);
                            break;
                        case vtkSelectBy.GeometryEdge:
                            PickByEdge(out pickedActor, x1, y1, false);
                            break;
                        case vtkSelectBy.GeometrySurface:
                            PickBySurface(out pickedActor, x1, y1, false);
                            break;
                        case vtkSelectBy.GeometryEdgeAngle:
                            PickByGeometryEdgeAngle(out pickedActor, x1, y1);
                            break;
                        case vtkSelectBy.GeometrySurfaceAngle:
                            PickBySurface(out pickedActor, x1, y1, false);
                            break;
                        case vtkSelectBy.QueryNode:
                            PickByNode(out pickedActor, x1, y1, true);
                            break;
                        case vtkSelectBy.QueryElement:
                            PickByCell(out pickedActor, x1, y1, true);
                            break;
                        case vtkSelectBy.QueryEdge:
                            PickByEdge(out pickedActor, x1, y1, true);
                            break;
                        case vtkSelectBy.QuerySurface:
                            PickBySurface(out pickedActor, x1, y1, true);
                            break;
                        case vtkSelectBy.QueryPart:
                            PickByActor(out pickedActor, x1, y1);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    //
                    if (pickedActor == null) ClearCurrentMouseSelection();
                }
            }
            catch { }
        }
        private void _style_PointPickedOnLeftUpEvt(int x1, int y1, bool rubberBandSelection, int x2, int y2)
        {
            // Default selection of parts
            if (_selectBy == vtkSelectBy.Off)
            {
                string[] pickedActorNames = null;
                // Point selection
                if (!rubberBandSelection)
                {
                    vtkActor pickedActor;
                    GetPickPoint(out pickedActor, x1, y1);
                    pickedActorNames = new string[] { GetActorName(pickedActor) };
                }
                // Area selection
                else
                {
                    PickByArea(x1, y1, x2, y2, false, out pickedActorNames);
                }
                MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, x1, y1, 0);
                //
                Controller_ActorsPicked?.Invoke(e, ModifierKeys, pickedActorNames.ToArray());
            }
            else
            {
                vtkSelectOperation selectOperation;
                if (Control.ModifierKeys == (Keys.Shift | Keys.Control)) selectOperation = vtkSelectOperation.Intersect;
                else if (Control.ModifierKeys == Keys.Shift) selectOperation = vtkSelectOperation.Add;
                else if (Control.ModifierKeys == Keys.Control) selectOperation = vtkSelectOperation.Subtract;
                else selectOperation = vtkSelectOperation.None;
                //
                vtkActor pickedActor = null;
                // Point selection
                if (!rubberBandSelection)
                {
                    double[] pickedPoint = GetPickPoint(out pickedActor, x1, y1);
                    OnMouseLeftButtonUpSelection?.Invoke(pickedPoint, null, selectOperation);
                }
                // Area selection
                else
                {
                    _areaPicker.AreaPick(x1, y1, x2, y2, _renderer);
                    vtkPlanes planes = _areaPicker.GetFrustum();
                    vtkPlane plane;
                    double[] origin;
                    double[] normal;
                    double[][] planeParameters = new double[planes.GetNumberOfPlanes()][];
                    for (int i = 0; i < planes.GetNumberOfPlanes(); i++)
                    {
                        plane = planes.GetPlane(i);
                        origin = plane.GetOrigin();
                        normal = plane.GetNormal();
                        planeParameters[i] = new double[] { origin[0], origin[1], origin[2], normal[0], normal[1], normal[2] };
                    }
                    OnMouseLeftButtonUpSelection?.Invoke(null, planeParameters, selectOperation);
                }
            }
        }
        private void _style_LeftButtonPressEvent(int x, int y)
        {
            //if (_selectBy == vtkSelectBy.Off)
            //{
            //    vtkActor pickedActor = null;
            //    GetPickPoint(out pickedActor, x, y);
            //    MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, x, y, 0);
            //    //
            //    Controller_ActorPicked?.Invoke(e, ModifierKeys, GetActorName(pickedActor));
            //}
        }
        private void _style_RightButtonPressEvent(int x, int y)
        {
            //if (_selectBy == vtkSelectBy.Off)
            {
                vtkActor pickedActor = null;
                GetPickPoint(out pickedActor, x, y);
                if (pickedActor != null)
                {
                    MouseEventArgs e = new MouseEventArgs(MouseButtons.Right, 1, x, y, 0);
                    Controller_ActorsPicked?.Invoke(e, ModifierKeys, new string[] { GetActorName(pickedActor) });
                }
            }
        }

        #region Selection  #########################################################################################################

        // On mouse move selection                                                                                                  
        private void PickByNode(out vtkActor pickedActor, int x, int y, bool showLabel)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }

            int globalPointId = GetNodeIdOnCellFaceClosestToPoint(pickedPoint);
            _mouseSelectionCurrentIds = new int[] { globalPointId };

            vtkMaxActorData data = Controller_GetNodeActorData(_mouseSelectionCurrentIds);
            _mouseSelectionActorCurrent = new vtkMaxActor(data, true, true);
            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);

            if (showLabel)
            {
                // Probe widget
                string format = _scalarBarWidget.GetLabelFormat();

                _renderer.SetWorldPoint(pickedPoint[0], pickedPoint[1], pickedPoint[2], 1.0);
                _renderer.WorldToDisplay();
                double[] display = _renderer.GetDisplayPoint();

                //double w = ((float)display[0] + 20) / Width;
                //double h = ((float)display[1] + 10) / Height;
                double w = x + 20d;
                double h = y + 10d;

                _probeWidget.SetPosition(w, h);
                _probeWidget.SetText("Node id: " + globalPointId);

                if (data.Geometry.Nodes.Values != null)
                {
                    // Probe widget
                    _probeWidget.SetText(_probeWidget.GetText() + Environment.NewLine +
                                         "Value: " + data.Geometry.Nodes.Values[0].ToString(format) + GetUnitAbbreviation());
                }

                if (_probeWidget.GetVisibility() == 0) _probeWidget.VisibilityOn();
            }
        }
        private void PickByCell(out vtkActor pickedActor, int x, int y, bool showLabel)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }

            vtkCell cell;
            vtkCellLocator cellLocator;
            string actorName = GetActorName(pickedActor);
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);

            _mouseSelectionCurrentIds = new int[] { globalCellId };

            vtkMaxActorData actorData = Controller_GetCellActorData(_mouseSelectionCurrentIds, null);
            vtkMaxActor actor = new vtkMaxActor(actorData, true, false);
            actor.Geometry.GetMapper().GetInput().GetPointData().RemoveArray(Globals.ScalarArrayName);
            _mouseSelectionActorCurrent = actor;
            
            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);

            if (actor.ElementEdges != null)
            {
                AddActorEdges(_mouseSelectionActorCurrent, false, vtkRendererLayer.Selection);
                _selectedActors.Remove(_mouseSelectionActorCurrent);
            }

            if (showLabel)
            {
                // Probe widget
                //if (_frustumLocators[pickedActor].GetDataSet().GetCellData().GetGlobalIds() != null)
                {
                    string format = _scalarBarWidget.GetLabelFormat();

                    double w = x + 20;
                    double h = y + 10;

                    _probeWidget.SetPosition(w, h);
                    _probeWidget.SetText("Element id: " + globalCellId);

                    if (_probeWidget.GetVisibility() == 0) _probeWidget.VisibilityOn();
                }
            }
        }
        private void PickByEdge(out vtkActor pickedActor, int x, int y, bool showLabel)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            //pickedPoint = new double[] { 69.5036385225361, 46.5475130793349, 18.5134144946738 };
            System.Diagnostics.Debug.WriteLine("x: " + pickedPoint[0] + "   y: " + pickedPoint[1] + "   z: " + pickedPoint[2]);
            //
            vtkCell cell;
            vtkCellLocator cellLocator;
            int globalPointId = GetNodeIdOnCellFaceClosestToPoint(pickedPoint);
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            vtkMaxActorData cellFaceData = Controller_GetCellFaceActorData(globalCellId, globalCellFaceNodeIds); // works on undeformed mesh
            //
            int[] nodeIds = null;
            double[][] nodeCoor = null;
            int[] edgeCell = null;
            int edgeCellType;
            GetClosestEdgeCell(pickedPoint, cellFaceData, out nodeIds, out nodeCoor, out edgeCell, out edgeCellType);

            vtkMaxActorData edgeData = Controller_GetEdgeActorData(globalCellId, nodeIds);
          
            if (edgeData != null)
            {
                vtkMaxActor actor = new vtkMaxActor(edgeData);
                _mouseSelectionActorCurrent = actor;

                AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
                _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);

                if (showLabel)
                {
                    string edgeId = edgeData.Name;

                    // Probe widget
                    string format = _scalarBarWidget.GetLabelFormat();

                    _renderer.SetWorldPoint(pickedPoint[0], pickedPoint[1], pickedPoint[2], 1.0);
                    _renderer.WorldToDisplay();
                    double[] display = _renderer.GetDisplayPoint();

                    double w = x + 20d;
                    double h = y + 10d;

                    _probeWidget.SetPosition(w, h);
                    _probeWidget.SetText("Edge id: " + edgeId);

                    if (_probeWidget.GetVisibility() == 0) _probeWidget.VisibilityOn();
                }
            }
        }
        private void PickByEdgeAngle(out vtkActor pickedActor, int x, int y)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }

            vtkCell cell;
            vtkCellLocator cellLocator;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            vtkMaxActorData cellFaceData = Controller_GetCellFaceActorData(globalCellId, globalCellFaceNodeIds); // works on undeformed mesh

            int[] nodeIds = null;
            double[][] nodeCoor = null;
            int[] edgeCell = null;
            int edgeCellType;
            GetClosestEdgeCell(pickedPoint, cellFaceData, out nodeIds, out nodeCoor, out edgeCell, out edgeCellType);


            vtkPoints p = vtkPoints.New();
            for (int i = 0; i < nodeCoor.Length; i++)
            {
                p.InsertNextPoint(nodeCoor[i][0], nodeCoor[i][1], nodeCoor[i][2]);
            }
            vtkUnstructuredGrid grid = vtkUnstructuredGrid.New();
            grid.SetPoints(p);

            vtkIdList pointIds = vtkIdList.New();
            for (int i = 0; i < edgeCell.Length; i++)  // renumber
            {
                pointIds.InsertNextId(edgeCell[i]);
            }
            grid.InsertNextCell(edgeCellType, pointIds);
            grid.Update();      // must create a new grid, not only grid from cell . is not drawn for some zoom values ???

            vtkMaxActor actor = new vtkMaxActor(grid);
            _mouseSelectionActorCurrent = actor;

            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
        }
        private void GetClosestEdgeCell(double[] pickedPoint, vtkMaxActorData cellFaceData, out int[] nodeIds,
                                        out double[][] nodeCoor, out int[] edgeCell, out int edgeCelltype)
        {
            nodeIds = null;
            nodeCoor = null;
            edgeCell = null;
            edgeCelltype = -1;
            if (cellFaceData == null) return;
            //
            double[][] allNodeCoor = cellFaceData.Geometry.Nodes.Coor;
            int[] allNodeIds = cellFaceData.Geometry.Nodes.Ids;

            int[][] cellEdges = GetVisualizationCellEdges(cellFaceData.Geometry.Nodes.Ids.Length);

            double[] d = new double[cellEdges.Length];
            for (int i = 0; i < cellEdges.Length; i++)
            {
                if (cellEdges[i].Length == 2)       // linear edge
                {
                    d[i] = PointToLineSegmentDistance(pickedPoint, allNodeCoor[cellEdges[i][0]], allNodeCoor[cellEdges[i][1]]);
                }
                else if (cellEdges[i].Length == 3)  // parabolic edge
                {
                    d[i] = Math.Min(PointToLineSegmentDistance(pickedPoint, allNodeCoor[cellEdges[i][0]], allNodeCoor[cellEdges[i][1]]),
                                    PointToLineSegmentDistance(pickedPoint, allNodeCoor[cellEdges[i][1]], allNodeCoor[cellEdges[i][2]]));
                }
            }

            double min = d[0];
            int minId = 0;
            for (int i = 1; i < d.Length; i++)
            {
                if (d[i] < min)
                {
                    min = d[i];
                    minId = i;
                }
            }

            int numOfNodes = cellEdges[minId].Length;
            nodeIds = new int[numOfNodes];
            nodeCoor = new double[numOfNodes][];
            for (int i = 0; i < nodeIds.Length; i++)
            {
                nodeIds[i] = allNodeIds[cellEdges[minId][i]];
                nodeCoor[i] = allNodeCoor[cellEdges[minId][i]];
            }
            if (numOfNodes == 2)
            {
                edgeCell = new int[] { 0, 1 };
                edgeCelltype = (int)vtkCellType.VTK_LINE;
            }
            else if (numOfNodes == 3)
            {
                edgeCell = new int[] { 0, 2, 1 };
                edgeCelltype = (int)vtkCellType.VTK_QUADRATIC_EDGE;
            }
            else throw new NotSupportedException();

            // swap edge node ids and coor so that first node is the closest
            double[] firstCoor = allNodeCoor[cellEdges[minId].First()];
            double[] lastCoor = allNodeCoor[cellEdges[minId].Last()];

            double d1 = Math.Pow(pickedPoint[0] - firstCoor[0], 2) +
                        Math.Pow(pickedPoint[1] - firstCoor[1], 2) +
                        Math.Pow(pickedPoint[2] - firstCoor[2], 2);

            double d2 = Math.Pow(pickedPoint[0] - lastCoor[0], 2) +
                        Math.Pow(pickedPoint[1] - lastCoor[1], 2) +
                        Math.Pow(pickedPoint[2] - lastCoor[2], 2);

            bool swap = d2 < d1;

            int tmpId;
            double[] tmpCoor;

            if (swap)
            {
                tmpId = nodeIds[0];
                nodeIds[0] = nodeIds[numOfNodes - 1];
                nodeIds[numOfNodes - 1] = tmpId;

                tmpCoor = nodeCoor[0];
                nodeCoor[0] = nodeCoor[numOfNodes - 1];
                nodeCoor[numOfNodes - 1] = tmpCoor;
            }
        }
        public int[][] GetVisualizationCellEdges(int numOfNodes)
        {
            // lookup - leave here for future searches

            int[] nodeIds = Enumerable.Range(0, numOfNodes).ToArray();
            if (nodeIds.Length == 3)
            {
                return new int[][] {    new int[] { nodeIds[0], nodeIds[1] },
                                        new int[] { nodeIds[1], nodeIds[2] },
                                        new int[] { nodeIds[2], nodeIds[0] } };
            }
            else if (nodeIds.Length == 4)
            {
                return new int[][] {    new int[] { nodeIds[0], nodeIds[1] },
                                        new int[] { nodeIds[1], nodeIds[2] },
                                        new int[] { nodeIds[2], nodeIds[3] },
                                        new int[] { nodeIds[3], nodeIds[0] } };
            }
            else if (nodeIds.Length == 6)
            {
                return new int[][] {    new int[] { nodeIds[0], nodeIds[3], nodeIds[1] },
                                        new int[] { nodeIds[1], nodeIds[4], nodeIds[2] },
                                        new int[] { nodeIds[2], nodeIds[5], nodeIds[0] } };
            }
            else if (nodeIds.Length == 8)
            {
                return new int[][] {    new int[] { nodeIds[0], nodeIds[4], nodeIds[1] },
                                        new int[] { nodeIds[1], nodeIds[5], nodeIds[2] },
                                        new int[] { nodeIds[2], nodeIds[6], nodeIds[3] },
                                        new int[] { nodeIds[3], nodeIds[7], nodeIds[0] } };
            }
            else throw new NotSupportedException();
        }
        private double PointToLineSegmentDistance(double[] p, double[] l1, double[] l2)
        {
            double[] n = new double[] { l2[0] - l1[0], l2[1] - l1[1], l2[2] - l1[2] };
            double d = Math.Sqrt(Math.Pow(n[0], 2) + Math.Pow(n[1], 2) + Math.Pow(n[2], 2));
            if (d <= 0) return 0;
            // normalize
            n[0] /= d;
            n[1] /= d;
            n[2] /= d;

            double[] a = new double[] { p[0] - l1[0], p[1] - l1[1], p[2] - l1[2] };
            double aProj = a[0] * n[0] + a[1] * n[1] + a[2] * n[2];

            if (aProj <= 0)
            {
                d = Math.Sqrt(Math.Pow(a[0], 2) + Math.Pow(a[1], 2) + Math.Pow(a[2], 2));
                return d;
            }
            else if (aProj >= d)
            {
                double[] b = new double[] { l2[0] - p[0], l2[1] - p[1], l2[2] - p[2] };
                d = Math.Sqrt(Math.Pow(b[0], 2) + Math.Pow(b[1], 2) + Math.Pow(b[2], 2));
                return d;
            }
            else
            {
                double[] c = new double[] { a[0] - aProj * n[0], a[1] - aProj * n[1], a[2] - aProj * n[2] };
                d = Math.Sqrt(Math.Pow(c[0], 2) + Math.Pow(c[1], 2) + Math.Pow(c[2], 2));
                return d;
            }
        }
        private void PickBySurface(out vtkActor pickedActor, int x, int y, bool showLabel)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            vtkCellLocator cellLocator;
            vtkCell cell;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            //
            vtkMaxActorData actorData = Controller_GetSurfaceEdgesActorDataFromElementId(globalCellId, globalCellFaceNodeIds);
            actorData.CanHaveElementEdges = true;
            //
            vtkMaxActor actor = new vtkMaxActor(actorData);
            _mouseSelectionActorCurrent = actor;
            //
            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
            //
            if (showLabel)
            {
                string surfaceId = actorData.Name;
                // Probe widget
                string format = _scalarBarWidget.GetLabelFormat();
                //
                _renderer.SetWorldPoint(pickedPoint[0], pickedPoint[1], pickedPoint[2], 1.0);
                _renderer.WorldToDisplay();
                double[] display = _renderer.GetDisplayPoint();
                //
                double w = x + 20d;
                double h = y + 10d;
                //
                _probeWidget.SetPosition(w, h);
                _probeWidget.SetText("Surface id: " + surfaceId);
                //
                if (_probeWidget.GetVisibility() == 0) _probeWidget.VisibilityOn();
            }
        }
        private void PickBySurfaceAngle(out vtkActor pickedActor, int x, int y)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }

            vtkCell cell;
            vtkCellLocator cellLocator;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);

            vtkMaxActorData data = Controller_GetCellFaceActorData(globalCellId, globalCellFaceNodeIds); // works on undeformed mesh

            vtkMaxActor actor = new vtkMaxActor(data);
            _mouseSelectionActorCurrent = actor;

            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);

            if (actor.ElementEdges != null)
            {
                AddActorEdges(_mouseSelectionActorCurrent, false, vtkRendererLayer.Selection);
                _selectedActors.Remove(_mouseSelectionActorCurrent);
            }
        }
        private void PickByActor(out vtkActor pickedActor, int x, int y)
        {
            double[] pickPoint = GetPickPoint(out pickedActor, x, y);
            if (pickPoint == null) return;
            //
            string actorName = GetActorName(pickedActor);
            vtkMaxActor actor = GetCopyOfModelEdgesActor(actorName);
            if (actor != null)
            {
                _mouseSelectionActorCurrent = actor;
                //
                AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
                _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
                //
                if (actor.ElementEdges != null)
                {
                    AddActorEdges(_mouseSelectionActorCurrent, false, vtkRendererLayer.Selection);
                    _selectedActors.Remove(_mouseSelectionActorCurrent);
                }
            }
        }
        private void PickByGeometry(out vtkActor pickedActor, int x, int y)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            //
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            vtkCellLocator cellLocator;
            vtkCell cell;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellEdgeNodeIds = GetEdgeNodeIds(pickedPoint, globalCellId, cell, cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            //double dist = vtkInteractorStyleControl.DisplayToWorldScale(_renderer, 7);
            //
            vtkMaxActorData actorData = Controller_GetGeometryActorData(pickedPoint,
                                                                        globalCellId, 
                                                                        globalCellEdgeNodeIds,
                                                                        globalCellFaceNodeIds);
            //
            if (actorData.Geometry.Nodes.Coor.Length == 1)
            {
                // Nodal actor data
                _mouseSelectionActorCurrent = new vtkMaxActor(actorData, false, true);
            }
            else
            {
                // Edge or surface actor data
                actorData.CanHaveElementEdges = true;
                _mouseSelectionActorCurrent = new vtkMaxActor(actorData);
            }
            //
            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
        }
        private void PickByGeometryVertex(out vtkActor pickedActor, int x, int y)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            //
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            vtkCellLocator cellLocator;
            vtkCell cell;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellEdgeNodeIds = GetEdgeNodeIds(pickedPoint, globalCellId, cell, cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            //double dist = vtkInteractorStyleControl.DisplayToWorldScale(_renderer, 7);
            //
            vtkMaxActorData actorData = Controller_GetGeometryVertexActorData(pickedPoint,
                                                                              globalCellId,
                                                                              globalCellEdgeNodeIds,
                                                                              globalCellFaceNodeIds);
            if (actorData != null)
            {
                _mouseSelectionActorCurrent = new vtkMaxActor(actorData, false, true);
                //
                AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
                _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
            }
        }
        private void PickByGeometryEdgeAngle(out vtkActor pickedActor, int x, int y)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }

            vtkCell cell;
            vtkCellLocator cellLocator;
            int globalPointId = GetNodeIdOnCellFaceClosestToPoint(pickedPoint);
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            vtkMaxActorData cellFaceData = Controller_GetCellFaceActorData(globalCellId, globalCellFaceNodeIds); // works on undeformed mesh

            int[] nodeIds = null;
            double[][] nodeCoor = null;
            int[] edgeCell = null;
            int edgeCellType;
            GetClosestEdgeCell(pickedPoint, cellFaceData, out nodeIds, out nodeCoor, out edgeCell, out edgeCellType);

            vtkMaxActorData edgeData = Controller_GetEdgeActorData(globalCellId, nodeIds);

            if (edgeData != null)
            {
                vtkMaxActor actor = new vtkMaxActor(edgeData);
                _mouseSelectionActorCurrent = actor;

                AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
                _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
            }
        }

        private void PickByArea(int x1, int y1, int x2, int y2, bool highlight, out string[] pickedActorNames)
        {
            _areaPicker.AreaPick(x1, y1, x2, y2, _renderer);
            vtkPlanes planes = _areaPicker.GetFrustum();
            //
            List<string> selectedActorNames = new List<string>();
            HashSet<int> selectedPointGlobalIds = new HashSet<int>();
            HashSet<int> selectedCellGlobalIds = new HashSet<int>();
            vtkActor pickedActor;
            vtkProp3DCollection pickedActors = _areaPicker.GetProp3Ds();
            //
            pickedActors.InitTraversal();
            pickedActor = (vtkActor)pickedActors.GetNextProp3D();
            //
            vtkCellLocator locator;
            vtkExtractSelectedFrustum extractor;
            //
            while (pickedActor != null)
            {
                string pickedActorName = GetActorName(pickedActor);                
                vtkMaxActor maxActor;
                //
                if (!(_actors.TryGetValue(pickedActorName, out maxActor) && maxActor.FrustumCellLocator != null))
                {
                    // Actor has no locator
                }
                else
                {
                    // Actors               
                    selectedActorNames.Add(pickedActorName);
                    //
                    locator = maxActor.FrustumCellLocator;
                    // Points               
                    extractor = vtkExtractSelectedFrustum.New(); // must be inside the loop
                    extractor.SetFieldType((int)vtkSelectionField.POINT);
                    extractor.SetContainingCells(0);
                    extractor.SetInput(0, locator.GetDataSet());
                    extractor.SetFrustum(planes);
                    extractor.Update();
                    //
                    vtkUnstructuredGrid selected = vtkUnstructuredGrid.New();
                    selected.ShallowCopy(extractor.GetOutput());
                    //
                    vtkDataArray globalPointIds = selected.GetPointData().GetGlobalIds();
                    if (globalPointIds == null)  // skip actor
                    {
                        pickedActor = (vtkActor)pickedActors.GetNextProp3D();
                        continue;
                    }
                    else
                    {
                        for (int i = 0; i < globalPointIds.GetNumberOfTuples(); i++)
                        {
                            selectedPointGlobalIds.Add((int)globalPointIds.GetTuple1(i));
                        }
                    }
                    // Cells                
                    extractor.SetFieldType((int)vtkSelectionField.CELL);
                    extractor.Update();
                    //
                    selected = vtkUnstructuredGrid.New();
                    selected.ShallowCopy(extractor.GetOutput());
                    //
                    vtkDataArray globalCellIds = selected.GetCellData().GetGlobalIds();
                    if (globalCellIds == null)  // skip actor
                    {
                        pickedActor = (vtkActor)pickedActors.GetNextProp3D();
                        continue;
                    }
                    else
                    {
                        for (int i = 0; i < globalCellIds.GetNumberOfTuples(); i++)
                        {
                            selectedCellGlobalIds.Add((int)globalCellIds.GetTuple1(i));
                        }
                    }
                }
                pickedActor = (vtkActor)pickedActors.GetNextProp3D();
            }
            //
            pickedActorNames = selectedActorNames.ToArray();
            //
            // Graphics
            if (highlight)
            {
                int[] pointIds = new int[selectedPointGlobalIds.Count];
                selectedPointGlobalIds.CopyTo(pointIds);
                //
                vtkMaxActorData data = null;
                if (_selectBy == vtkSelectBy.Node)
                {
                    if (pointIds.Length <= 0) return;
                    //
                    data = Controller_GetNodeActorData(pointIds);
                    //
                    _mouseSelectionCurrentIds = pointIds;
                    _mouseSelectionActorCurrent = new vtkMaxActor(data, true, true);
                }
                else if (_selectBy == vtkSelectBy.Element)
                {
                    if (selectedCellGlobalIds.Count <= 0) return;
                    //
                    int[] cellIds = new int[selectedCellGlobalIds.Count];
                    selectedCellGlobalIds.CopyTo(cellIds);
                    //
                    if (_selectItem == vtkSelectItem.Element && _selectBy == vtkSelectBy.Node)
                        data = Controller_GetCellActorData(cellIds, null);
                    else data = Controller_GetCellActorData(cellIds, pointIds);
                    //
                    _mouseSelectionCurrentIds = data.Geometry.Cells.Ids;
                    _mouseSelectionActorCurrent = new vtkMaxActor(data, true, false);
                }
                else if (_selectBy == vtkSelectBy.Part)
                {
                    //_mouseSelectionCurrentIds = ???;
                    _mouseSelectionActorCurrent = GetCopyOfModelEdgesActor(pickedActorNames);
                    //
                    if (_mouseSelectionActorCurrent == null) return;
                }
                else if (_selectBy == vtkSelectBy.Geometry)
                {
                    int[] cellIds = new int[selectedCellGlobalIds.Count];
                    selectedCellGlobalIds.CopyTo(cellIds);
                    //
                    vtkMaxActorData actorData = Controller_GetSurfaceEdgesActorDataFromNodeAndElementIds(pointIds, cellIds);
                    actorData.CanHaveElementEdges = true;
                    //
                    vtkMaxActor actor = new vtkMaxActor(actorData);
                    _mouseSelectionActorCurrent = actor;
                }
                else throw new NotSupportedException();
                //
                AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
                _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
                //
                if ((_selectBy == vtkSelectBy.Element || _selectBy == vtkSelectBy.Part)
                    && _mouseSelectionActorCurrent.ElementEdges != null)
                {
                    AddActorEdges(_mouseSelectionActorCurrent, false, vtkRendererLayer.Selection);
                    _selectedActors.Remove(_mouseSelectionActorCurrent);
                }
            }
        }
        private void PickAreaHardware(int x1, int y1, int x2, int y2)
        {

            vtkHardwareSelector selector = vtkHardwareSelector.New();
            selector.SetFieldAssociation((int)vtkFieldAssociations.FIELD_ASSOCIATION_CELLS);
            selector.SetRenderer(_renderer);
            selector.SetArea((uint)x1, (uint)y1, (uint)x2, (uint)y2);

            //static_cast<unsigned int>(this.Renderer.GetPickX1()),
            //static_cast<unsigned int>(this.Renderer.GetPickY1()),
            //static_cast<unsigned int>(this.Renderer.GetPickX2()),
            //static_cast<unsigned int>(this.Renderer.GetPickY2()));

            // Make the actual pick and pass the result to the convenience function
            // defined earlier
            _renderer.Render();

            vtkSelection result = selector.Select();
            uint numNodes = result.GetNumberOfNodes();
            vtkSelectionNode node;

            vtkDataSetAttributes fd1;
            vtkIdTypeArray fd1_array;

            for (uint i = 0; i < numNodes; i++)
            {
                node = result.GetNode(i);
                
                vtkIdTypeArray selIds = (vtkIdTypeArray)node.GetSelectionList();

                fd1 = node.GetSelectionData();

                if( fd1.GetNumberOfArrays() != 1) return;
                if (fd1.GetArray(0).GetDataType() != 12) return; // 12 ... VTK_ID_TYPE

                fd1_array = (vtkIdTypeArray)fd1.GetArray(0);

                long fd1_N = fd1_array.GetNumberOfTuples();

                List<long> fd1_list = new List<long>();
                for (int j = 0; j < fd1_N; j++) fd1_list.Add(fd1_array.GetValue(j));
            }
        }

        private double[] GetPickPoint(out vtkActor pickedActor, int x, int y)
        {
            double[] p = null;
            int cellId = -1;
            vtkCell cell;
            vtkCellLocator locator;
            // Make all actors visible
            foreach (var entry in _actors)
            {
                if (_selectableActorsFilter != null)
                {
                    // Change the visibility of geometry only to enable reset
                    if (_selectableActorsFilter.Contains(entry.Value.Name)) entry.Value.Geometry.VisibilityOn();
                    else entry.Value.Geometry.VisibilityOff();
                }
                // Set opacity for all visible actors to 1
                entry.Value.Geometry.GetProperty().SetOpacity(1);
            }
            // Pick actor by hardware rendering - render the sceene before Pick
            _renderer.Render();
            _propPicker.Pick(x, y, 0, _renderer);
            pickedActor = _propPicker.GetActor();
            // First pick
            if (pickedActor != null)
            {
                p = _propPicker.GetPickPosition();      // this function sometimes gives strange values
                cellId = GetGlobalCellIdClosestTo3DPoint(ref p, out cell, out locator);                
            }
            // Try some more picks
            if (cellId == -1)
            {
                int numOfLoops = 1;
                int stepSize = 5;
                //
                for (int i = 1; i <= numOfLoops; i++)   // try in a cross shape
                {
                    _propPicker.Pick(x + i * stepSize, y, 0, _renderer);
                    pickedActor = _propPicker.GetActor();
                    if (pickedActor != null)
                    {
                        p = _propPicker.GetPickPosition();  // this function sometimes gives strange values
                        cellId = GetGlobalCellIdClosestTo3DPoint(ref p, out cell, out locator);
                        if (cellId != -1) break;
                    }
                    //
                    _propPicker.Pick(x - i * stepSize, y, 0, _renderer);
                    pickedActor = _propPicker.GetActor();
                    if (pickedActor != null)
                    {
                        p = _propPicker.GetPickPosition();  // this function sometimes gives strange values
                        cellId = GetGlobalCellIdClosestTo3DPoint(ref p, out cell, out locator);
                        if (cellId != -1) break;
                    }
                    //
                    _propPicker.Pick(x, y + i * stepSize, 0, _renderer);
                    pickedActor = _propPicker.GetActor();
                    if (pickedActor != null)
                    {
                        p = _propPicker.GetPickPosition();  // this function sometimes gives strange values
                        cellId = GetGlobalCellIdClosestTo3DPoint(ref p, out cell, out locator);
                        if (cellId != -1) break;
                    }
                    //
                    _propPicker.Pick(x, y - i * stepSize, 0, _renderer);
                    pickedActor = _propPicker.GetActor();
                    if (pickedActor != null)
                    {
                        p = _propPicker.GetPickPosition();  // this function sometimes gives strange values
                        cellId = GetGlobalCellIdClosestTo3DPoint(ref p, out cell, out locator);
                        if (cellId != -1) break;
                    }
                }
            }
            // Reset the actor visibility
            foreach (var entry in _actors)
            {
                entry.Value.UpdateColor();
                ApplyEdgeVisibilityAndBackfaceCullingToActor(entry.Value.Geometry, vtkRendererLayer.Base);
            }
            //
            if (cellId == -1)
            {
                p = null;
                pickedActor = null;
            }
            //
            return p;
        }

        // Item selection - Point                                                                                                   
        private int GetGlobalCellIdClosestTo3DPoint(ref double[] point, out vtkCell cell, out vtkCellLocator cellLocator)
        {
            // a 3D point is saved in history and used for regenerate
            cell = null;
            cellLocator = null;
            //
            IntPtr x = System.Runtime.InteropServices.Marshal.AllocHGlobal(3 * 8);
            Marshal.Copy(point, 0, x, 3);
            //
            IntPtr closestPoint = System.Runtime.InteropServices.Marshal.AllocHGlobal(3 * 8);
            //
            int globalCellId = -1;
            double minDist = double.MaxValue;
            long localCellId = -1;
            int subId = -1;
            double distance2 = -1;
            double[] pointOut = new double[3];
            vtkCellLocator locator;
            //
            foreach (var entry in _actors)
            {
                // Set filtered actors
                if (_selectableActorsFilter != null)
                {
                    if (_selectableActorsFilter.Contains(entry.Value.Name)) entry.Value.Geometry.VisibilityOn();
                    else entry.Value.Geometry.VisibilityOff();
                }
                // Skip invisible actors
                if (entry.Value.Geometry.GetVisibility() != 0)
                {
                    //
                    locator = entry.Value.CellLocator;
                    if (locator != null)
                    {
                        locator.FindClosestPoint(x, closestPoint, ref localCellId, ref subId, ref distance2);
                        if (distance2 < minDist)
                        {
                            minDist = distance2;
                            cellLocator = locator;
                            cell = locator.GetDataSet().GetCell(localCellId);
                            globalCellId = (int)locator.GetDataSet().GetCellData().GetGlobalIds().GetTuple1(localCellId);
                            //
                            Marshal.Copy(closestPoint, pointOut, 0, 3);
                        }
                    }
                }
                // Reset visibility
                ApplyEdgeVisibilityAndBackfaceCullingToActor(entry.Value.Geometry, vtkRendererLayer.Base);
            }
            //
            Marshal.FreeHGlobal(x);
            Marshal.FreeHGlobal(closestPoint);
            //
            double maxErrorDistance2;
            if (_renderingOn)   // user mouse interaction
                maxErrorDistance2 = Math.Pow(vtkInteractorStyleControl.DisplayToWorldScale(_renderer, 7), 2);
            else                // regeneration - the distance to the closest item if items changed
                maxErrorDistance2 = double.MaxValue;
            //
            //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToLongTimeString() + " Min distance: " + minDist);
            if (minDist > maxErrorDistance2) return -1;
            else
            {
                point = pointOut;
                return globalCellId;
            }
        }
        private int[] GetEdgeNodeIds(double[] point, int globalCellId, vtkCell cell, vtkCellLocator cellLocator)
        {
            // cell face
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            vtkMaxActorData cellFaceData = Controller_GetCellFaceActorData(globalCellId, globalCellFaceNodeIds); // works on undeformed mesh

            // closest edge cell
            int[] nodeIds = null;
            double[][] nodeCoor = null;
            int[] edgeCell = null;
            int edgeCellType;
            GetClosestEdgeCell(point, cellFaceData, out nodeIds, out nodeCoor, out edgeCell, out edgeCellType);
           
            return nodeIds;
        }
        private int[] GetCellFaceNodeIds(vtkCell cell, vtkCellLocator cellLocator)
        {
            vtkDataArray globalIds = cellLocator.GetDataSet().GetPointData().GetGlobalIds();

            int[] faceGlobalNodeIds = new int[cell.GetPointIds().GetNumberOfIds()];
            for (int i = 0; i < faceGlobalNodeIds.Length; i++)
            {
                faceGlobalNodeIds[i] = (int)globalIds.GetTuple1(cell.GetPointId(i));
            }
            return faceGlobalNodeIds;
        }        
        private int GetNodeIdOnCellFaceClosestToPoint(double[] point)
        {
            vtkCell cell;
            vtkCellLocator cellLocator;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref point, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);

            double[][] coor = GetCellFaceNodeCoor(cell);

            double distance;
            double minDistance = double.MaxValue;
            int minId = -1;

            for (int i = 0; i < coor.Length; i++)
            {
                distance = Math.Pow(coor[i][0] - point[0], 2) + Math.Pow(coor[i][1] - point[1], 2) +
                           Math.Pow(coor[i][2] - point[2], 2);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minId = i;
                }
            }

            int globalPointId = globalCellFaceNodeIds[minId];

            return globalPointId;
        }
        private double[][] GetCellFaceNodeCoor(vtkCell cell)
        {
            vtkPoints points = cell.GetPoints();
            double[][] coor = new double[points.GetNumberOfPoints()][];

            for (int i = 0; i < coor.Length; i++)
            {
                coor[i] = points.GetPoint(i);
            }
            return coor;
        }
        public void GetGeometryPickProperties(double[] point, out int elementId, out int[] edgeNodeIds, out int[] cellFaceNodeIds)
        {
            // Turn rendering off in order to 
            vtkCellLocator cellLocator;
            vtkCell cell;
            elementId = GetGlobalCellIdClosestTo3DPoint(ref point, out cell, out cellLocator);
            edgeNodeIds = GetEdgeNodeIds(point, elementId, cell, cellLocator);
            cellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
        }
        public double GetSelectionPrecision()
        {
            return vtkInteractorStyleControl.DisplayToWorldScale(_renderer, 7);
        }

        // Item selection - Frustum                                                                                                 
        public void GetPointAndCellIdsInsideFrustum(double[][] planeParameters, out int[] pointIds, out int[] cellIds)
        {
            vtkDataArray normals = vtkDoubleArray.New();
            normals.SetNumberOfComponents(3);
            vtkPoints points = vtkPoints.New();
            //
            for (int i = 0; i < planeParameters.Length; i++)
            {
                normals.InsertNextTuple3(planeParameters[i][3], planeParameters[i][4], planeParameters[i][5]);
                points.InsertNextPoint(planeParameters[i][0], planeParameters[i][1], planeParameters[i][2]);
            }
            //
            vtkPlanes planes = vtkPlanes.New();
            planes.SetPoints(points);
            planes.SetNormals(normals);
            //
            pointIds = new int[0];
            cellIds = new int[0];
            //
            vtkExtractSelectedFrustum extractor;
            vtkDataArray globalPointIdsDataArray;
            vtkDataArray globalCellIdsDataArray;
            HashSet<int> globalPointIds = new HashSet<int>();
            HashSet<int> globalCellIds = new HashSet<int>();
            vtkCellLocator locator;
            //
            foreach (var entry in _actors)
            {
                // Set filtered actors
                if (_selectableActorsFilter != null)
                {
                    if (_selectableActorsFilter.Contains(entry.Value.Name)) entry.Value.Geometry.VisibilityOn();
                    else entry.Value.Geometry.VisibilityOff();
                }
                // Skip invisible actors
                if (entry.Value.Geometry.GetVisibility() == 0) continue;
                //
                locator = entry.Value.FrustumCellLocator;
                if (locator != null)
                {
                    // Inside points                
                    extractor = vtkExtractSelectedFrustum.New(); // must be inside the loop
                    extractor.SetFieldType((int)vtkSelectionField.POINT);
                    extractor.SetContainingCells(0);
                    extractor.SetInput(0, locator.GetDataSet());
                    extractor.SetFrustum(planes);
                    extractor.Update();
                    //
                    globalPointIdsDataArray = (extractor.GetOutput() as vtkUnstructuredGrid).GetPointData().GetGlobalIds();
                    if (globalPointIdsDataArray != null)
                    {
                        for (int i = 0; i < globalPointIdsDataArray.GetNumberOfTuples(); i++)
                        {
                            globalPointIds.Add((int)globalPointIdsDataArray.GetTuple1(i));
                        }
                    }
                    // Inside and border cells      
                    extractor.SetFieldType((int)vtkSelectionField.CELL);
                    extractor.Update();
                    //
                    globalCellIdsDataArray = (extractor.GetOutput() as vtkUnstructuredGrid).GetCellData().GetGlobalIds();
                    if (globalCellIdsDataArray != null)
                    {
                        for (int i = 0; i < globalCellIdsDataArray.GetNumberOfTuples(); i++)
                        {
                            globalCellIds.Add((int)globalCellIdsDataArray.GetTuple1(i));
                        }
                    }
                }
            }
            //
            pointIds = globalPointIds.ToArray();
            cellIds = globalCellIds.ToArray();
        }

        #endregion #################################################################################################################

        // Private methods                                                                                                          
        private void InitializeControl()
        {
            // Render window
            _renderWindow.PointSmoothingOn();
            _renderWindow.LineSmoothingOn();
            //_renderWindow.PolygonSmoothingOn();
            _renderWindow.SetMultiSamples(2);

            vtkLight light = vtkLight.New();
            light.SetPosition(-1, 1, 1);
            light.SetFocalPoint(0, 0, 0);
            light.SetColor(1, 1, 1);
            light.SetIntensity(0.5);
            light.SetLightTypeToCameraLight();
            _renderer.AddLight(light);
            _overlayRenderer.AddLight(light);
            _selectionRenderer.AddLight(light);

            light = vtkLight.New();
            light.SetPosition(1, 1, 1);
            light.SetFocalPoint(0, 0, 0);
            light.SetColor(1, 1, 1);
            light.SetIntensity(0.5);
            light.SetLightTypeToCameraLight();
            _renderer.AddLight(light);
            _overlayRenderer.AddLight(light);
            _selectionRenderer.AddLight(light);

            // coorSystem
            vtkAxesActor axes = vtkAxesActor.New();
            axes.GetXAxisTipProperty().SetColor(0.706, 0.016, 0.150);
            axes.GetXAxisShaftProperty().SetColor(0.706, 0.016, 0.150);
            axes.GetYAxisTipProperty().SetColor(0.130, 0.806, 0.150);
            axes.GetYAxisShaftProperty().SetColor(0.130, 0.806, 0.150);
            axes.GetZAxisTipProperty().SetColor(0.230, 0.299, 0.754);
            axes.GetZAxisShaftProperty().SetColor(0.230, 0.299, 0.754);

            vtkTextProperty tp = CreateNewTextProperty();
            axes.GetXAxisCaptionActor2D().SetCaptionTextProperty(tp);
            axes.GetYAxisCaptionActor2D().SetCaptionTextProperty(tp);
            axes.GetZAxisCaptionActor2D().SetCaptionTextProperty(tp);
            axes.SetShaftTypeToLine();

            _coorSys = vtkOrientationMarkerWidget.New();
            _coorSys.SetOutlineColor(0.9300, 0.5700, 0.1300);
            _coorSys.SetOrientationMarker(axes);
            _coorSys.SetInteractor(_renderWindowInteractor);
            _coorSys.KeyPressActivationOff();   // char i or I turns off the widget otherwise
            SetCoorSysVisibility(_drawCoorSys);
            _coorSys.InteractiveOff();  // must be after enabled ???


            // interactor style
            _style = vtkInteractorStyleControl.New();
            _style.AutoAdjustCameraClippingRangeOn();
            _style.SetDefaultRenderer(_renderer);
            _style.SetCurrentRenderer(_renderer);
            _style.SetOverlayRenderer(_overlayRenderer);
            _style.SetSelectionRenderer(_selectionRenderer);

            _style.PointPickedOnMouseMoveEvt += _style_PointPickedOnMouseMoveEvt;
            _style.PointPickedOnLeftUpEvt += _style_PointPickedOnLeftUpEvt;
            _style.ClearCurrentMouseSelection += ClearCurrentMouseSelection;
            _style.LeftButtonPressEvent += _style_LeftButtonPressEvent;
            _style.RightButtonPressEvent += _style_RightButtonPressEvent;
            _style.KeyPressEvt += _style_KeyPressEvt;
            _style.LeaveEvt += _style_LeaveEvt;
            _style.EnterEvt += _style_EnterEvt;
            _renderWindowInteractor.SetInteractorStyle(_style);
            _style.Reset();

            _renderWindowInteractor.ModifiedEvt += _renderWindowInteractor_ModifiedEvt;
            

            // background
            _renderer.SetBackground(0.4, 0.4, 0.4);     // bottm
            _renderer.SetBackground2(0.8, 0.8, 0.8);    // top
            _renderer.SetGradientBackground(true);


            // camera
            vtkCamera camera = _renderer.GetActiveCamera();
            camera.SetParallelProjection(1);
         

            // offset lines from polygons
            vtkPolyDataMapper.SetResolveCoincidentTopologyToPolygonOffset();


            // Scale bar
            _scaleWidget = new vtkMaxScaleWidget();
            _scaleWidget.SetInteractor(_selectionRenderer, _renderWindowInteractor);
            _scaleWidget.SetWidth(400);
            _scaleWidget.SetHorizontallyRelativePosition(435, 0);
            _scaleWidget.SetBorderColor(0, 0, 0);
            _scaleWidget.SetTextProperty(CreateNewTextProperty());
            _scaleWidget.SetPadding(5);
            _scaleWidget.GetBackgroundProperty().SetColor(1, 1, 1);
            _scaleWidget.VisibilityOn();
            _scaleWidget.BackgroundVisibilityOff();
            _scaleWidget.BorderVisibilityOff();


            // Scalar bar
            InitializeScalarBar();


            // Status block
            _statusBlockWidget = new vtkMaxStatusBlockWidget();
            //_statusBlock.SetRenderer(_selectionRenderer);
            _statusBlockWidget.SetInteractor(_selectionRenderer, _renderWindowInteractor);
            _statusBlockWidget.SetTextProperty(CreateNewTextProperty());            
            _statusBlockWidget.SetPadding(5);
            _statusBlockWidget.GetBackgroundProperty().SetColor(1, 1, 1);
            _statusBlockWidget.BackgroundVisibilityOff();
            _statusBlockWidget.VisibilityOff();
            _statusBlockWidget.MouseDoubleClick += widget_ShowPostSettings;


            // Max widget
            _maxValueWidget = new vtkMaxTextWithArrowWidget();
            _maxValueWidget.SetInteractor(_selectionRenderer, _renderWindowInteractor);
            _maxValueWidget.SetBorderColor(0, 0, 0);
            _maxValueWidget.SetTextProperty(CreateNewTextProperty());
            _maxValueWidget.SetPadding(5);
            _maxValueWidget.GetBackgroundProperty().SetColor(1, 1, 1);
            _maxValueWidget.SetText("Test");
            _maxValueWidget.VisibilityOff();


            // Min widget
            _minValueWidget = new vtkMaxTextWithArrowWidget();
            _minValueWidget.SetInteractor(_selectionRenderer, _renderWindowInteractor);
            _minValueWidget.SetBorderColor(0, 0, 0);
            _minValueWidget.SetTextProperty(CreateNewTextProperty());
            _minValueWidget.SetPadding(5);
            _minValueWidget.GetBackgroundProperty().SetColor(1, 1, 1);
            _minValueWidget.SetText("Test");
            _minValueWidget.VisibilityOff();            


            // Probe widget
            _probeWidget = new vtkMaxTextWidget();
            _probeWidget.SetInteractor(_selectionRenderer, _renderWindowInteractor);
            _probeWidget.SetBorderColor(0, 0, 0);
            _probeWidget.SetTextProperty(CreateNewTextProperty());
            _probeWidget.SetPadding(5);
            _probeWidget.GetBackgroundProperty().SetColor(1, 1, 1);
            _probeWidget.VisibilityOff();

            // Add the actors to the scene
            //Hexahedron();
            //Actor2D();
            //Timer timer = new Timer();
            //timer.Tick += Timer_Tick;
            //timer.Interval = 10 * 1000;
            //timer.Enabled = true;
        }
        private void InitializeScalarBar()
        {
            // Lookup table
            _lookupTable = vtkLookupTable.New();
            // Scalar bar
            _scalarBarWidget = new vtkMaxScalarBarWidget();
            _scalarBarWidget.SetInteractor(_renderer, _renderWindowInteractor);
            //_vtkMaxScalarBar.SetRenderer(_selectionRenderer);
            _scalarBarWidget.SetTextProperty(CreateNewTextProperty());
            _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), 0, 1);
            _scalarBarWidget.SetLabelFormat("E3");
            _scalarBarWidget.SetPadding(15);
            _scalarBarWidget.VisibilityOn();
            _scalarBarWidget.BackgroundVisibilityOn();
            _scalarBarWidget.BorderVisibilityOn();
            _scalarBarWidget.SetBackgroundColor(1, 1, 1);
            //
            _scalarBarWidget.MouseDoubleClick += widget_ShowPostSettings;
        }

        public void InitializeWidgetPositions()
        {
            _scalarBarWidget.SetTopLeftPosition(20, 20);
            _statusBlockWidget.SetTopLeftPosition(Width - _statusBlockWidget.GetWidth() - 20, 20);
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            ((Timer)sender).Enabled = false;
            AddDiskAnimation();
        }

        private void _style_KeyPressEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_scalarBarWidget.GetVisibility() == 1) _scalarBarWidget.OnRenderWindowModified();
            //
            sbyte key = _renderWindowInteractor.GetKeyCode();
            if (!_style.RubberBandCanceledByEsc) KeyPressEvent?.Invoke(key);
        }

        private vtkTextProperty CreateNewTextProperty()
        {
            vtkTextProperty textProperty = vtkTextProperty.New();
            textProperty.SetColor(0, 0, 0);
            textProperty.SetFontFamilyToArial();
            textProperty.SetFontSize(16);
            textProperty.SetLineOffset(-Math.Round(textProperty.GetFontSize() / 7.0));
            textProperty.SetLineSpacing(1.5);
            return textProperty;
        }
       
        private vtkActor GetActorEdgesFromGrid(vtkUnstructuredGrid uGridEdges)
        {
            if (uGridEdges.GetNumberOfCells() <= 0) return null;
            
            // extract visualization surface of the unstructured grid
            vtkUnstructuredGridGeometryFilter filter = vtkUnstructuredGridGeometryFilter.New();
            filter.SetInput(uGridEdges);
            filter.Update();

            // extract edges of the visualization surface
            vtkExtractEdges extractEdges = vtkExtractEdges.New();
            extractEdges.SetInput(filter.GetOutput());
            extractEdges.Update();
            
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInput(extractEdges.GetOutput());

            vtkProperty prop = vtkProperty.New();
            //prop.SetRepresentationToWireframe();
            prop.SetColor(0, 0, 0);
            prop.SetLighting(false);

            vtkActor edges = vtkActor.New();
            edges.SetMapper(mapper);
            edges.SetProperty(prop);
            edges.PickableOff();

            return edges;
        }
       
        // Formating
        private void ApplyEdgesFormatingToActor(vtkActor actor)
        {
            //actor.GetProperty().SetRepresentationToWireframe();
            
        }
        private void ApplyGeometryEdgesFormatingToActor(vtkActor actor, double[] color)
        {
            //actor.GetProperty().SetRepresentationToWireframe();
            actor.GetProperty().SetColor(color[0] * 0.8, color[1] * 0.8, color[2] * 0.8);
            actor.GetProperty().SetLighting(false);
            actor.GetProperty().SetLineWidth(0.5f);
        }
        private void ApplySelectionFormatingToActor(vtkMaxActor actor)
        {
            // Get actor cell type
            vtkCellTypes types = vtkCellTypes.New();
            actor.Geometry.GetMapper().GetInput().GetCellTypes(types);
            int num = types.GetNumberOfTypes();
            byte cellType;
            bool onlyEdges = true;
            bool onlyNodes = true;
            for (int i = 0; i < num; i++)
            {
                cellType = types.GetCellType(i);
                if (cellType != (int)vtkCellType.VTK_LINE && cellType != (int)vtkCellType.VTK_QUADRATIC_EDGE)
                {
                    onlyEdges = false;
                    //break;
                }
                if (cellType != (int)vtkCellType.VTK_VERTEX)
                {
                    onlyNodes = false;
                    //break;
                }
            }
            //
            double k;
            double ambient;
            double diffuse;
            if (onlyNodes || onlyEdges)
            {
                k = 1;
                ambient = 1;
                diffuse = 1;
            }
            else
            {
                k = 0.7;
                ambient = 0.6;
                diffuse = 0.6;
            }
            // Get property
            vtkProperty property = actor.Geometry.GetProperty();
            // if the point size was already set, do not change it
            if (property.GetPointSize() <= 1) property.SetPointSize(7);
            //
            Color highlightColor;
            if (actor.UseSecondaryHighightColor) highlightColor = _secundaryHighlightColor;
            else highlightColor = _primaryHighlightColor;
            //
            property.SetColor(highlightColor.R / 255d * k,
                              highlightColor.G / 255d * k,
                              highlightColor.B / 255d * k);
            property.SetAmbient(ambient);    // color background - even away from light
            property.SetDiffuse(diffuse);    // color from the lights
            property.SetLineWidth(2.2f);
            property.SetOpacity(1);
            //property.BackfaceCullingOn();
            actor.Geometry.PickableOff();
        }
        private void ApplySymbolFormatingToActor(vtkActor actor)
        {
            actor.GetProperty().BackfaceCullingOn();
            actor.GetProperty().SetAmbient(0.4);
            actor.GetProperty().SetDiffuse(0.6);
        }

        private void PrepareActorLookupTable(double scalarRangeMin, double scalarRangeMax)
        {
            vtkColorTransferFunction ctf = GetColorTransferFunction();

            double min = scalarRangeMin;
            double max = scalarRangeMax;
            double minNormalized = 0;
            double maxNormalized = 1;
            bool addMinColor = false;
            bool addMaxColor = false;

            // determine the need for min max color
            if (_colorSpectrum.MinMaxType == vtkColorSpectrumMinMaxType.Manual)
            {
                min = Math.Min(scalarRangeMin, _colorSpectrum.MinUserValue);
                max = Math.Max(scalarRangeMax, _colorSpectrum.MaxUserValue);

                if (_colorSpectrum.MinUserValue > min && max != min)
                {
                    addMinColor = true;
                    minNormalized = (_colorSpectrum.MinUserValue - min) / (max - min);
                }
                if (_colorSpectrum.MaxUserValue < max && max != min)
                {
                    addMaxColor = true;
                    maxNormalized = (_colorSpectrum.MaxUserValue - min) / (max - min);
                }
            }
            
            double[] color;
            double delta;
            _lookupTable = vtkLookupTable.New(); // this is a fix for a _lookupTable.DeepCopy later on
            _lookupTable.SetTableRange(min, max);
            
            // create numOfAllColors discrete colors in the lookup table and then apply it to the actor
            if (addMinColor || addMaxColor)
            {
                int colorCount = 0;
                int countStart = 0;
                int countEnd;
                // This sets the precision with which the colors are drawn on the actor: 5 is arbitrary factor
                int numOfAllColors = (int)Math.Ceiling(10 * _colorSpectrum.NumberOfColors / (maxNormalized - minNormalized));
                numOfAllColors = Math.Max(128, numOfAllColors);
                _lookupTable.SetNumberOfColors(numOfAllColors);

                // below range color
                if (addMinColor)
                {
                    color = new double[] { _colorSpectrum.MinColor.R / 256.0,
                                           _colorSpectrum.MinColor.G / 256.0,
                                           _colorSpectrum.MinColor.B / 256.0 };
                    countEnd = (int)Math.Round(minNormalized * numOfAllColors, 0);
                    for (int i = countStart; i < countEnd; i++) _lookupTable.SetTableValue(colorCount++, 
                                                                                           color[0],
                                                                                           color[1],
                                                                                           color[2],
                                                                                           1.0); //R,G,B,A
                    countStart = countEnd;
                }

                // between range color
                double endValue;
                delta = 1.0 / (_colorSpectrum.NumberOfColors - 1);
                double deltaNormalized = (maxNormalized - minNormalized) / (_colorSpectrum.NumberOfColors);

                for (int i = 0; i < _colorSpectrum.NumberOfColors; i++)
                {
                    color = ctf.GetColor(i * delta);
                    endValue = minNormalized + (i + 1) * deltaNormalized;
                    countEnd = (int)Math.Round(endValue * numOfAllColors, 0);

                    for (int j = countStart; j < countEnd; j++) _lookupTable.SetTableValue(colorCount++,
                                                                                           color[0],
                                                                                           color[1],
                                                                                           color[2],
                                                                                           1.0); //R,G,B,A
                    countStart = countEnd;
                }

                // above range color
                if (addMaxColor)
                {
                    color = new double[] { _colorSpectrum.MaxColor.R / 256.0,
                                           _colorSpectrum.MaxColor.G / 256.0,
                                           _colorSpectrum.MaxColor.B / 256.0 };
                    countEnd = (int)numOfAllColors;
                    for (int i = countStart; i < countEnd; i++) _lookupTable.SetTableValue(colorCount++,
                                                                                           color[0],
                                                                                           color[1],
                                                                                           color[2],
                                                                                           1.0); //R,G,B,A
                    countStart = countEnd;
                }
            }
            else
            {
                _lookupTable.SetNumberOfColors(_colorSpectrum.NumberOfColors);
                delta = 1.0 / (_lookupTable.GetNumberOfColors() - 1);
                for (int i = 0; i < _lookupTable.GetNumberOfColors(); i++)
                {
                    color = ctf.GetColor(i * delta);
                    _lookupTable.SetTableValue(i, color[0], color[1], color[2], 1.0); //R,G,B,A
                }
            }            
        }
        private vtkColorTransferFunction GetColorTransferFunction()
        {
            vtkColorTransferFunction ctf = vtkColorTransferFunction.New();
            if (_colorSpectrum.Type == vtkColorSpectrumType.Rainbow)
            {
                // Rainbow
                double b = 0.0;
                ctf.SetColorSpaceToHSV();
                ctf.AddRGBPoint(0.000, b, b, 1);
                ctf.AddRGBPoint(0.250, b, 1, 1);
                ctf.AddRGBPoint(0.500, b, 1, b);
                ctf.AddRGBPoint(0.750, 1, 1, b);
                ctf.AddRGBPoint(1.000, 1, b, b);
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.CoolWarm)
            {
                // Cool-Warm
                //http://aplotnikov.com/2016/simple-visualization-of-unstructured-grid-quality/
                //http://www.kennethmoreland.com/color-maps/
                ctf.SetColorSpaceToDiverging();
                ctf.AddRGBPoint(0, 0.230, 0.299, 0.754);
                ctf.AddRGBPoint(1, 0.706, 0.016, 0.150);
            }
            return ctf;
        }
        private void ReverseLookupTable(vtkLookupTable lookupTable)
        {
            long n = lookupTable.GetNumberOfColors();
            vtkUnsignedCharArray table = lookupTable.GetTable();
            byte[][] color = new byte[n][];

            //color[i][0] ... R
            //color[i][1] ... G
            //color[i][2] ... B
            //color[i][3] ... A

            for (int i = 0; i < n; i++)
            {
                color[i] = new byte[4];
                for (int j = 0; j < 4; j++)
                {
                    color[i][j] = table.GetValue(4 * i + j);
                }
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    table.SetValue(4 * i + j, color[n - 1 - i][j]);
                }
            }

            lookupTable.SetTable(table);

            //t = lookupTable.anno  etNumberOfAnnotatedValues() - 1;
            //for (vtkIdType i = t; i >= 0; --i)
            //{
            //    lutr.SetAnnotation(t - i, lut.GetAnnotation(i));
            //}
        }

        private void ApplyEdgesVisibilityAndBackfaceCulling()
        {
            // visibility functions must use vtkMaxActor.Visible property to determine visibility

            // base layer
            foreach (var entry in _actors)
            {
                ApplyEdgeVisibilityAndBackfaceCullingToActor(entry.Value.Geometry, vtkRendererLayer.Base);
                if (entry.Value.ElementEdges != null) ApplyEdgeVisibilityAndBackfaceCullingToActorEdges(entry.Value.ElementEdges, entry.Key);
                if (entry.Value.ModelEdges != null) ApplyEdgeVisibilityAndBackfaceCullingToModelEdges(entry.Value.ModelEdges, entry.Key);
            }

            // selection
            foreach (var selectedActor in _selectedActors)
            {
                ApplyEdgeVisibilityAndBackfaceCullingToActor(selectedActor.Geometry, vtkRendererLayer.Selection);
                if (selectedActor.ElementEdges != null) ApplyEdgeVisibilityAndBackfaceCullingToActorEdges(selectedActor.ElementEdges, null);
            }

            // overlay
            foreach (var entry in _overlayActors)
            {
                ApplyEdgeVisibilityAndBackfaceCullingToActor(entry.Value, vtkRendererLayer.Selection);
            }

            this.Invalidate();
        }
        private void ApplyEdgeVisibilityAndBackfaceCullingToActor(vtkActor actor, vtkRendererLayer layer)
        {
            // Visibility functions must use vtkMaxActor.Visible property to determine visibility
            //
            // Skip formatting for the overlay layer
            if (layer == vtkRendererLayer.Overlay) return;
            //
            if (_edgesVisibility == vtkEdgesVisibility.ElementEdges) actor.GetProperty().SetInterpolationToFlat();
            else actor.GetProperty().SetInterpolationToPhong();
            //
            if (_edgesVisibility == vtkEdgesVisibility.Wireframe && layer == vtkRendererLayer.Base)
            {
                actor.VisibilityOff();
            }
            else
            {                
                actor.GetProperty().SetRepresentationToSurface();
                if (layer == vtkRendererLayer.Base)
                {
                    string actorName = GetActorName(actor);
                    vtkMaxActor maxActor = _actors[actorName];
                    if (maxActor.VtkMaxActorVisible) actor.VisibilityOn();
                    else actor.VisibilityOff();
                    //
                    if (maxActor.BackfaceCulling) actor.GetProperty().BackfaceCullingOn();
                    else actor.GetProperty().BackfaceCullingOff();
                    //
                    if (maxActor.ActorRepresentation == vtkMaxActorRepresentation.SolidAsShell && _sectionView)
                        actor.GetProperty().BackfaceCullingOff();
                }
            }
            //actor.GetProperty().SetRepresentationToWireframe();
        }
        private void ApplyEdgeVisibilityAndBackfaceCullingToActorEdges(vtkActor actorEdges, string actorName)
        {
            // visibility functions must use vtkMaxActor.Visible property to determine visibility

            // turn off edges for hidden actors
            if (actorName != null && _actors.ContainsKey(actorName) && !_actors[actorName].VtkMaxActorVisible)
            {
                actorEdges.VisibilityOff();
            }
            else
            {
                // for visible actors
                if (_edgesVisibility == vtkEdgesVisibility.ElementEdges)
                {
                    actorEdges.VisibilityOn();
                }
                else
                {
                    actorEdges.VisibilityOff();
                }
            }
            actorEdges.GetProperty().BackfaceCullingOn();
        }
        private void ApplyEdgeVisibilityAndBackfaceCullingToModelEdges(vtkActor modelEdges, string actorName)
        {
            // visibility functions must use vtkMaxActor.Visible property to determine visibility

            // turn off edges for hidden actors
            if (actorName != null && _actors.ContainsKey(actorName) && !_actors[actorName].VtkMaxActorVisible)
            {
                modelEdges.VisibilityOff();
            }
            else
            {
                if (_edgesVisibility == vtkEdgesVisibility.ModelEdges || _edgesVisibility == vtkEdgesVisibility.Wireframe)
                {
                    modelEdges.VisibilityOn();
                }
                //else if (_edgesVisibility == vtkEdgesVisibility.ElementEdges && !_actors[actorName].BackfaceCulling)
                //{
                //    modelEdges.VisibilityOn();
                //}
                else
                {
                    modelEdges.VisibilityOff();
                }
                modelEdges.GetProperty().BackfaceCullingOn();
            }
        }

        // Public methods                                                                                                          
        #region Views  #############################################################################################################
        
        public void SetFrontBackView(bool animate, bool front)
        {
            if (_animating) return;
            _animating = animate;

            int delta = 1;
            if (!front) delta = -1;

            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);

            double[] fPos = camera.GetFocalPoint();
            camera.SetPosition(fPos[0], fPos[1], fPos[2] + delta);
            camera.SetViewUp(0, 1, 0);

            ResetCamera();
         
            if (animate) AnimateCamera(cameraStart, camera, camera);
            else this.Invalidate();
        }
        public void SetTopBottomView(bool animate, bool top)
        {
            if (_animating) return;
            _animating = animate;

            int delta = 1;
            if (!top) delta = -1;

            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);

            double[] fPos = camera.GetFocalPoint();
            camera.SetPosition(fPos[0], fPos[1] + delta, fPos[2]);
            camera.SetViewUp(0, 0, -delta);

            ResetCamera();

            if (animate) AnimateCamera(cameraStart, camera, camera);
            else this.Invalidate();
        }
        public void SetLeftRightView(bool animate, bool left)
        {
            if (_animating) return;
            _animating = animate;

            int delta = -1;
            if (!left) delta = 1;

            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);

            double[] fPos = camera.GetFocalPoint();
            camera.SetPosition(fPos[0] + delta, fPos[1], fPos[2]);
            camera.SetViewUp(0, 1, 0);

            ResetCamera();

            if (animate) AnimateCamera(cameraStart, camera, camera);
            else this.Invalidate();
        }
        public void SetVerticalView(bool animate, bool updateView)
        {
            if (updateView)
            {
                if (_animating) return;
                _animating = animate;
            }

            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);

            double[] zRange = camera.GetClippingRange();
            double aspect = (double)Width / Height;
            vtkMatrix4x4 m = camera.GetCompositeProjectionTransformMatrix(aspect, zRange[0], zRange[1]);
            double[] x = new double[2]; // 2d projection of the X axis
            double[] y = new double[2]; // 2d projection of the Y axis
            double[] z = new double[2]; // 2d projection of the Z axis

            x[0] = m.GetElement(0, 0);
            x[1] = m.GetElement(1, 0);

            y[0] = m.GetElement(0, 1);
            y[1] = m.GetElement(1, 1);

            z[0] = m.GetElement(0, 2);
            z[1] = m.GetElement(1, 2);

            // normalize
            double d;
            d = Math.Sqrt(Math.Pow(x[0], 2) + Math.Pow(x[1], 2));
            if (d != 0) x[1] /= d;
            x[0] = Math.Abs(x[1]);

            d = Math.Sqrt(Math.Pow(y[0], 2) + Math.Pow(y[1], 2));
            if (d != 0) y[1] /= d;
            y[0] = Math.Abs(y[1]);

            d = Math.Sqrt(Math.Pow(z[0], 2) + Math.Pow(z[1], 2));
            if (d != 0) z[1] /= d;
            z[0] = Math.Abs(z[1]);

            if (Math.Max(Math.Max(x[0], y[0]), Math.Max(y[0], z[0])) == x[0])
                camera.SetViewUp(Math.Sign(x[1]), 0, 0);
            else if (Math.Max(Math.Max(x[0], y[0]), Math.Max(y[0], z[0])) == y[0])
                camera.SetViewUp(0, Math.Sign(y[1]), 0);
            else if (Math.Max(Math.Max(x[0], y[0]), Math.Max(y[0], z[0])) == z[0])
                camera.SetViewUp(0, 0, Math.Sign(z[1]));

            if (updateView)
            {
                if (animate) AnimateCamera(cameraStart, camera, camera);
                else this.Invalidate();
            }
        }
        public void SetNormalView(bool animate)
        {
            if (_animating) return;
            _animating = animate;

            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);

            
            double[] x = { 1, 0, 0 };
            double[] y = { 0, 1, 0 };
            double[] z = { 0, 0, 1 };
            double[] direction = camera.GetViewPlaneNormal();

            double angle1 = GetAngle(x, direction);
            double angle2 = GetAngle(y, direction);
            double angle3 = GetAngle(z, direction);

            double[] fPoint = camera.GetFocalPoint();

            if (Math.Min(Math.Min(angle1, angle2), Math.Min(angle2, angle3)) == angle1)
                camera.SetPosition(fPoint[0] + Math.Sign(direction[0]), fPoint[1], fPoint[2]);
            if (Math.Min(Math.Min(angle1, angle2), Math.Min(angle2, angle3)) == angle2)
                camera.SetPosition(fPoint[0], fPoint[1] + Math.Sign(direction[1]), fPoint[2]);
            if (Math.Min(Math.Min(angle1, angle2), Math.Min(angle2, angle3)) == angle3)
                camera.SetPosition(fPoint[0], fPoint[1], fPoint[2] + Math.Sign(direction[2]));

            ResetCamera();

            SetVerticalView(false, false);

            if (animate) AnimateCamera(cameraStart, camera, camera);
            else this.Invalidate();
        }
        public void SetIsometricView(bool animate, bool positive)
        {
            if (_animating) return;
            _animating = animate;

            int delta = 1;
            if (!positive) delta = -1;

            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);

            SetVerticalView(false, false);

            double[] fPos = camera.GetFocalPoint();
            int q = GetClosestIsoDirectionQuadrant(camera.GetViewPlaneNormal());

            if (q == 1)      camera.SetPosition(fPos[0] + delta, fPos[1] + delta, fPos[2] + delta);
            else if (q == 2) camera.SetPosition(fPos[0] - delta, fPos[1] + delta, fPos[2] + delta);
            else if (q == 3) camera.SetPosition(fPos[0] - delta, fPos[1] - delta, fPos[2] + delta);
            else if (q == 4) camera.SetPosition(fPos[0] + delta, fPos[1] - delta, fPos[2] + delta);
            else if (q == 5) camera.SetPosition(fPos[0] + delta, fPos[1] + delta, fPos[2] - delta);
            else if (q == 6) camera.SetPosition(fPos[0] - delta, fPos[1] + delta, fPos[2] - delta);
            else if (q == 7) camera.SetPosition(fPos[0] - delta, fPos[1] - delta, fPos[2] - delta);
            else if (q == 8) camera.SetPosition(fPos[0] + delta, fPos[1] - delta, fPos[2] - delta);

            camera.OrthogonalizeViewUp(); // not needed ?
            double[] up = camera.GetViewUp();

            int maxID = -1;
            double max = -double.MaxValue;
            for (int i = 0; i < up.Length; i++)
            {
                if (Math.Abs(up[i]) > max)
                {
                    max = Math.Abs(up[i]);
                    maxID = i;
                }
            }

            if (maxID == 0) camera.SetViewUp(Math.Sign(up[0]), 0, 0);
            else if (maxID == 1) camera.SetViewUp(0, Math.Sign(up[1]), 0);
            else camera.SetViewUp(0, 0, Math.Sign(up[2]));
            
            ResetCamera();

            if (animate) AnimateCamera(cameraStart, camera, camera);
            else this.Invalidate();
        }
        public void SetZoomToFit(bool animate)
        {
            if (_animating) return;
            _animating = animate;

            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);

            ResetCamera();

            //_renderWindowInteractor.Modified(); // this updates the vtkMax annotation objects
            if (animate) AnimateCamera(cameraStart, camera, camera);
            else this.Invalidate();
        }

        private void ResetCamera()
        {
            if (_overlayActors.Count > 0)
            {
                double[] b1 = _renderer.ComputeVisiblePropBounds();
                double[] b2 = _overlayRenderer.ComputeVisiblePropBounds();
                
                //double[] b3 = new double[] { Math.Min(b1[0], b2[0]), Math.Max(b1[1], b2[1]),
                //                             Math.Min(b1[2], b2[2]), Math.Max(b1[3], b2[3]),
                //                             Math.Min(b1[4], b2[4]), Math.Max(b1[5], b2[5])};
                //
                //_renderer.ResetCamera(b3[0], b3[1], b3[2], b3[3], b3[4], b3[5]);

                //bool left, right, bottom, top;
                //left = b2[0] < b1[0];
                //right = b2[1] > b1[1];
                //bottom = b2[2] < b1[2];
                //top = b2[4] > b1[4];

                int wFactor = 0;
                int hFactor = 0;

                //if (left) wFactor++;
                //if (right) wFactor++;
                //if (bottom) hFactor++;
                //if (top) hFactor++;

                // simple solution
                wFactor = 2;
                hFactor = 2;

                double w = Width - wFactor * _maxSymbolSize;
                double h = Height - hFactor * _maxSymbolSize;
                double zoomW = Width / w;
                double zoomH = Height / h;

                double mid;
                double l;

                // width
                mid = (b1[0] + b1[1]) / 2;
                l = b1[1] - b1[0];
                //if (wFactor == 1)
                //{
                //    if (left) mid += l * (1 - zoomW) / 2;
                //    if (right) mid -= l * (1 - zoomW) / 2;
                //}
                l *= zoomW;
                b1[0] = mid - l / 2;
                b1[1] = mid + l / 2;

                // height
                mid = (b1[2] + b1[3]) / 2;
                l = b1[3] - b1[2];
                //if (hFactor == 1)
                //{
                //    if (bottom) mid += l * (1 - zoomH) / 2;
                //    if (top) mid -= l * (1 - zoomH) / 2;
                //}
                l *= zoomH;
                b1[2] = mid - l / 2;
                b1[3] = mid + l / 2;

                _renderer.ResetCamera(b1[0], b1[1], b1[2], b1[3], b1[4], b1[5]);
            }
            else _renderer.ResetCamera();

            _style.ResetClippingRange();
        }

        private int GetClosestIsoDirectionQuadrant(double[] camera)
        {
            double[] quadrant = new double[3];
            double[] angles = new double[4];
            // I
            quadrant[0] = 1;
            quadrant[1] = 1;
            quadrant[2] = 1;
            angles[0] = GetAngle(camera, quadrant);

            // II
            quadrant[0] = -1;
            quadrant[1] = 1;
            quadrant[2] = 1;
            angles[1] = GetAngle(camera, quadrant);

            // III
            quadrant[0] = -1;
            quadrant[1] = -1;
            quadrant[2] = 1;
            angles[2] = GetAngle(camera, quadrant);

            // IV
            quadrant[0] = 1;
            quadrant[1] = -1;
            quadrant[2] = 1;
            angles[3] = GetAngle(camera, quadrant);

            // find min angle
            int minId = -1;
            double min = double.MaxValue;
            for (int i = 0; i < angles.Length; i++)
            {
                if (angles[i] < min)
                {
                    min = angles[i];
                    minId = i + 1;
                }
            }

            if (minId == 1)
            {
                if (camera[0] >= 0 && camera[1] >= 0 && camera[2] >= 0) return 1;
                else return 7;
            }
            else if (minId == 2)
            {
                if (camera[0] <= 0 && camera[1] >= 0 && camera[2] >= 0) return 2;
                else return 8;
            }
            else if (minId == 3)
            {
                if (camera[0] <= 0 && camera[1] <= 0 && camera[2] >= 0) return 3;
                else return 5;
            }
            else //if (minId == 4)
            {
                if (camera[0] >= 0 && camera[1] <= 0 && camera[2] >= 0) return 4;
                else return 6;
            }
        }
        public double[] GetViewPlaneNormal()
        {
            vtkCamera camera = _renderer.GetActiveCamera();
            double[] fPos = camera.GetFocalPoint();
            double[] vpn = camera.GetViewPlaneNormal();
            return vpn;
        }
        public double GetAngle(double[] v1, double[] v2)
        {
            double angle = v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
            double abs1 = Math.Sqrt(Math.Pow(v1[0], 2) + Math.Pow(v1[1], 2) + Math.Pow(v1[2], 2));
            double abs2 = Math.Sqrt(Math.Pow(v2[0], 2) + Math.Pow(v2[1], 2) + Math.Pow(v2[2], 2));
            if (abs1 == 0 || abs2 == 0) return 0;
            else return Math.Acos(Math.Min(1, Math.Abs(angle / (abs1 * abs2))));
        }
        public double[] Substract(double[] v1, double[] v2)
        {
            double[] v = new double[3];
            v[0] = v1[0] - v2[0];
            v[1] = v1[1] - v2[1];
            v[2] = v1[2] - v2[2];
            return v;
        }
        
        public void AnimateCamera_(vtkCamera cameraStart, vtkCamera cameraEnd, vtkCamera camera)
        {
            vtkCameraInterpolator interpolator = vtkCameraInterpolator.New();
            interpolator.SetInterpolationTypeToSpline();

            interpolator.AddCamera(0, cameraStart);
            interpolator.AddCamera(1, cameraEnd);

            //double zEnd = camera.GetParallelScale();

            //interpolator.InterpolateCamera(0.5, camera);

            //double zStart = cameraStart.GetParallelScale();
            //double zMid = camera.GetParallelScale();
            

            double[] v1 = cameraStart.GetDirectionOfProjection();
            double[] v2 = cameraEnd.GetDirectionOfProjection();

            double angle = v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];

            if (angle < -0.99)
            {
                cameraStart.OrthogonalizeViewUp();
                cameraEnd.OrthogonalizeViewUp();
                double[] up1 = cameraStart.GetViewUp();
                double[] up2 = cameraEnd.GetViewUp();

                double[] up3 = new double[3];
                up3[0] = (up1[0] + up2[0]) / 2;
                up3[1] = (up1[1] + up2[1]) / 2;
                up3[2] = (up1[2] + up2[2]) / 2;

                if (up3[0] == 0 && up3[1] == 0 && up3[2] == 0)
                {
                    up3[0] = v1[0];
                    up3[1] = v1[1];
                    up3[2] = v1[2];
                }

                double[] v3 = new double[3];
                v3[0] = v1[1] * up1[2] - v1[2] * up1[1];
                v3[1] = -(v1[0] * up1[2] - v1[2] * up1[0]);
                v3[2] = v1[0] * up1[1] - v1[1] * up1[0];

                double[] focal = cameraStart.GetFocalPoint();
                double distance = Math.Max(cameraStart.GetDistance(), cameraEnd.GetDistance());
                cameraStart.SetPosition(focal[0] + v3[0] * distance, focal[1] + v3[1] * distance, focal[2] + v3[2] * distance);
                cameraStart.SetViewUp(up3[0], up3[1], up3[2]);
                cameraStart.OrthogonalizeViewUp();

                interpolator.AddCamera(0.5, cameraStart);
            }

            DateTime start = DateTime.Now;
            int delta = 200; //ms
            double currentDelta = 0;
            while (currentDelta < delta)
            {
                currentDelta = (DateTime.Now - start).TotalMilliseconds;
                interpolator.InterpolateCamera(currentDelta / delta, camera);
                camera.OrthogonalizeViewUp();
                _style.AdjustCameraDistanceAndClipping();
                System.Threading.Thread.Sleep(1);
                this.Invalidate();
                Application.DoEvents();
            }

            interpolator.InterpolateCamera(1, camera);
            this.Invalidate();
        }
        public void AnimateCameraQuat(vtkCamera cameraStart, vtkCamera cameraEnd, vtkCamera camera)
        {
            vtkMatrix4x4 mCamStart = cameraStart.GetViewTransformMatrix();
            double[][] rStart = new double[3][];
            for (int i = 0; i < 3; i++)
            {
                rStart[i] = new double[3];
                for (int j = 0; j < 3; j++)
                {
                    rStart[i][j] = mCamStart.GetElement(i, j);
                }
            }
            double[] tStart = new double[3];
            for (int i = 0; i < 3; i++)
            {
                tStart[i] = mCamStart.GetElement(i, 3);
            }
            double[] fpStart = cameraStart.GetFocalPoint();
            double zStart = cameraStart.GetParallelScale();
            double[] q1 = QuaternionHelper.QuaternionFromMatrix3x3(rStart);

            vtkMatrix4x4 mCamEnd = cameraEnd.GetViewTransformMatrix();
            double[][] rEnd = new double[3][];
            for (int i = 0; i < 3; i++)
            {
                rEnd[i] = new double[3];
                for (int j = 0; j < 3; j++)
                {
                    rEnd[i][j] = mCamEnd.GetElement(i, j);
                }
            }
            double[] tEnd = new double[3];
            for (int i = 0; i < 3; i++)
            {
                tEnd[i] = mCamEnd.GetElement(i, 3);
            }
            double[] fpEnd = cameraEnd.GetFocalPoint();
            double zEnd = cameraEnd.GetParallelScale();
            double[] q2 = QuaternionHelper.QuaternionFromMatrix3x3(rEnd);

            double[] q;
            double[][] rOut;
            double[][] rOutT;
            double[] transform;
            double[] fp;
            double[] pos = new double[3];
            double[] up = new double[3];
            double z;
            double t;

            DateTime start = DateTime.Now;
            int delta = 500; //ms
            double currentDelta = 0;

            do
            {
                //http://math.stackexchange.com/questions/82602/how-to-find-camera-position-and-rotation-from-a-4x4-matrix
                //
                //     |         |     
                // M = |   R   T |     
                //     |         |     
                //     | 0 0 0 1 |     
                //                     
                // camera positon C:   
                // C = -R(transponse)*T
                //                     

                currentDelta = (DateTime.Now - start).TotalMilliseconds;
                t = currentDelta / delta;
                if (t > 1) t = 1;

                q = QuaternionHelper.QuaternionSlerp(q1, q2, t);
                transform = QuaternionHelper.VectorLerp(tStart, tEnd, t);
                fp = QuaternionHelper.VectorLerp(fpStart, fpEnd, t);
                z = zStart + (t) * (zEnd - zStart);
                rOut = QuaternionHelper.Matrix3x3FromQuaternion(q);
                rOutT = QuaternionHelper.TransponseMatrix3x3(rOut);

                pos[0] = -(rOutT[0][0] * transform[0] + rOutT[0][1] * transform[1] + rOutT[0][2] * transform[2]);
                pos[1] = -(rOutT[1][0] * transform[0] + rOutT[1][1] * transform[1] + rOutT[1][2] * transform[2]);
                pos[2] = -(rOutT[2][0] * transform[0] + rOutT[2][1] * transform[1] + rOutT[2][2] * transform[2]);

                up[0] = rOutT[0][1];
                up[1] = rOutT[1][1];
                up[2] = rOutT[2][1];

                camera.SetPosition(pos[0], pos[1], pos[2]);
                camera.SetFocalPoint(fp[0], fp[1], fp[2]);
                camera.SetViewUp(up[0], up[1], up[2]);
                camera.SetParallelScale(z);
                camera.OrthogonalizeViewUp();
                _style.AdjustCameraDistanceAndClipping();
                System.Threading.Thread.Sleep(5);
                this.Invalidate();
                Application.DoEvents();
            } while (currentDelta < delta);

            _animating = false;
        }
        public void AnimateCamera(vtkCamera cameraStart, vtkCamera cameraEnd, vtkCamera camera)
        {
            ((vtkInteractorStyleControl)_renderWindowInteractor.GetInteractorStyle()).Animating = true;
            IntPtr pq1 = new IntPtr();
            IntPtr pq2 = new IntPtr();
            IntPtr pq = new IntPtr();
            try
            {
                vtkMatrix4x4 mCamStart = cameraStart.GetViewTransformMatrix();
                double[][] rStart = new double[3][];
                for (int i = 0; i < 3; i++)
                {
                    rStart[i] = new double[3];
                    for (int j = 0; j < 3; j++)
                    {
                        rStart[i][j] = mCamStart.GetElement(i, j);
                    }
                }
                double[] tStart = new double[3];
                for (int i = 0; i < 3; i++)
                {
                    tStart[i] = mCamStart.GetElement(i, 3);
                }
                double[] fpStart = cameraStart.GetFocalPoint();
                double zStart = cameraStart.GetParallelScale();
                double[] q1 = QuaternionHelper.QuaternionFromMatrix3x3(rStart);
                pq1 = QuaternionHelper.IntPtrFromQuaternion(q1);

                vtkMatrix4x4 mCamEnd = cameraEnd.GetViewTransformMatrix();
                double[][] rEnd = new double[3][];
                for (int i = 0; i < 3; i++)
                {
                    rEnd[i] = new double[3];
                    for (int j = 0; j < 3; j++)
                    {
                        rEnd[i][j] = mCamEnd.GetElement(i, j);
                    }
                }
                double[] tEnd = new double[3];
                for (int i = 0; i < 3; i++)
                {
                    tEnd[i] = mCamEnd.GetElement(i, 3);
                }
                double[] fpEnd = cameraEnd.GetFocalPoint();
                double zEnd = cameraEnd.GetParallelScale();
                double[] q2 = QuaternionHelper.QuaternionFromMatrix3x3(rEnd);
                pq2 = QuaternionHelper.IntPtrFromQuaternion(q2);

                double[] q;
                double[][] rOut;
                double[][] rOutT;
                double[] transform;
                double[] fp;
                double[] pos = new double[3];
                double[] up = new double[3];
                double z;
                double t;
                
                vtkQuaternionInterpolator qi = vtkQuaternionInterpolator.New();
                qi.SetInterpolationTypeToLinear();
                qi.AddQuaternion(0, pq1);
                qi.AddQuaternion(1, pq2);
                pq = Marshal.AllocHGlobal(Marshal.SizeOf(q1[0]) * q1.Length);

                DateTime start = DateTime.Now;
                int delta = 500; //ms
                double currentDelta = 0;

                do
                {
                    //http://math.stackexchange.com/questions/82602/how-to-find-camera-position-and-rotation-from-a-4x4-matrix
                    //
                    //     |         |     
                    // M = |   R   T |     
                    //     |         |     
                    //     | 0 0 0 1 |     
                    //                     
                    // camera positon C:   
                    // C = -R(transponse)*T
                    //                     

                    currentDelta = (DateTime.Now - start).TotalMilliseconds;
                    t = currentDelta / delta ;
                    if (t > 1) t = 1;

                    if (Math.Abs(q1[0] - q2[0]) + Math.Abs(q1[1] - q2[1]) + Math.Abs(q1[2] - q2[2]) + Math.Abs(q1[3] - q2[3]) < 0.00001)
                        q = q1;
                    else
                    {
                        qi.InterpolateQuaternion(t, pq);
                        q = QuaternionHelper.QuaternionFromIntPtr(pq);
                    }

                    transform = QuaternionHelper.VectorLerp(tStart, tEnd, t);
                    fp = QuaternionHelper.VectorLerp(fpStart, fpEnd, t);
                    z = zStart + (t) * (zEnd - zStart);
                    rOut = QuaternionHelper.Matrix3x3FromQuaternion(q);
                    rOutT = QuaternionHelper.TransponseMatrix3x3(rOut);

                    pos[0] = -(rOutT[0][0] * transform[0] + rOutT[0][1] * transform[1] + rOutT[0][2] * transform[2]);
                    pos[1] = -(rOutT[1][0] * transform[0] + rOutT[1][1] * transform[1] + rOutT[1][2] * transform[2]);
                    pos[2] = -(rOutT[2][0] * transform[0] + rOutT[2][1] * transform[1] + rOutT[2][2] * transform[2]);

                    up[0] = rOutT[0][1];
                    up[1] = rOutT[1][1];
                    up[2] = rOutT[2][1];

                    camera.SetPosition(pos[0], pos[1], pos[2]);
                    camera.SetFocalPoint(fp[0], fp[1], fp[2]);
                    camera.SetViewUp(up[0], up[1], up[2]);
                    camera.SetParallelScale(z);
                    camera.OrthogonalizeViewUp();
                    _style.AdjustCameraDistanceAndClipping();

                    //System.Threading.Thread.Sleep(5);
                    _renderWindowInteractor.Modified(); // this updates the vtkMax annotation objects
                    this.Invalidate();
                    
                    Application.DoEvents();
                }
                while (currentDelta < delta);
            }
            finally
            {
                // Free the unmanaged memory.
                Marshal.FreeHGlobal(pq1);
                Marshal.FreeHGlobal(pq2);
                Marshal.FreeHGlobal(pq);
                _animating = false;

                ((vtkInteractorStyleControl)_renderWindowInteractor.GetInteractorStyle()).Animating = false;
            }
        }

        public void AdjustCameraDistanceAndClipping()
        {
            _style.AdjustCameraDistanceAndClipping();
        }

        #endregion  ################################################################################################################

        #region Add geometry  ######################################################################################################
        public void AddPoints(vtkMaxActorData data)
        {
            vtkMaxActor actor = new vtkMaxActor(data, false, true);
            AddActorGeometry(actor, data.Layer);
            //
            _style.AdjustCameraDistanceAndClipping();
            this.Invalidate();
        }
        public void AddCells(vtkMaxActorData data)
        {
            // Create actor
            vtkMaxActor actor = new vtkMaxActor(data);
            //
            AddActor(actor, data.Layer, data.CanHaveElementEdges);
            //
            _style.AdjustCameraDistanceAndClipping();
            this.Invalidate();
        }
        
        public void AddSphereActor(vtkMaxActorData data, double symbolSize)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            //
            double[][] centers = data.Geometry.Nodes.Coor;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < data.Geometry.Nodes.Coor.GetLength(0); i++)
            {
                pointData.InsertNextPoint(data.Geometry.Nodes.Coor[i][0], data.Geometry.Nodes.Coor[i][1], data.Geometry.Nodes.Coor[i][2]);
            }
            // Polydata
            vtkPolyData polydata = vtkPolyData.New();
            polydata.SetPoints(pointData);
            // Source object
            vtkSphereSource sphereSource = vtkSphereSource.New();
            sphereSource.SetRadius(0.2);
            sphereSource.SetPhiResolution(15);
            sphereSource.SetThetaResolution(15);
            sphereSource.Update();
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polydata);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(sphereSource.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(1.0);
            glyph.OrientOn();
            glyph.Update();
            //
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(0, glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            //
            data.Name += Globals.NameSeparator + "sphere";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            //
            ApplySymbolFormatingToActor(actor.Geometry);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddOrientedDisplacementConstraintActor(vtkMaxActorData data, double symbolSize)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            //
            double[][] points = data.Geometry.Nodes.Coor;
            double[][] normals = data.Geometry.Nodes.Normals;
            // points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < points.GetLength(0); i++)
            {
                pointData.InsertNextPoint(points[i][0], points[i][1], points[i][2]);
            }
            // normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3);
            for (int i = 0; i < normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(normals[i][0], normals[i][1], normals[i][2]);
            }
            // polydata
            vtkPolyData polydata = vtkPolyData.New();
            polydata.SetPoints(pointData);
            polydata.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polydata);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            //
            vtkConeSource cone = vtkConeSource.New();
            cone.SetCenter(-0.5, 0, 0);
            cone.SetResolution(31);
            //
            //vtkPolyDataNormals normalGenerator = vtkPolyDataNormals.New();
            //normalGenerator.SetInput(cone.GetOutput());
            //normalGenerator.SetFeatureAngle(180);
            //normalGenerator.ComputePointNormalsOn();
            //normalGenerator.ComputeCellNormalsOff();
            //normalGenerator.Update();
            //
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(cone.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(0.3);
            glyph.OrientOn();
            glyph.Update();
            //
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            //
            data.Name += Globals.NameSeparator + "cones";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            //
            ApplySymbolFormatingToActor(actor.Geometry);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddOrientedRotationalConstraintActor(vtkMaxActorData data, double symbolSize)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            //
            double[][] points = data.Geometry.Nodes.Coor;
            double[][] normals = data.Geometry.Nodes.Normals;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < points.GetLength(0); i++)
            {
                pointData.InsertNextPoint(points[i][0], points[i][1], points[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3);
            for (int i = 0; i < normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(normals[i][0], normals[i][1], normals[i][2]);
            }
            // Polydata
            vtkPolyData polydata = vtkPolyData.New();
            polydata.SetPoints(pointData);
            polydata.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polydata);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Create a cube.
            vtkCubeSource cubeSource =  vtkCubeSource.New();
            cubeSource.SetCenter(-1.55, 0.0, 0.0);
            cubeSource.SetXLength(0.4);
            cubeSource.SetYLength(0.9);
            cubeSource.SetZLength(0.9);
            // Line
            vtkLineSource line = vtkLineSource.New();
            line.SetPoint1(0, 0, 0);
            line.SetPoint2(-1.3, 0, 0);
            // Append
            vtkAppendPolyData appendFilter = vtkAppendPolyData.New();
            appendFilter.AddInput(cubeSource.GetOutput());
            appendFilter.AddInput(line.GetOutput());
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(appendFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(0.3);
            glyph.OrientOn();
            glyph.Update();
            //
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            //
            data.Name += Globals.NameSeparator + "cylinder";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.Geometry.GetProperty().SetInterpolationToFlat();
            //
            ApplySymbolFormatingToActor(actor.Geometry);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddOrientedRotationalConstraintActor_(vtkMaxActorData data, double symbolSize)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            //
            double[][] points = data.Geometry.Nodes.Coor;
            double[][] normals = data.Geometry.Nodes.Normals;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < points.GetLength(0); i++)
            {
                pointData.InsertNextPoint(points[i][0], points[i][1], points[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3);
            for (int i = 0; i < normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(normals[i][0], normals[i][1], normals[i][2]);
            }
            // Polydata
            vtkPolyData polydata = vtkPolyData.New();
            polydata.SetPoints(pointData);
            polydata.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polydata);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Line 1
            vtkLineSource line1 = vtkLineSource.New();
            line1.SetPoint1(-1.3, 0, 0);
            line1.SetPoint2(-1.8, 0, 0);
            // Tube
            vtkTubeFilter tube = vtkTubeFilter.New();
            tube.SetInputConnection(line1.GetOutputPort());
            tube.SetRadius(0.5);
            tube.SetNumberOfSides(6);
            tube.CappingOn();
            // Line 2
            vtkLineSource line2 = vtkLineSource.New();
            line2.SetPoint1(0, 0, 0);
            line2.SetPoint2(-1.3, 0, 0);
            // Append
            vtkAppendPolyData appendFilter = vtkAppendPolyData.New();
            appendFilter.AddInput(tube.GetOutput());
            appendFilter.AddInput(line2.GetOutput());
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(appendFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(0.3);
            glyph.OrientOn();
            glyph.Update();
            //
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            //
            data.Name += Globals.NameSeparator + "cylinder";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.Geometry.GetProperty().SetInterpolationToFlat();
            //
            ApplySymbolFormatingToActor(actor.Geometry);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddOrientedArrowsActor(vtkMaxActorData data, double symbolSize, bool invert)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < data.Geometry.Nodes.Coor.GetLength(0); i++)
            {
                pointData.InsertNextPoint(data.Geometry.Nodes.Coor[i][0], data.Geometry.Nodes.Coor[i][1], data.Geometry.Nodes.Coor[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3); //3d normals (ie x,y,z)
            for (int i = 0; i < data.Geometry.Nodes.Normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(data.Geometry.Nodes.Normals[i][0], data.Geometry.Nodes.Normals[i][1], data.Geometry.Nodes.Normals[i][2]);
            }
            // Polydata
            vtkPolyData polydata = vtkPolyData.New();
            polydata.SetPoints(pointData);
            polydata.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polydata);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Source for the glyph filter
            vtkArrowSource arrow = vtkArrowSource.New();
            arrow.SetTipResolution(21);
            arrow.SetTipLength(0.3);
            arrow.SetTipRadius(0.1);
            arrow.SetShaftResolution(11);
            arrow.SetShaftRadius(0.03);            
            // Transform
            vtkTransform transform = vtkTransform.New();
            transform.Identity();
            if (invert) transform.Translate(-1.05, 0, 0);
            else transform.Translate(0.05, 0, 0);
            // Transform filter
            vtkTransformFilter transformFilter = vtkTransformFilter.New();
            transformFilter.SetInput(arrow.GetOutput());
            transformFilter.SetTransform(transform);
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(transformFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(1.0);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(0, glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Actor
            data.Name += Globals.NameSeparator + "arrow";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            // Add
            ApplySymbolFormatingToActor(actor.Geometry);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddOrientedDoubleArrowsActor(vtkMaxActorData data, double symbolSize)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            // Double arrow for moent loads
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < data.Geometry.Nodes.Coor.GetLength(0); i++)
            {
                pointData.InsertNextPoint(data.Geometry.Nodes.Coor[i][0], data.Geometry.Nodes.Coor[i][1], data.Geometry.Nodes.Coor[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3); //3d normals (ie x,y,z)
            for (int i = 0; i < data.Geometry.Nodes.Normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(data.Geometry.Nodes.Normals[i][0], data.Geometry.Nodes.Normals[i][1], data.Geometry.Nodes.Normals[i][2]);
            }
            // Polydata
            vtkPolyData polydata = vtkPolyData.New();
            polydata.SetPoints(pointData);
            polydata.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polydata);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Source for the glyph filter      
            vtkArrowSource arrow = vtkArrowSource.New();
            arrow.SetTipResolution(21);
            arrow.SetTipLength(0.3);
            arrow.SetTipRadius(0.1);
            arrow.SetShaftResolution(11);
            arrow.SetShaftRadius(0.03);
            // Cone
            vtkConeSource cone = vtkConeSource.New();
            cone.SetResolution(21);
            cone.SetHeight(0.3);
            cone.SetRadius(0.1);
            cone.SetCenter(0.65, 0, 0);
            // Append
            vtkAppendPolyData appendFilter = vtkAppendPolyData.New();
            appendFilter.AddInput(arrow.GetOutput());
            appendFilter.AddInput(cone.GetOutput());
            //
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(appendFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(1.0);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Add
            data.Name += Globals.NameSeparator + "doubleArrow";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            //
            ApplySymbolFormatingToActor(actor.Geometry);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        private void AddSymbolEdges(vtkMaxActorData data, vtkAlgorithmOutput output)
        {
            // Compute the silhouette                                                        
            vtkPolyDataSilhouette silhouette = vtkPolyDataSilhouette.New();
            silhouette.SetInputConnection(0, output);
            silhouette.SetCamera(_renderer.GetActiveCamera());
            silhouette.SetEnableFeatureAngle(1);
            silhouette.SetFeatureAngle(60);
            // Create mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(silhouette.GetOutputPort());
            // Create actor
            data.Name += Globals.NameSeparator + "silhouette";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.Geometry.GetProperty().SetLineWidth(1f);
            // Add
            AddActorGeometry(actor, data.Layer);
        }
        //
        private void AddActor(vtkMaxActor actor, vtkRendererLayer layer, bool canHaveElementEdges)
        {
            // Add actor
            AddActorGeometry(actor, layer);
            // Add actorElementEdges
            if (actor.ElementEdges != null) AddActorEdges(actor, false, layer);
            // Add modelEdges
            if (actor.ModelEdges != null) AddActorEdges(actor, true, layer);
        }
        private void AddActorGeometry(vtkMaxActor actor, vtkRendererLayer layer)
        {
            // Add actor
            if (layer == vtkRendererLayer.Base)
            {
                if (actor.Name == null) actor.Name = (_actors.Count + 1).ToString();
                _actors.Add(actor.Name, actor);
                _renderer.AddActor(actor.Geometry);
                //
                if (actor.Geometry.GetPickable() == 1)
                {
                    if (actor.CellLocator != null) _cellPicker.AddLocator(actor.CellLocator);
                    _propPicker.AddPickList(actor.Geometry);
                }
            }
            else if (layer == vtkRendererLayer.Overlay)
            {
                if (actor.Name == null) actor.Name = (_overlayActors.Count + 1).ToString();
                _overlayActors.Add(actor.Name, actor.Geometry);
                _overlayRenderer.AddActor(actor.Geometry);
                //
                actor.Geometry.PickableOff();
            }
            else if (layer == vtkRendererLayer.Selection)
            {
                if (actor.Name == null) actor.Name = (_selectedActors.Count + 1).ToString();
                _selectedActors.Add(actor);
                if (actor.DrawOnGeometry) _renderer.AddActor(actor.Geometry);
                else _selectionRenderer.AddActor(actor.Geometry);
                //
                actor.Geometry.PickableOff();
                ApplySelectionFormatingToActor(actor);
            }
            //
            ApplyEdgeVisibilityAndBackfaceCullingToActor(actor.Geometry, layer);            
        }
        private void AddActorEdges(vtkMaxActor actor, bool isModelEdge, vtkRendererLayer layer)
        {
            if (layer == vtkRendererLayer.Base)
            {
                if (actor.Name == null) actor.Name = (_actors.Count + 1).ToString();
                //
                if (_actors.ContainsKey(actor.Name))
                {
                    if (isModelEdge && _actors[actor.Name].ModelEdges != actor.ModelEdges) throw new Exception("Animation changes exception.");
                    if (!isModelEdge && _actors[actor.Name].ElementEdges != actor.ElementEdges) throw new Exception("Animation changes exception.");
                }
                else
                {
                    _actors.Add(actor.Name, actor);
                }
                //
                if (isModelEdge) _renderer.AddActor(actor.ModelEdges);
                else _renderer.AddActor(actor.ElementEdges);
            }
            else if (layer == vtkRendererLayer.Overlay)
            {
                _overlayActors.Add(actor.Name, actor.Geometry);
                //
                if (isModelEdge) _overlayRenderer.AddActor(actor.ModelEdges);
                else _overlayRenderer.AddActor(actor.ElementEdges);
            }
            else if (layer == vtkRendererLayer.Selection)
            {                
                // wireframe selection
                if (!_selectedActors.Contains(actor)) _selectedActors.Add(actor);
                //
                vtkActor edgeActor;
                if (isModelEdge) edgeActor = actor.ModelEdges;
                else edgeActor = actor.ElementEdges;
                //
                if (actor.DrawOnGeometry) _renderer.AddActor(edgeActor);
                else _selectionRenderer.AddActor(edgeActor);
            }
            //if (isModelEdge) ApplyEdgesFormatingToActor(actor.ModelEdges);
            //else ApplyEdgesFormatingToActor(actor.ElementEdges);
            if (isModelEdge) ApplyEdgeVisibilityAndBackfaceCullingToModelEdges(actor.ModelEdges, actor.Name);
            else ApplyEdgeVisibilityAndBackfaceCullingToActorEdges(actor.ElementEdges, actor.Name);
        }

        #endregion  ################################################################################################################

        #region Highlight geometry  ################################################################################################

        public void HighlightActor(string actorName)
        {
            string sectionViewActorName = GetSectionViewActorName(actorName);
            HighlightActorByName(actorName);
            if (_sectionView) HighlightActorByName(sectionViewActorName);
        }
        private void HighlightActorByName(string actorName)
        {
            //vtkMaxActor actor = GetCopyOfActor(actorName);
            vtkMaxActor actorModelEdges = GetCopyOfModelEdgesActor(actorName);
            //
            //if (actor != null && actorModelEdges != null)
            if (actorModelEdges != null)
            {
                actorModelEdges.Geometry.GetProperty().SetOpacity(1);
                AddActorGeometry(actorModelEdges, vtkRendererLayer.Selection);
                //actor.DrawOnGeometry = true;
                //actor.Geometry.GetProperty().SetOpacity(1);
                //AddActorGeometry(actor, vtkRendererLayer.Selection);
                this.Invalidate();
            }
        }
        private vtkMaxActor GetCopyOfModelEdgesActor(string actorName)
        {
            vtkMaxActor actor = null;
            if (_actors.ContainsKey(actorName))
            {
                vtkMaxActor actorToHighLight = _actors[actorName];
                //
                vtkPolyData data = vtkPolyData.New();
                data.DeepCopy(actorToHighLight.ModelEdges.GetMapper().GetInput());
                //
                vtkDataSetMapper mapper = vtkDataSetMapper.New();
                mapper.SetInput(data);
                //
                actor = new vtkMaxActor();
                actor.Geometry.SetMapper(mapper);
                actor.Geometry.PickableOff();
            }
            return actor;
        }
        private vtkMaxActor GetCopyOfModelEdgesActor(string[] actorNames)
        {
            if (actorNames == null || actorNames.Length == 0) return null;
            //
            if (actorNames.Length == 1) return GetCopyOfModelEdgesActor(actorNames[0]);
            //
            vtkMaxActor actor = null;
            vtkMaxActor actorToHighLight;
            vtkPolyData data;
            vtkAppendPolyData appendFilter = vtkAppendPolyData.New();
            //
            foreach (var actorName in actorNames)
            {
                if (_actors.ContainsKey(actorName))
                {
                    actorToHighLight = _actors[actorName];
                    //
                    data = vtkPolyData.New();
                    data.DeepCopy(actorToHighLight.ModelEdges.GetMapper().GetInput());
                    //
                    appendFilter.AddInput(data);
                }
            }
            //
            vtkDataSetMapper mapper = vtkDataSetMapper.New();
            mapper.SetInput(appendFilter.GetOutput());
            //
            actor = new vtkMaxActor();
            actor.Geometry.SetMapper(mapper);
            actor.Geometry.PickableOff();
            //
            return actor;
        }

        private vtkMaxActor GetCopyOfActor(string actorName)
        {
            vtkMaxActor actor = null;
            if (_actors.ContainsKey(actorName))
            {
                vtkMaxActor actorToHighLight = _actors[actorName];

                vtkPolyData data = vtkPolyData.New();
                data.DeepCopy(actorToHighLight.Geometry.GetMapper().GetInput());

                vtkDataSetMapper mapper = vtkDataSetMapper.New();
                mapper.SetInput(data);

                actor = new vtkMaxActor();
                actor.Geometry.SetMapper(mapper);
                actor.Geometry.PickableOff();
            }
            return actor;
        }

        #endregion  ################################################################################################################

        #region Hide/Show geometry  ################################################################################################

        public void HideActors(string[] actorNames, bool updateColorContours)
        {
            HideShowActors(actorNames, updateColorContours, false);
        }
        public void ShowActors(string[] actorNames, bool updateColorContours)
        {
            HideShowActors(actorNames, updateColorContours, true);
        }
        private void HideShowActors(string[] actorNames, bool updateColorContours, bool visible)
        {
            string[] tmp;
            string[] splitter = new string[] { "_animation-frame-" };
            HashSet<string> actorNamesHash = new HashSet<string>(actorNames);
            foreach (var entry in _actors)
            {
                // Check for animated actors
                tmp = entry.Key.Split(splitter, StringSplitOptions.None);
                if (tmp.Length == 2)
                {
                    // Name splitting also takes care of section view actors since they have the same base name
                    if (actorNamesHash.Contains(tmp[0]))
                    {
                        entry.Value.VtkMaxActorVisible = visible;
                        // Animated actors
                        _animationFrameData.ActorVisible[tmp[0]] = visible;
                    }
                }
                // Non animated actors
                else
                {
                    if (actorNamesHash.Contains(entry.Key)) HideShowActor(entry.Value, visible);
                }
            }
            // Overlay actors
            vtkActor actor;
            foreach (var name in actorNames)
            {
                if (_overlayActors.TryGetValue(name, out actor)) actor.VisibilityOn();
            }
            //
            if (updateColorContours) UpdateScalarFormatting();
            //
            ApplyEdgesVisibilityAndBackfaceCulling();
            _style.AdjustCameraDistanceAndClipping();
        }
        private void HideShowActor(vtkMaxActor actor, bool visible)
        {
            // Use vtkMaxActor Visibility setting
            actor.VtkMaxActorVisible = visible;
            // Section view
            if (_sectionView && actor.SectionViewActor != null) actor.SectionViewActor.VtkMaxActorVisible = visible;
            // Transformed copies
            foreach (var copy in actor.Copies) HideShowActor(copy, visible);
        }

        #endregion  ################################################################################################################

        #region Section view  ######################################################################################################
        public void ApplySectionView(double[] point, double[] normal)
        {
            lock (myLock)
            {
                if (!_sectionView)
                {
                    _sectionView = true;
                    // Set section cut plane
                    _sectionViewPlane = vtkPlane.New();
                    _sectionViewPlane.SetOrigin(point[0], point[1], point[2]);
                    _sectionViewPlane.SetNormal(normal[0], normal[1], normal[2]);
                    //
                    foreach (var entry in _actors)
                    {
                        if (entry.Value.SectionViewPossible) entry.Value.AddClippingPlane(_sectionViewPlane);
                    }
                    // Add new section cut actors
                    vtkMaxActor sectionViewActor;
                    foreach (var actor in _actors.Values.ToArray())     // must be copied to array
                    {
                        if (actor.SectionViewPossible)
                        {
                            sectionViewActor = GetSectionViewActor(actor);
                            if (sectionViewActor != null)
                            {
                                AddActor(sectionViewActor, vtkRendererLayer.Base, true);
                                actor.SectionViewActor = sectionViewActor;
                            }
                        }
                    }
                    //
                    UpdateScalarFormatting();
                    //
                    this.Invalidate();
                }
            }
        }
        public void UpdateSectionView(double[] point, double[] normal)
        {
            lock (myLock)
            {
                if (_sectionView)
                {
                    // Remove previous section cut actors
                    ClearSectionViewActors();
                    // Modify section cut plane
                    _sectionViewPlane.SetOrigin(point[0], point[1], point[2]);
                    _sectionViewPlane.SetNormal(normal[0], normal[1], normal[2]);
                    // Add new section cut actors
                    vtkMaxActor sectionViewActor;
                    foreach (var actor in _actors.Values.ToArray())
                    {
                        if (actor.SectionViewPossible)
                        {
                            sectionViewActor = GetSectionViewActor(actor);
                            if (sectionViewActor != null)
                            {
                                AddActor(sectionViewActor, vtkRendererLayer.Base, true);
                                actor.SectionViewActor = sectionViewActor;
                            }
                        }
                    }
                    //
                    UpdateScalarFormatting();
                    //
                    this.Invalidate();
                }
            }
        }
        public void RemoveSectionView()
        {
            lock (myLock)
            {
                if (_sectionView)
                {
                    foreach (var entry in _actors) entry.Value.RemoveAllClippingPlanes();
                    // Remove previous section cut actors
                    ClearSectionViewActors();
                    //
                    _sectionView = false;
                    _sectionViewPlane = null;
                    //
                    UpdateScalarFormatting();
                    //
                    this.Invalidate();
                }
            }
        }
        //
        private void ClearSectionViewActors()
        {
            vtkMaxActor sectionVewActor;
            foreach (var entry in _actors.ToArray())
            {
                sectionVewActor = entry.Value.SectionViewActor;
                if (sectionVewActor != null)
                {
                    _renderer.RemoveActor(sectionVewActor.Geometry);
                    if (sectionVewActor.ElementEdges != null) _renderer.RemoveActor(sectionVewActor.ElementEdges);
                    if (sectionVewActor.ModelEdges != null) _renderer.RemoveActor(sectionVewActor.ModelEdges);
                    //
                    _actors.Remove(sectionVewActor.Name);
                }
            }
        }
        private vtkMaxActor GetSectionViewActor(vtkMaxActor actor)
        {
            try
            {
                //if (!actor.VtkMaxActorVisible) return null;

                if (actor.ActorRepresentation == vtkMaxActorRepresentation.Solid ||
                    actor.ActorRepresentation == vtkMaxActorRepresentation.Shell)
                {
                    return GetSectionViewActorFromSolid(actor);
                }
                else if (actor.ActorRepresentation == vtkMaxActorRepresentation.SolidAsShell)
                {
                    //return null;
                    return GetSectionViewActorFromSolidAsShell(actor);
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }
        private vtkMaxActor GetSectionViewActorFromSolid(vtkMaxActor actor)
        {
            vtkMaxActor sectionViewActor = new vtkMaxActor(actor);
            sectionViewActor.Name = GetSectionViewActorName(actor.Name);
            sectionViewActor.BackfaceCulling = false;

            // Get the unstructured grid
            vtkUnstructuredGrid grid = vtkUnstructuredGrid.New();
            grid.DeepCopy(actor.FrustumCellLocator.GetDataSet());

            // Create a cutter
            vtkCutter cutter = vtkCutter.New();
            cutter.SetCutFunction(_sectionViewPlane);
            cutter.SetInput(grid);
            cutter.GenerateCutScalarsOff();
            cutter.Update();

            // GEOMETRY  ##########################################################################

            // Create polydata for the geometry actor
            vtkGeometryFilter geometryFilter = vtkGeometryFilter.New();
            geometryFilter.SetInput(cutter.GetOutput());
            geometryFilter.Update();
            vtkPolyData polyData = vtkPolyData.New();
            polyData = geometryFilter.GetOutput();
            // Create the mapper for the geometry actor
            vtkPolyDataMapper polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(polyData);
            // Create actor
            sectionViewActor.Geometry = vtkActor.New();
            sectionViewActor.Geometry.SetMapper(polyMapper);
            sectionViewActor.Geometry.GetProperty().DeepCopy(actor.Geometry.GetProperty());
            sectionViewActor.Geometry.PickableOff();
            sectionViewActor.Geometry.SetVisibility(actor.Geometry.GetVisibility());

            // EDGES  #############################################################################

            // The best function would be:                                                          
            //  - vtkExtractEdges                                                                   
            // but vtkExtractEdges produces internal edges for quadratic cells                      
            // The solution:                                                                        
            //  - extract the faces of all cells and cut the faces to get cell edges in the plane   
            //  - to make it faster first find the cells on the plane                               
            vtkExtractGeometry extract = vtkExtractGeometry.New();
            extract.SetImplicitFunction(_sectionViewPlane);
            extract.ExtractBoundaryCellsOn();
            extract.ExtractOnlyBoundaryCellsOn();
            extract.SetInput(grid);
            extract.Update();

            vtkCell cell;
            vtkCell cellFace;
            vtkCellArray cellArray = vtkCellArray.New();
            vtkUnstructuredGrid cellGrid = vtkUnstructuredGrid.New();
            cellGrid.SetPoints(extract.GetOutput().GetPoints());

            for (int i = 0; i < extract.GetOutput().GetNumberOfCells(); i++)
            {
                cell = extract.GetOutput().GetCell(i);
                for (int j = 0; j < cell.GetNumberOfFaces(); j++)
                {
                    cellFace = cell.GetFace(j);
                    cellGrid.InsertNextCell(cellFace.GetCellType(), cellFace.GetPointIds());
                }
            }
            cellGrid.Update();

            // Sometimes when cutting with a parallel plane, no section is found                                
            // The solution:                                                                                    
            //  - move the cutting plane to find some edges                                                     
            //  - project the edges bacl to the plane to prevent them from hiding behind the section cut surface
            vtkPlane cutPlane = vtkPlane.New();
            double[] origin = _sectionViewPlane.GetOrigin();
            double[] normal = _sectionViewPlane.GetNormal();
            cutPlane.SetOrigin(origin[0], origin[1], origin[2]);
            cutPlane.SetNormal(normal[0], normal[1], normal[2]);

            cutter = vtkCutter.New();
            cutter.SetCutFunction(cutPlane);
            cutter.SetInput(cellGrid);
            cutter.GenerateCutScalarsOff();
            cutter.Update();

            int count = 0;
            int step = 0;
            double delta = 0.01;
            double[] b = GetBoundingBoxSize();
            double[] v = new double[] { b[0] * delta * normal[0], b[1] * delta * normal[1], b[2] * delta * normal[2] };

            if (cellGrid.GetNumberOfCells() > 0 && cutter.GetOutput().GetNumberOfCells() == 0)
            {
                // try to move the cut plane forward and backward   
                while (count++ <= 4)
                {
                    if (cutter.GetOutput().GetNumberOfCells() == 0)
                    {
                        step = (int)Math.Pow(-1, count) * (count + 1) / 2;
                        cutPlane.SetOrigin(origin[0] + step * v[0], origin[1] + step * v[1], origin[2] + step * v[2]);
                        cutter.Update();
                    }
                    else
                    {
                        // the section was found - move the points back to the plane
                        vtkPoints points = cutter.GetOutput().GetPoints();
                        double[] coor;
                        for (int i = 0; i < points.GetNumberOfPoints(); i++)
                        {
                            coor = points.GetPoint(i);
                            coor[0] -= step * v[0];
                            coor[1] -= step * v[1];
                            coor[2] -= step * v[2];
                            points.SetPoint(i, coor[0], coor[1], coor[2]);
                        }
                        break;
                    }
                }
            }

            // Create the mapper for the element edges actor
            polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(cutter.GetOutput());
            // Create actor
            sectionViewActor.ElementEdges = vtkActor.New();
            sectionViewActor.ElementEdges.SetMapper(polyMapper);
            sectionViewActor.ElementEdges.GetProperty().DeepCopy(actor.ElementEdges.GetProperty());
            sectionViewActor.ElementEdges.PickableOff();
            sectionViewActor.ElementEdges.SetVisibility(actor.ElementEdges.GetVisibility());
            sectionViewActor.ElementEdges.GetMapper().GetInput().GetPointData().RemoveArray(Globals.ScalarArrayName);

            // MODEL EDGES  #######################################################################

            vtkFeatureEdges featureEdges = vtkFeatureEdges.New();
            featureEdges.SetInput(polyData);
            featureEdges.BoundaryEdgesOn();
            featureEdges.FeatureEdgesOff();
            featureEdges.ManifoldEdgesOff();
            featureEdges.NonManifoldEdgesOff();
            featureEdges.SetColoring(0);
            featureEdges.Update();
            // Create the mapper for the model edges actor
            polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(featureEdges.GetOutput());
            // Create actor
            sectionViewActor.ModelEdges = vtkActor.New();
            sectionViewActor.ModelEdges.SetMapper(polyMapper);
            sectionViewActor.ModelEdges.GetProperty().DeepCopy(actor.ModelEdges.GetProperty());
            sectionViewActor.ModelEdges.PickableOff();
            sectionViewActor.ModelEdges.SetVisibility(actor.ModelEdges.GetVisibility());
            sectionViewActor.ModelEdges.GetMapper().GetInput().GetPointData().RemoveArray(Globals.ScalarArrayName);

            return sectionViewActor;
        }
        private vtkMaxActor GetSectionViewActorFromSolidAsShell(vtkMaxActor actor)
        {
            vtkMaxActor sectionViewActor = new vtkMaxActor(actor);
            sectionViewActor.Name = GetSectionViewActorName(actor.Name);
            sectionViewActor.BackfaceCulling = false;            

            // Get the unstructured grid
            vtkUnstructuredGrid grid = vtkUnstructuredGrid.New();
            grid.DeepCopy(actor.FrustumCellLocator.GetDataSet());

            // Create a cutter
            vtkCutter cutter = vtkCutter.New();
            cutter.SetCutFunction(_sectionViewPlane);
            //cutter.SetInput(grid);
            cutter.SetInput(actor.Geometry.GetMapper().GetInput());
            cutter.Update();

            vtkStripper cutStrips = vtkStripper.New();
            cutStrips.SetInput(cutter.GetOutput());
            cutStrips.Update();

            vtkPolyData polyData;
            vtkPolyDataMapper polyMapper;
            ///////////////////////////////////////////////////////////////////////////////////////
            polyData = vtkPolyData.New();
            polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(polyData);

            sectionViewActor.Geometry = vtkActor.New();
            sectionViewActor.Geometry.SetMapper(polyMapper);

            polyData = vtkPolyData.New();
            polyData = cutStrips.GetOutput();
            polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(polyData);

            sectionViewActor.ElementEdges = vtkActor.New();
            sectionViewActor.ElementEdges.SetMapper(polyMapper);
            sectionViewActor.ElementEdges.GetProperty().DeepCopy(actor.ElementEdges.GetProperty());
            sectionViewActor.ElementEdges.PickableOff();
            sectionViewActor.ElementEdges.SetVisibility(actor.ElementEdges.GetVisibility());

            sectionViewActor.ModelEdges = vtkActor.New();
            sectionViewActor.ModelEdges.SetMapper(polyMapper);
            sectionViewActor.ModelEdges.GetProperty().DeepCopy(actor.ModelEdges.GetProperty());
            sectionViewActor.ModelEdges.PickableOff();
            sectionViewActor.ModelEdges.SetVisibility(actor.ModelEdges.GetVisibility());

            return sectionViewActor;
            ///////////////////////////////////////////////////////////////////////////////////////

            if (cutter.GetOutput().GetNumberOfPoints() == 0) return null;

            System.Diagnostics.Debug.WriteLine("Cutter num. of points: " + cutter.GetOutput().GetNumberOfPoints());
            System.Diagnostics.Debug.WriteLine("Cutter num. of cells: " + cutter.GetOutput().GetNumberOfCells());
            
            // GEOMETRY                                                                             
            System.Diagnostics.Debug.WriteLine("CutStrips num. of points: " + cutStrips.GetOutput().GetNumberOfPoints());
            System.Diagnostics.Debug.WriteLine("CutStrips num. of cells: " + cutStrips.GetOutput().GetNumberOfCells());

           
            // Noraml
            double[] n = _sectionViewPlane.GetNormal();
            // Axis = n X z(0,0,1)
            double[] axis = new double[] { n[1], -n[0], 0 };
            // Angle
            double l2 = Math.Pow(n[0], 2) + Math.Pow(n[1], 2) + Math.Pow(n[2], 2);
            double phi = Math.Acos(n[2] / Math.Sqrt(l2));
            vtkTransform transform = vtkTransform.New();
            transform.RotateWXYZ(phi, axis[0], axis[1], axis[2]);

            n[0] = Math.Abs(n[0]);
            n[1] = Math.Abs(n[1]);
            n[2] = Math.Abs(n[2]);

            if (cutStrips.GetOutput().GetNumberOfCells() > 1
                && cutStrips.GetOutput().GetCell(0) is vtkPolyLine
                && n[2] == Math.Max(Math.Max(n[0], n[1]), n[2])
                //&& false
                )
            {                
                polyData = cutStrips.GetOutput();

                vtkPolyLine line0 = vtkPolyLine.New();
                vtkPolyLine line1 = vtkPolyLine.New();
                line0.DeepCopy(polyData.GetCell(0));
                line1.DeepCopy(polyData.GetCell(1));


                // Define a polygonal hole with a clockwise polygon
                vtkPolygon polygon0 = vtkPolygon.New();
                vtkPolygon polygon1 = vtkPolygon.New();
                double[] coor;
                long id;

                for (int i = 0; i < line0.GetNumberOfPoints(); i++)
                //for (int i = (int)line0.GetNumberOfPoints() - 1; i >= 0 ; i--)
                {
                    id = line0.GetPointId(i);
                    polygon0.GetPointIds().InsertNextId(id);
                    coor = line0.GetPoints().GetPoint(i);
                    polygon0.GetPoints().InsertNextPoint(coor[0], coor[1], coor[2]);
                }

                for (int i = 0; i < line1.GetNumberOfPoints(); i++)
                //for (int i = (int)line1.GetNumberOfPoints() - 1; i >= 0 ; i--)
                {
                    id = line1.GetPointId(i);
                    polygon1.GetPointIds().InsertNextId(id);
                    coor = line1.GetPoints().GetPoint(i);
                    polygon1.GetPoints().InsertNextPoint(coor[0], coor[1], coor[2]);
                }

                double[] bounds0 = polygon0.GetBounds();
                double[] bounds1 = polygon1.GetBounds();

                vtkDelaunay3D test = vtkDelaunay3D.New();

                // Create a cell array to store the polygon in
                vtkCellArray polygonArray = vtkCellArray.New();
                //polygonArray.InsertNextCell(polygon0);
                //polygonArray.InsertNextCell(polygon1);

                vtkPolyData allPoints = vtkPolyData.New();
                allPoints.SetPoints(cutStrips.GetOutput().GetPoints());

                // Create a polydata to store the boundary. The points must be the
                // same as the points we will triangulate.
                vtkPolyData boundary = vtkPolyData.New();       // this is the boundary
                boundary.SetPoints(allPoints.GetPoints());
                boundary.SetPolys(polygonArray);

                // Triangulate the grid points
                vtkDelaunay2D delaunay = vtkDelaunay2D.New();
                delaunay.SetTransform(transform);
                delaunay.SetInput(0, allPoints);
                delaunay.SetInput(1, boundary);
                delaunay.Update();

                polyData = delaunay.GetOutput();

                //polydata = boundary;

                //if (polydata.GetNumberOfPoints() == 0)
                //    return null;

                polyMapper = vtkPolyDataMapper.New();
                polyMapper.SetInput(polyData);
                //polyMapper.SetInputConnection(delaunay.GetOutputPort());
            }
            else
            {
                polyData = vtkPolyData.New();
                polyData.SetPoints(cutStrips.GetOutput().GetPoints());
                polyData.SetPolys(cutStrips.GetOutput().GetLines());

                vtkTriangleFilter cutTriangles = vtkTriangleFilter.New();
                cutTriangles.SetInput(polyData);
                cutTriangles.Update();

                polyData = cutTriangles.GetOutput();

                // Create the mapper for the geometry actor
                polyMapper = vtkPolyDataMapper.New();
                polyMapper.SetInput(polyData);
                //polyMapper.SetInputConnection(cutTriangles.GetOutputPort());
            }

            if (polyData.GetNumberOfPoints() == 0) return null;

            // Create Actor
            sectionViewActor.Geometry = vtkActor.New();
            sectionViewActor.Geometry.SetMapper(polyMapper);
            sectionViewActor.Geometry.GetProperty().DeepCopy(actor.Geometry.GetProperty());
            sectionViewActor.Geometry.PickableOff();
            sectionViewActor.Geometry.SetVisibility(actor.Geometry.GetVisibility());
            ////////////////////////////////////////////////////////////////////////////////////////
            //vtkClipClosedSurface clipClosed = vtkClipClosedSurface.New();                         
            ////////////////////////////////////////////////////////////////////////////////////////

            // EDGES                                                                                

            System.Diagnostics.Debug.WriteLine("Num. of points: " + polyData.GetNumberOfPoints());
            System.Diagnostics.Debug.WriteLine("Num. of cells: " + polyData.GetNumberOfCells());

            vtkExtractEdges extractEdges = vtkExtractEdges.New();
            extractEdges.SetInput(polyData);
            extractEdges.Update();

            // Create the mapper for the geometry actor
            polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(extractEdges.GetOutput());

            sectionViewActor.ElementEdges = vtkActor.New();
            sectionViewActor.ElementEdges.SetMapper(polyMapper);
            sectionViewActor.ElementEdges.GetProperty().DeepCopy(actor.ElementEdges.GetProperty());
            sectionViewActor.ElementEdges.PickableOff();
            sectionViewActor.ElementEdges.SetVisibility(actor.ElementEdges.GetVisibility());


            // MODEL EDGES                                                                          

            vtkFeatureEdges featureEdges = vtkFeatureEdges.New();
            featureEdges.SetInput(polyData);
            featureEdges.BoundaryEdgesOn();
            featureEdges.FeatureEdgesOff();
            featureEdges.ManifoldEdgesOff();
            featureEdges.NonManifoldEdgesOff();
            featureEdges.SetColoring(0);
            featureEdges.Update();

            // Create the mapper for the geometry actor
            polyMapper = vtkPolyDataMapper.New();
            //polyMapper.SetInput(featureEdges.GetOutput());
            polyMapper.SetInput(cutStrips.GetOutput());

            sectionViewActor.ModelEdges = vtkActor.New();
            sectionViewActor.ModelEdges.SetMapper(polyMapper);
            sectionViewActor.ModelEdges.GetProperty().DeepCopy(actor.ModelEdges.GetProperty());
            sectionViewActor.ModelEdges.PickableOff();
            sectionViewActor.ModelEdges.SetVisibility(actor.ModelEdges.GetVisibility());

            return sectionViewActor;
        }
        //
        private string GetSectionViewActorName(string actorName)
        {
            string name = actorName + Globals.NameSeparator + Globals.SectionViewSuffix + Globals.NameSeparator;
            name = NamedClass.GetNewValueName(_actors.Keys, name, Globals.NameSeparator);
            return name;
        }

        #endregion  ################################################################################################################

        #region Transformations  ###################################################################################################
        public void AddSymetry(int symetryPlane, double[] symetryPoint)
        {
            vtkTransform transform = vtkTransform.New();
            transform.Translate(symetryPoint[0], symetryPoint[1], symetryPoint[2]);
            if (symetryPlane == 0) transform.Scale(-1.0, 1.0, 1.0);
            else if (symetryPlane == 1) transform.Scale(1.0, -1.0, 1.0);
            else if (symetryPlane == 2) transform.Scale(1.0, 1.0, -1.0);
            else throw new NotSupportedException();
            transform.Translate(-symetryPoint[0], -symetryPoint[1], -symetryPoint[2]);
            //
            _transforms.Add(transform, 2);  // must be 2 for 1 loop
        }
        public void AddLinearPattern(double[] displacement, int numOfItems)
        {
            vtkTransform transform = vtkTransform.New();
            transform.Translate(displacement[0], displacement[1], displacement[2]);
            _transforms.Add(transform, numOfItems);
        }
        public void AddCircularPattern(double[] axisPoint, double[] axisNormal, double angle, int numOfItems)
        {
            vtkTransform transform = vtkTransform.New();
            transform.Translate(axisPoint[0], axisPoint[1], axisPoint[2]);
            transform.RotateWXYZ(angle, axisNormal[0], axisNormal[1], axisNormal[2]);
            transform.Translate(-axisPoint[0], -axisPoint[1], -axisPoint[2]);
            //
            _transforms.Add(transform, numOfItems);
        }
        public void ApplyTransforms()
        {
            foreach (var transform in _transforms) TransformAllMaxActors(transform.Key, transform.Value);
            //
            UpdateScalarFormatting();
            //
            this.Invalidate();
        }
        public void RemoveTransformedActors()
        {
            bool copyExists = true;
            while (copyExists)
            {
                copyExists = false;
                foreach (vtkMaxActor actor in _actors.Values.ToArray())
                {
                    if (actor.Copies != null && actor.Copies.Count > 0)
                    {
                        foreach (var copy in actor.Copies) RemoveActorWithCopies(copy);
                        actor.Copies.Clear();
                        //
                        copyExists = true;
                    }
                }
            }
        }
        private void RemoveActorWithCopies(vtkMaxActor actor)
        {
            // Transformed copies
            foreach (var copy in actor.Copies) RemoveActorWithCopies(copy);
            //
            actor.Copies.Clear();
            //
            _actors.Remove(actor.Name);
            //
            _renderer.RemoveActor(actor.Geometry);
            _renderer.RemoveActor(actor.ElementEdges);
            _renderer.RemoveActor(actor.ModelEdges);
        }
        //
        public void TransformAllMaxActors(vtkTransform transform, int numOfItems)
        {
            vtkMaxActor baseActor;
            vtkMaxActor transformedActor;
            foreach (var actor in _actors.Values.ToArray())
            {
                baseActor = actor;
                //
                for (int i = 0; i < numOfItems - 1; i++)
                {
                    transformedActor = new vtkMaxActor(baseActor);
                    transformedActor.Name = GetTransformedActorName(transformedActor.Name);
                    //
                    baseActor.Copies.Add(transformedActor);
                    //
                    TransformMaxActor(transformedActor, transform);
                    //
                    AddActor(transformedActor, vtkRendererLayer.Base, true);
                    //
                    baseActor = transformedActor;
                }
            }
        }
        private void TransformMaxActor(vtkMaxActor vtkMaxActor, vtkTransform transform)
        {
            TransformActor(vtkMaxActor.Geometry, transform);
            if (vtkMaxActor.ElementEdges != null) TransformActor(vtkMaxActor.ElementEdges, transform);
            if (vtkMaxActor.ModelEdges != null) TransformActor(vtkMaxActor.ModelEdges, transform);
            // Transofrm locator to enable section view
            if (vtkMaxActor.FrustumCellLocator != null)
            {
                vtkTransformFilter transformFilter = vtkTransformFilter.New();
                transformFilter.SetInput(vtkMaxActor.FrustumCellLocator.GetDataSet());
                transformFilter.SetTransform(transform);
                transformFilter.Update();
                //
                vtkMaxActor.FrustumCellLocator.SetDataSet(transformFilter.GetOutput());
            }
        }
        private void TransformActor(vtkActor actor, vtkTransform transform)
        {
            vtkPolyData polyData = (vtkPolyData)actor.GetMapper().GetInput();
            //
            vtkTransformPolyDataFilter transformFilter = vtkTransformPolyDataFilter.New();
            transformFilter.SetInput(polyData);
            transformFilter.SetTransform(transform);
            transformFilter.Update();
            //
            double[] scale = transform.GetScale();
            if (scale[0] < 0 || scale[1] < 0 || scale[2] < 0)
            {
                vtkReverseSense reverseSense = vtkReverseSense.New();
                reverseSense.SetInputConnection(transformFilter.GetOutputPort());
                reverseSense.ReverseNormalsOff();
                reverseSense.ReverseCellsOn();
                reverseSense.Update();
                //
                actor.GetMapper().SetInputConnection(reverseSense.GetOutputPort());
            }
            else
            {
                actor.GetMapper().SetInputConnection(transformFilter.GetOutputPort());
            }
            //
            actor.PickableOff();
        }
        //
        private string GetTransformedActorName(string actorName)
        {
            string name = actorName + Globals.NameSeparator + Globals.TransformationSuffix + Globals.NameSeparator;
            name = NamedClass.GetNewValueName(_actors.Keys, name, Globals.NameSeparator);
            return name;
        }

        

        #endregion  ################################################################################################################

        #region Settings ###########################################################################################################
        public void SetCoorSysVisibility(bool visibility)
        {
            if (_coorSys == null) return;
            //
            _drawCoorSys = visibility;
            if (_coorSys != null)
            {
                if (_drawCoorSys)
                {
                    _coorSys.SetViewport(1 - 200f / Width, 0, 1, 200f / Height);
                    _coorSys.SetEnabled(1);
                }
                else _coorSys.SetEnabled(0);
            }
        }
        public void SetScaleWidgetVisibility(bool visibility)
        {
            if (visibility) _scaleWidget.VisibilityOn();
            else _scaleWidget.VisibilityOff();
        }
        public void SetScaleWidgetUnit(string unit)
        {
            _scaleWidget.SetUnit(unit);
            //
            Invalidate();
        }
        public void SetColorSpectrum(vtkMaxColorSpectrum colorSpectrum)
        {
            _colorSpectrum.DeepCopy(colorSpectrum);
            //
            _scalarBarWidget.SetNumberOfColors(_colorSpectrum.NumberOfColors);  // for the scalar bar
            _scalarBarWidget.MinColor = colorSpectrum.MinColor;
            _scalarBarWidget.MaxColor = colorSpectrum.MaxColor;            
        }       
        public void SetScalarBarNumberFormat(string numberFormat)
        {
            _scalarBarWidget.SetLabelFormat(numberFormat);
        }
        public void SetScalarBarText(string fieldName, string componentName, string unitAbbreviation, string minMaxType)
        {
            _scalarBarWidget.SetText(fieldName, componentName, unitAbbreviation, minMaxType);
            //
            UpdateScalarFormatting();
        }
        private string GetUnitAbbreviation()
        {
            if (_scalarBarWidget.GetVisibility() == 0) return "";
            //
            string unitAbbreviation = _scalarBarWidget.UnitAbbreviation;
            if (unitAbbreviation == "/") unitAbbreviation = "";
            else unitAbbreviation = " " + unitAbbreviation;
            return unitAbbreviation;
        }
        public void DrawLegendBackground(bool drawBackground)
        {
            _scalarBarWidget.SetBackgroundColor(1, 1, 1);
            if (drawBackground) _scalarBarWidget.BackgroundVisibilityOn();
            else _scalarBarWidget.BackgroundVisibilityOff();
        }
        public void DrawLegendBorder(bool drawBorder)
        {
            if (drawBorder) _scalarBarWidget.BorderVisibilityOn();
            else _scalarBarWidget.BorderVisibilityOff();
        }
        public void DrawStatusBlockBackground(bool drawBackground)
        {
            _statusBlockWidget.SetBackgroundColor(1, 1, 1);
            if (drawBackground) _statusBlockWidget.BackgroundVisibilityOn();
            else _statusBlockWidget.BackgroundVisibilityOff();
        }
        public void DrawStatusBlockBorder(bool drawBorder)
        {
            if (drawBorder) _statusBlockWidget.BorderVisibilityOn();
            else _statusBlockWidget.BorderVisibilityOff();
        }
        public void SetStatusBlock(string name, DateTime dateTime, float analysisTimeOrFrequency, string unit,
                                   float scaleFactor, DataFieldType fieldType, int incrementNumber)
        {
            if (_statusBlockWidget == null) return;
            _statusBlockWidget.Name = name;
            _statusBlockWidget.AnalysisTime = analysisTimeOrFrequency;
            _statusBlockWidget.AnalysisTimeUnit = unit;
            _statusBlockWidget.DateTime = dateTime;
            _statusBlockWidget.DeformationScaleFactor = scaleFactor;
            _statusBlockWidget.FieldType = fieldType;
            _statusBlockWidget.AnimationScaleFactor = -1;
            _statusBlockWidget.IncrementNumber = incrementNumber;
            _statusBlockWidget.VisibilityOn();
        }
        public void SetBackground(bool gradient, Color topColor, Color bottomColor, bool redraw)
        {
            if (_renderer != null)
            {
                _renderer.SetGradientBackground(gradient);
                _renderer.SetBackground2(topColor.R / 255.0, topColor.G / 255.0, topColor.B / 255.0);
                _renderer.SetBackground(bottomColor.R / 255.0, bottomColor.G / 255.0, bottomColor.B / 255.0);
                if (redraw) this.Invalidate();
            }
        }
        public void SetLighting(double ambient, double diffuse, bool redraw)
        {
            foreach (var entry in _actors)
            {
                entry.Value.Ambient = ambient; // calls update color
                entry.Value.Diffuse = diffuse; // calls update color
            }
            if (redraw) this.Invalidate();
        }
        public void SetSmoothing(bool pointSmoothing, bool lineSmoothing, bool redraw)
        {
            if (_renderWindow != null)
            {
                if (pointSmoothing != (_renderWindow.GetPointSmoothing() == 1) ||
                    lineSmoothing != (_renderWindow.GetLineSmoothing() == 1))
                {
                    _renderWindow.Dispose();
                    _renderWindowInteractor.Dispose();

                    _renderWindow = vtkRenderWindow.New();
                    _renderWindow.SetParentId(this.Handle);
                    _renderWindow.SetNumberOfLayers(3);

                    _renderWindow.AddRenderer(_renderer);
                    _renderWindow.AddRenderer(_overlayRenderer);
                    _renderWindow.AddRenderer(_selectionRenderer);

                    SyncRenderWindowSize();

                    _renderWindowInteractor = vtkRenderWindowInteractor.New();
                    _renderWindowInteractor.SetInteractorStyle(_style);
                    _style.Reset();

                    _renderWindow.SetInteractor(_renderWindowInteractor);

                    _coorSys.SetInteractor(_renderWindowInteractor);
                    //_scaleBarWidget.SetInteractor(_selectionRenderer, _renderWindowInteractor);
                    //_scalarBarWidget.SetInteractor(_selectionRenderer, _renderWindowInteractor);
                    //_statusBlockWidget.SetInteractor(_selectionRenderer, _renderWindowInteractor);
                    //_minValueWidget.SetInteractor(_selectionRenderer, _renderWindowInteractor);
                    //_maxValueWidget.SetInteractor(_selectionRenderer, _renderWindowInteractor);
                    //_probeWidget.SetInteractor(_selectionRenderer, _renderWindowInteractor);

                    if (_drawCoorSys) _coorSys.EnabledOn();
                    else _coorSys.EnabledOff();

                    if (pointSmoothing) _renderWindow.PointSmoothingOn();
                    else _renderWindow.PointSmoothingOff();

                    if (lineSmoothing) _renderWindow.LineSmoothingOn();
                    else _renderWindow.LineSmoothingOff();
                }
            }
            
            if (redraw) this.Invalidate();
        }
        public void SetHighlightColor(Color primaryHighlightColor, Color secundaryHighlightColor)
        {
            _primaryHighlightColor = primaryHighlightColor;
            _secundaryHighlightColor = secundaryHighlightColor;
            //
            Color highlightColor;
            foreach (var actor in _selectedActors)
            {
                if (actor.UseSecondaryHighightColor) highlightColor = secundaryHighlightColor;
                else highlightColor = primaryHighlightColor;
                //
                actor.Geometry.GetProperty().SetColor(highlightColor.R / 255.0,
                                                      highlightColor.G / 255.0,
                                                      highlightColor.B / 255.0);
            }
            this.Invalidate();
        }
        public void SetMouseHighlightColor(Color mousehighlightColor)
        {
            Globals.CurrentMouseHighlightColor = mousehighlightColor;
            Globals.Initialize();
        }
        public void SetDrawSymbolEdges(bool drawSymbolEdges)
        {
            _drawSymbolEdges = drawSymbolEdges;
        }
        
        #endregion #################################################################################################################

        #region Scalar fields ######################################################################################################

        public void AddScalarFieldOnCells(vtkMaxActorData data)
        {
            // Create actor
            vtkMaxActor actor = new vtkMaxActor(data);

            // Add actor
            AddActorGeometry(actor, vtkRendererLayer.Base);

            // Add actorEdges
            if (data.CanHaveElementEdges && actor.ElementEdges != null) AddActorEdges(actor, false, vtkRendererLayer.Base);

            // Add modelEdges
            if (actor.ModelEdges != null) AddActorEdges(actor, true, data.Layer);

            // Update scalar field
            UpdateScalarFormatting();

            _style.AdjustCameraDistanceAndClipping();
            this.Invalidate();
        }
        //
        public bool AddAnimatedScalarFieldOnCells(vtkMaxActorData data)
        {
            if (_animationAcceleration) return AddAnimatedScalarFieldOnCellsAllActors(data);
            else return AddAnimatedScalarFieldOnCellsAddRemoveActors(data);
        }
        public bool AddAnimatedScalarFieldOnCellsAddRemoveActors(vtkMaxActorData data)
        {
            //countError = 0;
            int n = data.Geometry.NodesAnimation.Length;

            // Create actor
            vtkMaxActor actor;
            vtkMaxActor baseActor = new vtkMaxActor(data);

            // Add actor
            AddActorGeometry(baseActor, data.Layer);
            _animationFrameData.MemMb += n * baseActor.Geometry.GetMapper().GetInput().GetActualMemorySize() / 1024.0;

            // Add actorElementEdges
            if (data.CanHaveElementEdges && baseActor.ElementEdges != null)
            {
                AddActorEdges(baseActor, false, data.Layer);
                _animationFrameData.MemMb += n * baseActor.ElementEdges.GetMapper().GetInput().GetActualMemorySize() / 1024.0;
            }

            // Add modelEdges
            if (baseActor.ModelEdges != null)
            {
                AddActorEdges(baseActor, true, data.Layer);
                _animationFrameData.MemMb += n * baseActor.ModelEdges.GetMapper().GetInput().GetActualMemorySize() / 1024.0;
            }

            string name;
            vtkMaxActor[] actors = new vtkMaxActor[n];
            for (int i = 0; i < n; i++)
            {
                name = data.Name + "_animation-frame-" + i;
                // Create actor
                actor = new vtkMaxActor(baseActor);
                actor.SetAnimationFrame(data, i);
                actor.Name = name;

                // Add actor
                actors[i] = actor;
            }

            _animationActors.Add(baseActor.Name, actors);

            _style.AdjustCameraDistanceAndClipping();
            this.Invalidate();

            return true;
        }
        public bool AddAnimatedScalarFieldOnCellsAllActors(vtkMaxActorData data)
        {
            string name;
            Dictionary<int, string> animatedActorNames = new Dictionary<int, string>();

            vtkMaxActor actor;
            vtkMaxActor baseActor = new vtkMaxActor(data);

            int n = data.Geometry.NodesAnimation.Length;
            for (int i = 0; i < n; i++)
            {
                // Animated actor names
                name = data.Name + "_animation-frame-" + i;
                animatedActorNames.Add(i, name);

                // Create actor
                actor = new vtkMaxActor(baseActor);
                actor.Name = name;
                actor.SetAnimationFrame(data, i);

                // Add actor
                AddActorGeometry(actor, vtkRendererLayer.Base);
                _animationFrameData.MemMb += actor.Geometry.GetMapper().GetInput().GetActualMemorySize() / 1024.0;

                // Add actorEdges
                if (data.CanHaveElementEdges && actor.ElementEdges != null)
                {
                    AddActorEdges(actor, false, vtkRendererLayer.Base);
                    _animationFrameData.MemMb += actor.ElementEdges.GetMapper().GetInput().GetActualMemorySize() / 1024.0;
                }

                // Add modelEdges
                if (actor.ModelEdges != null)
                {
                    AddActorEdges(actor, true, data.Layer);
                    _animationFrameData.MemMb += actor.ModelEdges.GetMapper().GetInput().GetActualMemorySize() / 1024.0;
                }
                int memLimit = 1000;
                if (_animationFrameData.MemMb > memLimit)
                {
                    if (MessageBox.Show("The size of the problem requires more than " + memLimit + " MB of RAM." +
                                        "This might cause the application to shut down unexpectedly. Continue?",
                                        "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        _animationFrameData.MemMb = -1000000;   // prevet the messagebox from reappearing
                    }
                    else return false;
                }
            }

            _animationFrameData.ActorVisible.Add(data.Name, true);
            _animationFrameData.AnimatedActorNames.Add(animatedActorNames);

            // UpdateScalarFormatting();   // this is called for the first time in the SetAnimationFrame

            _style.AdjustCameraDistanceAndClipping();
            this.Invalidate();

            return true;
        }
        //
        private void UpdateScalarFormatting()
        {
            vtkMaxActor actor;
            vtkMapper mapper;
            vtkPointData pointData;

            // Legend
            _scalarBarWidget.VisibilityOff();

            bool minVisible = _minValueWidget.GetVisibility() == 1;
            bool maxVisible = _maxValueWidget.GetVisibility() == 1;
            _minValueWidget.VisibilityOff();
            _maxValueWidget.VisibilityOff();

            vtkMaxExtreemeNode minNode = null;
            vtkMaxExtreemeNode maxNode = null;

            // Find min and max value on actors
            foreach (var entry in _actors)
            {
                actor = entry.Value;
                if (entry.Value == null) throw new ArgumentNullException("_actors.Actor", "The actor does not exist.");
                mapper = actor.Geometry.GetMapper();
                pointData = mapper.GetInput().GetPointData();

                // if the part does not have scalar data
                if (actor.MinNode == null || actor.MaxNode == null) continue;

                if (actor.ColorContours)
                {
                    if (pointData.GetAttribute(0) == null || pointData.GetAttribute(0).GetName() != Globals.ScalarArrayName)
                        pointData.SetActiveScalars(Globals.ScalarArrayName);
                    if (mapper.GetInterpolateScalarsBeforeMapping() != 1)
                        mapper.SetInterpolateScalarsBeforeMapping(1); // discrete colors
                }
                else
                {
                    if (pointData.GetAttribute(0) != null && pointData.GetAttribute(0).GetName() != "none")
                        pointData.SetActiveScalars("none");
                    if (mapper.GetInterpolateScalarsBeforeMapping() != 0)
                        mapper.SetInterpolateScalarsBeforeMapping(0); // discrete colors must be turned off
                    actor.Geometry.GetProperty().SetColor(actor.Color.R / 255d, actor.Color.G / 255d, actor.Color.B / 255d);
                }

                if (!actor.VtkMaxActorVisible || !actor.ColorContours) continue;

                if (minNode == null || maxNode == null)     // the first time through
                {
                    minNode = actor.MinNode;
                    maxNode = actor.MaxNode;
                }
                else
                {
                    if (actor.MinNode != null && actor.MinNode.Value < minNode.Value) minNode = actor.MinNode;
                    if (actor.MaxNode != null && actor.MaxNode.Value > maxNode.Value) maxNode = actor.MaxNode;
                }
            }
            if (minNode == null || maxNode == null) return;
            
            // Scalar bar and min/max values of actor scalar range
            if (_colorSpectrum.MinMaxType == vtkColorSpectrumMinMaxType.Automatic)
            {
                if (_animationFrameData != null && _animationFrameData.UseAllFrameData)   // animation from all frames
                {
                    double[] animRange = _animationFrameData.AllFramesScalarRange;
                    _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), animRange[0], animRange[1]);
                    PrepareActorLookupTable(animRange[0], animRange[1]);
                }
                else // min max from current frame
                {
                    _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), minNode.Value, maxNode.Value);
                    PrepareActorLookupTable(minNode.Value, maxNode.Value);
                }
            }
            else  // Manual and min max from current frame
            {
                _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), minNode.Value, maxNode.Value, 
                                                   _colorSpectrum.MinUserValue, _colorSpectrum.MaxUserValue);
                PrepareActorLookupTable(minNode.Value, maxNode.Value);
            }
            
            // Edit actors mapper
            double[] actorRange = _lookupTable.GetTableRange();
            vtkLookupTable lookup = vtkLookupTable.New();
            lookup.DeepCopy(_lookupTable);
            foreach (var entry in _actors)
            {
                actor = entry.Value;
                mapper = actor.Geometry.GetMapper();
                pointData = mapper.GetInput().GetPointData();
                if (actor.ColorContours && actor.VtkMaxActorVisible && pointData.GetScalars() != null
                    && !_animationFrameData.InitializedActorNames.Contains(entry.Value.Name)) // animation speedup
                {
                    mapper.SetScalarRange(actorRange[0], actorRange[1]);
                    mapper.SetLookupTable(lookup);
                }
            }

            // Scalar bar
            _scalarBarWidget.VisibilityOn();
           
            // Min Max widgets
            string format = _scalarBarWidget.GetLabelFormat();
           
            //
            double[] coor;
            if (minVisible)
            {
                _minValueWidget.VisibilityOn();
                coor = minNode.Coor;
                _minValueWidget.SetText("Min: " + minNode.Value.ToString(format) + GetUnitAbbreviation() + Environment.NewLine +
                                        "Node id: " + minNode.Id);
                _minValueWidget.SetAnchorPoint(coor[0], coor[1], coor[2]);
            }

            if (maxVisible)
            {
                _maxValueWidget.VisibilityOn();
                coor = maxNode.Coor;
                _maxValueWidget.SetText("Max: " + maxNode.Value.ToString(format) + GetUnitAbbreviation() + Environment.NewLine +
                                        "Node id: " + maxNode.Id);
                _maxValueWidget.SetAnchorPoint(coor[0], coor[1], coor[2]);
            }
        }
        public void UpdateActorScalarField(string actorName, float[] values, NodesExchangeData extremeNodes,
                                           float[] frustumCellLocatorValues)
        {
            if (System.Diagnostics.Debugger.IsAttached && false)
            {
                Test(actorName, values, extremeNodes, frustumCellLocatorValues);
                return;
            }
            
            // Add scalars
            if (values != null)
            {
                vtkFloatArray scalars = vtkFloatArray.New();
                scalars.SetName(Globals.ScalarArrayName);
                scalars.SetNumberOfValues(values.Length);
                for (int i = 0; i < values.Length; i++) scalars.SetValue(i, values[i]);
                //
                vtkFloatArray frustumScalars = vtkFloatArray.New();
                frustumScalars.SetName(Globals.ScalarArrayName);
                frustumScalars.SetNumberOfValues(frustumCellLocatorValues.Length);
                for (int i = 0; i < frustumCellLocatorValues.Length; i++) frustumScalars.SetValue(i, frustumCellLocatorValues[i]);
                //
                UpdateActorScalarField(actorName, scalars, frustumScalars, extremeNodes);
                //
                UpdateScalarFormatting();
                //
                this.Invalidate();
            }
        }
        private void UpdateActorScalarField(string actorName, vtkFloatArray scalars, vtkFloatArray frustumScalars,
                                            NodesExchangeData extremeNodes)
        {
            vtkMaxActor actor = _actors[actorName];
            // Transformed copies
            foreach (var copy in actor.Copies) UpdateActorScalarField(copy.Name, scalars, frustumScalars, extremeNodes);
            //
            // Set scalars
            actor.Geometry.GetMapper().GetInput().GetPointData().SetScalars(scalars);
            actor.MinNode = new vtkMaxExtreemeNode(extremeNodes.Ids[0], extremeNodes.Coor[0], extremeNodes.Values[0]);
            actor.MaxNode = new vtkMaxExtreemeNode(extremeNodes.Ids[1], extremeNodes.Coor[1], extremeNodes.Values[1]);
            // Set frustum scalars
            actor.FrustumCellLocator.GetDataSet().GetPointData().SetScalars(frustumScalars);
        }
        private void Test(string actorName, float[] values, CaeGlobals.NodesExchangeData extremeNodes,
                                            float[] frustumCellLocatorValues)
        {
            // Add scalars
            if (values != null)
            {
                vtkFloatArray scalars = vtkFloatArray.New();
                scalars.SetName(Globals.ScalarArrayName);
                scalars.SetNumberOfValues(values.Length);
                for (int i = 0; i < values.Length; i++)
                {
                    scalars.SetValue(i, values[i]);
                }

                vtkFloatArray frustumScalars = vtkFloatArray.New();
                frustumScalars.SetName(Globals.ScalarArrayName);
                frustumScalars.SetNumberOfValues(frustumCellLocatorValues.Length);
                for (int i = 0; i < frustumCellLocatorValues.Length; i++)
                {
                    frustumScalars.SetValue(i, frustumCellLocatorValues[i]);
                }
                
                if (_actors.ContainsKey(actorName)) // added for testing
                {
                    // Set scalars
                    _actors[actorName].GeometryPointData.SetScalars(scalars);
                    //
                    _actors[actorName].MinNode = new vtkMaxExtreemeNode(extremeNodes.Ids[0], extremeNodes.Coor[0], extremeNodes.Values[0]);
                    _actors[actorName].MaxNode = new vtkMaxExtreemeNode(extremeNodes.Ids[1], extremeNodes.Coor[1], extremeNodes.Values[1]);
                    //Set frustum scalars
                    _actors[actorName].FrustumPointData.SetScalars(frustumScalars);
                    //up to here 185000++
                }
                
                //UpdateScalarFormatting();
                if (true)
                {
                    vtkMaxActor actor;
                    //vtkMapper mapper;
                    vtkPointData pointData;

                    bool minVisible = _minValueWidget.GetVisibility() == 1;
                    bool maxVisible = _maxValueWidget.GetVisibility() == 1;
                    _minValueWidget.VisibilityOff();
                    _maxValueWidget.VisibilityOff();

                    vtkMaxExtreemeNode minNode = null;
                    vtkMaxExtreemeNode maxNode = null;

                    // Find min and max value on actors
                    foreach (var entry in _actors)
                    {
                        actor = entry.Value;
                        if (entry.Value == null) throw new ArgumentNullException("_actors.Actor", "The actor does not exist.");
                        //mapper = actor.Geometry.GetMapper();
                        pointData = actor.GeometryPointData;

                        // if the part does not have scalar data
                        if (actor.MinNode == null || actor.MaxNode == null) continue;

                        if (actor.ColorContours)
                        {
                            if (pointData.GetAttribute(0) == null || pointData.GetAttribute(0).GetName() != Globals.ScalarArrayName)
                                pointData.SetActiveScalars(Globals.ScalarArrayName);
                            //if (mapper.GetInterpolateScalarsBeforeMapping() != 1)
                            //    mapper.SetInterpolateScalarsBeforeMapping(1); // discrete colors
                        }
                        else
                        {
                            if (pointData.GetAttribute(0) != null && pointData.GetAttribute(0).GetName() != "none")
                                pointData.SetActiveScalars("none");
                            //if (mapper.GetInterpolateScalarsBeforeMapping() != 0)
                            //    mapper.SetInterpolateScalarsBeforeMapping(0); // discrete colors must be turned off
                            actor.Geometry.GetProperty().SetColor(actor.Color.R / 255d, actor.Color.G / 255d, actor.Color.B / 255d);
                        }

                        if (!actor.VtkMaxActorVisible || !actor.ColorContours) continue;

                        if (minNode == null || maxNode == null)     // the first time through
                        {
                            minNode = actor.MinNode;
                            maxNode = actor.MaxNode;
                        }
                        else
                        {
                            if (actor.MinNode != null && actor.MinNode.Value < minNode.Value) minNode = actor.MinNode;
                            if (actor.MaxNode != null && actor.MaxNode.Value > maxNode.Value) maxNode = actor.MaxNode;
                        }
                    }

                    // Working to here





                    //if (minNode == null || maxNode == null) return;

                    //// Scalar bar and min/max values of actor scalar range
                    //if (_colorSpectrum.MinMaxType == vtkColorSpectrumMinMaxType.Automatic)
                    //{
                    //    if (_animationFrameData != null && _animationFrameData.UseAllFrameData)   // animation from all frames
                    //    {
                    //        double[] animRange = _animationFrameData.AllFramesScalarRange;
                    //        _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), animRange[0], animRange[1]);
                    //        PrepareActorLookupTable(animRange[0], animRange[1]);
                    //    }
                    //    else // min max from current frame
                    //    {
                    //        _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), minNode.Value, maxNode.Value);
                    //        PrepareActorLookupTable(minNode.Value, maxNode.Value);
                    //    }
                    //}
                    //else  // Manual and min max from current frame
                    //{
                    //    _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), minNode.Value, maxNode.Value, _colorSpectrum.MinUserValue, _colorSpectrum.MaxUserValue);
                    //    PrepareActorLookupTable(minNode.Value, maxNode.Value);
                    //}
                }

                this.Invalidate();

                countError++;
                if (countError % 1000 == 0) System.Diagnostics.Debug.WriteLine("Count: " + countError);
            }
        }
        public void UpdateActorsColorContoursVisibility(string[] actorNames, bool colorContours)
        {
            foreach (var name in actorNames) UpdateActorColorContoursVisibility(name, colorContours);
            //
            UpdateScalarFormatting();
            //
            this.Invalidate();
        }
        private void UpdateActorColorContoursVisibility(string actorName, bool colorContours)
        {
            vtkMaxActor actor = _actors[actorName];
            actor.ColorContours = colorContours;
            // Transformed copies
            foreach (var copy in actor.Copies) UpdateActorColorContoursVisibility(copy.Name, colorContours);
            // Section view
            if (actor.SectionViewActor != null) actor.SectionViewActor.ColorContours = colorContours;
        }


        #endregion #################################################################################################################

        #region Animation ##########################################################################################################
        public void SetAnimationFrameData(float[] time, float[] scale, double[] allFramesScalarRange)
        {
            _animationFrameData.Time = time;
            _animationFrameData.ScaleFactor = scale;
            _animationFrameData.AllFramesScalarRange = allFramesScalarRange;
        }

        public void SetAnimationFrame(int frameNumber, bool scalarRangeFromAllFrames)
        {
            if (_animationAcceleration) SetAnimationFrameAllActors(frameNumber, scalarRangeFromAllFrames);
            else SetAnimationFrameAddRemoveActors(frameNumber, scalarRangeFromAllFrames);
        }
        private void SetAnimationFrameAddRemoveActors(int frameNumber, bool scalarRangeFromAllFrames)
        {
            _animationFrameData.UseAllFrameData = scalarRangeFromAllFrames;
            //
            if (_statusBlockWidget != null && _animationFrameData != null)
            {
                _statusBlockWidget.AnalysisTime = _animationFrameData.Time[frameNumber];
                _statusBlockWidget.AnimationScaleFactor = _animationFrameData.ScaleFactor[frameNumber];
            }
            //
            List<string> visibleActors = new List<string>();
            vtkMaxActor actor;
            bool applySectionView = false;
            double[] point = null;
            double[] normal = null;
            if (_sectionView)
            {
                point = _sectionViewPlane.GetOrigin();
                normal = _sectionViewPlane.GetNormal();
                applySectionView = true;
                RemoveSectionView();
            }
            //
            RemoveTransformedActors();
            //
            bool visible;
            foreach (var entry in _animationActors)
            {
                // Find actor
                if (_actors.TryGetValue(entry.Key, out actor))
                {
                    visible = actor.VtkMaxActorVisible;
                    // Remove actor
                    RemoveActorWithCopies(actor);
                    _actors.Remove(entry.Key);  // must be here to remove the actor - it has a different key than name
                    //
                    if (visible)
                    {
                        // Get animation actor
                        actor = entry.Value[frameNumber];
                        // Add actor
                        _actors.Add(entry.Key, actor);
                        _renderer.AddActor(actor.Geometry);
                        _renderer.AddActor(actor.ElementEdges);
                        _renderer.AddActor(actor.ModelEdges);
                        // Add actor name
                        visibleActors.Add(actor.Name);
                    }
                }
            }
            //
            ApplyTransforms();
            //
            if (applySectionView) ApplySectionView(point, normal);
            //
            UpdateScalarFormatting();
            //
            _animationFrameData.InitializedActorNames.UnionWith(visibleActors); // add visible actors to initialized actors
            //
            ApplyEdgesVisibilityAndBackfaceCulling();   // calls invalidate
            _style.AdjustCameraDistanceAndClipping();
            //
            //countError++;
            //if (countError % 10 == 0) System.Diagnostics.Debug.WriteLine("Count: " + countError);
        }
        
        private void SetAnimationFrameAllActors(int frameNumber, bool scalarRangeFromAllFrames)
        {
            _animationFrameData.UseAllFrameData = scalarRangeFromAllFrames;
            //
            if (_statusBlockWidget != null && _animationFrameData != null)
            {
                _statusBlockWidget.AnalysisTime = _animationFrameData.Time[frameNumber];
                _statusBlockWidget.AnimationScaleFactor = _animationFrameData.ScaleFactor[frameNumber];
            }
            //
            bool visible;
            vtkMaxActor maxActor;
            List<string> visibleActors = new List<string>();
            string[] tmp;
            string[] splitter = new string[] { "_animation-frame-" };
            foreach (var listEntry in _animationFrameData.AnimatedActorNames)
            {
                foreach (var entry in listEntry)
                {
                    tmp = entry.Value.Split(splitter, StringSplitOptions.None);
                    if (_animationFrameData.ActorVisible[tmp[0]])
                    {
                        if (_actors.TryGetValue(entry.Value, out maxActor))
                        {
                            visible = entry.Key == frameNumber;
                            //
                            HideShowActor(maxActor, visible);
                            if (visible) visibleActors.Add(entry.Value);
                        }
                    }
                }
            }
            //
            UpdateScalarFormatting();
            //
            _animationFrameData.InitializedActorNames.UnionWith(visibleActors); // add visible actors to initialized actors
            //
            ApplyEdgesVisibilityAndBackfaceCulling();   // calls invalidate
            _style.AdjustCameraDistanceAndClipping();
        }

        public void SaveAnimationAsAVI(string fileName, int[] firstLastFrame, int step, int fps, bool scalarRangeFromAllFrames, bool swing, bool encoderOptions)
        {
            if (step < 1) step = 1;

            if (System.IO.File.Exists(fileName))
            {
                if (CaeGlobals.Tools.IsFileLocked(fileName))
                {
                    MessageBox.Show("The selected file can not be replaced.", "Error", MessageBoxButtons.OK);
                    return;
                }
            }

            vtkWindowToImageFilter windowToImage = vtkWindowToImageFilter.New();
            windowToImage.SetInput(_renderWindow);
            windowToImage.SetInputBufferTypeToRGB();

            vtkAVIWriter avw = vtkAVIWriter.New();
            avw.SetInputConnection(windowToImage.GetOutputPort());
            avw.SetFileName(fileName);
            avw.SetQuality(2);
            avw.SetRate(fps);
            if (encoderOptions) avw.SetPromptCompressionOptions(1);

            vtkObject.GlobalWarningDisplayOff();    // if the video compression window is closed an error occurs
            avw.Start();
            vtkObject.GlobalWarningDisplayOn();

            for (int i = firstLastFrame[0]; i <= firstLastFrame[1]; i += step)
            {
                SetAnimationFrame(i, scalarRangeFromAllFrames);
                windowToImage.Modified();
                avw.Write();
            }
            if (swing)
            {
                for (int i = firstLastFrame[1]; i >= firstLastFrame[0]; i -= step)
                {
                    SetAnimationFrame(i, scalarRangeFromAllFrames);
                    windowToImage.Modified();
                    avw.Write();
                }
            }
            avw.End();
        }
        public void SaveAnimationAsImages(string fileName, int[] firstLastFrame, int step, bool scalarRangeFromAllFrames, bool swing)
        {
            if (step < 1) step = 1;

            int count = 0;
            for (int i = firstLastFrame[0]; i <= firstLastFrame[1]; i += step) count++;
            if (swing) for (int i = firstLastFrame[1]; i >= firstLastFrame[0]; i -= step) count++;

            // create file names
            string[] fileNames = new string[count];
            int numOfPlaces = (int)Math.Log10(count) + 1;
            string path = System.IO.Path.GetDirectoryName(fileName);
            fileName = System.IO.Path.GetFileName(fileName);

            for (int i = 0; i < fileNames.Length; i++)
            {
                fileNames[i] = System.IO.Path.Combine(path, (i + 1).ToString().PadLeft(numOfPlaces, '0') + "_" + fileName);

                if (System.IO.File.Exists(fileNames[i]))
                {
                    if (CaeGlobals.Tools.IsFileLocked(fileNames[i]))
                    {
                        MessageBox.Show("The file '" + fileNames[i] + "' can not be replaced.", "Error", MessageBoxButtons.OK);
                        return;
                    }
                }
            }

            vtkWindowToImageFilter windowToImage = vtkWindowToImageFilter.New();
            windowToImage.SetInput(_renderWindow);
            windowToImage.SetInputBufferTypeToRGB();

            vtkPNGWriter pngWriter = vtkPNGWriter.New();
            pngWriter.SetInputConnection(windowToImage.GetOutputPort());

            count = 0;
            for (int i = firstLastFrame[0]; i <= firstLastFrame[1]; i += step)
            {
                SetAnimationFrame(i, scalarRangeFromAllFrames);
                windowToImage.Modified();
                pngWriter.SetFileName(fileNames[count++]);
                pngWriter.Write();
            }
            if (swing)
            {
                for (int i = firstLastFrame[1]; i >= firstLastFrame[0]; i -= step)
                {
                    SetAnimationFrame(i, scalarRangeFromAllFrames);
                    windowToImage.Modified();
                    pngWriter.SetFileName(fileNames[count++]);
                    pngWriter.Write();
                }
            }
        }

        #endregion #################################################################################################################

        #region Clear ##############################################################################################################
        public void Clear()
        {
            if (_sectionView) RemoveSectionView();
            _transforms.Clear();
            //
            foreach (var entry in _actors)
            {
                _renderer.RemoveActor(entry.Value.Geometry);
                _renderer.RemoveActor(entry.Value.ElementEdges);
                _renderer.RemoveActor(entry.Value.ModelEdges);
            }
            //_renderer.RemoveAllViewProps();  this removes all other actors for min/max values ...
            _actors.Clear();            
            _cellPicker.RemoveAllLocators();
            _propPicker.InitializePickList();
            _animationActors.Clear();
            _animationFrameData = new vtkMaxAnimationFrameData();
            //
            if (_scalarBarWidget != null) _scalarBarWidget.VisibilityOff();
            if (_statusBlockWidget != null) _statusBlockWidget.VisibilityOff();
            if (_minValueWidget != null) _minValueWidget.VisibilityOff();
            if (_maxValueWidget != null) _maxValueWidget.VisibilityOff();
            if (_style != null) _style.Reset();
            //
            ClearSelection();
            ClearOverlay();
            RemoveSectionView();
        }
        public void ClearButKeepParts(string[] partNames)
        {
            vtkMaxActor actor;
            List<string> actorsToRemove = new List<string>();

            // Add section cut actors if they exist
            HashSet<string> partNamesToKeep = new HashSet<string>(partNames);
            if (_sectionView)
            {
                for (int i = 0; i < partNames.Length; i++)
                    partNamesToKeep.Add(GetSectionViewActorName(partNames[i]));
            }

            foreach (var entry in _actors)
            {
                actor = entry.Value;
                if (!partNamesToKeep.Contains(actor.Name))
                {
                    actorsToRemove.Add(actor.Name);
                    // remove from renderer
                    _renderer.RemoveActor(actor.Geometry);
                    _renderer.RemoveActor(actor.ElementEdges);
                    _renderer.RemoveActor(actor.ModelEdges);
                    // remove cell locator and picker
                    if (actor.Geometry.GetPickable() == 1)
                    {
                        if (actor.CellLocator != null) _cellPicker.RemoveLocator(actor.CellLocator);
                        _propPicker.DeletePickList(actor.Geometry);
                    }
                }
            }
            // remove actors
            foreach (string name in actorsToRemove) _actors.Remove(name);

            ClearSelection();
            ClearOverlay();
        }
        public void ClearSelection(bool clearAllMouseSelection = true)
        {
            if (clearAllMouseSelection)
            {
                if (_mouseSelectionAllIds.Count > 0) _mouseSelectionAllIds.Clear();
                _mouseSelectionCurrentIds = null;
            }
            //
            ClearCurrentMouseSelection();
            // Actors
            foreach (var entry in _actors)
            {
                entry.Value.UpdateColor();
            }
            // Actors and edges - leave mouse
            foreach (vtkMaxActor actor in _selectedActors)
            {
                _selectionRenderer.RemoveActor(actor.Geometry);
                _selectionRenderer.RemoveActor(actor.ElementEdges);
                //
                _renderer.RemoveActor(actor.Geometry);
                _renderer.RemoveActor(actor.ElementEdges);
            }
            _selectedActors.Clear();
            //
            this.Invalidate();
        }
        public void ClearCurrentMouseSelection()
        {
            if (_mouseSelectionActorCurrent != null)
            {
                _selectionRenderer.RemoveActor(_mouseSelectionActorCurrent.Geometry);
                _selectionRenderer.RemoveActor(_mouseSelectionActorCurrent.ElementEdges);
                _selectedActors.Remove(_mouseSelectionActorCurrent);
                _mouseSelectionActorCurrent = null;
            }

            _mouseSelectionCurrentIds = null;

            if (_probeWidget != null && _probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
            this.Invalidate();
        }
        public void ClearOverlay()
        {
            _maxSymbolSize = 0;
            foreach (var entry in _overlayActors)
            {
                _overlayRenderer.RemoveActor(entry.Value);
            }
            _overlayActors.Clear();
            this.Invalidate();
        }
        #endregion #################################################################################################################



        public bool ContainsActor(string actorName)
        {
            return _actors.ContainsKey(actorName);
        }
        private string GetActorName(vtkActor actor)
        {
            if (actor == null) return null;
            //
            foreach (var entry in _actors)
            {
                if (entry.Value.Geometry == actor || entry.Value.ElementEdges == actor ||entry.Value.ModelEdges == actor)
                    return entry.Key;
            }
            //
            return null;
        }
        public void UpdateActor(string oldName, string newName, Color newColor)
        {            
            if (!_actors.ContainsKey(oldName)) return;
            //
            vtkMaxActor actor = _actors[oldName];
            actor.Name = newName;
            //
            UpdateActorColor(actor, newColor);
            //
            _actors.Remove(oldName);
            _actors.Add(newName, actor);
            //
            ApplyEdgesVisibilityAndBackfaceCulling();
            //
            if (_renderingOn) this.Invalidate();
        }
        public void UpdateActorColor(vtkMaxActor actor, Color newColor)
        {
            actor.Color = newColor;
            // Transformed copies
            foreach (var copy in actor.Copies) UpdateActorColor(copy, newColor);
            // Section view
            if (_sectionView && actor.SectionViewActor != null) actor.SectionViewActor.Color = newColor;
        }
        public double[] GetBoundingBox()
        {
            // xmin, xmax, ymin, ymax, zmin, zmax
            return _renderer.ComputeVisiblePropBounds();
        }
        public double[] GetBoundingBoxSize()
        {
            // xmin, xmax, ymin, ymax, zmin, zmax
            double[] b = _renderer.ComputeVisiblePropBounds();
            return new double[] { b[1] - b[0], b[3] - b[2], b[5] - b[4] };
        }
        public double GetBoundingBoxDiagonal()
        {
            double[] b = GetBoundingBoxSize();
            return Math.Sqrt(Math.Pow(b[0], 2) + Math.Pow(b[1], 2) + Math.Pow(b[2], 2));
        }




        public void CropPartWithCylinder(string partName, double r, string fileName)
        {
            vtkMaxActor actor = _actors[partName];
            actor.CropWithCylinder(r, fileName);
        }


















        public void AddTetrahedronFaces(double[][] nodes, int[][] tetrahedrons, Color color, int faceId, vtkRendererLayer layer)
        {
            // Create the points
            vtkPoints points = vtkPoints.New();
            points.SetNumberOfPoints(nodes.GetLength(0));

            for (int i = 0; i < nodes.GetLength(0); i++)
            {
                points.SetPoint(i, nodes[i][0], nodes[i][1], nodes[i][2]);
            }

            // Node Ids from face ids
            int[] nodeIds = new int[3];
            switch (faceId)
            {
                case 1:
                    nodeIds = new int[] { 0, 2, 1 };
                    break;
                case 2:
                    nodeIds = new int[] { 0, 1, 3 };
                    break;
                case 3:
                    nodeIds = new int[] { 1, 2, 3 };
                    break;
                case 4:
                    nodeIds = new int[] { 2, 0, 3 };
                    break;
                default:
                    throw new NotImplementedException();
            }

            // Elements to cells
            vtkCellArray cells = vtkCellArray.New();
            for (int i = 0; i < tetrahedrons.GetLength(0); i++)
            {
                cells.InsertNextCell(3);
                cells.InsertCellPoint(tetrahedrons[i][nodeIds[0]]);
                cells.InsertCellPoint(tetrahedrons[i][nodeIds[1]]);
                cells.InsertCellPoint(tetrahedrons[i][nodeIds[2]]);
            }

            // Unstructured grid
            vtkUnstructuredGrid uGrid = vtkUnstructuredGrid.New();
            uGrid.SetPoints(points);
            uGrid.SetCells((int)vtkCellType.VTK_TRIANGLE, cells);
            uGrid.Update();

            // Edges
            vtkExtractEdges extractEdges = vtkExtractEdges.New();
            extractEdges.SetInput(uGrid);
            extractEdges.Update();

            // Visualize mesh
            vtkDataSetMapper mapperGrid = vtkDataSetMapper.New();
            mapperGrid.SetInput(uGrid);
            vtkMaxActor actorGrid = new vtkMaxActor();
            actorGrid.Geometry.SetMapper(mapperGrid);
            actorGrid.Geometry.GetProperty().SetColor(color.R / 255d, color.G / 255d, color.B / 255d);
            actorGrid.Geometry.GetProperty().SetAmbient(0.5);
            actorGrid.Geometry.GetProperty().BackfaceCullingOn();

            AddActorGeometry(actorGrid, layer);

            this.Invalidate();
        }
        
        public void AddTest()
        {
            double[,] p = new double[,] {
            { 0.0, 0.0, 0.0 },
            { 1.0, 0.0, 0.0 },
            { 1.0, 1.0, 0.0 },
            { 0.0, 1.0, 0.0 },
            { 0.0, 0.0, 1.0 },
            { 1.0, 0.0, 1.0 },
            { 1.0, 1.0, 1.0 },
            { 0.0, 1.0, 1.0 },
            { 0.0, 1.0, 2.0 }
            };

            // Create the points
            vtkPoints points = vtkPoints.New();
            points.SetNumberOfPoints(p.GetLength(0));

            for (int i = 0; i < p.GetLength(0); i++)
            {
                points.SetPoint(i, p[i, 0], p[i, 1], p[i, 2]);
            }

            // Elements to cells
            //vtkCellArray cells = vtkCellArray.New();
            //for (int i = 0; i < tetrahedrons.GetLength(0); i++)
            //{
            //    cells.InsertNextCell(4);
            //    cells.InsertCellPoint(tetrahedrons[i][0]);
            //    cells.InsertCellPoint(tetrahedrons[i][1]);
            //    cells.InsertCellPoint(tetrahedrons[i][2]);
            //    cells.InsertCellPoint(tetrahedrons[i][3]);
            //}

            
            

            // Create a hexahedron from the points

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            // Unstructured grid
            vtkUnstructuredGrid uGrid = vtkUnstructuredGrid.New();
            uGrid.SetPoints(points);

            vtkIdList list = vtkIdList.New();
            for (int i = 0; i < 1; i++)
            {
                list.SetNumberOfIds(8);
                for (int j = 0; j < 8; j++)
                {
                    list.SetId(j, j);
                }
                uGrid.InsertNextCell((int)vtkCellType.VTK_HEXAHEDRON, list);

                list.SetNumberOfIds(4);
                list.SetId(0, 4);
                list.SetId(1, 6);
                list.SetId(2, 7);
                list.SetId(3, 8);
                uGrid.InsertNextCell((int)vtkCellType.VTK_TETRA, list);
            }
            watch.Stop();
            //uGrid.InsertNextCell(hex.GetCellType(), hex.GetPointIds());

            uGrid.Update();

            // Visualize mesh
            vtkDataSetMapper mapperGrid = vtkDataSetMapper.New();
            mapperGrid.SetInput(uGrid);
            vtkMaxActor actorGrid = new vtkMaxActor();
            actorGrid.Geometry.SetMapper(mapperGrid);
            actorGrid.Geometry.GetProperty().SetColor(1, 0.5, 0.5);

            // Visualize edges
            // Extract edges
            vtkExtractEdges extractEdges = vtkExtractEdges.New();
            extractEdges.SetInput(uGrid);
            extractEdges.Update();

            vtkPolyDataMapper mapperEdges = vtkPolyDataMapper.New();
            mapperEdges.SetInputConnection(extractEdges.GetOutputPort());
            vtkMaxActor actorEdges = new vtkMaxActor();
            actorEdges.Geometry.SetMapper(mapperEdges);
            actorEdges.Geometry.GetProperty().SetColor(0, 0, 0);
            actorEdges.Geometry.GetProperty().SetLineWidth(1);
            actorEdges.Geometry.PickableOff();

            AddActorGeometry(actorGrid, vtkRendererLayer.Base);

            this.Invalidate();
        }
        private void Hexahedron()
        {
            // Setup the coordinates of eight points 
            // (faces must be in counter clockwise order as viewed from the outside)
            double[,] p = new double[,] {
            { 0.0, 0.0, 2.0 },
            { 1.0, 0.0, 2.0 },
            { 1.0, 1.0, 2.0 },
            { 0.0, 1.0, 2.0 },
            { 0.0, 0.0, 3.0 },
            { 1.0, 0.0, 3.0 },
            { 1.0, 1.0, 3.0 },
            { 0.0, 1.0, 3.0 }
            };

            // Create the points
            vtkPoints points = vtkPoints.New();
            for (int i = 0; i < 8; i++)
                points.InsertNextPoint(p[i, 0], p[i, 1], p[i, 2]);

            // Create a hexahedron from the points
            vtkHexahedron hex = vtkHexahedron.New();
            for (int i = 0; i < 8; i++)
                hex.GetPointIds().SetId(i, i);

            // Add the hexahedron to a cell array
            vtkCellArray hexs = vtkCellArray.New();
            hexs.InsertNextCell(hex);

            // Add the points and hexahedron to an unstructured grid
            vtkUnstructuredGrid uGrid = vtkUnstructuredGrid.New();
            uGrid.SetPoints(points);
            uGrid.InsertNextCell(hex.GetCellType(), hex.GetPointIds());

            // Edges
            vtkExtractEdges extractEdges = vtkExtractEdges.New();
            extractEdges.SetInput(uGrid);
            extractEdges.Update();

            // Visualize
            vtkDataSetMapper mapper = vtkDataSetMapper.New();
            mapper.SetInput(uGrid);
            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            actor.GetProperty().BackfaceCullingOn();

            // Visualize edges
            vtkPolyDataMapper mapperEdges = vtkPolyDataMapper.New();
            mapperEdges.SetInputConnection(extractEdges.GetOutputPort());
            vtkPolyDataMapper.SetResolveCoincidentTopologyToPolygonOffset();
            vtkActor actorEdges = vtkActor.New();
            actorEdges.SetMapper(mapperEdges);
            actorEdges.GetProperty().SetColor(0, 0, 0);
            actorEdges.GetProperty().SetLineWidth(1);

            _renderer.AddActor(actor);
            _renderer.AddActor(actorEdges);

            // fit all in


            //double[] bounds = m_Renderer.ComputeVisiblePropBounds();
            IntPtr test = System.Runtime.InteropServices.Marshal.AllocHGlobal(6 * 8);
            _renderer.ComputeVisiblePropBounds(test);
            _renderer.ResetCamera(test);

            this.Invalidate();
        }
        public void AddSphere()
        {
            // source object
            vtkSphereSource SphereSource = vtkSphereSource.New();
            SphereSource.SetRadius(100);
            //SphereSource.SetEndTheta(180);
            //SphereSource.SetEndPhi(180);

            SphereSource.SetPhiResolution(50);
            SphereSource.SetThetaResolution(50);

            SphereSource.Update();

            // mapper
            vtkPolyDataMapper sphereMapper = vtkPolyDataMapper.New();
            sphereMapper.SetInputConnection(SphereSource.GetOutputPort());

            // actor
            vtkActor sphereActor = vtkActor.New();
            sphereActor.GetProperty().SetColor(1, 0, 0);
            sphereActor.SetMapper(sphereMapper);
            //sphereActor.GetProperty().SetInterpolationToFlat();
            //sphereActor.GetProperty().SetInterpolationToGouraud();
            sphereActor.GetProperty().SetInterpolationToPhong();
            sphereActor.GetProperty().SetAmbient(0.5);
            sphereActor.GetProperty().SetDiffuse(0.5);
            sphereActor.GetProperty().SetSpecular(0.6);
            sphereActor.GetProperty().SetSpecularColor(1, 1, 1);
            sphereActor.GetProperty().SetSpecularPower(100);

            // add actor to the renderer
            _renderer.AddActor(sphereActor);
            //_selectedActors.Add(sphereActor);

            // ensure all actors are visible (in this example not necessarely needed,
            // but in case more than one actor needs to be shown it might be a good idea)
            _renderer.ResetCamera();

            this.Invalidate();
        }
        private void Actor2D()
        {
            // Create the geometry of a point (the coordinate)
            vtkPoints points = vtkPoints.New();
            //points.InsertNextPoint(rotationCenter[0], rotationCenter[1], rotationCenter[2]);
            points.InsertNextPoint(100, 100, 0);
            points.InsertNextPoint(200, 200, 0);
            points.InsertNextPoint(200, 10, 200);

            vtkPolyData pointsPolyData = vtkPolyData.New();
            pointsPolyData.SetPoints(points);

            vtkVertexGlyphFilter glyphFilter = vtkVertexGlyphFilter.New();
            glyphFilter.SetInputConnection(pointsPolyData.GetProducerPort());
            glyphFilter.Update();

            // Visualize
            vtkPolyDataMapper2D mapper = vtkPolyDataMapper2D.New();
            mapper.SetInputConnection(glyphFilter.GetOutputPort());
            mapper.Update();

            vtkActor2D actor = vtkActor2D.New();
            actor.SetMapper(mapper);
            actor.GetProperty().SetColor(1, 0, 0);
            actor.GetProperty().SetPointSize(5);

            _renderer.AddViewProp(actor);
            _renderer.ResetCamera();
        }
        private void AddGlyphDemo()
        {
            vtkPoints points = vtkPoints.New();
            points.InsertNextPoint(77.5, 65, 0);
            points.InsertNextPoint(78.5, 65, 0);
            points.InsertNextPoint(79.5, 65, 0);

            vtkPolyData polydata = vtkPolyData.New();
            polydata.SetPoints(points);

            // Create anything you want here, we will use a polygon for the demo.
            vtkRegularPolygonSource polygonSource = vtkRegularPolygonSource.New(); //default is 6 sides

            vtkGlyph2D glyph2D = vtkGlyph2D.New();
            glyph2D.SetSourceConnection(polygonSource.GetOutputPort());
            glyph2D.SetInput(polydata);
            glyph2D.Update();

            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyph2D.GetOutputPort());
            mapper.Update();

            // Create a subclass of vtkActor: a vtkFollower that remains facing the camera
            vtkFollower follower = vtkFollower.New();
            follower.SetMapper(mapper);
            follower.GetProperty().SetColor(1, 0, 0); // red 
            
            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);

            _renderer.AddActor(actor);
            _renderer.AddActor(follower);

            follower.SetCamera(_renderer.GetActiveCamera());

            this.Invalidate();
        }
        private void AddCaptionWidget()
        {
            // Create the widget and its representation
            vtkCaptionRepresentation captionRepresentation = vtkCaptionRepresentation.New();
            captionRepresentation.GetCaptionActor2D().SetCaption("Test caption");
            captionRepresentation.GetCaptionActor2D().GetTextActor().GetTextProperty().SetFontSize(16);

            double[] pos = new double[] { 66, 66, 20 };
            IntPtr posPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(4 * 8);
            Marshal.Copy(pos, 0, posPtr, 2);
            captionRepresentation.SetAnchorPosition(posPtr);
            Marshal.FreeHGlobal(posPtr);

            vtkPointHandleRepresentation3D anchorRepresentation = captionRepresentation.GetAnchorRepresentation();
            vtkPropCollection coll = null;
            captionRepresentation.GetVolumes(coll);

            vtkCaptionWidget captionWidget = vtkCaptionWidget.New();
            captionWidget.SetInteractor(_renderWindowInteractor);
            captionWidget.SetRepresentation(captionRepresentation);
            captionWidget.SetCurrentRenderer(_selectionRenderer);
            captionWidget.SetDefaultRenderer(_selectionRenderer);

            // Add the actors to the scene

            captionWidget.On();
        }
        private void AddDiskAnimation()
        {
            //vtkCamera camera = vtkCamera.New();
            //camera.Elevation(-45);
            //_renderer.SetActiveCamera(camera);

            //# Creating disk mesh
            vtkDiskSource disk = vtkDiskSource.New();
            disk.SetInnerRadius(0.1);
            disk.SetOuterRadius(2.0);
            disk.SetRadialResolution(800);
            disk.SetCircumferentialResolution(800);
            disk.Update();
            vtkPolyData polyData = disk.GetOutput();

            Console.WriteLine(polyData.GetNumberOfPoints() + " nodes");
            Console.WriteLine(polyData.GetNumberOfCells() + " elements");

            //# Setup actor and mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInput(polyData);
            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);

            _renderer.AddActor(actor);

            int n = 36;
            double[] nodePos;
            polyData = disk.GetOutput();
            vtkPoints[] vtkPointsList = new vtkPoints[n];

            Console.WriteLine("Calculate...");
            for (int i = 0; i < n; i++)
            {
                vtkPointsList[i] = vtkPoints.New();
                vtkPointsList[i].DeepCopy(polyData.GetPoints());
                for (int j = 0; j < polyData.GetNumberOfPoints(); j++)
                {
                    nodePos = vtkPointsList[i].GetPoint(j);
                    vtkPointsList[i].SetPoint(j, nodePos[0], nodePos[1], 0.3 * Math.Sin(nodePos[0] * 2 + 360 / (n - 1) * i * Math.PI / 180));
                }
                polyData.SetPoints(vtkPointsList[i]);
                _renderWindow.Render();
                Application.DoEvents();
            }

            Console.WriteLine("Done. Animate.");
            System.Threading.Thread.Sleep(2000);

            //# First animation without color
            DateTime start_time = DateTime.Now;

            //for (int j = 0; j < 10; j++)
            //{
            //    for (int i = 0; i < n; i++)
            //    {
            //        polyData.SetPoints(vtkPointsList[i]);
            //        _renderWindow.Render();
            //        //Application.DoEvents();
            //        //this.Invalidate();
            //    }
            //}

            double fps = 10 * n / (DateTime.Now - start_time).TotalSeconds;
            Console.WriteLine(fps + " FPS");
            //MessageBox.Show(fps + " FPS");

            Console.WriteLine("Done. Animate actor array.");

            _renderer.RemoveActor(actor);
            vtkActor[] actors = new vtkActor[n];
            for (int i = 0; i < n; i++)
            {
                disk = vtkDiskSource.New();
                disk.SetInnerRadius(0.1);
                disk.SetOuterRadius(2.0);
                disk.SetRadialResolution(800);
                disk.SetCircumferentialResolution(800);
                disk.Update();
                polyData = disk.GetOutput();
                polyData.SetPoints(vtkPointsList[i]);
                mapper = vtkPolyDataMapper.New();
                mapper.SetInput(polyData);
                actors[i] = vtkActor.New();
                actors[i].SetMapper(mapper);
                _renderer.AddActor(actors[i]);
                _renderWindow.Render();
                Application.DoEvents();
                actors[i].VisibilityOff();
            }

            start_time = DateTime.Now;
            for (int j = 0; j < 10; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    //for (int k = 0; k < n; k++)
                    //{
                    //   actors[k].VisibilityOff();
                    //}
                    actors[(i + n - 1) % n].VisibilityOff();
                    actors[i].VisibilityOn();
                    _renderWindow.Render();
                }
            }

            fps = 10 * n / (DateTime.Now - start_time).TotalSeconds;
            Console.WriteLine(fps + " FPS");
            MessageBox.Show(fps + " FPS");


        }
    }
}

