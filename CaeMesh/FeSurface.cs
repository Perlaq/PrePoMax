﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicTypeDescriptor;
using CaeGlobals;

namespace CaeMesh
{
    [Serializable]
    public enum FeSurfaceCreatedFrom
    {
        [StandardValue("Selection", DisplayName = "Selection")]
        Selection,
        [StandardValue("NodeSet", DisplayName = "Node set")]
        NodeSet,
        [StandardValue("Faces", Visible = false)]
        Faces
    }

    // used in Calculix inp file
    [Serializable]
    public enum FeSurfaceType
    {
        Element,
        Node
    }

    [Serializable]
    public class FeSurface : NamedClass
    {
        // Variables                                                                                                                
        private FeSurfaceType _type;
        private FeSurfaceCreatedFrom _createdFrom;
        private string _nodeSetName;
        private string _createdFromNodeSetName;
        private int[] _faceIds;
        private double _area;
        private Dictionary<FeFaceName, string> _elementFaces;
        private Selection _creationData;


        // Properties                                                                                                               
        public FeSurfaceType Type
        {
            get { return _type; }
            set { _type = value; }
        }
        public FeSurfaceCreatedFrom CreatedFrom 
        { 
            get { return _createdFrom; } 
            set 
            {
                if (_createdFrom != value)
                {
                    Clear();
                    _createdFrom = value;
                }
            } 
        }
        /// <summary>
        /// The name of the internal node set name that represents all the nodes of the surface
        /// </summary>
        public string NodeSetName { get { return _nodeSetName; } set { _nodeSetName = value; } }
        /// <summary>
        /// The name of the internal node set name the surface was created from
        /// </summary>
        public string CreatedFromNodeSetName { get { return _createdFromNodeSetName; } set { _createdFromNodeSetName = value; } }
        public int[] FaceIds { get { return _faceIds; } set { _faceIds = value; } }
        public double Area { get { return _area; } set { _area = value; } }
        public Dictionary<FeFaceName, string> ElementFaces { get { return _elementFaces; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        

        // Constructors                                                                                                             
        public FeSurface(string name)
            : base(name)
        {
            _type = FeSurfaceType.Element;
            Clear();
        }
        public FeSurface(string name, string nodeSetName)
            : this(name)
        {
            _createdFromNodeSetName = nodeSetName;
        }
        public FeSurface(string name, int[] faceIds, Selection creationDataClone)
            : this(name)
        {
            _faceIds = faceIds;
            _creationData = creationDataClone;
        }
        public FeSurface(FeSurface surface)
            : this(surface.Name)
        {
            _type = surface.Type;
            _createdFrom = surface.CreatedFrom;
            _nodeSetName = surface.NodeSetName;
            _createdFromNodeSetName = surface.CreatedFromNodeSetName;
            _faceIds = surface.FaceIds != null ? surface.FaceIds.ToArray() : null;
            _area = surface.Area;
            if (surface.ElementFaces != null)
            {
                _elementFaces = new Dictionary<FeFaceName, string>();
                foreach (var entry in surface.ElementFaces) _elementFaces.Add(entry.Key, entry.Value);
            }
            _creationData = surface.CreationData != null ? surface.CreationData.DeepClone() : null;
        }


        // Methods                                                                                                                  
        private void Clear()
        {
            _createdFrom = FeSurfaceCreatedFrom.Selection;
            _nodeSetName = null;
            _createdFromNodeSetName = null;
            _faceIds = null;
            _area = -1;
            _elementFaces = null;
            _creationData = null;
        }
        public void AddElementFace(FeFaceName faceName, string elementSetName)
        {
            if (faceName == FeFaceName.Empty) throw new CaeException("The face name of the surface can not be 'Empty'.");
            if (_elementFaces == null) _elementFaces = new Dictionary<FeFaceName, string>();
            //_type = FeSurfaceType.Element;
            _elementFaces.Add(faceName, elementSetName);
        }
        public void ClearElementFaces()
        {
            _elementFaces = null;
            _area = 0;
        }
    }
}