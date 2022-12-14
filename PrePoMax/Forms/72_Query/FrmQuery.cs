using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeGlobals;
using CaeMesh;

namespace PrePoMax.Forms
{
    public partial class FrmQuery : UserControls.PrePoMaxChildForm
    {
        // Variables                                                                                                                
        private int _numNodesToSelect;
        private double[][] _coorNodesToDraw;
        private double[][] _coorLinesToDraw;
        private Controller _controller;


        // Callbacks                                                                                                               
        public Action<string> Form_WriteDataToOutput;

        
        // Constructors                                                                                                             
        public FrmQuery()
        {
            InitializeComponent();
            _numNodesToSelect = -1;
        }


        // Event hadlers                                                                                                            
        private void lvQueries_MouseDown(object sender, MouseEventArgs e)
        {
            lvQueries.SelectedItems.Clear();
        }
        private void lvQueries_MouseUp(object sender, MouseEventArgs e)
        {
        }
        private void lvQueries_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvQueries.SelectedItems.Count > 0)
            {
                switch (lvQueries.SelectedItems[0].Text)
                {
                    case ("Point/Node"):
                        _controller.SelectBy = vtkSelectBy.QueryNode;
                        _controller.Selection.SelectItem = vtkSelectItem.Node;
                        _numNodesToSelect = 1;
                        break;
                    case ("Element"):
                        _controller.SelectBy = vtkSelectBy.QueryElement;
                        _controller.Selection.SelectItem = vtkSelectItem.Element;
                        _numNodesToSelect = -1;
                        break;
                    case ("Edge"):
                        _controller.SelectBy = vtkSelectBy.QueryEdge;
                        _controller.Selection.SelectItem = vtkSelectItem.Edge;
                        _numNodesToSelect = -1;
                        break;
                    case ("Surface"):
                        _controller.SelectBy = vtkSelectBy.QuerySurface;
                        _controller.Selection.SelectItem = vtkSelectItem.Surface;
                        _numNodesToSelect = -1;
                        break;
                    case ("Part"):
                        _controller.SelectBy = vtkSelectBy.QueryPart;
                        _controller.Selection.SelectItem = vtkSelectItem.Part;
                        _numNodesToSelect = -1;
                        break;
                    case ("Assembly"):
                        _controller.SelectBy = vtkSelectBy.Off;
                        _controller.Selection.SelectItem = vtkSelectItem.None;
                        OutputAssemblyData();
                        _numNodesToSelect = -1;
                        break;
                    case ("Bounding box size"):
                        _controller.SelectBy = vtkSelectBy.Off;
                        _controller.Selection.SelectItem = vtkSelectItem.None;
                        OutputBoundingBox();
                        _numNodesToSelect = -1;
                        break;
                    case ("Distance"):
                        _controller.SelectBy = vtkSelectBy.QueryNode;
                        _controller.Selection.SelectItem = vtkSelectItem.Node;
                        _numNodesToSelect = 2;
                        break;
                    case ("Angle"):
                        _controller.SelectBy = vtkSelectBy.QueryNode;
                        _controller.Selection.SelectItem = vtkSelectItem.Node;
                        _numNodesToSelect = 3;
                        break;
                    case ("Circle"):
                        _controller.SelectBy = vtkSelectBy.QueryNode;
                        _controller.Selection.SelectItem = vtkSelectItem.Node;
                        _numNodesToSelect = 3;
                        break;
                    default:
                        break;
                }
                _controller.ClearSelectionHistoryAndSelectionChanged();
            }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            Hide();
        }
        private void FrmQuery_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }
        private void FrmQuery_VisibleChanged(object sender, EventArgs e)
        {
            // This is called if some other form is shown to close all other forms
            // This is called after the form visibility changes
            // The form was hidden 
            if (!this.Visible)
            {
                _controller.SelectBy = vtkSelectBy.Off;
                _controller.ClearSelectionHistoryAndSelectionChanged();
            }                
        }


        // Methods                                                                                                                  
        public void PrepareForm(Controller controller)
        {
            // To prevent the call to frmMain.itemForm_VisibleChanged when minimized
            this.DialogResult = DialogResult.None;      
            //
            _controller = controller;
            lvQueries.HideSelection = false;
            lvQueries.SelectedIndices.Clear();
        }
        //
        public void PickedIds(int[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0) return;
                //
                if (_controller.SelectBy == vtkSelectBy.QueryElement && ids.Length == 1) OneElementPicked(ids[0]);
                else if (_controller.SelectBy == vtkSelectBy.QueryEdge && ids.Length == 1) OneEdgePicked(ids[0]);
                else if (_controller.SelectBy == vtkSelectBy.QuerySurface)
                {
                    SelectionNodeMouse selectionNodeMouse = _controller.Selection.Nodes[0] as SelectionNodeMouse;
                    if (selectionNodeMouse != null)
                    {
                        ids = _controller.GetGeometryIdsAtPoint(selectionNodeMouse);
                        OneSurfacePicked(ids[0]);
                    }
                }
                else if (_controller.SelectBy == vtkSelectBy.QueryPart && ids.Length == 1) OnePartPicked(ids[0]);
                else if (ids.Length == _numNodesToSelect)
                {
                    // One node
                    if (ids.Length == 1) OneNodePicked(ids[0]);
                    // Two nodes
                    else if (ids.Length == 2) TwoNodesPicked(ids[0], ids[1]);
                    // Three nodes
                    else if (ids.Length == 3) ThreeNodesPicked(ids[0], ids[1], ids[2]);
                    //
                    _controller.ClearSelectionHistoryAndSelectionChanged();
                    HighlightNodes();
                }
            }
            catch
            { }
        }
        //
        public void OneNodePicked(int nodeId)
        {
            if (Form_WriteDataToOutput != null)
            {
                string data;
                string lenUnit = GetLengthUnit();
                _coorNodesToDraw = new double[_numNodesToSelect][];
                //
                Vec3D baseV = new Vec3D(_controller.GetNode(nodeId).Coor);
                //
                Form_WriteDataToOutput("");
                data = string.Format("{0,16}{1,8}{2,16}{3,16}", "Node".PadRight(16), "[/]", "id:", nodeId);
                Form_WriteDataToOutput(data);
                data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}",
                                     "Base".PadRight(16), lenUnit, "x, y, z:", baseV.X, baseV.Y, baseV.Z);
                Form_WriteDataToOutput(data);
                //
                if (_controller.CurrentView == ViewGeometryModelResults.Results)
                {
                    float fieldValue = _controller.GetNodalValue(nodeId);
                    string fieldUnit = "[" + _controller.GetCurrentResultsUnitAbbreviation() + "]";
                    //
                    Vec3D trueScaledV = new Vec3D(_controller.GetScaledNode(1, nodeId).Coor);
                    Vec3D disp = trueScaledV - baseV;
                    //
                    data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}",
                                         "Deformed".PadRight(16), lenUnit, "x, y, z:", trueScaledV.X, trueScaledV.Y, trueScaledV.Z);
                    Form_WriteDataToOutput(data);
                    data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}",
                                         "Displacement".PadRight(16), lenUnit, "x, y, z:", disp.X, disp.Y, disp.Z);
                    Form_WriteDataToOutput(data);
                    data = string.Format("{0,16}{1,8}{2,16}{3,16:E}", "Field value".PadRight(16), fieldUnit, ":", fieldValue);
                    Form_WriteDataToOutput(data);
                    //
                    float scale = _controller.GetScale();
                    baseV = new Vec3D(_controller.GetScaledNode(scale, nodeId).Coor); // for the _coorNodesToDraw
                }
                //
                Form_WriteDataToOutput("");
                //
                _coorNodesToDraw[0] = baseV.Coor;
                _coorLinesToDraw = null;
            }
        }
        public void OneElementPicked(int id)
        {
            Form_WriteDataToOutput("");
            string data = string.Format("{0,16}{1,8}", "Element id:".PadRight(16), id);
            Form_WriteDataToOutput(data);
            Form_WriteDataToOutput("");
            //
            _controller.ClearSelectionHistoryAndSelectionChanged();
            //
            _controller.HighlightElement(id);
        }
        public void OneEdgePicked(int geometryId)
        {
            int[] itemTypePart = CaeMesh.FeMesh.GetItemTypePartIdsFromGeometryId(geometryId);
            CaeMesh.BasePart part = _controller.DisplayedMesh.GetPartById(itemTypePart[2]);
            double length1 = _controller.DisplayedMesh.GetEdgeLength(geometryId);
            string lenUnit = GetLengthUnit();
            //
            Form_WriteDataToOutput("");
            string data = string.Format("Edge on part: {0}", part.Name);            
            Form_WriteDataToOutput(data);
            data = string.Format("{0,16}{1,8}{2,16}{3,16}", "Edge".PadRight(16), "[/]", "id:", itemTypePart[0]);
            Form_WriteDataToOutput(data);
            data = string.Format("{0,16}{1,8}{2,16}{3,16:E}", "Base".PadRight(16), lenUnit, "L:", length1);
            Form_WriteDataToOutput(data);
            //
            if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                int[] nodeIds;
                _controller.DisplayedMesh.GetEdgeNodeCoor(geometryId, out nodeIds, out double[][] nodeCoor);
                FeNode[] nodes = _controller.GetScaledNodes(1, nodeIds);
                double length2 = 0;
                Vec3D n1;
                Vec3D n2;
                for (int i = 0; i < nodes.Length - 1; i++)
                {
                    n1 = new Vec3D(nodes[i].Coor);
                    n2 = new Vec3D(nodes[i + 1].Coor);
                    length2 += (n2 - n1).Len;
                }
                data = string.Format("{0,16}{1,8}{2,16}{3,16:E}", "Deformed".PadRight(16), lenUnit, "L:", length2);
                Form_WriteDataToOutput(data);
                data = string.Format("{0,16}{1,8}{2,16}{3,16:E}", "Delta".PadRight(16), lenUnit, "L:", length2 - length1);
                Form_WriteDataToOutput(data);
            }
            Form_WriteDataToOutput("");
            //
            _controller.ClearSelectionHistoryAndSelectionChanged();
            //
            _controller.HighlightItemsByGeometryEdgeIds(new int[] { geometryId }, false);
        }
        public void OneSurfacePicked(int geometryId)
        {
            int[] itemTypePart = CaeMesh.FeMesh.GetItemTypePartIdsFromGeometryId(geometryId);
            CaeMesh.BasePart part = _controller.DisplayedMesh.GetPartById(itemTypePart[2]);
            int faceId = itemTypePart[0];
            double area1 = _controller.DisplayedMesh.GetSurfaceArea(geometryId);
            string areaUnit = GetAreaUnit();
            //
            Form_WriteDataToOutput("");
            string data = string.Format("Surface on part: {0}", part.Name);
            if (part.Visualization.FaceTypes != null)
            {
                data += string.Format("   Surface type: {0}", part.Visualization.FaceTypes[faceId]);
            }
            Form_WriteDataToOutput(data);
            data = string.Format("{0,16}{1,8}{2,16}{3,16}", "Surface".PadRight(16), "[/]", "id:", faceId);
            Form_WriteDataToOutput(data);
            data = string.Format("{0,16}{1,8}{2,16}{3,16:E}", "Base".PadRight(16), areaUnit, "A:", area1);
            Form_WriteDataToOutput(data);
            //
            if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                int[] nodeIds;
                _controller.DisplayedMesh.GetFaceNodes(geometryId, out nodeIds);
                FeNode[] nodes = _controller.GetScaledNodes(1, nodeIds);
                Dictionary<int, FeNode> nodesDic = new Dictionary<int, FeNode>();
                for (int i = 0; i < nodes.Length; i++) nodesDic.Add(nodes[i].Id, nodes[i]);
                double area2 = _controller.DisplayedMesh.ComputeFaceArea(part.Visualization, faceId, nodesDic);
                //
                data = string.Format("{0,16}{1,8}{2,16}{3,16:E}", "Deformed".PadRight(16), areaUnit, "A:", area2);
                Form_WriteDataToOutput(data);
                data = string.Format("{0,16}{1,8}{2,16}{3,16:E}", "Delta".PadRight(16), areaUnit, "A:", area2 - area1);
                Form_WriteDataToOutput(data);
            }
            Form_WriteDataToOutput("");
            //
            _controller.ClearSelectionHistoryAndSelectionChanged();    // in order to prevent SHIFT ADD
            //
            _controller.HighlightItemsBySurfaceIds(new int[] { geometryId }, false);
        }
        public void OnePartPicked(int id)
        {
            CaeMesh.FeMesh mesh = _controller.DisplayedMesh;
            //
            CaeMesh.BasePart part = null;
            foreach (var entry in mesh.Parts)
            {
                if (entry.Value.PartId == id) part = entry.Value;
            }
            //
            if (part == null) throw new NotSupportedException();
            //
            Form_WriteDataToOutput("");
            string data = string.Format("Part name: {0}", part.Name);
            Form_WriteDataToOutput(data);
            data = string.Format("Part type: {0}", part.PartType);
            Form_WriteDataToOutput(data);
            //data = string.Format("Part id: {0}{1}", part.PartId, Environment.NewLine);
            //WriteLineToOutputWithDate(data);
            data = string.Format("Number of elements: {0}", part.Labels.Length);
            Form_WriteDataToOutput(data);
            data = string.Format("Number of nodes: {0}", part.NodeLabels.Length);
            Form_WriteDataToOutput(data);
            Form_WriteDataToOutput("");
            //
            _controller.ClearSelectionHistoryAndSelectionChanged();
            //
            _controller.Highlight3DObjects(new object[] { part });
        }
        private void OutputAssemblyData()
        {
            CaeMesh.FeMesh mesh = _controller.DisplayedMesh;
            if (mesh == null) return;
            double[] bb = _controller.GetBoundingBox();
            double[] size = new double[] { bb[1] - bb[0], bb[3] - bb[2], bb[5] - bb[4] };
            //
            Form_WriteDataToOutput("");
            string data = string.Format("Assembly");
            Form_WriteDataToOutput(data);
            data = string.Format("Number of parts: {0}", mesh.Parts.Count);
            Form_WriteDataToOutput(data);
            data = string.Format("Number of elements: {0}", mesh.Elements.Count);
            Form_WriteDataToOutput(data);
            data = string.Format("Number of nodes: {0}", mesh.Nodes.Count);
            Form_WriteDataToOutput(data);
            Form_WriteDataToOutput("");
        }
        private void OutputBoundingBox()
        {
            double[] bb = _controller.GetBoundingBox();
            double[] size = new double[] { bb[1] - bb[0], bb[3] - bb[2], bb[5] - bb[4] };
            string lenUnit = GetLengthUnit();
            //
            Form_WriteDataToOutput("");
            string data = string.Format("Bounding box");
            Form_WriteDataToOutput(data);
            if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                double scale = _controller.GetScale();
                data = string.Format("{0,17}{1,7}{2,16}{3,16:E}", "Def. scale factor".PadRight(17), "[/]", "sf:", scale);
                Form_WriteDataToOutput(data);
            }
            data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}", "Min".PadRight(16), lenUnit, "x, y, z:", bb[0], bb[2], bb[4]);
            Form_WriteDataToOutput(data);
            data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}", "Max".PadRight(16), lenUnit, "x, y, z:", bb[1], bb[3], bb[5]);
            Form_WriteDataToOutput(data);
            data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}", "Size".PadRight(16), lenUnit, "x, y, z:", size[0], size[1], size[2]);
            Form_WriteDataToOutput(data);
        }
        public void TwoNodesPicked(int nodeId1, int nodeId2)
        {
            if (Form_WriteDataToOutput != null)
            {
                string data;
                _coorNodesToDraw = new double[_numNodesToSelect][];
                //
                Vec3D baseV1 = new Vec3D(_controller.GetNode(nodeId1).Coor);
                Vec3D baseV2 = new Vec3D(_controller.GetNode(nodeId2).Coor);
                Vec3D baseD = baseV2 - baseV1;
                string lenUnit = GetLengthUnit();
                //
                Form_WriteDataToOutput("");
                data = string.Format("{0,16}{1,8}{2,16}{3,16}, {4,16}", "Distance".PadRight(16), "[/]", "id1, id2:", nodeId1, nodeId2);
                Form_WriteDataToOutput(data);
                data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}, {6,16:E}",
                                     "Base".PadRight(16), lenUnit, "dx, dy, dz, D:", baseD.X, baseD.Y, baseD.Z, baseD.Len);
                Form_WriteDataToOutput(data);
                //
                if (_controller.CurrentView == ViewGeometryModelResults.Results)
                {
                    Vec3D trueScaledV1 = new Vec3D(_controller.GetScaledNode(1, nodeId1).Coor);
                    Vec3D trueScaledV2 = new Vec3D(_controller.GetScaledNode(1, nodeId2).Coor);
                    Vec3D trueScaledD = trueScaledV2 - trueScaledV1;
                    Vec3D delta = trueScaledD - baseD;
                    //
                    data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}, {6,16:E}",
                                         "Deformed".PadRight(16), lenUnit, "dx, dy, dz, D:", trueScaledD.X, trueScaledD.Y, trueScaledD.Z, trueScaledD.Len);
                    Form_WriteDataToOutput(data);
                    data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}, {6,16:E}",
                                         "Delta".PadRight(16), lenUnit, "dx, dy, dz, D:", delta.X, delta.Y, delta.Z, trueScaledD.Len - baseD.Len);
                    Form_WriteDataToOutput(data);
                    //
                    float scale = _controller.GetScale();
                    baseV1 = new Vec3D(_controller.GetScaledNode(scale, nodeId1).Coor);    // for the _coorNodesToDraw
                    baseV2 = new Vec3D(_controller.GetScaledNode(scale, nodeId2).Coor);    // for the _coorNodesToDraw
                }
                Form_WriteDataToOutput("");
                //
                _coorNodesToDraw[0] = baseV1.Coor;
                _coorNodesToDraw[1] = baseV2.Coor;
                _coorLinesToDraw = _coorNodesToDraw;
            }
        }
        public void ThreeNodesPicked(int nodeId1, int nodeId2, int nodeId3)
        {
            if (Form_WriteDataToOutput != null)
            {
                if (lvQueries.SelectedItems[0].Text == "Angle") ComputeAngle(nodeId1, nodeId2, nodeId3);
                else if (lvQueries.SelectedItems[0].Text == "Circle") ComputeCircle(nodeId1, nodeId2, nodeId3);
            }
        }
        //
        private void ComputeAngle(int nodeId1, int nodeId2, int nodeId3)
        {
            string data;
            double angle;
            Vec3D p;
            Vec3D axis;
            Vec3D baseV1 = new Vec3D(_controller.GetNode(nodeId1).Coor);
            Vec3D baseV2 = new Vec3D(_controller.GetNode(nodeId2).Coor);
            Vec3D baseV3 = new Vec3D(_controller.GetNode(nodeId3).Coor);
            //
            angle = ComputeAngle(baseV1, baseV2, baseV3, out p, out axis);
            string angleUnit = "[°]";
            //
            data = string.Format("{0,16}{1,8}{2,16}{3,16}, {4,16}, {5,16}",
                                 "Angle".PadRight(16), "[/]", "id1, id2, id3:", nodeId1, nodeId2, nodeId3);
            Form_WriteDataToOutput(data);
            data = string.Format("{0,16}{1,8}{2,16}{3,16:E}", "Base".PadRight(16), angleUnit, "ϕ:", angle);
            Form_WriteDataToOutput(data);
            //
            if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                baseV1 = new Vec3D(_controller.GetScaledNode(1, nodeId1).Coor);
                baseV2 = new Vec3D(_controller.GetScaledNode(1, nodeId2).Coor);
                baseV3 = new Vec3D(_controller.GetScaledNode(1, nodeId3).Coor);
                //
                double angle2 = ComputeAngle(baseV1, baseV2, baseV3, out p, out axis);
                double delta = angle2 - angle;
                //
                data = string.Format("{0,16}{1,8}{2,16}{3,16:E}", "Deformed".PadRight(16), angleUnit, "ϕ:", angle2);
                Form_WriteDataToOutput(data);
                data = string.Format("{0,16}{1,8}{2,16}{3,16:E}", "Delta".PadRight(16), angleUnit, "ϕ:", delta);
                Form_WriteDataToOutput(data);
                //
                float scale = _controller.GetScale();
                baseV1 = new Vec3D(_controller.GetScaledNode(scale, nodeId1).Coor);    // for the _coorNodesToDraw
                baseV2 = new Vec3D(_controller.GetScaledNode(scale, nodeId2).Coor);    // for the _coorNodesToDraw
                baseV3 = new Vec3D(_controller.GetScaledNode(scale, nodeId3).Coor);    // for the _coorNodesToDraw
                //
                angle = ComputeAngle(baseV1, baseV2, baseV3, out p, out axis);
            }
            Form_WriteDataToOutput("");
            //
            _coorNodesToDraw = new double[_numNodesToSelect][];
            _coorNodesToDraw[0] = baseV1.Coor;
            _coorNodesToDraw[1] = baseV2.Coor;
            _coorNodesToDraw[2] = baseV3.Coor;
            //
            List<double[]> coorLines = new List<double[]>() { baseV1.Coor, baseV2.Coor, baseV3.Coor, baseV2.Coor };
            coorLines.AddRange(ComputeCirclePoints(baseV2, axis, p, angle * Math.PI / 180));
            _coorLinesToDraw = coorLines.ToArray();
           
        }
        private void ComputeCircle(int nodeId1, int nodeId2, int nodeId3)
        {
            //https://en.wikipedia.org/wiki/Circumscribed_circle
            string data;
            double r;
            Vec3D center;
            Vec3D axis;
            Vec3D baseV1 = new Vec3D(_controller.GetNode(nodeId1).Coor);
            Vec3D baseV2 = new Vec3D(_controller.GetNode(nodeId2).Coor);
            Vec3D baseV3 = new Vec3D(_controller.GetNode(nodeId3).Coor);
            //
            ComputeCircle(baseV1, baseV2, baseV3, out r, out center, out axis);
            string lenUnit = GetLengthUnit();
            //
            Form_WriteDataToOutput("");
            data = string.Format("{0,16}{1,8}{2,16}{3,16}, {4,16}, {5,16}",
                                 "Circle".PadRight(16), "[/]", "id1, id2, id3:", nodeId1, nodeId2, nodeId3);
            Form_WriteDataToOutput(data);
            data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}, {6,16:E}",
                                 "Base".PadRight(16), lenUnit, "x, y, z, R:", center.X, center.Y, center.Z, r);
            Form_WriteDataToOutput(data);
            data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}",
                                 "Base axis".PadRight(16), lenUnit, "x, y, z:", axis.X, axis.Y, axis.Z);
            Form_WriteDataToOutput(data);
            //
            if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                baseV1 = new Vec3D(_controller.GetScaledNode(1, nodeId1).Coor);
                baseV2 = new Vec3D(_controller.GetScaledNode(1, nodeId2).Coor);
                baseV3 = new Vec3D(_controller.GetScaledNode(1, nodeId3).Coor);
                //
                ComputeCircle(baseV1, baseV2, baseV3, out r, out center, out axis);
                //
                data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}, {6,16:E}",
                                     "Deformed".PadRight(16), lenUnit, "x, y, z, R:", center.X, center.Y, center.Z, r);
                Form_WriteDataToOutput(data);
                data = string.Format("{0,16}{1,8}{2,16}{3,16:E}, {4,16:E}, {5,16:E}",
                                     "Deformed axis".PadRight(16), lenUnit, "x, y, z:", axis.X, axis.Y, axis.Z);
                Form_WriteDataToOutput(data);
                //
                float scale = _controller.GetScale();
                baseV1 = new Vec3D(_controller.GetScaledNode(scale, nodeId1).Coor);    // for the _coorNodesToDraw
                baseV2 = new Vec3D(_controller.GetScaledNode(scale, nodeId2).Coor);    // for the _coorNodesToDraw
                baseV3 = new Vec3D(_controller.GetScaledNode(scale, nodeId3).Coor);    // for the _coorNodesToDraw
                //
                ComputeCircle(baseV1, baseV2, baseV3, out r, out center, out axis);
            }
            Form_WriteDataToOutput("");
            //
            _coorNodesToDraw = new double[_numNodesToSelect + 1][];
            _coorNodesToDraw[0] = baseV1.Coor;
            _coorNodesToDraw[1] = baseV2.Coor;
            _coorNodesToDraw[2] = baseV3.Coor;
            _coorNodesToDraw[3] = center.Coor;
            //
            _coorLinesToDraw = ComputeCirclePoints(center, axis, baseV1, 2 * Math.PI);
        }
        private double ComputeAngle(Vec3D baseV1, Vec3D baseV2, Vec3D baseV3, out Vec3D p, out Vec3D axis)
        {
            Vec3D line1 = baseV1 - baseV2;
            Vec3D line2 = baseV3 - baseV2;
            //
            if (line1.Len2 > line2.Len2)  // find shorter line 
            {
                p = baseV2 + line2 * 0.5;
                axis = Vec3D.CrossProduct(line2, line1);
            }
            else
            {
                p = baseV2 + line1 * 0.5;
                axis = Vec3D.CrossProduct(line1, line2);
            }
            axis.Normalize();
            line1.Normalize();
            line2.Normalize();
            //
            double angle = Math.Acos(Vec3D.DotProduct(line1, line2)) * 180 / Math.PI;
            return angle;
        }
        private void ComputeCircle(Vec3D baseV1, Vec3D baseV2, Vec3D baseV3, out double r, out Vec3D center, out Vec3D axis)
        {
            Vec3D n12 = baseV1 - baseV2;
            Vec3D n21 = baseV2 - baseV1;
            Vec3D n23 = baseV2 - baseV3;
            Vec3D n32 = baseV3 - baseV2;
            Vec3D n13 = baseV1 - baseV3;
            Vec3D n31 = baseV3 - baseV1;
            Vec3D n12xn23 = Vec3D.CrossProduct(n12, n23);
            //
            r = (n12.Len * n23.Len * n31.Len) / (2 * n12xn23.Len);
            //
            double denominator = 2 * n12xn23.Len2;
            double alpha = n23.Len2 * Vec3D.DotProduct(n12, n13) / denominator;
            double beta = n13.Len2 * Vec3D.DotProduct(n21, n23) / denominator;
            double gama = n12.Len2 * Vec3D.DotProduct(n31, n32) / denominator;
            //
            center = alpha * baseV1 + beta * baseV2 + gama * baseV3;
            //
            axis = Vec3D.CrossProduct(n21, n32);
            axis.Normalize();
        }
        private double[][] ComputeCirclePoints(Vec3D c, Vec3D axis, Vec3D p, double angle)
        {
            // The circe is constructed by moving the r vector around the axis
            // d vector is the change in normal n and perpendicular r direction
            int segments = 40;
            double [][] coorLines = new double[segments + 1][];
            double dAngle = angle / segments;
            Vec3D r = p - c;
            double rLen = r.Len;
            Vec3D n;
            Vec3D d;

            coorLines[0] = p.Coor;
            for (int i = 0; i < segments; i++)
            {
                n = Vec3D.CrossProduct(axis, r);
                n.Normalize();

                d = rLen * Math.Sin(dAngle) * n - (1 - Math.Cos(dAngle)) * r;
                p = p + d;
                r = r + d;

                coorLines[i + 1] = p.Coor;
            }
            return coorLines;
        }
        //
        private void HighlightNodes()
        {
            Color color = Color.Red;
            vtkControl.vtkRendererLayer layer = vtkControl.vtkRendererLayer.Selection;
            //
            if (_coorNodesToDraw != null)
            {
                if (_coorNodesToDraw.GetLength(0) == 1)
                {
                    _controller.DrawNodes("Querry", _coorNodesToDraw, color, layer, 7);
                }
                else if (_coorNodesToDraw.GetLength(0) >= 2)
                {
                    _controller.DrawNodes("Querry", _coorNodesToDraw, color, layer, 7);
                    _controller.HighlightConnectedLines(_coorLinesToDraw);
                }
            }
        }
        //
        private string GetLengthUnit()
        {
            string unit = "";
            //
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry ||
                _controller.CurrentView == ViewGeometryModelResults.Model)
                unit = _controller.Model.UnitSystem.LengthUnitAbbreviation;
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                unit = _controller.Results.UnitSystem.LengthUnitAbbreviation;
            else throw new NotSupportedException();
            //
            return "[" + unit + "]";
        }
        private string GetAreaUnit()
        {
            string unit = "";
            //
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry ||
                _controller.CurrentView == ViewGeometryModelResults.Model)
                unit = _controller.Model.UnitSystem.AreaUnitAbbreviation;
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                unit = _controller.Results.UnitSystem.AreaUnitAbbreviation;
            else throw new NotSupportedException();
            //
            return "[" + unit + "]";
        }

        
    }
}




















