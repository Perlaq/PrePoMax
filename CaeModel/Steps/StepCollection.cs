using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class StepCollection
    {
        // Variables                                                                                                                
        private List<Step> _steps;


        // Properties                                                                                                               
        public List<Step> StepsList { get { return _steps; } }


        // Constructors                                                                                                             
        public StepCollection()
        {
            _steps = new List<Step>();
        }

        // Methods                                                                                                                         
        public void AddStep(Step step, bool copyBCsAndLoads = true)
        {
            if (copyBCsAndLoads && _steps.Count >= 1)
            {
                Step lastStep = _steps.Last();
                //
                foreach (var entry in lastStep.BoundaryConditions)
                {
                    step.AddBoundaryCondition(entry.Value.DeepClone());
                }
                foreach (var entry in lastStep.Loads)
                {
                    if (step.SupportsLoads) step.AddLoad(entry.Value.DeepClone());
                }
            }
            _steps.Add(step);
        }
        public Step GetStep(string stepName)
        {
            foreach (var step in _steps)
            {
                if (step.Name == stepName) return step;
            }
            return null;
        }
        public string[] GetStepNames()
        {
            List<string> names = new List<string>();
            foreach (var step in _steps) names.Add(step.Name);
            return names.ToArray();
        }
        public void ReplaceStep(string oldStepName, Step newStep)
        {
            List<Step> newSteps = new List<Step>();
            foreach (var step in _steps)
            {
                if (step.Name == oldStepName) newSteps.Add(newStep);
                else newSteps.Add(step);
            }
            _steps = newSteps;
        }
        public Step RemoveStep(string stepName)
        {
            Step stepToRemove = null;
            foreach (var step in _steps)
            {
                if (step.Name == stepName)
                {
                    stepToRemove = step;
                    break;
                }
            }
            if (stepToRemove != null) _steps.Remove(stepToRemove);
            //
            return stepToRemove;
        }
        // History
        public void AddHistoryOutput(HistoryOutput historyOutput, string stepName)
        {
            foreach (var step in _steps)
            {
                if (step.Name == stepName) step.AddHistoryOutput(historyOutput);
            }
        }
        public string[] GetHistoryOutputNames()
        {
            List<string> names = new List<string>();
            foreach (var step in _steps)
            {
                foreach (var history in step.HistoryOutputs) names.Add(step.Name + " -> " + history.Key);
            }
            return names.ToArray();
        }
        public Dictionary<string, int> GetHistoryOutputRegionsCount()
        {
            Dictionary<string, int> regionsCount = new Dictionary<string, int>();
            foreach (var step in _steps)
            {
                foreach (var entry in step.HistoryOutputs)
                {
                    if (regionsCount.ContainsKey(entry.Value.RegionName)) regionsCount[entry.Value.RegionName]++;
                    else regionsCount.Add(entry.Value.RegionName, 1);
                }
            }
            return regionsCount;
        }
        // Field
        public void AddFieldOutput(FieldOutput fieldOutput, string stepName)
        {
            foreach (var step in _steps)
            {
                if (step.Name == stepName) step.AddFieldOutput(fieldOutput);
            }
        }
        public string[] GetFieldOutputNames()
        {
            List<string> names = new List<string>();
            foreach (var step in _steps)
            {
                foreach (var field in step.FieldOutputs) names.Add(step.Name + " -> " + field.Key);
            }
            return names.ToArray();
        }
        // Boundary condition
        public string[] GetAllBoundaryConditionNames()
        {
            HashSet<string> allNames = new HashSet<string>();
            foreach (var step in _steps) allNames.UnionWith(step.BoundaryConditions.Keys);
            return allNames.ToArray();
        }
        public void AddBoundaryCondition(BoundaryCondition boundaryCondition, string stepName)
        {
            foreach (var step in _steps)
            {
                if (step.Name == stepName) step.AddBoundaryCondition(boundaryCondition);
            }
        }
        public Dictionary<string, int> GetBoundaryConditionRegionsCount()
        {
            Dictionary<string, int> regionsCount = new Dictionary<string, int>();
            foreach (var step in _steps)
            {
                foreach (var entry in step.BoundaryConditions)
                {
                    if (regionsCount.ContainsKey(entry.Value.RegionName)) regionsCount[entry.Value.RegionName]++;
                    else regionsCount.Add(entry.Value.RegionName, 1);
                }
            }
            return regionsCount;
        }
        public Step GetBoundaryConditionStep(BoundaryCondition boundaryCondition)
        {
            BoundaryCondition existing;
            foreach (var step in _steps)
            {
                if (step.BoundaryConditions.TryGetValue(boundaryCondition.Name, out existing) && existing == boundaryCondition)
                    return step;
            }
            return null;
        }
        // Load
        public string[] GetAllLoadNames()
        {
            HashSet<string> allNames = new HashSet<string>();
            foreach (var step in _steps) allNames.UnionWith(step.Loads.Keys);
            return allNames.ToArray();
        }
        public void AddLoad(Load load, string stepName)
        {
            foreach (var step in _steps)
            {
                if (step.Name == stepName)  step.AddLoad(load);
            }
        }
        public Dictionary<string, int> GetLoadRegionsCount()
        {
            Dictionary<string, int> regionsCount = new Dictionary<string, int>();
            foreach (var step in _steps)
            {
                foreach (var entry in step.Loads)
                {
                    if (regionsCount.ContainsKey(entry.Value.RegionName)) regionsCount[entry.Value.RegionName]++;
                    else regionsCount.Add(entry.Value.RegionName, 1);
                }
            }
            return regionsCount;
        }
        public Step GetLoadStep(Load load)
        {
            Load existing;
            foreach (var step in _steps)
            {
                if (step.Loads.TryGetValue(load.Name, out existing) && existing == load)
                    return step;
            }
            return null;
        }

    }
}
