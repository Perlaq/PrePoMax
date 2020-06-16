﻿using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserControls;

namespace PrePoMax
{
    [Serializable]
    public class AdvisorCreator
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public AdvisorCreator()
        {
           
        }


        // Methods                                                                                                                  
        public static AdvisorControl CreateControl(FrmMain frmMain)
        {
            AdvisorControl advisorControl = new AdvisorControl();
            //advisorControl.AddPage(Introduction(frmMain));
            //advisorControl.AddPage(ImportGeometry(frmMain));
            //advisorControl.AddPage(PrepareFeMesh(frmMain));
            //advisorControl.AddPage(AssignMaterialProperties(frmMain));
            //advisorControl.AddPage(SelectTheAnalysisType(frmMain));
            //advisorControl.AddPage(CreateTheBoundaryConditions(frmMain));
            advisorControl.AddPage(DefineTheLoads(frmMain));
            //
            advisorControl.CloseButtonPressed += frmMain.tsmiAdvisor_Click;
            //
            return advisorControl;
        }

        private static AdvisorPage Introduction(FrmMain frmMain)
        {
            AdvisorPage advisorPage = new AdvisorPage();
            advisorPage.Title = "Introduction";
            advisorPage.AssociatedView = UserControls.ViewGeometryModelResults.Geometry;
            //
            AdvisorItemLabel advisorLabel1 = new AdvisorItemLabel();
            advisorLabel1.Text = "This advisor helps you to build a simple finite element (FE) model. It limits the FE model to a single component and a linear elastic material model.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "No constraints or contacts are currently supported by the advisor.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Three different types of FE analysis can be performed using the advisor: static linear analysis, frequency analysis and buckling analysis.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "To create a new FE model select the option 1 and choose the desired system of units.";
            advisorPage.Items.Add(advisorLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel1 = new AdvisorItemLinkLabel();
            advisorLinkLabel1.Text = "1 Create new FE model";
            advisorLinkLabel1.AddAction(frmMain.tsmiNew_Click);
            advisorLinkLabel1.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel1);
            //
            AdvisorItemLabel advisorLabel2 = new AdvisorItemLabel();
            advisorLabel2.Text = "After the unit system is created, select Next to continue.";
            advisorPage.Items.Add(advisorLabel2);
            //
            return advisorPage;
        }
        private static AdvisorPage ImportGeometry(FrmMain frmMain)
        {
            AdvisorPage advisorPage = new AdvisorPage();
            advisorPage.Title = "Import Geometry";
            advisorPage.AssociatedView = UserControls.ViewGeometryModelResults.Geometry;
            //
            AdvisorItemLabel advisorLabel1 = new AdvisorItemLabel();
            advisorLabel1.Text = "The geometry can be imported into PrePoMax from other CAD programs using .stp/.step or .stl file formats.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "To import the geometry, select the option 1 and browse for the geometry file to import. Wait until the geometry is imported and then check the model tree on the left that only one part was imported. To rename the part select the option 2 and change the Name field.";
            advisorPage.Items.Add(advisorLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel1 = new AdvisorItemLinkLabel();
            advisorLinkLabel1.Text = "1 Import geometry";
            advisorLinkLabel1.AddAction(frmMain.tsmiImportFile_Click);
            advisorLinkLabel1.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel2 = new AdvisorItemLinkLabel();
            advisorLinkLabel2.Text = "2 Rename geometry part";
            advisorLinkLabel2.AddAction(frmMain.tsmiEditGeometryPart_Click);
            advisorLinkLabel2.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel2);
            //
            AdvisorItemLabel advisorLabel2 = new AdvisorItemLabel();
            advisorLabel2.Text = "Select Next to continue.";
            advisorPage.Items.Add(advisorLabel2);
            //
            return advisorPage;
        }
        private static AdvisorPage PrepareFeMesh(FrmMain frmMain)
        {
            AdvisorPage advisorPage = new AdvisorPage();
            advisorPage.Title = "Prepare the FE Maesh";
            advisorPage.AssociatedView = UserControls.ViewGeometryModelResults.Geometry;
            //
            AdvisorItemLabel advisorLabel1 = new AdvisorItemLabel();
            advisorLabel1.Text = "The geometry must be discretized into finite elements, to perform any type of analysis in the PrePoMax. Smaller finite elements result in better accuracy but require longer computational time.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "To use the default finite element size, select the option 2 to create the FE mesh. To change the size of the largest allowable finite element in the mesh first, choose the option 1 and change the Max element size field. Then create the mesh using the option 2.";
            advisorPage.Items.Add(advisorLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel1 = new AdvisorItemLinkLabel();
            advisorLinkLabel1.Text = "1 Meshing parameters";
            advisorLinkLabel1.AddAction(frmMain.tsmiMeshingParameters_Click);
            advisorLinkLabel1.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel2 = new AdvisorItemLinkLabel();
            advisorLinkLabel2.Text = "2 Create mesh";
            advisorLinkLabel2.AddAction(frmMain.tsmiCreateMesh_Click);
            advisorLinkLabel2.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel2);
            //
            AdvisorItemLabel advisorLabel2 = new AdvisorItemLabel();
            advisorLabel2.Text = "After the mesh is generated the size of the finite elements can be changed by repeating steps 1 and 2.";
            advisorLabel2.Text += "\n\n";
            advisorLabel2.Text += "Select Next to continue.";
            advisorPage.Items.Add(advisorLabel2);
            //
            return advisorPage;
        }
        private static AdvisorPage AssignMaterialProperties(FrmMain frmMain)
        {
            AdvisorPage advisorPage = new AdvisorPage();
            advisorPage.Title = "Assign Material Properties";
            advisorPage.AssociatedView = UserControls.ViewGeometryModelResults.Geometry;
            //
            AdvisorItemLabel advisorLabel1 = new AdvisorItemLabel();
            advisorLabel1.Text = "Material properties are defined using a material model. A new material model can be created using the option 1. Material models can also be stored or retrieved from the material library using the option 2.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "After the definition of the material model its properties must be assigned to the FE mesh. Use the option 3 to assign the created material model to the FE mesh. The FE mesh must be selected using the mouse.";
            advisorPage.Items.Add(advisorLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel1 = new AdvisorItemLinkLabel();
            advisorLinkLabel1.Text = "1 Create material model";
            advisorLinkLabel1.AddAction(frmMain.tsmiCreateMaterial_Click);
            advisorLinkLabel1.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel2 = new AdvisorItemLinkLabel();
            advisorLinkLabel2.Text = "2 Use material library";
            advisorLinkLabel2.AddAction(frmMain.tsmiMaterialLibrary_Click);
            advisorLinkLabel2.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel2);
            //
            AdvisorItemLinkLabel advisorLinkLabel3 = new AdvisorItemLinkLabel();
            advisorLinkLabel3.Text = "3 Apply material model";
            advisorLinkLabel3.AddAction(frmMain.tsmiCreateSection_Click);
            advisorLinkLabel3.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel3);
            //
            AdvisorItemLabel advisorLabel2 = new AdvisorItemLabel();
            advisorLabel2.Text = "Select Next to continue.";
            advisorPage.Items.Add(advisorLabel2);
            //
            return advisorPage;
        }
        private static AdvisorPage SelectTheAnalysisType(FrmMain frmMain)
        {
            AdvisorPage advisorPage = new AdvisorPage();
            advisorPage.Title = "Select the Analysis Type";
            advisorPage.AssociatedView = UserControls.ViewGeometryModelResults.Geometry;
            //
            AdvisorItemLabel advisorLabel1 = new AdvisorItemLabel();
            advisorLabel1.Text = "Different analysis types can be performed using this advisor. For most cases, the default analysis settings can be used.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "If displacements, stresses and strains are to be computed, create a static linear analysis using the option 1 (Static step).";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "To find the lowest eigenfrequencies of the FE model, select the option 2 for the frequency analysis (Frequency step).";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "When the FE model is loaded in compression, buckling might occur. To determine the buckling factor choose the option 3 (Buckle step).";
            advisorPage.Items.Add(advisorLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel1 = new AdvisorItemLinkLabel();
            advisorLinkLabel1.Text = "1 Static linear analysis";
            advisorLinkLabel1.AddAction(frmMain.tsmiCreateStep_Click, null, new EventArgs<int>(0));
            advisorLinkLabel1.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel2 = new AdvisorItemLinkLabel();
            advisorLinkLabel2.Text = "2 Frequency analysis";
            advisorLinkLabel2.AddAction(frmMain.tsmiCreateStep_Click, null, new EventArgs<int>(1));
            advisorLinkLabel2.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel2);
            //
            AdvisorItemLinkLabel advisorLinkLabel3 = new AdvisorItemLinkLabel();
            advisorLinkLabel3.Text = "3 Buckling analysis";
            advisorLinkLabel3.AddAction(frmMain.tsmiCreateStep_Click, null, new EventArgs<int>(2));
            advisorLinkLabel3.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel3);
            //
            AdvisorItemLabel advisorLabel2 = new AdvisorItemLabel();
            advisorLabel2.Text = "Select Next to continue.";
            advisorPage.Items.Add(advisorLabel2);
            //
            return advisorPage;
        }
        private static AdvisorPage CreateTheBoundaryConditions(FrmMain frmMain)
        {
            AdvisorPage advisorPage = new AdvisorPage();
            advisorPage.Title = "Create the Boundary Conditions";
            advisorPage.AssociatedView = UserControls.ViewGeometryModelResults.Geometry;
            //
            AdvisorItemLabel advisorLabel1 = new AdvisorItemLabel();
            advisorLabel1.Text = "For a static and buckling analysis, the movement of the FE model must be prevented using a boundary condition. For a frequency analysis, the boundary condition is optional.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "To add a boundary condition that prevents the movement in all directions, choose the option 1 (Fixed).";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "To individually specify the directions in which the movement is prescribed select the option 2 (Displacement/Rotation).";
            advisorPage.Items.Add(advisorLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel1 = new AdvisorItemLinkLabel();
            advisorLinkLabel1.Text = "1 Fixed ";
            advisorLinkLabel1.AddAction(frmMain.tsmiCreateBC_Click, null, new EventArgs<int>(0));
            advisorLinkLabel1.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel2 = new AdvisorItemLinkLabel();
            advisorLinkLabel2.Text = "2 Displacement/Rotation";
            advisorLinkLabel2.AddAction(frmMain.tsmiCreateBC_Click, null, new EventArgs<int>(1));
            advisorLinkLabel2.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel2);
            //
            AdvisorItemLabel advisorLabel2 = new AdvisorItemLabel();
            advisorLabel2.Text = "Select Next to continue.";
            advisorPage.Items.Add(advisorLabel2);
            //
            return advisorPage;
        }
        private static AdvisorPage DefineTheLoads(FrmMain frmMain)
        {
            AdvisorPage advisorPage = new AdvisorPage();
            advisorPage.Title = "Define the Loads";
            advisorPage.AssociatedView = UserControls.ViewGeometryModelResults.Geometry;
            //
            AdvisorItemLabel advisorLabel1 = new AdvisorItemLabel();
            advisorLabel1.Text = "For a static and buckling analysis the loads must be defined. For a frequency analysis, no loads are allowed.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "To add a load defined by a force magnitude and direction, select the option 1 (Surface traction).";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "If the FE model is loaded by a pressure, select the option 2 (Pressure).";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "When the FE model is large and bulky, the gravity must be taken into account. To assign a gravity load, select the option 3 (Gravity load).";
            advisorPage.Items.Add(advisorLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel1 = new AdvisorItemLinkLabel();
            advisorLinkLabel1.Text = "1 Surface traction ";
            advisorLinkLabel1.AddAction(frmMain.tsmiCreateLoad_Click, null, new EventArgs<int>(3));
            advisorLinkLabel1.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel1);
            //
            AdvisorItemLinkLabel advisorLinkLabel2 = new AdvisorItemLinkLabel();
            advisorLinkLabel2.Text = "2 Pressure";
            advisorLinkLabel2.AddAction(frmMain.tsmiCreateLoad_Click, null, new EventArgs<int>(2));
            advisorLinkLabel2.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel2);
            //
            AdvisorItemLinkLabel advisorLinkLabel3 = new AdvisorItemLinkLabel();
            advisorLinkLabel3.Text = "3 Gravity load";
            advisorLinkLabel3.AddAction(frmMain.tsmiCreateLoad_Click, null, new EventArgs<int>(4));
            advisorLinkLabel3.IndentLevel = 1;
            advisorPage.Items.Add(advisorLinkLabel3);
            //
            AdvisorItemLabel advisorLabel2 = new AdvisorItemLabel();
            advisorLabel2.Text = "Select Next to continue.";
            advisorPage.Items.Add(advisorLabel2);
            //
            return advisorPage;
        }
    }
}

