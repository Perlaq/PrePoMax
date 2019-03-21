﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;

namespace vtkControl
{
    class vtkMaxTextWidget : vtkMaxBorderWidget
    {
        // Variables                                                                                                                
        protected vtkActor2D _textActor;
        protected vtkTextMapper _textMapper;

        protected int _padding;
        


        // Constructors                                                                                                             
        public vtkMaxTextWidget()
        {
            _padding = 0;

            // Text property
            vtkTextProperty textProperty = vtkTextProperty.New();

            // Mapper
            _textMapper = vtkTextMapper.New();
            _textMapper.SetTextProperty(textProperty);

            // Actor
            _textActor = vtkActor2D.New();
            _textActor.SetMapper(_textMapper);

            // Set relative text position
            _textActor.GetPositionCoordinate().SetCoordinateSystemToDisplay();  // set offsets in pixels
            _textActor.GetPositionCoordinate().SetReferenceCoordinate(_positionCoordinate);
            _textActor.GetPositionCoordinate().SetValue(_padding, _padding);
        }


        // Public methods                                                                                                           
        public override void VisibilityOn()
        {
            if (_visibility == false)
            {
                OnSizeChanged();    // the text might chnage when the widget is turened off
                base.VisibilityOn();
                if (_textActor != null) _renderer.AddActor(_textActor);
            }
        }
        public override void VisibilityOff()
        {
            if (_visibility == true)
            {
                base.VisibilityOff();
                if (_textActor != null) _renderer.RemoveActor(_textActor);
            }
        }

        public override void OnSizeChanged()
        {
            int[] textSize = GetTextSize(_textMapper);
            _size[0] = textSize[0] + 2 * _padding;
            _size[1] = textSize[1] + 2 * _padding;
            base.OnSizeChanged();
        }


        // Private methods                                                                                                          
        protected int[] GetTextSize(vtkTextMapper textMapper)
        {
            int[] size = new int[2];

            if (_renderer != null)
            {
                IntPtr sizeIntPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(2 * 4);
                textMapper.GetSize(_renderer, sizeIntPtr);
                System.Runtime.InteropServices.Marshal.Copy(sizeIntPtr, size, 0, 2);
            }
            else
            {
                size[0] = size[1] = 50;
            }

            return size;
        }


        // Public setters                                                                                                           
        public override void SetInteractor(vtkRenderWindowInteractor renderWindowInteractor)
        {
            base.SetInteractor(renderWindowInteractor);
            _renderer.AddActor(_textActor);
        }
        public virtual void SetTextProperty(vtkTextProperty textProperty)
        {
            _textMapper.SetTextProperty(textProperty);
        }
        public virtual void SetText(string text)
        {
            _textMapper.SetInput(text);
            OnSizeChanged();
        }
        public void SetPadding(int padding)
        {
            if (padding != _padding)
            {
                _padding = padding;
                if (_textActor != null)
                {
                    _textActor.GetPositionCoordinate().SetValue(_padding, _padding);
                    OnSizeChanged();
                }
            }
        }


        // Public getters                                                                                                           
        public string GetText()
        {
            return _textMapper.GetInput();
        }
    }
}
