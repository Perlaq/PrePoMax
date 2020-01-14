﻿using System;
using System.Collections.Generic;
using System.Text;
using Kitware.VTK;

namespace vtkControl
{
    class vtkInteractorStyleControl : vtkInteractorStyleTrackballCamera
    {
        // Variables                                                                                                                
        public const int VTKIS_START = 0;
        public const int VTKIS_NONE = 0;
        public const int VTKIS_ROTATE = 1;
        public const int VTKIS_PAN = 2;
        public const int VTKIS_SPIN = 3;
        public const int VTKIS_DOLLY = 4;
        public const int VTKIS_ZOOM = 5;
        public const int VTKIS_USCALE = 6;
        public const int VTKIS_TIMER = 7;
        public const int VTKIS_FORWARDFLY = 8;
        public const int VTKIS_REVERSEFLY = 9;
        public const int VTKIS_SELECTION = 10;
        public const int VTKIS_ANIM_OFF = 0;
        public const int VTKIS_ANIM_ON = 1;
       
        private enum EventIds
        {
            NoEvent = 0,
            AnyEvent,
            DeleteEvent,
            StartEvent,
            EndEvent,
            RenderEvent,
            ProgressEvent,
            PickEvent,
            StartPickEvent,
            EndPickEvent,
            AbortCheckEvent,
            ExitEvent,
            LeftButtonPressEvent,
            LeftButtonReleaseEvent,
            MiddleButtonPressEvent,
            MiddleButtonReleaseEvent,
            RightButtonPressEvent,
            RightButtonReleaseEvent,
            EnterEvent,
            LeaveEvent,
            KeyPressEvent,
            KeyReleaseEvent,
            CharEvent,
            ExposeEvent,
            ConfigureEvent,
            TimerEvent,
            MouseMoveEvent,
            MouseWheelForwardEvent,
            MouseWheelBackwardEvent,
            ResetCameraEvent,
            ResetCameraClippingRangeEvent,
            ModifiedEvent,
            WindowLevelEvent,
            StartWindowLevelEvent,
            EndWindowLevelEvent,
            ResetWindowLevelEvent,
            SetOutputEvent,
            ErrorEvent,
            WarningEvent,
            StartInteractionEvent, //mainly used
            InteractionEvent,
            EndInteractionEvent,
            EnableEvent,
            DisableEvent,
            CreateTimerEvent,
            DestroyTimerEvent,
            PlacePointEvent,
            PlaceWidgetEvent,
            CursorChangedEvent,
            ExecuteInformationEvent,
            RenderWindowMessageEvent,
            WrongTagEvent,
            StartAnimationCueEvent, // used
            AnimationCueTickEvent,
            EndAnimationCueEvent,
            VolumeMapperRenderProgressEvent,
            VolumeMapperComputeGradientsEndEvent,
            VolumeMapperComputeGradientsProgressEvent,
            VolumeMapperComputeGradientsStartEvent,
            WidgetModifiedEvent,
            WidgetValueChangedEvent,
            WidgetActivateEvent,
            UserEvent = 1000
        }


        private double _motionFactor;
        private double[] _rotationCenterWorld;
        private double[] _rotationCenterDisplay;
        private int[] _clickPos;
        private vtkProp _centerAnnotationActor;
        private bool _selection;
        private bool _rubberBandEnabled;
        private bool _animating;
        private System.Windows.Threading.DispatcherTimer _timer;
        private int _x;
        private int _y;

        protected bool _leftMouseButtonPressed;
        protected bool _rubberBandSelection;
        protected bool _selectionCanceled;
        protected vtkRenderer _selectionRenderer;
        protected vtkRenderer _overlayRenderer;
        protected vtkPolyDataMapper2D _backgroundMapper;
        protected vtkActor2D _backgroundActor;
        protected vtkActor2D _borderActor;

        protected List<vtkMaxBorderWidget> _widgets;

        
        // Properties                                                                                                               
        public new const string MRFullTypeName = "Kitware.VTK.vtkInteractorStyleControl";
        public bool Selection { get { return _selection; } set { _selection = value; } }
        public bool RubberBandEnabled { get { return _rubberBandEnabled; } set { _rubberBandEnabled = value; } }
        public bool Animating { get { return _animating; } set { _animating = value; } }


        // Events                                                                                                                   
        public event Action<int, int, bool, int, int> PointPickedOnMouseMoveEvt;
        public event Action<int, int, bool, int, int> PointPickedOnLeftUpEvt;
        public event Action ClearCurrentMouseSelection;
        public event Action<int, int> LeftButtonPressEvent;
        public event Action<int, int> RightButtonPressEvent;


        // Constructors                                                                                                             
        public vtkInteractorStyleControl()
        {
            _motionFactor = 10.0;
            _rotationCenterWorld = null;
            _selection = false;
            _animating = false;
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _timer.Tick += timer_Tick;

            _rubberBandEnabled = true;
            _widgets = new List<vtkMaxBorderWidget>();

            this.LeftButtonPressEvt += vtkInteractorStyleControl_LeftButtonPressEvt;
            this.LeftButtonReleaseEvt += vtkInteractorStyleControl_LeftButtonReleaseEvt;
            this.MiddleButtonPressEvt += vtkInteractorStyleControl_MiddleButtonPressEvt;
            this.MiddleButtonReleaseEvt += vtkInteractorStyleControl_MiddleButtonReleaseEvt;
            this.RightButtonPressEvt += vtkInteractorStyleControl_RightButtonPressEvt;
            this.RightButtonReleaseEvt += vtkInteractorStyleControl_RightButtonReleaseEvt;
            this.MouseMoveEvt += vtkInteractorStyleControl_MouseMoveEvt;
            this.MouseWheelForwardEvt += vtkInteractorStyleControl_MouseWheelForwardEvt;
            this.MouseWheelBackwardEvt += vtkInteractorStyleControl_MouseWheelBackwardEvt;
            this.KeyPressEvt += VtkInteractorStyleControl_KeyPressEvt;
        }
        new public static vtkInteractorStyleControl New()
        {
            return new vtkInteractorStyleControl();
        }


        // Event handlers                                                                                                           
        void vtkInteractorStyleControl_LeftButtonPressEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            //int[] clickPos = this.GetInteractor().GetEventPosition();
            //vtkRenderer renderer = this.GetInteractor().GetRenderWindow().GetRenderers().GetFirstRenderer();

            //double[] worldPos = DisplayToWorld(renderer, new double[] { clickPos[0], clickPos[1] });

            //vtkCamera camera = renderer.GetActiveCamera();
            //camera.SetParallelScale(camera.GetParallelScale() / 1.2);

            //double[] worldPosAfter = DisplayToWorld(renderer, new double[] { clickPos[0], clickPos[1] });

            //double[] motionVector = new double[3];
            //motionVector[0] = worldPosAfter[0] - worldPos[0];
            //motionVector[1] = worldPosAfter[1] - worldPos[1];
            //motionVector[2] = worldPosAfter[2] - worldPos[2];

            //double[] cPos = camera.GetPosition();
            //double[] fPos = camera.GetFocalPoint();

            //// Camera motion is reversed
            //camera.SetPosition(cPos[0] - motionVector[0], cPos[1] - motionVector[1], cPos[2] - motionVector[2]);
            //camera.SetFocalPoint(fPos[0] - motionVector[0], fPos[1] - motionVector[1], fPos[2] - motionVector[2]);

            //DrawSphereOnLeft(sender, e);

            vtkRenderWindowInteractor rwi = this.GetInteractor();

            int x = rwi.GetEventPosition()[0];
            int y = rwi.GetEventPosition()[1];
            this.FindPokedRenderer(x, y);

            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;

            // Widgets - left pressed
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                if (widget.LeftButtonPress(x, y)) return;
            }

            _clickPos = rwi.GetEventPosition();     // set global variable

            if (_selection)
            {
                _leftMouseButtonPressed = true;
                _rubberBandSelection = false;
                _selectionCanceled = false;

                vtkPoints backgroundPoints = _backgroundMapper.GetInput().GetPoints();

                backgroundPoints.SetPoint(0, _clickPos[0], _clickPos[1], 0.0);
                backgroundPoints.SetPoint(1, _clickPos[0], _clickPos[1], 0.0);
                backgroundPoints.SetPoint(2, _clickPos[0], _clickPos[1], 0.0);
                backgroundPoints.SetPoint(3, _clickPos[0], _clickPos[1], 0.0);
                backgroundPoints.Modified();

                _backgroundActor.VisibilityOn();
                _borderActor.VisibilityOn();
            }
            else LeftButtonPressEvent?.Invoke(_clickPos[0], _clickPos[1]);
        }
        void vtkInteractorStyleControl_LeftButtonReleaseEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            // Widgets
            foreach (vtkMaxBorderWidget widget in _widgets) widget.LeftButtonRelease();

            if (_selection)
            {
                if (!_selectionCanceled)
                {
                    vtkRenderWindowInteractor rwi = this.GetInteractor();
                    int[] mousePos = rwi.GetEventPosition();

                    if (PointPickedOnLeftUpEvt != null)
                    {
                        if (_clickPos == null) PointPickedOnLeftUpEvt(mousePos[0], mousePos[1], _rubberBandSelection, 0, 0);
                        else PointPickedOnLeftUpEvt(mousePos[0], mousePos[1], _rubberBandSelection, _clickPos[0], _clickPos[1]);
                    }
                }
                
                _clickPos = null;
                _leftMouseButtonPressed = false;
                _rubberBandSelection = false;

                _backgroundActor.VisibilityOff();
                _borderActor.VisibilityOff();
            }
        }
        
        void vtkInteractorStyleControl_MiddleButtonPressEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            base.OnLeftButtonDown();

            vtkRenderWindowInteractor rwi = this.GetInteractor();

            int x = rwi.GetEventPosition()[0];
            int y = rwi.GetEventPosition()[1];
            this.FindPokedRenderer(x, y);

            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;

            _clickPos = rwi.GetEventPosition();     // set global variable

            UpdateRotationCenterDisplay();

            AddAnnotationForCenter3D(rwi, renderer);

            // Widgets - middle pressed
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.MiddleButtonPress(x, y);
            }

            rwi.Render();
        }
        void vtkInteractorStyleControl_MiddleButtonReleaseEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            //switch (this.GetState())
            //{
            //    case VTKIS_DOLLY:
            //        this.EndDolly();
            //        break;

            //    case VTKIS_PAN:
            //        this.EndPan();
            //        break;

            //    case VTKIS_SPIN:
            //        this.EndSpin();
            //        break;

            //    case VTKIS_ROTATE:
            //        this.EndRotate();
            //        break;
            //}

            //if (this.GetInteractor() == null)
            //{
            //    this.ReleaseFocus();
            //}
            
            
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;

            // remove first in order not to affect the bounds of the model
            RemoveAnnotationForCenter(renderer);

            //int layer = renderer.GetLayer();
            //double[] b = renderer.ComputeVisiblePropBounds();

            vtkRenderWindowInteractor rwi = this.GetInteractor();
            int[] currClickPos = rwi.GetEventPosition();

            if (_clickPos != null && Math.Abs(_clickPos[0] - currClickPos[0]) + Math.Abs(_clickPos[0] - currClickPos[0]) < 2)
            {
                // only click happened
                vtkPropPicker picker = vtkPropPicker.New();
                renderer.Render(); // to redraw the first - BASE - layer of geometry again
                picker.Pick(currClickPos[0], currClickPos[1], 0, renderer);
                _rotationCenterWorld = picker.GetPickPosition();

                if (picker.GetActor() == null)
                {
                    // the click was not a hit
                    // use the center of the bounding box
                    double[] bounds = renderer.ComputeVisiblePropBounds();
                    _rotationCenterWorld[0] = (bounds[1] + bounds[0]) / 2;
                    _rotationCenterWorld[1] = (bounds[3] + bounds[2]) / 2;
                    _rotationCenterWorld[2] = (bounds[5] + bounds[4]) / 2;
                }
                else
                {
                    // the click was a hit
                    vtkCamera camera = renderer.GetActiveCamera();
                    double[] center = camera.GetFocalPoint();
                    PanCamera(camera, center, _rotationCenterWorld);
                    AdjustCameraDistance(renderer, camera);
                }
            }

            // adjust the clipping range and lights
            if (this.GetAutoAdjustCameraClippingRange() == 1) ResetClippingRange();
            if (rwi.GetLightFollowCamera() == 1) renderer.UpdateLightsGeometryToFollowCamera();

            // Widgets - middle released
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.MiddleButtonRelease(currClickPos[0], currClickPos[1]);
            }

            //rwi.Render();
            rwi.Modified();

            _clickPos = null;

            base.OnLeftButtonUp();  // left button is rotation by default
        }
        
        void vtkInteractorStyleControl_RightButtonPressEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            int x = rwi.GetEventPosition()[0];
            int y = rwi.GetEventPosition()[1];

            this.FindPokedRenderer(x, y);

            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;

            int[] clickPos = rwi.GetEventPosition();

            // Widgets - right pressed
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                if (widget.RightButtonPress(x, y)) return;
            }

            RightButtonPressEvent?.Invoke(clickPos[0], clickPos[1]);
        }
        void vtkInteractorStyleControl_RightButtonReleaseEvt(vtkObject sender, vtkObjectEventArgs e)
        {
        }
        
        void vtkInteractorStyleControl_MouseMoveEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            _x = this.GetInteractor().GetEventPosition()[0];
            _y = this.GetInteractor().GetEventPosition()[1];

            // Widgets - Move
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                if (widget.MouseMove(_x, _y))
                {
                    this.GetInteractor().Render();
                    return;
                }
            }

            switch (this.GetState())
            {
                case VTKIS_NONE:
                    if (_selection)
                    {
                        _timer.Stop();
                        _timer.Start();
                        //this.FindPokedRenderer(_x, _y);
                        //this.Select(_x, _y);

                        if (_rubberBandEnabled)
                        {
                            if (!_rubberBandSelection && _leftMouseButtonPressed && _clickPos != null && Math.Abs(_x - _clickPos[0]) > 1 && Math.Abs(_y - _clickPos[1]) > 1)
                            {
                                _rubberBandSelection = true;
                            }
                            if (_rubberBandSelection) DrawRubberBandSelection(_x, _y);
                        }
                    }
                    break;
                case VTKIS_ROTATE:
                    this.FindPokedRenderer(_x, _y);
                    //renderer = this.GetCurrentRenderer();
                    //vtkCamera camera = renderer.GetActiveCamera();
                    //double[] focalPos1 = camera.GetFocalPoint();
                    this.Rotate();
                    this.InvokeEvent((uint)EventIds.InteractionEvent, IntPtr.Zero);
                    //double[] focalPos2 = camera.GetFocalPoint();
                    //double[] output = new double[3];
                    //output[0] = focalPos1[0] - focalPos2[0];
                    //Console.WriteLine(output[0].ToString() + ": " + output[1].ToString() + ": " + output[2].ToString());
                    break;

                case VTKIS_PAN:
                    this.FindPokedRenderer(_x, _y);
                    this.Pan();
                    this.InvokeEvent((uint)EventIds.InteractionEvent, IntPtr.Zero);
                    break;

                //case VTKIS_DOLLY:
                //    this.FindPokedRenderer(_x, _y);
                //    this.Dolly();
                //    this.InvokeEvent((uint)EventIds.InteractionEvent, IntPtr.Zero);
                //    break;

                //case VTKIS_SPIN:
                //    this.FindPokedRenderer(_x, _y);
                //    this.Spin();
                //    this.InvokeEvent((uint)EventIds.InteractionEvent, IntPtr.Zero);
                //    break;
            }
        }
        
        void vtkInteractorStyleControl_MouseWheelForwardEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (this.GetState() == VTKIS_NONE)
            {
                int[] clickPos = this.GetInteractor().GetEventPosition();
                this.FindPokedRenderer(clickPos[0], clickPos[1]);
                vtkRenderer renderer = this.GetCurrentRenderer();

                double[] worldPos = DisplayToWorld(renderer, new double[] { clickPos[0], clickPos[1] });

                vtkCamera camera = renderer.GetActiveCamera();
                double factor = Math.Pow(1.1, this._motionFactor * 0.2 * this.GetMouseWheelMotionFactor());
                camera.SetParallelScale(camera.GetParallelScale() * factor);

                double[] worldPosAfter = DisplayToWorld(renderer, new double[] { clickPos[0], clickPos[1] });

                PanCamera(camera, worldPosAfter, worldPos);

                // Widgets - mouse wheel scrolled
                foreach (vtkMaxBorderWidget widget in _widgets)
                {
                    widget.MouseWheelScrolled();
                }

                this.GetInteractor().Modified();
                this.GetInteractor().Render();
            }            
        }
        void vtkInteractorStyleControl_MouseWheelBackwardEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (this.GetState() == VTKIS_NONE)
            {
                int[] clickPos = this.GetInteractor().GetEventPosition();
                this.FindPokedRenderer(clickPos[0], clickPos[1]);
                vtkRenderer renderer = this.GetCurrentRenderer();

                double[] worldPos = DisplayToWorld(renderer, new double[] { clickPos[0], clickPos[1] });

                vtkCamera camera = renderer.GetActiveCamera();
                double factor = Math.Pow(1.1, this._motionFactor * 0.2 * this.GetMouseWheelMotionFactor());
                //factor = 1.001;
                camera.SetParallelScale(camera.GetParallelScale() / factor);

                double[] worldPosAfter = DisplayToWorld(renderer, new double[] { clickPos[0], clickPos[1] });

                PanCamera(camera, worldPosAfter, worldPos);

                // Widgets - mouse wheel scrolled
                foreach (vtkMaxBorderWidget widget in _widgets)
                {
                    widget.MouseWheelScrolled();
                }

                this.GetInteractor().Modified();
                this.GetInteractor().Render();                
            }
        }

        private void VtkInteractorStyleControl_KeyPressEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;

            vtkRenderWindowInteractor rwi = this.GetInteractor();
            sbyte key = rwi.GetKeyCode();

            if (key >= 37 && key <= 40)
            {
                UpdateRotationCenterDisplay();

                double delta = 5;

                rwi.Modified();

                if (key == 37) Rotate(+delta, 0);       // left
                else if (key == 39) Rotate(-delta, 0);  // right
                else if (key == 38) Rotate(0, -delta);  // up
                else if (key == 40) Rotate(0, +delta);  // down

                rwi.Modified();
                rwi.Render();
            }
            else if (key == 27) // Escape
            {
                // When making area selection, Esc key cancels it

                _selectionCanceled = true;
                _clickPos = null;
                //_leftMouseButtonPressed = false;
                _rubberBandSelection = false;

                _backgroundActor.VisibilityOff();
                _borderActor.VisibilityOff();

                ClearCurrentMouseSelection?.Invoke();

                rwi.Render();
            }
        }


        // Public setters                                                                                                           
        public void SetSelectionRenderer(vtkRenderer renderer)
        {
            _selectionRenderer = renderer;
            InitializeRubberBandSelection();
        }
        public void SetOverlayRenderer(vtkRenderer renderer)
        {
            _overlayRenderer = renderer;
        }


        // Methods                                                                                                                  
        void timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            this.FindPokedRenderer(_x, _y);
            this.Select(_x, _y);
        }

        public void AddVtkMaxWidget(vtkMaxBorderWidget widget)
        {
            _widgets.Add(widget);
        }

        public void Reset()
        {
            _rotationCenterWorld = null;
        }

        private void UpdateRotationCenterDisplay()
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;

            if (_rotationCenterWorld == null)
            {
                // for the first time use the center of the bounding box
                double[] bounds = renderer.ComputeVisiblePropBounds();
                _rotationCenterWorld = new double[3];
                _rotationCenterWorld[0] = (bounds[1] + bounds[0]) / 2;
                _rotationCenterWorld[1] = (bounds[3] + bounds[2]) / 2;
                _rotationCenterWorld[2] = (bounds[5] + bounds[4]) / 2;
            }

            // must update since zoom can also change
            _rotationCenterDisplay = WorldToDisplay(renderer, _rotationCenterWorld);
        }

        public static double[] WorldToDisplay(vtkRenderer renderer, double[] worldPos)
        {
            IntPtr posOutPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(2 * 8);
            ComputeWorldToDisplay(renderer, worldPos[0], worldPos[1], worldPos[2], posOutPtr);
            double[] displayPos = new double[2];
            System.Runtime.InteropServices.Marshal.Copy(posOutPtr, displayPos, 0, 2);
            return displayPos;
        }
        public static double[] DisplayToWorld(vtkRenderer renderer, double[] displayPos)
        {
            IntPtr posOutPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(4 * 8);
            ComputeDisplayToWorld(renderer, displayPos[0], displayPos[1], 0, posOutPtr);
            double[] worldPos = new double[3];
            System.Runtime.InteropServices.Marshal.Copy(posOutPtr, worldPos, 0, 3);
            return worldPos;
        }

        public static double DisplayToWorldScale(vtkRenderer renderer, int size = 1)
        {
            int delta = 1 * size;
            int x = 0;
            int y = 0;
            double[] p1 = vtkInteractorStyleControl.DisplayToWorld(renderer, new double[] { x, y });
            double[] p2 = vtkInteractorStyleControl.DisplayToWorld(renderer, new double[] { x + delta, y });
            return Math.Sqrt(Math.Pow(p1[0] - p2[0], 2)
                             + Math.Pow(p1[1] - p2[1], 2)
                             + Math.Pow(p1[2] - p2[2], 2));
        }
        public override void Pan()
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
         
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            int[] clickPos = rwi.GetEventPosition();

            double[] newPickPoint;
            double[] oldPickPoint;

            newPickPoint = DisplayToWorld(renderer, new double[] { clickPos[0], clickPos[1] });
            oldPickPoint = DisplayToWorld(renderer, new double[] { rwi.GetLastEventPosition()[0], rwi.GetLastEventPosition()[1] });

            vtkCamera camera = renderer.GetActiveCamera();
            PanCamera(camera, newPickPoint, oldPickPoint);

            if (rwi.GetLightFollowCamera() == 1) renderer.UpdateLightsGeometryToFollowCamera();

            // Widgets - Pan
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                // compute the delta
                int dx = clickPos[0] - rwi.GetLastEventPosition()[0];
                int dy = clickPos[1] - rwi.GetLastEventPosition()[1];

                widget.MousePan(dx, dy);
            }

            rwi.Render();
        }
        private void PanCamera(vtkCamera camera, double[] posStart, double[] posEnd)
        {
            double[] motionVector = new double[3];
            motionVector[0] = posEnd[0] - posStart[0];
            motionVector[1] = posEnd[1] - posStart[1];
            motionVector[2] = posEnd[2] - posStart[2];

            double[] cPos = camera.GetPosition();
            double[] fPos = camera.GetFocalPoint();

            //double[] clip = camera.GetClippingRange();
            camera.SetPosition(cPos[0] + motionVector[0], cPos[1] + motionVector[1], cPos[2] + motionVector[2]);
            camera.SetFocalPoint(fPos[0] + motionVector[0], fPos[1] + motionVector[1], fPos[2] + motionVector[2]);

            AdjustCameraDistanceAndClipping();
        }

        public override void Rotate()
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;

            vtkRenderWindowInteractor rwi = this.GetInteractor();
            int[] clickPos = rwi.GetEventPosition();

            // compute the angle
            int dx = clickPos[0] - rwi.GetLastEventPosition()[0];
            int dy = clickPos[1] - rwi.GetLastEventPosition()[1];

            double delta_elevation = -20.0 / 500;
            double delta_azimuth = -20.0 / 500;

            double rxf = dx * delta_azimuth * this._motionFactor;
            double ryf = dy * delta_elevation * this._motionFactor;

            Rotate(rxf, ryf);
        }
        private void Rotate(double azimuthAngle, double ElevationAngle)
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;

            vtkCamera camera = renderer.GetActiveCamera();
            vtkRenderWindowInteractor rwi = this.GetInteractor();

            camera.Azimuth(azimuthAngle);
            camera.Elevation(ElevationAngle);
            camera.OrthogonalizeViewUp();

            // move the cameras focal point back to the display position of the first click . the opposite direction!
            double[] wpa = DisplayToWorld(renderer, _rotationCenterDisplay);
            double[] n = camera.GetViewPlaneNormal();

            // compute a plane through rotationWorldCenter and project the rotationDisplayCenter on it
            double[] v = new double[3];                                 // vector from rotationWorldCenter to DisplayToWorld point
            v[0] = wpa[0] - _rotationCenterWorld[0];
            v[1] = wpa[1] - _rotationCenterWorld[1];
            v[2] = wpa[2] - _rotationCenterWorld[2];
            double d = n[0] * v[0] + n[1] * v[1] + n[2] * v[2];        // distance of DisplayToWorld point from plane
            wpa[0] -= n[0] * d;
            wpa[1] -= n[1] * d;
            wpa[2] -= n[2] * d;

            PanCamera(camera, wpa, _rotationCenterWorld);
            AdjustCameraDistance(renderer, camera);

            // adjust the clipping range and lights
            if (this.GetAutoAdjustCameraClippingRange() == 1) ResetClippingRange();
            if (rwi.GetLightFollowCamera() == 1) renderer.UpdateLightsGeometryToFollowCamera();

            // Widgets - Rotate
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.MouseRotate(azimuthAngle, ElevationAngle);
            }

            rwi.Modified();
            rwi.Render();
        }


        private void Select(int x, int y)
        {
            if (PointPickedOnMouseMoveEvt != null)
            {
                if (_clickPos == null) PointPickedOnMouseMoveEvt(x, y, _rubberBandSelection, 0, 0);
                else PointPickedOnMouseMoveEvt(x, y, _rubberBandSelection, _clickPos[0], _clickPos[1]);
            }
        }
        private void DrawRubberBandSelection(int x, int y)
        {
            if (_clickPos == null) return;

            vtkRenderWindowInteractor rwi = this.GetInteractor();
            vtkPoints backgroundPoints = _backgroundMapper.GetInput().GetPoints();

            int id1 = 1;
            int id2 = 2;
            int id3 = 3;

            // switch ids 1 and 3 if II or IV quadrant
            if ((x < _clickPos[0] && y > _clickPos[1]) || (x > _clickPos[0] && y < _clickPos[1]))
            {
                id1 = 3;
                id3 = 1;
            }

            backgroundPoints.SetPoint(id1, x, _clickPos[1], 0.0);
            backgroundPoints.SetPoint(id2, x, y, 0.0);
            backgroundPoints.SetPoint(id3, _clickPos[0], y, 0.0);
            backgroundPoints.Modified();

            rwi.Render();
        }

        private void AddAnnotationForCenter3D(vtkRenderWindowInteractor interactor, vtkRenderer renderer)
        {
            if (_centerAnnotationActor != null) RemoveAnnotationForCenter(renderer);

            int[] size = renderer.GetSize();
            double minSize = Math.Min(size[0], size[1]);
            vtkCamera camera = renderer.GetActiveCamera();
            double scale = camera.GetParallelScale();
            double dMin = 30 / minSize * scale;
            double dMax = 100 / minSize * scale;
            
            vtkPoints points = vtkPoints.New();
            points.InsertNextPoint(_rotationCenterWorld[0] + dMin, _rotationCenterWorld[1], _rotationCenterWorld[2]);
            points.InsertNextPoint(_rotationCenterWorld[0] + dMax, _rotationCenterWorld[1], _rotationCenterWorld[2]);

            points.InsertNextPoint(_rotationCenterWorld[0] - dMin, _rotationCenterWorld[1], _rotationCenterWorld[2]);
            points.InsertNextPoint(_rotationCenterWorld[0] - dMax, _rotationCenterWorld[1], _rotationCenterWorld[2]);
            
            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1] + dMin, _rotationCenterWorld[2]);
            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1] + dMax, _rotationCenterWorld[2]);

            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1] - dMin, _rotationCenterWorld[2]);
            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1] - dMax, _rotationCenterWorld[2]);

            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1], _rotationCenterWorld[2] + dMax * 1.3);
            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1], _rotationCenterWorld[2] - dMax * 0.7);


            vtkCellArray lineIndices = vtkCellArray.New();
            for (int i = 0; i < 5; i++)
            {
                lineIndices.InsertNextCell(2);
                lineIndices.InsertCellPoint(2 * i);
                lineIndices.InsertCellPoint(2 * i + 1);
            }

            vtkPolyData pointsPolyData = vtkPolyData.New();
            pointsPolyData.SetPoints(points);
            pointsPolyData.SetLines(lineIndices);

            // Visualize
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInput(pointsPolyData);
            mapper.Update();

            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            actor.GetProperty().SetColor(1, 0.7, 0);
            //actor.GetProperty().SetLineWidth(1);

            renderer.AddActor(actor);
            _centerAnnotationActor = actor;
        }
        private void RemoveAnnotationForCenter(vtkRenderer renderer)
        {
            if (_centerAnnotationActor != null)
            {
                renderer.RemoveActor(_centerAnnotationActor);
                _centerAnnotationActor.Dispose();
                _centerAnnotationActor = null;
            }
        }
        public void AdjustCameraDistanceAndClipping()
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            vtkCamera camera = renderer.GetActiveCamera();

            AdjustCameraDistance(renderer, camera);
            ResetClippingRange();

            // Widgets - Modified
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.CameraModified();
            }
        }
        private void AdjustCameraDistance(vtkRenderer renderer, vtkCamera camera)
        {
            // camera should not be to close

            double[] b1 = renderer.ComputeVisiblePropBounds();
            double[] b2 = _selectionRenderer.ComputeVisiblePropBounds();
            double[] b3 = _overlayRenderer.ComputeVisiblePropBounds();

            double[] b = new double[] { Math.Min(b1[0], Math.Min(b2[0], b3[0])), Math.Max(b1[1], Math.Max(b2[1], b3[1])),
                                        Math.Min(b1[2], Math.Min(b2[2], b3[2])), Math.Max(b1[3], Math.Max(b2[3], b3[3])),
                                        Math.Min(b1[4], Math.Min(b2[4], b3[4])), Math.Max(b1[5], Math.Max(b2[5], b3[5]))};

            double D = Math.Sqrt(Math.Pow(b[0] - b[1], 2) + Math.Pow(b[2] - b[3], 2) + Math.Pow(b[4] - b[5], 2));   // diameter
            double[] center = new double[] { (b[0] + b[1]) / 2, (b[2] + b[3]) / 2, (b[4] + b[5]) / 2 };

            double[] pos = camera.GetPosition();
            //System.Diagnostics.Debug.WriteLine(string.Format("pos x: {0},  y: {1},  z: {1}", pos[0], pos[1], pos[2]));

            double[] delta = new double[3];
            //delta[0] = _rotationCenterWorld[0] - pos[0];
            //delta[1] = _rotationCenterWorld[1] - pos[1];
            //delta[2] = _rotationCenterWorld[2] - pos[2];

            delta[0] = center[0] - pos[0];
            delta[1] = center[1] - pos[1];
            delta[2] = center[2] - pos[2];

            double[] direction = camera.GetViewPlaneNormal();
            double d1 = -(direction[0] * delta[0] + direction[1] * delta[1] + direction[2] * delta[2]);

            if (d1 < D)
            {
                double[] newPos = new double[3];
                newPos[0] = pos[0] + direction[0] * D;
                newPos[1] = pos[1] + direction[1] * D;
                newPos[2] = pos[2] + direction[2] * D;
                camera.SetPosition(newPos[0], newPos[1], newPos[2]);
            }
        }
        private void AdjustCameraDistance__(vtkRenderer renderer, vtkCamera camera)
        {
            double[] b = renderer.ComputeVisiblePropBounds();
            double l = 0.01 * Math.Sqrt(Math.Pow(b[0] - b[1], 2) + Math.Pow(b[2] - b[3], 2) + Math.Pow(b[4] - b[5], 2));

            double[] pos = camera.GetPosition();
            double[] direction = camera.GetViewPlaneNormal();
            double[] point = null;
            if (direction[0] > 0 && direction[1] > 0 && direction[2] > 0) point = new double[] { b[1], b[3], b[5] };
            else if (direction[0] <= 0 && direction[1] > 0 && direction[2] > 0) point = new double[] { b[0], b[3], b[5] };
            else if (direction[0] <= 0 && direction[1] <= 0 && direction[2] > 0) point = new double[] { b[0], b[2], b[5] };
            else if (direction[0] > 0 && direction[1] <= 0 && direction[2] > 0) point = new double[] { b[1], b[2], b[5] };

            else if (direction[0] > 0 && direction[1] > 0 && direction[2] <= 0) point = new double[] { b[1], b[3], b[4] };
            else if (direction[0] <= 0 && direction[1] > 0 && direction[2] <= 0) point = new double[] { b[0], b[3], b[4] };
            else if (direction[0] <= 0 && direction[1] <= 0 && direction[2] <= 0) point = new double[] { b[0], b[2], b[4] };
            else if (direction[0] > 0 && direction[1] <= 0 && direction[2] <= 0) point = new double[] { b[1], b[2], b[4] };
            else throw new Exception();
            
            double[] relDir = new double[] { point[0] - pos[0], point[1] - pos[1], point[2] - pos[2] };

            // positive value means the camera distance
            double d = -(direction[0] * relDir[0] + direction[1] * relDir[1] + direction[2] * relDir[2]);
            d -= l;

            double[] newPos = new double[3];
            newPos[0] = pos[0] - d * direction[0];
            newPos[1] = pos[1] - d * direction[1];
            newPos[2] = pos[2] - d * direction[2];
            camera.SetPosition(newPos[0], newPos[1], newPos[2]);
        }
        private void AdjustCameraDistance_(vtkRenderer renderer, vtkCamera camera)
        {
            double[] b = renderer.ComputeVisiblePropBounds();
            double[] center = new double[] { (b[0] + b[1]) / 2, (b[2] + b[3]) / 2, (b[4] + b[5]) / 2 };
            double R = Math.Sqrt(Math.Pow(b[0] - b[1], 2) + Math.Pow(b[2] - b[3], 2) + Math.Pow(b[4] - b[5], 2)) / 2;

            double[] pos = camera.GetPosition();
            double[] direction = camera.GetViewPlaneNormal();
            double[] point = new double[] { center[0] + R * direction[0], center[1] + R * direction[1], center[2] + R * direction[2] };
            double[] relDir = new double[] { point[0] - pos[0], point[1] - pos[1], point[2] - pos[2] };

            
            // positive value means the camera will move forward
            double d = -(direction[0] * relDir[0] + direction[1] * relDir[1] + direction[2] * relDir[2]);
            d -= R * 0.01;

            camera.SetDistance(d + 10);

            double[] newPos = new double[3];
            newPos[0] = pos[0] - d * direction[0];
            newPos[1] = pos[1] - d * direction[1];
            newPos[2] = pos[2] - d * direction[2];
            camera.SetPosition(newPos[0], newPos[1], newPos[2]);

            //System.Diagnostics.Debug.WriteLine(string.Format("newPos x: {0},  y: {1},  z: {1}", newPos[0], newPos[1], newPos[2]));
        }

        public void ResetClippingRange()
        {
            double min, max, minSel, maxSel, minOver, maxOver;
            min = max = minSel = maxSel = minOver = maxOver = 0;

            GetCurrentRenderer().ResetCameraClippingRange();
            GetCurrentRenderer().GetActiveCamera().GetClippingRange(ref min, ref max);

            _selectionRenderer.ResetCameraClippingRange();
            _selectionRenderer.GetActiveCamera().GetClippingRange(ref minSel, ref maxSel);

            _overlayRenderer.ResetCameraClippingRange();
            _overlayRenderer.GetActiveCamera().GetClippingRange(ref minOver, ref maxOver);

            // near and far settng for the Z-buffer
            GetCurrentRenderer().GetActiveCamera().SetClippingRange(Math.Min(min, Math.Min(minSel, minOver)), Math.Max(max, Math.Max(maxSel, maxOver)));
        }

        private void InitializeRubberBandSelection()
        {
            vtkPoints backgroundPoints = vtkPoints.New();
            backgroundPoints.SetNumberOfPoints(4);
            backgroundPoints.SetPoint(0, 0.0, 0.0, 0.0);
            backgroundPoints.SetPoint(1, 10.5, 0.0, 0.0);
            backgroundPoints.SetPoint(2, 10.5, 10.5, 0.0);
            backgroundPoints.SetPoint(3, 0.0, 10.5, 0.0);

            vtkCellArray backgroundPolygon = vtkCellArray.New();
            backgroundPolygon.InsertNextCell(4);
            backgroundPolygon.InsertCellPoint(0);
            backgroundPolygon.InsertCellPoint(1);
            backgroundPolygon.InsertCellPoint(2);
            backgroundPolygon.InsertCellPoint(3);

            vtkPolyData backgroundPolyData = vtkPolyData.New();
            backgroundPolyData.SetPoints(backgroundPoints);
            backgroundPolyData.SetPolys(backgroundPolygon);

            _backgroundMapper = vtkPolyDataMapper2D.New();
            _backgroundMapper.SetInput(backgroundPolyData);

            _backgroundActor = vtkActor2D.New();
            _backgroundActor.SetMapper(_backgroundMapper);
            _backgroundActor.GetProperty().SetOpacity(0.2);
            _backgroundActor.VisibilityOff();
            _backgroundActor.GetProperty().SetColor(0.2, 0.5, 1);
            _selectionRenderer.AddActor(_backgroundActor);


            vtkCellArray borderPolygon = vtkCellArray.New();
            borderPolygon.InsertNextCell(5);
            borderPolygon.InsertCellPoint(0);
            borderPolygon.InsertCellPoint(1);
            borderPolygon.InsertCellPoint(2);
            borderPolygon.InsertCellPoint(3);
            borderPolygon.InsertCellPoint(0);

            vtkPolyData borderPolyData = vtkPolyData.New();
            borderPolyData.SetPoints(backgroundPoints);
            borderPolyData.SetLines(borderPolygon);

            vtkPolyDataMapper2D borderMapper = vtkPolyDataMapper2D.New();
            borderMapper.SetInput(borderPolyData);

            _borderActor = vtkActor2D.New();
            _borderActor.SetMapper(borderMapper);
            _borderActor.GetProperty().SetColor(0.3, 0.3, 1.0);
            _borderActor.VisibilityOff();

            _selectionRenderer.AddActor(_borderActor);
        }

        private void DrawSphereOnLeft(vtkObject sender, vtkObjectEventArgs e)
        {
            vtkInteractorStyleControl interactor = (vtkInteractorStyleControl)sender;
            vtkRenderer renderer = interactor.GetInteractor().GetRenderWindow().GetRenderers().GetFirstRenderer();

            int[] clickPos = interactor.GetInteractor().GetEventPosition();

            // Pick from this location.
            vtkPropPicker picker = vtkPropPicker.New();

            //vtkRendererCollection renderers = interactor.GetInteractor().GetRenderWindow().GetRenderers();
            //interactor.GetInteractor().GetRenderWindow().RemoveRenderer(renderers.GetNextItem());

            renderer.Render(); // to redraw the first layer of geometry
            picker.Pick(clickPos[0], clickPos[1], 0, renderer);
            


            double[] pos = picker.GetPickPosition();
            Console.WriteLine("Pick position (world coordinates) is: " + pos[0].ToString() + "  "
                                                                       + pos[1].ToString() + "  "
                                                                       + pos[2].ToString());

            //Console.WriteLine("Picked actor: " + picker.GetActor().ToString());

            //Create a sphere
            vtkSphereSource sphereSource = vtkSphereSource.New();
            sphereSource.SetCenter(pos[0], pos[1], pos[2]);
            sphereSource.SetRadius(1);

            //Create a mapper and actor
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(sphereSource.GetOutputPort());

            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            actor.PickableOff();

            //this.GetInteractor().GetRenderWindow().GetRenderers().GetDefaultRenderer().AddActor(actor);
            renderer.AddActor(actor);

            // Forward events
            interactor.OnLeftButtonUp();

            this.GetInteractor().Render();
        }

    }
}